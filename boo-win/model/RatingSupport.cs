using BooWin.client;
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

    [JsonIgnore]
    public int ComplementalRating { get; set; } = 0;
}
public class RatingSupport {
    public int DefaultRating { get; set; }
    public List<Rating> Ratings { get; set; }
    public bool IsSupported => Ratings.Count > 0;

    private RatingSupport(int defaultRating=3, List<Rating>? ratings=null) {
        DefaultRating = defaultRating;
        Ratings = ratings ?? new List<Rating>();
    }

    public static RatingSupport Empty => new RatingSupport();

    public static RatingSupport FromJson(string? jsonString) {
        try {
            var json = JsonUtil.ParseOrThrow(jsonString);
            var defaultRating = json.GetInt("defaultRating", 3);

            var ratings = json["ratings"]?.Deserialize<List<Rating>>(JsonUtil.SerializeCaseInsensitive);
            if (ratings != null) {
                foreach (var r in ratings) {
                    r.ComplementalRating = (r.Value <= 0) ? defaultRating : r.Value;
                }
                return new RatingSupport(defaultRating, ratings);
            }
        }
        catch (Exception ex) {
            BooApi.Logger.Error(ex);
        }
        return new RatingSupport();
    }

}
