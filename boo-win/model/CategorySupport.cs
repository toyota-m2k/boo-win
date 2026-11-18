using BooWin.client;
using io.github.toyota32k.BooLib.Utils;
using System;
using System.Collections.Generic;
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

    public bool Match(string? categoryName) {
        if (this == BuiltInCategory.All) {
            return true;
        }
        if (string.IsNullOrEmpty(categoryName)) {
            return this == BuiltInCategory.Unspecified;
        }
        return Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase);
    }
}
public class  Category : ICategory {

    [JsonPropertyName("label")]
    public string Name { get; set; } = "";
    [JsonPropertyName("sort")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int SortIndex { get; set; } = 0;
    [JsonPropertyName("color")]
    [JsonConverter(typeof(NumberOrStringConverter))]
    public string ColorName { get; set; } = "";
    [JsonPropertyName("svg")]
    public string SvgPath { get; set; } = "";
    public bool IsBuiltIn => false;
    //[JsonIgnore]
    //public Color Color => Color.FromArgb(ARGB);

    public static ICategory All => BuiltInCategory.All;
    public static ICategory Unspecified => BuiltInCategory.Unspecified;
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
    public string UnspecifiedCategoryName { get; }

    public ICategory GetCategoryByName(string name) {
        return Categories.FirstOrDefault(it => it.Match(name)) ?? Category.Unspecified;
    }

    public List<ICategory> GetCategoryList(bool withAll, bool withUnspecified) {
        if (!IsSupported) {
            return new List<ICategory>();
        }
        var list = Categories.Where(c=> 
                           !c.Name.Equals(BuiltInCategory.All.Name, StringComparison.InvariantCultureIgnoreCase)
                        && !c.Name.Equals(UnspecifiedCategoryName, StringComparison.InvariantCultureIgnoreCase)).ToList();
        if (withAll) {
            list.Insert(0, BuiltInCategory.All);
        }
        if (withUnspecified) {
            list.Add(BuiltInCategory.Unspecified);
        }
        return list;
    }

    private CategorySupport(List<ICategory>? list, bool allowReadWrite, string unspecifiedCategoryName) {
        Categories = list ?? new List<ICategory>();
        AllowReadWrite = allowReadWrite && IsSupported;
        UnspecifiedCategoryName = unspecifiedCategoryName;
    }
    public static CategorySupport Empty => new CategorySupport(null, false, BuiltInCategory.Unspecified.Name);

    public static CategorySupport FromJson(string? jsonString, bool allowReadWrite) {
        try {
            var json = JsonUtil.ParseOrThrow(jsonString);
            var unspecified = json.GetString("unchecked", BuiltInCategory.Unspecified.Name);
            var categories = json["categories"].Deserialize<List<Category>>(JsonUtil.SerializeCaseInsensitive);
            if (categories != null) {
                return new CategorySupport(categories.OrderByDescending(it => it.SortIndex).Cast<ICategory>().ToList(), allowReadWrite, unspecified);
            }
        }
        catch (Exception ex) {
            BooApi.Logger.Error(ex);
        }
        return Empty;
    }
}
