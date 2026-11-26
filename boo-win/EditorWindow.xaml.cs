using BooWin.DI.contract;
using BooWin.model;
using BooWin.model.editor;
using BooWin.view.page;
using BooWin.view.viewmodel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BooWin;

/// <summary>
/// 動画編集ウィンドウ
/// 複数インスタンスの同時起動が可能。Windowごとに独立したDIスコープを持つ。
/// </summary>
public sealed partial class EditorWindow : Window {
    public Frame RootFrame => _rootFrame;
    private readonly IServiceScope _scope;
    public IServiceProvider Services => _scope.ServiceProvider;
    /// <summary>
    /// 編集対象の動画ファイルパス（コマンドライン/ファイル関連付けから起動された場合）
    /// </summary>
    public EditorWindowViewModel ViewModel;

    public EditorWindow(IEditorItem item, List<Chapter>? chapters) {
        _scope = App.CreateWindowScope();
        ViewModel = GetService<EditorWindowViewModel>();
        InitializeComponent();
        AppWindow.Closing += AppWindow_Closing;
        Closed += OnClosed;
        GetService<IPageService>().Startup(this, RootFrame, typeof(EditorPage), this);
        ViewModel.WindowTitle.Subscribe(title => {
            this.Title = title;
        });
        ViewModel.SetTargetItem(item, chapters);
    }


    //public EditorWindow() {
    //    _scope = App.CreateWindowScope();
    //    InitializeComponent();
    //    Closed += OnClosed;
    //    GetService<IPageService>().Startup(this, RootFrame, typeof(EditorPage), this);
    //    ViewModel.WindowTitle.Subscribe(title => {
    //        this.Title = title;
    //    });
    //}

    public T GetService<T>() where T : class {
        return Services.GetRequiredService<T>();
    }

    private bool _closeConfirmed = false;
    private async void ConfirmClose() {
        if (ViewModel.PlayerModel.ChapterModel.IsDirty) {
            var r = await GetService<IDialogService>()
                .ShowYesNo("Discard Changes", "Chapters are modified. \r\nAre you sure you want to exit?", yesAsDefault: true);
            if (r!=true) {
                return;
            }
        }
        _closeConfirmed = true;
        // ウィンドウ破棄前にビジュアルツリーを外し、Win2D(CanvasControl) 等の Unloaded を
        // ウィンドウが有効なうちに走らせる（最後のウィンドウ終了時のネイティブ例外対策）
        _rootFrame.Content = null;
        await Task.Delay(500);
        Close();
    }

    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args) {
        if (!_closeConfirmed) {
            args.Cancel = true;
            ConfirmClose();
            return;
        }
    }

    private void OnClosed(object sender, WindowEventArgs args) {
        Closed -= OnClosed;
        _rootFrame.Content = null;
        _scope.Dispose();
    }
}
