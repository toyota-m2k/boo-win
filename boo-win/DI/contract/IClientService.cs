using BooWin.model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BooWin.DI.contract; 

public interface IItemService {
    Uri? GetItemUrl(IMediaItem item, string cmdName = "item");
    Task<List<Chapter>> LoadChapterAsync(IMediaItem item);
    Task<bool> SaveChapterAsync(IMediaItem item, List<Chapter> chapters);
}

public interface IClientService : IItemService {
    Task<bool> SetCurrentHostAsync(PeerHost host);
    PeerHost CurrentHost { get; }
    bool IsAvailable { get; }

    Capability Capability { get; }
    CategorySupport CategorySupport { get; }
    RatingSupport RatingSupport { get; }
    MarkSupport MarkSupport { get; }

    Task<MediaItemList> GetMediaItemListAsync(
        ListSourceMode listMode = ListSourceMode.ALL,
        MediaType mediaTypes = MediaType.ALL,
        long? since = null,
        int ratingThreshold = 0,
        int[]? marks = null,
        string? category = null,
        string? searchText = null
    );
    //string GetItemUrl(IMediaItem item, string cmdName="item");

    Task<bool> CheckHostAvailabilityAsync(PeerHost host);

    Task<bool> UpdateItemPropertiesAsync(string itemId, string? category, int? rating, int? mark, string? name, string? label);

    Task<bool> DownloadItemAsync(IMediaItem item, string destPath, CancellationToken token, Action<long, long>? progress = null);
}
