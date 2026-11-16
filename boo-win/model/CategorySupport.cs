using BooWin.client;
using io.github.toyota32k.BooLib.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BooWin.model;

public interface ICategory {
    public string Name { get; }
    public int SortIndex { get; }
    public string ColorName { get; }
    public string SvgPath { get; }
    public bool IsBuiltIn { get; }
}
public class  Category : ICategory {

    [JsonPropertyName("label")]
    public string Name { get; set; } = "";
    [JsonPropertyName("sort")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int SortIndex { get; set; } = 0;
    [JsonPropertyName("color")]
    public string ColorName { get; set; } = "";
    [JsonPropertyName("svg")]
    public string SvgPath { get; set; } = "";
    public bool IsBuiltIn => false;
    //[JsonIgnore]
    //public Color Color => Color.FromArgb(ARGB);
}

public class BuiltInCategory : ICategory {
    public string Name { get; }

    public int SortIndex { get; }

    public string ColorName => "Black";

    public string SvgPath => "";

    public bool IsBuiltIn => true;
    private BuiltInCategory(string name, int sortIndex) {
        Name = name;
        SortIndex = sortIndex;
    }
    public static ICategory All { get; } = new BuiltInCategory("All", int.MaxValue);
    public static ICategory Unspecified { get; } = new BuiltInCategory("Unspecified", int.MaxValue - 1);

}

public class CategorySupport {
    public List<ICategory> Categories { get; }
    public bool IsSupported => Categories.Count > 0;
    public bool AllowReadWrite { get; set; } = false;

    private CategorySupport(List<ICategory>? list=null, bool allowReadWrite=false) {
        Categories = list ?? new List<ICategory>();
        AllowReadWrite = allowReadWrite;
    }
    public static CategorySupport Empty => new CategorySupport(null);

    public static CategorySupport FromJson(string? jsonString, bool allowReadWrite) {
        try {
            var json = JsonUtil.ParseOrThrow(jsonString);
            var categories = json["categories"].Deserialize<List<Category>>(JsonUtil.SerializeCaseInsensitive);
            if (categories != null) {
                return new CategorySupport(categories.OrderByDescending(it => it.SortIndex).Cast<ICategory>().ToList(), allowReadWrite);
            }
        }
        catch (Exception ex) {
            BooApi.Logger.Error(ex);
        }
        return Empty;
    }
}
