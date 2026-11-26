using BooWin.DI.contract;
using io.github.toyota32k.BooLib.Media;
using io.github.toyota32k.BooLib.Utils;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BooWin.view.dialog;

public sealed partial class ConvertProgressDialog : ContentDialog {
    private readonly TextBlock _messageText;
    private readonly ProgressBar _progressBar;
    private readonly TextBlock _statusText;
    private readonly CancellationTokenSource _cts = new();
    private readonly Func<CancellationToken, Progress, Task<IConvertResult>> _process;
    private readonly DispatcherQueue _dispatcherQueue;
    private IConvertResult? _result = null;
    private bool _completed = false;
    public UtLog Logger = new UtLog("CPDlg");
    private ConvertProgressDialog(Func<CancellationToken, Progress, Task<IConvertResult>> process) {
        _process = process;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _messageText = new TextBlock { Text = "", TextWrapping = TextWrapping.Wrap };
        _progressBar = new ProgressBar { Minimum = 0, Maximum = 100, IsIndeterminate = true, Margin = new Thickness(0, 8, 0, 4) };
        _statusText = new TextBlock { Text = "", HorizontalAlignment = HorizontalAlignment.Right };
        Content = new StackPanel {
            MinWidth = 320,
            Children = {
                _messageText,
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
            var progress =new Progress();
            progress.OnProgressChanged += OnProgress;
            _result = await _process(_cts.Token, progress);
        }
        catch (OperationCanceledException) {
            _result = null;
        }
        catch (Exception e) {
            Logger.Error(e, "Cannot be converted.");
            _result = null;
        }
        _completed = true;
        Hide();
    }

    private void OnProgress(Progress progress) {
        _dispatcherQueue.TryEnqueue(() => {
            _messageText.Text = progress.ProcessId switch {
                Progress.Id.ANALYZING => "Analyzing...",
                Progress.Id.COMPRESSING => "Compressing...",
                Progress.Id.EXTRACTING => "Extracting...",
                Progress.Id.SPLITTING => "Splitting...",
                Progress.Id.COMBINING => "Combining...",
                _ => "Preparing...",
            };
            if (progress.TotalDuration == TimeSpan.Zero) {
                _progressBar.IsIndeterminate = false;
                _progressBar.Value = Math.Min(100.0, progress.PercentOfTime);
                _statusText.Text = progress.ComposeDurationMessage();
            }
            else {
                _progressBar.IsIndeterminate = true;
                _statusText.Text = "...";
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
    public static async Task<IConvertResult?> ShowAsync(IDialogService dialogService, 
        string title, Func<CancellationToken, Progress, Task<IConvertResult>> process) {
        var dialog = new ConvertProgressDialog(process);
        await dialogService
            .Builder(dialog)
            .Title(title)
            .CloseButton("Cancel", true, null)
            .ShowAsync();
        return dialog._result;
    }
}
