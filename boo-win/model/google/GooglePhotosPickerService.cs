using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using io.github.toyota32k.BooLib.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace BooWin.model.google;

/**
 * Google Photos Picker API を使って、ユーザーが選択したアイテムを取得・ダウンロードするサービス
 * https://developers.google.com/photos/picker/reference/rest
 */
public class GooglePhotosPickerService : IDisposable {
    private static readonly UtLog Logger = new UtLog("GPPS");

    // ビルド時に secrets.props または環境変数から生成される（Secrets.g.cs）
    private const string CLIENT_ID = Secrets.GoogleClientId;
    private const string CLIENT_SECRET = Secrets.GoogleClientSecret;

    private const string SCOPE = "https://www.googleapis.com/auth/photospicker.mediaitems.readonly";
    private const string API_BASE = "https://photospicker.googleapis.com/v1";

    private readonly string _tokenStorePath;
    private readonly HttpClient _httpClient = new();
    private UserCredential? _credential;

    public GooglePhotosPickerService(string tokenStorePath) {
        _tokenStorePath = tokenStorePath;
    }

    #region Models
    public class PollingConfig {
        [JsonPropertyName("pollInterval")] public string? PollInterval { get; set; }
        [JsonPropertyName("timeoutIn")] public string? TimeoutIn { get; set; }
    }
    public class PickingSession {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("pickerUri")] public string PickerUri { get; set; } = "";
        [JsonPropertyName("pollingConfig")] public PollingConfig? PollingConfig { get; set; }
        [JsonPropertyName("mediaItemsSet")] public bool MediaItemsSet { get; set; }
    }
    public class MediaFileMetadata {
        [JsonPropertyName("width")] public int Width { get; set; }
        [JsonPropertyName("height")] public int Height { get; set; }
    }
    public class MediaFile {
        [JsonPropertyName("baseUrl")] public string BaseUrl { get; set; } = "";
        [JsonPropertyName("mimeType")] public string MimeType { get; set; } = "";
        [JsonPropertyName("filename")] public string Filename { get; set; } = "";
        [JsonPropertyName("mediaFileMetadata")] public MediaFileMetadata? Metadata { get; set; }
    }
    public class PickedMediaItem {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("createTime")] public DateTimeOffset CreateTime { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; } = "";
        [JsonPropertyName("mediaFile")] public MediaFile? MediaFile { get; set; }
        public bool IsVideo => Type == "VIDEO";
    }
    private class ListMediaItemsResponse {
        [JsonPropertyName("mediaItems")] public List<PickedMediaItem>? MediaItems { get; set; }
        [JsonPropertyName("nextPageToken")] public string? NextPageToken { get; set; }
    }
    #endregion

    /**
     * OAuth 認証（システムブラウザ + loopback）。トークンは tokenStorePath にキャッシュされる。
     */
    public async Task AuthorizeAsync(CancellationToken token) {
        if (_credential != null) {
            await _credential.GetAccessTokenForRequestAsync(cancellationToken: token);
            return;
        }
        _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            new ClientSecrets { ClientId = CLIENT_ID, ClientSecret = CLIENT_SECRET },
            [SCOPE],
            "user",
            token,
            new FileDataStore(_tokenStorePath, true));
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string url, CancellationToken token) {
        if (_credential == null) throw new InvalidOperationException("Not authorized.");
        var accessToken = await _credential.GetAccessTokenForRequestAsync(cancellationToken: token);
        var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return req;
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage req, CancellationToken token) {
        using var res = await _httpClient.SendAsync(req, token);
        var body = await res.Content.ReadAsStringAsync(token);
        if (!res.IsSuccessStatusCode) {
            throw new HttpRequestException($"{(int)res.StatusCode} {res.ReasonPhrase}: {body}");
        }
        return JsonSerializer.Deserialize<T>(body) ?? throw new InvalidDataException("Empty response.");
    }

    public async Task<PickingSession> CreateSessionAsync(CancellationToken token) {
        using var req = await CreateRequestAsync(HttpMethod.Post, $"{API_BASE}/sessions", token);
        req.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        return await SendAsync<PickingSession>(req, token);
    }

    public async Task<PickingSession> GetSessionAsync(string sessionId, CancellationToken token) {
        using var req = await CreateRequestAsync(HttpMethod.Get, $"{API_BASE}/sessions/{sessionId}", token);
        return await SendAsync<PickingSession>(req, token);
    }

    public async Task DeleteSessionAsync(string sessionId) {
        try {
            using var req = await CreateRequestAsync(HttpMethod.Delete, $"{API_BASE}/sessions/{sessionId}", CancellationToken.None);
            using var res = await _httpClient.SendAsync(req);
        }
        catch (Exception ex) {
            Logger.Error(ex);
        }
    }

    /**
     * ユーザーが Picker でアイテムを選択し終えるまでポーリングする。
     */
    public async Task<PickingSession> WaitForSelectionAsync(PickingSession session, CancellationToken token) {
        var interval = ParseDuration(session.PollingConfig?.PollInterval) ?? TimeSpan.FromSeconds(5);
        var timeout = ParseDuration(session.PollingConfig?.TimeoutIn) ?? TimeSpan.FromMinutes(30);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        cts.CancelAfter(timeout);
        while (true) {
            await Task.Delay(interval, cts.Token);
            var s = await GetSessionAsync(session.Id, cts.Token);
            if (s.MediaItemsSet) return s;
            interval = ParseDuration(s.PollingConfig?.PollInterval) ?? interval;
        }
    }

    public async Task<List<PickedMediaItem>> ListMediaItemsAsync(string sessionId, CancellationToken token) {
        var list = new List<PickedMediaItem>();
        string? pageToken = null;
        do {
            var url = $"{API_BASE}/mediaItems?sessionId={Uri.EscapeDataString(sessionId)}&pageSize=100";
            if (pageToken != null) url += $"&pageToken={Uri.EscapeDataString(pageToken)}";
            using var req = await CreateRequestAsync(HttpMethod.Get, url, token);
            var res = await SendAsync<ListMediaItemsResponse>(req, token);
            if (res.MediaItems != null) list.AddRange(res.MediaItems);
            pageToken = res.NextPageToken;
        } while (!string.IsNullOrEmpty(pageToken));
        return list;
    }

    /**
     * アイテムをダウンロードする（動画は "=dv"、写真は "=d"）。
     */
    public async Task<bool> DownloadAsync(PickedMediaItem item, string path, CancellationToken token, Action<long, long> progress) {
        if (item.MediaFile == null) return false;
        var url = item.MediaFile.BaseUrl + (item.IsVideo ? "=dv" : "=d");
        try {
            using var req = await CreateRequestAsync(HttpMethod.Get, url, token);
            using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, token);
            res.EnsureSuccessStatusCode();
            var total = res.Content.Headers.ContentLength ?? -1;
            await using var src = await res.Content.ReadAsStreamAsync(token);
            await using var dst = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            var buffer = new byte[81920];
            long received = 0;
            int read;
            while ((read = await src.ReadAsync(buffer, token)) > 0) {
                await dst.WriteAsync(buffer.AsMemory(0, read), token);
                received += read;
                progress(received, total);
            }
            return true;
        }
        catch (Exception ex) {
            Logger.Error(ex);
            FileUtil.SafeDelete(path);
            if (ex is OperationCanceledException) throw;
            return false;
        }
    }

    private static TimeSpan? ParseDuration(string? s) {
        // google.protobuf.Duration 形式 ("5s", "1.5s")
        if (string.IsNullOrEmpty(s) || !s.EndsWith('s')) return null;
        if (double.TryParse(s[..^1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var sec)) {
            return TimeSpan.FromSeconds(sec);
        }
        return null;
    }

    public void Dispose() {
        _httpClient.Dispose();
    }
}
