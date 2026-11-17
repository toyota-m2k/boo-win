using BooWin.client;
using BooWin.DI.contract;
using BooWin.model;
using io.github.toyota32k.BooLib.Utils;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BooWin.DI.implement;

public class ClientService : IClientService {
    public UtLog Logger = new UtLog("ClientService");

    public ClientService(IHttpClientFactory httpClientFactory, IDialogService dialogService) {
        _httpClientFactory = httpClientFactory;
        _dialogService = dialogService;
    }

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDialogService _dialogService;

    private PeerHost? _currentHost = null;
    public PeerHost CurrentHost {
        get {
            if (_currentHost == null) {
                throw new InvalidOperationException("CurrentHost is not set.");
            }
            return _currentHost;
        }
    }

    public bool IsAvailable => _capability != null;

    public void Reset() {
        _api?.Dispose();
        _api = null;
        _currentHost = null;
        _capability = null;
        _categorySupport = null;
        _ratingSupport = null;
        _markSupport = null;
    }

    private BooApi? _api = null;
    public async Task<bool> SetCurrentHostAsync(PeerHost host) {
        Reset();
        
        _api = host.CreateApi(_httpClientFactory, _dialogService);
        if (_api == null) {
            return false;
        }
        return await Task.Run(async () => {
            try {
                _capability = await _api.GetCapabilityAsync();
                _categorySupport = await _api.GetCategorySupportAsync();
                _ratingSupport = await _api.GetRatingSupportAsync();
                _markSupport = await _api.GetMarkSupportAsync();
                _currentHost = host;
                return true;
            }
            catch (Exception ex) {
                Logger.Error(ex);
                Reset();
                return false;
            }
        });
    }

    private Capability? _capability = null;
    public Capability Capability {
        get {
            if (_capability == null) {
                throw new InvalidOperationException("Capability is not available. CurrentHost is not set.");
            }
            return _capability;
        }
    }

    private CategorySupport? _categorySupport = null;
    public CategorySupport CategorySupport {
        get {
            if (_categorySupport == null) {
                throw new InvalidOperationException("CategorySupport is not available. CurrentHost is not set.");
            }
            return _categorySupport;
        }
    }

    private RatingSupport? _ratingSupport = null;
    public RatingSupport RatingSupport {
        get {
            if (_ratingSupport == null) {
                throw new InvalidOperationException("RatingSupport is not available. CurrentHost is not set.");
            }
            return _ratingSupport;
        }
    }

    private MarkSupport? _markSupport = null;
    public MarkSupport MarkSupport {
        get {
            if (_markSupport == null) {
                throw new InvalidOperationException("MarkSupport is not available. CurrentHost is not set.");
            }
            return _markSupport;
        }
    }

    public string GetItemUrl(MediaItem item, string cmdName = "item") {
        if (_api == null) {
            throw new InvalidOperationException("API is not available. CurrentHost is not set.");
        }
        return _api.GetItemUrl(item, cmdName);
    }

    //public string GetVideoUrl(MediaItem item) {
    //    return GetItemUrl(item, "video");
    //}
    //public string GetAudioUrl(MediaItem item) {
    //    return GetItemUrl(item, "audio");
    //}
    //public string GetPhotoUrl(MediaItem item) {
    //    return GetItemUrl(item, "photo");
    //}


    public Task<MediaItemList> GetMediaItemListAsync(
        ListSourceMode listMode = ListSourceMode.ALL,
        MediaType mediaTypes = MediaType.ALL,
        long? since = null,
        int ratingThreshold = 0,
        int[]? marks = null,
        string? category = null,
        string? searchText = null
        ) {
        if (_api == null) {
            throw new InvalidOperationException("API is not available. CurrentHost is not set.");
        }
        return _api.GetMediaItemListAsync(listMode, mediaTypes, since, ratingThreshold, marks, category, searchText);
    }

    public async Task<List<Chapter>> LoadChapterAsync(MediaItem item) {
        if (_api == null) {
            throw new InvalidOperationException("API is not available. CurrentHost is not set.");
        }
        var chapterList = await _api.GetChapterAsync(item) ?? new List<Chapter>();
        return ChapterListEditor.MergeChaptersAndStartEnd(chapterList, item.Start, item.End);
    }

    public async Task<bool> CheckHostAvailabilityAsync(PeerHost host) {
        using (var api = host.CreateApi(_httpClientFactory, _dialogService)) {
            if (api == null) {
                return false;
            }
            try {
                return await api.NopAsync();
            }
            catch (Exception ex) {
                Logger.Error(ex);
                return false;
            }
        }
    }

    public async Task<bool> UpdateItemPropertiesAsync(string itemId, string? category, int? rating, int? mark, string? name, string? label) {
        if (_api == null) {
            throw new InvalidOperationException("API is not available. CurrentHost is not set.");
        }
        return await _api.PutPropertiesAsync(itemId.ToString(), category, rating, mark, name, label);
    }

    
}
