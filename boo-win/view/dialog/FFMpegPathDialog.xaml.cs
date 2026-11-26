using BooWin.DI.contract;
using io.github.toyota32k.BooLib.Utils;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Reactive.Bindings;
using System.Threading.Tasks;

namespace BooWin.view.dialog;

public sealed partial class FFMpegPathDialog : ContentDialog {
    ReactiveProperty<string> FFMpegPath { get; }
    ReactiveProperty<bool> UsePathEnv { get; }
    private readonly Window? _parentWindow;

    private FFMpegPathDialog(string? currentPath, Window? parentWindow) {
        _parentWindow = parentWindow;
        FFMpegPath = new(currentPath ?? string.Empty);
        UsePathEnv = new(string.IsNullOrEmpty(currentPath));
        InitializeComponent();
    }

    private bool IsNotUsePathEnv(bool usePathEnv) => !usePathEnv;

    private async void OnBrowseClick(object sender, RoutedEventArgs e) {
        if (_parentWindow == null) return;
        var path = await FileOpenPickerBuilder
            .Create(_parentWindow)
            .AddFileType("FFMpeg", ".exe")
            .SetIdentifier("SelectFFMpegPath")
            .PickAsync();
        if (path != null) {
            FFMpegPath.Value = path;
        }
    }

    /// <returns>パス文字列 / 空文字列(PATH環境変数に従う) / null(キャンセル)</returns>
    public static async Task<string?> ShowAsync(IDialogService dialogService, Window? parentWindow, string? currentPath) {
        var dialog = new FFMpegPathDialog(currentPath, parentWindow);
        var result = await dialogService
            .Builder(dialog)
            .ShowOkCancel();
        if (!result) return null;
        return dialog.UsePathEnv.Value ? string.Empty : dialog.FFMpegPath.Value.Trim();
    }
}
