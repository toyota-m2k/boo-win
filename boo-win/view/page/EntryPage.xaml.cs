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
using System.Runtime.InteropServices.WindowsRuntime;
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
}
