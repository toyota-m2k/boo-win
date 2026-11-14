using BooWin.client;
using io.github.toyota32k.BooLib.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BooWin.model;

public class  Category {

    [JsonPropertyName("label")]
    public string Name { get; set; } = "";
    [JsonPropertyName("sort")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int SortIndex { get; set; } = 0;
    [JsonPropertyName("color")]
    public string ColorName { get; set; } = "";
    [JsonPropertyName("svg")]
    public string SvgPath { get; set; } = "";

    //[JsonIgnore]
    //public Color Color => Color.FromArgb(ARGB);
}

public class CategorySupport {
    public List<Category> Categories { get; }
    public bool IsSupported => Categories.Count > 0;
    
    private CategorySupport(List<Category>? list=null) {
        Categories = list ?? new List<Category>();
    }
    public static CategorySupport Empty => new CategorySupport(null);

    public static CategorySupport FromJson(string? jsonString) {
        try {
            var json = JsonUtil.ParseOrThrow(jsonString);
            var categories = json["categories"].Deserialize<List<Category>>(JsonUtil.SerializeCaseInsensitive);
            if (categories != null) {
                return new CategorySupport(categories.OrderByDescending(it => it.SortIndex).ToList());
            }
        }
        catch (Exception ex) {
            BooApi.Logger.Error(ex);
        }
        return new CategorySupport(null);
    }
}
