using BooWin.model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BooWin.DI.contract; 
public interface IClientService {
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
    string GetItemUrl(MediaItem item, string cmdName="item");
    Task<List<Chapter>> LoadChapterAsync(MediaItem item);
    
    Task<bool> CheckHostAvailabilityAsync(PeerHost host);

    Task<bool> UpdateItemPropertiesAsync(string itemId, string? category, int? rating, int? mark, string? name, string? label);
}
