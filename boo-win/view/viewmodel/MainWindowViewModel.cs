using Reactive.Bindings;

namespace BooWin.view.viewmodel; 
public class MainWindowViewModel {
    public ReactivePropertySlim<string> WindowTitle { get; } = new ReactivePropertySlim<string>("BooPlayer for Win");
}
