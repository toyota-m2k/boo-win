using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Reactive.Bindings;
using System.Threading;

namespace BooWin.DI.contract;

public interface INavigationAware {
    void OnPageActivated();
    void OnPageLeaving();
}

public interface IPageService {
    ReadOnlyReactivePropertySlim<bool> CanGoBack { get; }
    void Startup(Frame rootFrame);
    void GoBack();
    Page? CurrentPage { get; }
    XamlRoot XamlRoot { get; }

    void GoToEntryPage();
    void GoToPlayerPage();
    DispatcherQueue? DispatcherQueue { get; }
    SynchronizationContext UIContext { get; }
}
