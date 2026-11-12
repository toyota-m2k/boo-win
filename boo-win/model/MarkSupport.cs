using BooWin.client;
using io.github.toyota32k.BooLib.Utils;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BooWin.model;

public class Mark {
    [JsonPropertyName("mark")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int Value { get; set; }
    [JsonPropertyName("label")]
    public string Label { get; set; } = "";
    [JsonPropertyName("svg")]
    public string SvgPath { get; set; } = "";
}

public class MarkSupport {
    public List<Mark> Marks { get; set; }

    public bool IsSupported => Marks.Count > 0;

    private MarkSupport(List<Mark>? marks=null) {
        Marks = marks ?? new List<Mark>();
    }

    public static MarkSupport Empty => new MarkSupport(null);

    public static MarkSupport FromJson(string jsonString) {
        var json = JsonUtil.Parse(jsonString);
        if (json != null) {
            try {
                var marks = json["marks"]?.Deserialize<List<Mark>>(JsonUtil.SerializeCaseInsensitive);
                if (marks != null) {
                    return new MarkSupport(marks);
                }
            }
            catch (Exception e) {
                BooApi.Logger.Error(e);
            }
        }
        return new MarkSupport();
    }
}
