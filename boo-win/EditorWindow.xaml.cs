using BooWin.DI.contract;
using BooWin.view.page;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace BooWin;

/// <summary>
/// 動画編集ウィンドウ
/// 複数インスタンスの同時起動が可能。Windowごとに独立したDIスコープを持つ。
/// </summary>
public sealed partial class EditorWindow : Window {
    public Frame RootFrame => _rootFrame;
    private readonly IServiceScope _scope;
    public IServiceProvider Services => _scope.ServiceProvider;

    public EditorWindow() {
        _scope = App.CreateWindowScope();
        InitializeComponent();
        Closed += OnClosed;
        GetService<IPageService>().Startup(this, RootFrame, typeof(EditorPage));
    }

    public T GetService<T>() where T : class {
        return Services.GetRequiredService<T>();
    }

    private void OnClosed(object sender, WindowEventArgs args) {
        Closed -= OnClosed;
        _scope.Dispose();
    }
}
