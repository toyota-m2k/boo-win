using BooWin.client;
using io.github.toyota32k.BooLib.Utils;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BooWin.model;

public class Chapter {
    [JsonPropertyName("position")]
    public long Position { get; set; }
    [JsonPropertyName("label")]
    public string? Label { get; set; }
    [JsonPropertyName("skip")]
    public bool Skip { get; set; }

    [JsonIgnore]
    public bool Enabled => !Skip;

    public static List<Chapter> ListFromJson(string? jsonString) {
        if (jsonString != null) {
            try {
                var json = JsonUtil.Parse(jsonString);
                if (json != null) {
                    var chapters = json["chapters"]?.Deserialize<List<Chapter>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (chapters!=null) {
                        return chapters;
                    }
                }
            } catch(Exception e) {
                BooApi.Logger.Error(e);
            }
        }
        return new List<Chapter>();
    }

    public Chapter? CopyChapterIfModifed(string? label, bool? skip) {
        if ((label==null || label == this.Label) && (skip==null || skip==this.Skip)) {
            return null;    // 変更なし
        }
        return new Chapter {
            Position = this.Position,
            Label = label ?? this.Label,
            Skip = skip ?? this.Skip
        };
    }

}
