using BooWin.client;
using io.github.toyota32k.BooLib.Utils;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BooWin.model;

public class Rating {
    [JsonPropertyName("rating")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int Value { get; set; } = 0;

    [JsonPropertyName("label")]
    public string Label { get; set; } = "";

    [JsonPropertyName("svg")]
    public string SvgPath { get; set; } = "";

    public static Rating None => new Rating { Value = 0, Label = "", SvgPath = "" };
}

public class RatingSupport {
    public int DefaultRating { get; set; } = 3;
    public List<Rating> Ratings { get; set; }
    public bool IsSupported => Ratings.Count > 0;
    public bool AllowReadWrite { get; set; } = false;

    public int ComplementalRating(IMediaItem item) {
        return (item.Rating == 0) ? DefaultRating : item.Rating;
    }

    private RatingSupport(int defaultRating=3, List<Rating>? ratings=null, bool allowReadWrite=false) {
        DefaultRating = defaultRating;
        Ratings = ratings ?? new List<Rating>();
        AllowReadWrite = allowReadWrite && IsSupported;
    }

    public static RatingSupport Empty => new RatingSupport();

    public static RatingSupport FromJson(string? jsonString, bool allowReadWrite) {
        try {
            var json = JsonUtil.ParseOrThrow(jsonString);
            var defaultRating = json.GetInt("defaultRating", 3);

            var ratings = json["ratings"]?.Deserialize<List<Rating>>(JsonUtil.SerializeCaseInsensitive);
            if (ratings != null) {
                return new RatingSupport(defaultRating, ratings, allowReadWrite);
            }
        }
        catch (Exception ex) {
            BooApi.Logger.Error(ex);
        }
        return Empty;
    }

}
