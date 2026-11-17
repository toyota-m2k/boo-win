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


    public static Mark None => new Mark { Value = 0, Label = "", SvgPath = "" };
}

public class MarkSupport {
    public List<Mark> Marks { get; set; }

    public bool IsSupported => Marks.Count > 0;

    public bool AllowReadWrite { get; set; } = false;

    private MarkSupport(List<Mark>? marks=null, bool allowReadWrite=false) {
        Marks = marks ?? new List<Mark>();
        AllowReadWrite = allowReadWrite && IsSupported;
    }

    public static MarkSupport Empty => new MarkSupport(null);

    public static MarkSupport FromJson(string jsonString, bool allowReadWrite) {
        var json = JsonUtil.Parse(jsonString);
        if (json != null) {
            try {
                var marks = json["marks"]?.Deserialize<List<Mark>>(JsonUtil.SerializeCaseInsensitive);
                if (marks != null) {
                    return new MarkSupport(marks, allowReadWrite);
                }
            }
            catch (Exception e) {
                BooApi.Logger.Error(e);
            }
        }
        return Empty;
    }
}
