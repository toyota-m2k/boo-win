using BooWin.DI.contract;
using BooWin.model;
using BooWin.view.viewmodel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BooWin.view.page;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class EntryPage : Page {
    public EntryPageViewModel ViewModel { get; }
    public EntryPage() {
        ViewModel = App.GetService<EntryPageViewModel>();
        InitializeComponent();
    }

    // ListView のアイテムのコンテキストメニューのクリックイベントハンドラ
    // リストアイテムビューは、x:Bindのルートがリストアイテム(PeerHost)に固定され、
    // ViewModelにアクセスできないため、コードビハインドで処理する。
    // 代案：
    // - Binding を使う（Binding なら ElementNameでルートを EntryPage にできる）
    // - EntryPageViewModel を static にして、x:Bind のルートを EntryPageViewModel にする
    // - PeerHost に、DeleteCommand, EditCommand を持たせる（または、それらを持ったラッパーを挟む）
    // Bindingはリフレクションベースなので、WinUI3時代にはあまり使いたくない。
    // EntryPageViewModelはシングルトンなので、それでもよいが、あまりスマートではない。
    // PeerHost にコマンドを持たせるのは、PeerHost はモデルなので、UIに依存するコマンドを持たせるのは不自然。
    // ラッパーを挟むのは、コードが複雑になる。
    // 以上の検討の結果、残念ながら素直にコードビハインドで処理することとした。

    private void OnDeleteHostClick(object sender, RoutedEventArgs e) {
        if (((FrameworkElement)sender).DataContext is PeerHost host) {
            ViewModel.DeleteHostCommand.Execute(host);
        }
    }

    private void OnEditHostClick(object sender, RoutedEventArgs e) {
        if (((FrameworkElement)sender).DataContext is PeerHost host) {
            ViewModel.EditHostCommand.Execute(host);
        }
    }

    private void OnHostItemPointerEntered(object sender, PointerRoutedEventArgs e) {
        SetItemActionsVisible(sender, true);
    }

    private void OnHostItemPointerExited(object sender, PointerRoutedEventArgs e) {
        SetItemActionsVisible(sender, false);
    }

    private static void SetItemActionsVisible(object sender, bool visible) {
        if (sender is not Panel panel) return;
        var actions = panel.Children.OfType<FrameworkElement>().FirstOrDefault(c => c.Tag as string == "ItemActions");
        if (actions == null) return;
        actions.Opacity = visible ? 1 : 0;
        actions.IsHitTestVisible = visible;
    }

    private async void OnListDoubleTapped(object sender, DoubleTappedRoutedEventArgs e) {
        // 
        if ((e.OriginalSource as FrameworkElement)?.DataContext is not PeerHost host) {
            return;
        }
        ViewModel.CurrentHost.Value = host;
        if (!await SafeGotoPlayerPage()) {
            await App.GetService<IDialogService>().ShowMessage("Host", $"{host.ServiceName} is not available.");
        }
    }

    public async Task<bool> SafeGotoPlayerPage() {
        if (ViewModel.CurrentHost.Value == null) {
            return false;
        }
        try {
            // CanOpenPlayerPageがtrue になるまで最大１秒待機
            await ViewModel.CanOpenPlayerPage.Where(x => x)
                      .FirstAsync()
                      .Timeout(TimeSpan.FromSeconds(1));
            ViewModel.GotoPlayerPageCommand.Execute();
            return true;
        }
        catch (TimeoutException) {
            // タイムアウト
            return false;
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e) {
        if (ViewModel.FirstTime) {
            ViewModel.FirstTime = false;
            if (IsLoaded) {
                _ = StartupAsync();
            } else {
                Loaded += OnFirstLoaded;
            }
        }
    }

    private void OnFirstLoaded(object sender, RoutedEventArgs e) {
        Loaded -= OnFirstLoaded;
        _ = StartupAsync();
    }

    /// <summary>
    /// 起動直後の処理：編集中セッションがあれば一覧を表示し、その後 AutoStart を処理する
    /// </summary>
    private async Task StartupAsync() {
        await ViewModel.ShowEditSessionsAsync(onlyIfAny: true);
        if (ViewModel.AutoStartPlayer.Value) {
            await SafeGotoPlayerPage();
        }
    }
}
