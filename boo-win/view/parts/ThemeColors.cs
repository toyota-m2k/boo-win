using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace BooWin.view.parts;

/**
 * Win2D など、XAML の ThemeResource を直接使えない描画処理のために、
 * 要素の ActualTheme に応じたシステムカラーを取得するヘルパー。
 */
internal static class ThemeColors {
    public static Color Get(FrameworkElement element, string brushKey, Color fallback) {
        var themeKey = element.ActualTheme == ElementTheme.Light ? "Light" : "Dark";
        if (TryFind(Application.Current.Resources, themeKey, brushKey, out var value)
            || (themeKey == "Dark" && TryFind(Application.Current.Resources, "Default", brushKey, out value))) {
            return ToColor(value, fallback);
        }
        if (Application.Current.Resources.TryGetValue(brushKey, out var v)) {
            return ToColor(v, fallback);
        }
        return fallback;
    }

    private static Color ToColor(object value, Color fallback) => value switch {
        SolidColorBrush b => b.Color,
        Color c => c,
        _ => fallback,
    };

    private static bool TryFind(ResourceDictionary dic, string themeKey, string key, out object value) {
        if (dic.ThemeDictionaries.TryGetValue(themeKey, out var td) && td is ResourceDictionary themeDic
            && themeDic.TryGetValue(key, out value)) {
            return true;
        }
        foreach (var merged in dic.MergedDictionaries) {
            if (TryFind(merged, themeKey, key, out value)) {
                return true;
            }
        }
        value = null!;
        return false;
    }
}
