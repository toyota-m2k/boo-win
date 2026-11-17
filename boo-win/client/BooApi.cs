using BooWin.DI.contract;
using BooWin.model;
using io.github.toyota32k.BooLib.Server.http;
using io.github.toyota32k.BooLib.Utils;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace BooWin.client;

public class BooApi : IDisposable {
    private const string CT_JSON = "application/json";
    private const string CT_TEXT = "text/plain";

    public static UtLog Logger = new UtLog("BooApi");
    private PeerHost Host { get; }
    private IDialogService _dialogService;

    private IHttpClient _httpClient { get; }

    public BooApi(PeerHost peer, IHttpClientFactory httpClientFactory, IDialogService dialogService) {
        Host = peer;
        _dialogService = dialogService;
        _httpClient = Host.CreateHttpClient(httpClientFactory);
    }
    public void Dispose() {
        _httpClient.Dispose();
    }

    #region Capability / Supported Features

    public async Task<bool> NopAsync() {
        try {
            var url = $"{Host.BaseUrl}/nop";
            var response = await _httpClient.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) {
            Logger.Error(ex, $"Failed to send nop to {Host.Address}");
            return false;
        }
    }

    private Capability? _capability = null;
    public async Task<Capability?> GetCapabilityAsync() {
        try {
            if (_capability != null) {
                return _capability;
            }
            var url = $"{Host.BaseUrl}/capability?v=3";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) {
                Logger.Error($"Failed to get capability from {Host.Address}. StatusCode: {response.StatusCode}");
                return null;
            }
            var json = await response.Content.ReadAsStringAsync();
            var cap = Capability.FromJson(json);
            _capability = cap;
            return cap;
        }
        catch (Exception ex) {
            Logger.Error(ex, $"Failed to get capability from {Host.Address}");
            return null;
        }
    }

    public string MakeUrl(string path) {
        var cap = _capability;
        if (cap == null) {
            throw new InvalidOperationException("no capability");
        }
        return $"{Host.BaseUrl}{cap.Root}{path.TrimStart('/')}";
    }


    public async Task<CategorySupport> GetCategorySupportAsync() {
        try {
            var capCategory = _capability?.Category ?? 0;
            if (capCategory <= 0) {
                return CategorySupport.Empty;
            }
            var url = MakeUrl("categories");
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) {
                Logger.Error($"Failed to get category support from {Host.Address}. StatusCode: {response.StatusCode}");
                return CategorySupport.Empty;
            }
            var json = await response.Content.ReadAsStringAsync();
            var categorySupport = CategorySupport.FromJson(json, capCategory>1);
            return categorySupport;
        }
        catch (Exception ex) {
            Logger.Error(ex, $"Failed to get category support from {Host.Address}");
            return CategorySupport.Empty;
        }
    }

    public async Task<RatingSupport> GetRatingSupportAsync() {
        try {
            var capRating = _capability?.Rating ?? 0;
            if (capRating <= 0) {
                return RatingSupport.Empty;
            }
            var url = MakeUrl("ratings");
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) {
                Logger.Error($"Failed to get rating support from {Host.Address}. StatusCode: {response.StatusCode}");
                return RatingSupport.Empty;
            }
            var json = await response.Content.ReadAsStringAsync();
            var ratingSupport = RatingSupport.FromJson(json, capRating>1);
            return ratingSupport;
        }
        catch (Exception ex) {
            Logger.Error(ex, $"Failed to get rating support from {Host.Address}");
            return RatingSupport.Empty;
        }
    }

    public async Task<MarkSupport> GetMarkSupportAsync() {
        try {
            var capMark = _capability?.Mark ?? 0;
            if (capMark <= 0) {
                return MarkSupport.Empty;
            }
            var url = MakeUrl("marks");
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) {
                Logger.Error($"Failed to get mark support from {Host.Address}. StatusCode: {response.StatusCode}");
                return MarkSupport.Empty;
            }
            var json = await response.Content.ReadAsStringAsync();
            var markSupport = MarkSupport.FromJson(json, capMark>1);
            return markSupport;
        }
        catch (Exception ex) {
            Logger.Error(ex, $"Failed to get mark support from {Host.Address}");
            return MarkSupport.Empty;
        }
    }

    #endregion

    #region Authentication

    private string? AuthToken = null;
    private string? Challenge = null;
    private const string PWD_SEED = "y6c46S/PBqd1zGFwghK2AFqvSDbdjl+YL/DKXgn/pkECj0x2fic5hxntizw5";

    private async Task<string> GetChallengeFromResponse(HttpResponseMessage res) {
        if (res.StatusCode != HttpStatusCode.Unauthorized || res.Content.Headers.ContentType?.MediaType != CT_JSON) {
            throw new InvalidDataException("unexpected response");
        }
        var json = JsonUtil.ParseOrThrow(await res.Content.ReadAsStringAsync());
        return json.GetStringOrThrow("challenge");
    }


    public async Task<string> GetChallenge() {
        var url = MakeUrl("auth/");
        var response = await _httpClient.GetAsync(url);
        return await GetChallengeFromResponse(response);
    }

    public string CreateHashedPassword(string pwd) {
        return HashHelper.SHA256(pwd, PWD_SEED).AsHexString;
    }
    private string CreatePassPhrase(string challenge, string password) {
        return HashHelper.SHA256(challenge, password).AsBase64String;
    }

    private async Task<string?> GetAuthTokenFromResponse(HttpResponseMessage res) {
        if (!res.IsSuccessStatusCode || res.Content.Headers.ContentType?.MediaType != CT_JSON) {
            throw new InvalidDataException("unexpected response");
        }
        var json = JsonUtil.ParseOrThrow(await res.Content.ReadAsStringAsync());
        return json.GetStringOrNull("token");
    }

    private async Task<string?> AuthWithPasswordAndGetToken(string? challenge, string password) {
        challenge = challenge ?? await GetChallenge();
        var hashedPassword = CreateHashedPassword(password);
        var passPhrase = CreatePassPhrase(challenge, hashedPassword);
        var url = MakeUrl($"auth");
        var res = await _httpClient.PutAsync(url, new StringContent(passPhrase, Encoding.UTF8, CT_TEXT));
        if (res.IsSuccessStatusCode) {
            return await GetAuthTokenFromResponse(res);
        }
        else {
            // error ... try again with new challenge
            var c = await GetChallengeFromResponse(res);
            if (c != challenge) {
                return await AuthWithPasswordAndGetToken(c, password);
            }
            else {
                return null;
            }
        }
    }

    /**
     * 現在の authToken が有効かどうかを確認する。
     * 有効であれば null を返す。
     * 無効であれば、サーバからの challenge を返すので、それを使って再度認証を行う必要がある。
     */
    private async Task<string?> CheckAuthToken(string currentAuthToken) {
        var url = MakeUrl($"auth/{currentAuthToken}");
        var response = await _httpClient.GetAsync(url);
        if (response.IsSuccessStatusCode) {
            return null;
        }
        else {
            return await GetChallengeFromResponse(response);
        }
    }

    public async Task<bool> Authenticate(bool force = false) {
        var cap = _capability;
        if (cap == null) return false;
        if (!cap.Authentication) return true;   // 認証不要
        if (!force && AuthToken != null) {
            var challenge = await CheckAuthToken(AuthToken);
            if (challenge == null) {
                return true;    // 認証済み
            }
            Challenge = challenge;   // 認証トークンが無効になったので再認証する
        }

        while (true) {
            var password = await _dialogService.ShowInputPassword("Authenticate", "Please enter your password:");
            if (password == null) {
                Logger.Debug("Cancelled");
                return false;   // キャンセル
            }
            var token = await AuthWithPasswordAndGetToken(Challenge, password);
            if (token != null) {
                AuthToken = token;
                return true;
            }
        }
    }

    #endregion

    #region List Files

    private async Task<HttpResponseMessage> RequestWithAuth(string url) {
        var response = await _httpClient.GetAsync(url);
        if (response.IsSuccessStatusCode) {
            return response;
        }
        else if (response.StatusCode == HttpStatusCode.Unauthorized) {
            if (await Authenticate(true) && !string.IsNullOrEmpty(AuthToken)) {
                // 認証成功したので、authパラメータを差し替えて再度リクエスト
                var dic = QueryParser.Parse(url);
                dic["auth"] = AuthToken;
                int queryIndex = url.IndexOf('?');
                if (queryIndex >= 0) {
                    url = url.Substring(0, queryIndex);
                }
                url += MakeQueryString(dic);
                return await _httpClient.GetAsync(url);
            }
        }
        return response;

    }

    private string MakeQueryString(Dictionary<string, string> parameters) {
        if (parameters.Count == 0) return string.Empty;
        var query = string.Join("&", parameters.Select(kv => $"{kv.Key}={WebUtility.UrlEncode(kv.Value)}"));
        return "?" + query;
    }

    public async Task<MediaItemList> GetMediaItemListAsync(
            ListSourceMode listMode=ListSourceMode.ALL, 
            MediaType mediaTypes = MediaType.ALL,
            long? since = null,
            int ratingThreshold = 0,
            int[]? marks = null,
            string? category = null,
            string? searchText = null
            ) {
        var url = MakeUrl("list?s=1");
        var dic = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(AuthToken)) {
            dic["auth"] = AuthToken;
        }
        if (listMode != ListSourceMode.ALL) {
            dic["s"] = ((int)listMode).ToString();
        }
        if (mediaTypes!=0 && mediaTypes != MediaType.ALL) {
            dic["f"] = MediaItem.MediaTypeToString(mediaTypes);
        }
        if (since.HasValue) {
            dic["d"] = since.Value.ToString();
        }
        if (category!=null) {
            dic["c"] = category;
        }
        if (ratingThreshold > 0) {
            dic["r"] = ratingThreshold.ToString();
        }
        if (marks != null && marks.Length > 0) {
            dic["m"] = string.Join(".", marks);
        }
        if (!string.IsNullOrWhiteSpace(searchText)) {
            dic["t"] = searchText;
        }
       
        url += MakeQueryString(dic);
        var response = await RequestWithAuth(url);
        var json = await response.Content.ReadAsStringAsync();
        return MediaItemList.FromJson(json);
    }

    public string GetItemUrl(MediaItem item, string cmdName="item") {
        var cap = _capability;
        if (cap == null) {
            throw new InvalidOperationException("no capability");
        }
        var dic = new Dictionary<string, string>() {
            { "id", item.Id }
        };
        if (!string.IsNullOrEmpty(AuthToken)) {
            dic["auth"] = AuthToken;
        }
        return $"{Host.BaseUrl}{cap.Root}{cmdName}{MakeQueryString(dic)}";
    }


    public async Task<List<Chapter>> GetChapterAsync(MediaItem item) {
        var cap = _capability;
        if (cap == null) {
            throw new InvalidOperationException("no capability");
        }
        var dic = new Dictionary<string, string>() {
            { "id", item.Id }
        };
        if (!string.IsNullOrEmpty(AuthToken)) {
            dic["auth"] = AuthToken;
        }
        var url = MakeUrl($"chapter{MakeQueryString(dic)}");
        var response = await RequestWithAuth(url);
        if (!response.IsSuccessStatusCode) {
            return new List<Chapter>();
        }
        var json = await response.Content.ReadAsStringAsync();
        return Chapter.ListFromJson(json);
    }

    public async Task<bool> PutPropertiesAsync(string id, string? category, int? rating, int? mark, string? name, string? label) {
        var cap = _capability;
        if (cap == null) {
            throw new InvalidOperationException("no capability");
        }
        if (category==null && rating == null && mark == null) {
            return true;    // 何も変更しないので成功扱い
        }
        var dic = new Dictionary<string, string>() {
            { "id", id }
        };
        if (!string.IsNullOrEmpty(AuthToken)) {
            dic["auth"] = AuthToken;
        }
        if (category != null) {
            dic["category"] = category;
        }
        if (rating.HasValue) {
            dic["rating"] = rating.Value.ToString();
        }
        if (mark.HasValue) {
            dic["mark"] = mark.Value.ToString();
        }
        if (name != null) {
            dic["name"] = name;
        }
        if (label != null) {
            dic["label"] = label;
        }
        var url = MakeUrl($"props");
        
        var response = await _httpClient.PutAsync(url, new StringContent(JsonSerializer.Serialize(dic), Encoding.UTF8, CT_JSON));
        return response.IsSuccessStatusCode;
    }

    #endregion
}
