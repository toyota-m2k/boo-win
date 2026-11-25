using BooWin.client;
using io.github.toyota32k.BooLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace BooWin.model; 

public class MediaItemList {
    public List<IMediaItem> Items { get; }

    public long ListTime { get; }

    public static MediaItemList Empty { get; } = new MediaItemList();

    private MediaItemList() {
        Items = new List<IMediaItem>();
        ListTime = 0;
    }
    private MediaItemList(List<IMediaItem> items, long listTime) {
        Items = items;
        ListTime = listTime;
    }

    public static MediaItemList FromJson(string jsonString) {
        var json = JsonUtil.Parse(jsonString);
        if (json != null) {
            var ml = new MediaItemList();
            try {
                var date = json.GetLong("date", 0);
                var items = json["list"]?
                        .Deserialize<List<MediaItem>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?
                        .Cast<IMediaItem>()
                        .ToList() ?? new List<IMediaItem>();
                return new MediaItemList(items, 0);
            }
            catch (Exception ex) {
                BooApi.Logger.Error(ex);
            }
        }
        return Empty;
    }

}
