using BooWin.model;
using Reactive.Bindings;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using io.github.toyota32k.BooLib.Server.mdns;
using io.github.toyota32k.BooLib.Utils;
using BooWin.DI.contract;

namespace BooWin.view.viewmodel; 
public class DiscoverPeerDialogViewModel : IDisposable {
    IMainThreadService _mainThreadService;

    public DiscoverPeerDialogViewModel(IMainThreadService mainThreadService) {
        _mainThreadService = mainThreadService;
        Selected = SelectedPeer.Select(peer => peer != null).ToReadOnlyReactiveProperty();
    }

    // Browser に渡す discovery 結果コレクション (参照は不変)。
    public ObservableCollection<PeerHost> DiscoveredPeers { get; } = new();
    public ReactivePropertySlim<bool> NoPeer { get; } = new(true);
    public ReactivePropertySlim<PeerHost?> SelectedPeer { get; } = new ();
    //public ReactiveCommand CancelDiscoveringCommand { get; } = new();
    public ReadOnlyReactiveProperty<bool> Selected { get; }

    //private TaskCompletionSource<PeerHost?>? _tcs = null;
    private MdnsBrowser? mBrowser = null;

    private int findPeerIndex(DiscoveredPeer peer) {
        for(int i=0; i<DiscoveredPeers.Count; i++) {
            if(string.Compare(DiscoveredPeers[i].ServiceName, peer.InstanceName, StringComparison.CurrentCultureIgnoreCase)==0) {
                return i;
            }
        }
        return -1;
    }

    //public void Release() {
    //    _tcs?.TrySetResult(null);
    //    _tcs = null;
    //}

    public void Discover() {
        //if (_tcs != null) return await _tcs.Task;

        var browser = new MdnsBrowser(info => {
            _mainThreadService.Run(() => {
                var i = findPeerIndex(info.Peer);
                if (info.Type == MdnsBrowser.UpdateInfo.UpdateType.AddOrUpdate) {
                    if (i >= 0) {
                        DiscoveredPeers[i] = PeerHost.FromDiscoveredPeer(info.Peer);
                    }
                    else {
                        DiscoveredPeers.Add(PeerHost.FromDiscoveredPeer(info.Peer));
                    }
                }
                else if (info.Type == MdnsBrowser.UpdateInfo.UpdateType.Remove) {
                    if (i >= 0) {
                        DiscoveredPeers.RemoveAt(i);
                    }
                }
                NoPeer.Value = DiscoveredPeers.IsEmpty();
            });
        });
        mBrowser = browser;
        browser.Start();
    }

    public void Dispose() {
        mBrowser?.Dispose();
        mBrowser = null;
    }
}
