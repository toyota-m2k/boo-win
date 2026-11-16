using BooWin.client;
using io.github.toyota32k.BooLib.Utils;
using System;
using System.Text;

namespace BooWin.model; 
public class Capability {
    public string ServerName { get; }
    public int Version { get; }

    public string Root { get; }
    public int Category { get; }
    public int Rating { get; }
    public int Mark { get; }
    public bool Chapter { get; }
    public bool HasView { get; }
    public string Types { get; }
    public string CategoryLabel { get; }
    public bool Authentication { get; }

    private Capability(
        string serverName,
        int version,
        string root,
        int category,
        int rating,
        int mark,
        bool chapter,
        bool hasView,
        string types,
        string categoryLabel,
        bool authentication
        ) {
        ServerName = serverName;
        Version = version;
        Root = root;
        Category = category;
        Rating = rating;
        Mark = mark;
        Chapter = chapter;
        HasView = hasView;
        Types = types;
        CategoryLabel = categoryLabel;
        Authentication = authentication;
    }

    public static Capability? FromJson(string? jsonString) {
        if (jsonString == null) return null;
        try {
            var json = JsonUtil.ParseOrThrow(jsonString);
            var serverName = json.GetStringOrNull("serverName") ?? "unknown";
            var root = json.GetStringOrNull("root") ?? "/";
            if (!root.StartsWith('/')) {
                root = '/' + root;
            }
            if (!root.EndsWith('/')) {
                root = root + '/';
            }
            var category = 0;
            var rating = 0;
            var mark = 0;
            var version = json.GetInt("version", 1);
            if (version==3) {
                category = json.GetInt("category", 0);
                rating = json.GetInt("rating", 0);
                mark = json.GetInt("mark", 0);
            } else {
                var reputation = json.GetInt("reputation", 0);
                category = rating = mark = reputation;
            }
            var chapter = json.GetBool("chapter");
            var hasView = json.GetBool("hasView");
            var types = json.GetString("types", "vap");
            var categoryLabel = json.GetString("categoryLabel", "Category");
            var authentication = json.GetBool("authentication");
            return new Capability(serverName, version, root, category, rating, mark, chapter, hasView, types, categoryLabel, authentication);
        } catch(Exception e) {
            BooApi.Logger.Error(e);
            return null;
        }
        
        
    }
}
