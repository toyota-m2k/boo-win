using BooWin.DI.contract;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BooWin.view.dialog;

/**
 * ダウンロードの進捗を表示し、キャンセルボタンでダウンロードを中止できるダイアログ
 */
public sealed partial class DownloadProgressDialog : ContentDialog {
    private readonly ProgressBar _progressBar;
    private readonly TextBlock _statusText;
    private readonly CancellationTokenSource _cts = new();
    private readonly Func<CancellationToken, Action<long, long>, Task<bool>> _download;
    private readonly DispatcherQueue _dispatcherQueue;
    private bool _result = false;
    private bool _completed = false;

    private DownloadProgressDialog(string message, Func<CancellationToken, Action<long, long>, Task<bool>> download) {
        _download = download;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _progressBar = new ProgressBar { Minimum = 0, Maximum = 100, IsIndeterminate = true, Margin = new Thickness(0, 8, 0, 4) };
        _statusText = new TextBlock { Text = "", HorizontalAlignment = HorizontalAlignment.Right };
        Content = new StackPanel {
            MinWidth = 320,
            Children = {
                new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                _progressBar,
                _statusText,
            }
        };
        Opened += OnOpened;
        Closing += (_, _) => {
            if (!_completed) {
                _cts.Cancel();
            }
        };
    }

    private async void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args) {
        try {
            _result = await _download(_cts.Token, OnProgress);
        }
        catch (OperationCanceledException) {
            _result = false;
        }
        catch (Exception) {
            _result = false;
        }
        _completed = true;
        Hide();
    }

    private void OnProgress(long received, long total) {
        _dispatcherQueue.TryEnqueue(() => {
            if (total > 0) {
                _progressBar.IsIndeterminate = false;
                _progressBar.Value = Math.Min(100.0, received * 100.0 / total);
                _statusText.Text = $"{FormatSize(received)} / {FormatSize(total)} ({_progressBar.Value:F0}%)";
            }
            else {
                _progressBar.IsIndeterminate = true;
                _statusText.Text = FormatSize(received);
            }
        });
    }

    private static string FormatSize(long size) {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double v = size;
        int i = 0;
        while (v >= 1024 && i < units.Length - 1) {
            v /= 1024;
            i++;
        }
        return i == 0 ? $"{size} {units[i]}" : $"{v:F1} {units[i]}";
    }

    /**
     * ダウンロード進捗ダイアログを表示してダウンロードを実行する。
     * @return true: ダウンロード成功 / false: 失敗またはキャンセル
     */
    public static async Task<bool> Show(IDialogService dialogService, string title, string message, Func<CancellationToken, Action<long, long>, Task<bool>> download) {
        var dialog = new DownloadProgressDialog(message, download);
        await dialogService
            .Builder(dialog)
            .Title(title)
            .CloseButton("Cancel", true, null)
            .ShowAsync();
        return dialog._completed && dialog._result;
    }
}
