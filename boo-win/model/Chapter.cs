using BooWin.client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BooWin.model;

public class Chapter {
    [JsonPropertyName("position")]
    public long Position { get; set; }
    [JsonPropertyName("label")]
    public string? Label { get; set; }
    [JsonPropertyName("skip")]
    public long Skip { get; set; }

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
}
