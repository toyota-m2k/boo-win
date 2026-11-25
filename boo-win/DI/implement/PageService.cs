using BooWin.DI.contract;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Reactive.Bindings;
using System;
using System.Reactive.Linq;
using System.Threading;
using Windows.UI.WebUI;

namespace BooWin.DI.implement;

public class PageService : IPageService {
    private Page? _currentPage = null;
    private Frame? _rootFrame = null;
    private ReactivePropertySlim<Type?> _currentPageType = new(null);
    private Type _homePageType = typeof(view.page.EntryPage);
    public ReadOnlyReactivePropertySlim<bool> CanGoBack { get; }

    public Page? CurrentPage => _currentPage;
    public XamlRoot XamlRoot {
        get {
            if (_rootFrame == null) {
                throw new InvalidOperationException("Root frame is not set. Call Startup() first.");
            }
            return _rootFrame.XamlRoot;
        }
    }

    public PageService() {
        CanGoBack = _currentPageType
            .Select(x => x != null && x != _homePageType)
            .ToReadOnlyReactivePropertySlim();
    }

    public void GoBack() {
        if (CanGoBack.Value) {
            // 戻り先は、必ずホームページ（MainWindowならEntryPage）
            NavigateTo(_homePageType);
        }
    }

    public void GoToEntryPage() {
        NavigateTo(typeof(view.page.EntryPage));
    }

    public void GoToPlayerPage() {
        NavigateTo(typeof(view.page.PlayerPage));
    }

    public Window? Window { get; private set; }

    public void Startup(Window window, Frame rootFrame, Type? homePageType = null) {
        Window = window;
        _rootFrame = rootFrame;
        _homePageType = homePageType ?? typeof(view.page.EntryPage);
        rootFrame.Navigated += OnNavigated;
        NavigateTo(_homePageType);
    }

    private void OnNavigated(object sender, NavigationEventArgs e) {
        if (sender is Frame frame) {
            // BackStackは自力で管理する。
            frame.BackStack.Clear();
            if (e.Content is Page page) {
                _currentPage = page;
                if (page is INavigationAware after) {
                    after.OnPageActivated();
                }
            }
        }
    }

    private void NavigateTo(Type pageType) {
        if (_rootFrame == null) {
            throw new InvalidOperationException("Root frame is not set. Call Startup() first.");
        }
        if (_currentPageType.Value != pageType) {
            _currentPageType.Value = pageType;
            if (_currentPage is INavigationAware before) {
                before.OnPageLeaving();
            }
            _rootFrame?.Navigate(pageType);
        }
    }

    public DispatcherQueue DispatcherQueue => _rootFrame?.DispatcherQueue ?? throw new InvalidOperationException("Root frame is not set. Call Startup() first.");
    private SynchronizationContext? _uiContext = null;
    public SynchronizationContext UIContext {
        get {
            if (_uiContext == null) {
                _uiContext = new DispatcherQueueSynchronizationContext(DispatcherQueue);
            }
            return _uiContext;
        }
    }
}
