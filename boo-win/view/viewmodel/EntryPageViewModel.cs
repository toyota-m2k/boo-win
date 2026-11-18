using BooWin.DI.contract;
using BooWin.model;
using BooWin.view.dialog;
using io.github.toyota32k.BooLib.Utils;
using Reactive.Bindings;
using Reactive.Bindings.TinyLinq;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BooWin.view.viewmodel; 

public class EntryPageViewModel {
    public UtLog Logger = new UtLog("EPVM");
    private readonly ISettingsService _settingsService;
    private readonly IMainThreadService _mainThreadService;
    private readonly IDialogService _dialogService;
    private readonly IPageService _pageService;
    private readonly IClientService _clientService;

    public ObservableCollection<PeerHost> HostList { get; }
    public ReactiveProperty<PeerHost?> CurrentHost { get; } = new();
    public ReactiveProperty<bool> IsHostAvailable { get; } = new(false);

    public ReadOnlyReactiveProperty<bool> CanOpenPlayerPage { get; }

    public ReactiveCommand AddHostCommand { get; } = new();
    public ReactiveCommand DiscoverHostCommand { get; } = new();
    public ReactiveCommand<PeerHost> DeleteHostCommand { get; } = new();
    public ReactiveCommand<PeerHost> EditHostCommand { get; } = new();
    public ReactiveCommand GotoPlayerPageCommand { get; } = new();

    private bool HostListModified = false;

    public EntryPageViewModel(
        ISettingsService settingsService, 
        IMainThreadService mainThreadService,
        IDialogService dialogService,
        IPageService pageService,
        IClientService clientService    
        ) {
        _settingsService = settingsService;
        _mainThreadService = mainThreadService;
        _dialogService = dialogService;
        _pageService = pageService;
        _clientService = clientService;

        AddHostCommand.Subscribe(AddHost);
        DiscoverHostCommand.Subscribe(DiscoverPeer);
        DeleteHostCommand.Subscribe(DeleteHost);
        EditHostCommand.Subscribe(EditHost);
        GotoPlayerPageCommand.Subscribe(GotoPlayerPage);

        CanOpenPlayerPage = CurrentHost.CombineLatest(IsHostAvailable, (host,a) => host != null && a).ToReadOnlyReactiveProperty();
        CurrentHost.Subscribe(CheckHostAvailability);

        HostList = new ObservableCollection<PeerHost>(settingsService.Hosts);
    }

    public async void DiscoverPeer() {
        var peer = await DiscoverPeerDialog.Discover(_dialogService);
        if (peer != null) {
            if (!HostList.Any(it => it.Address == peer.Address)) {
                HostList.Add(peer);
                HostListModified = true;
            }
        }
    }

    public void Save() {
        _settingsService.UpdateHostList(HostList, CurrentHost.Value);
        _settingsService.Save();
        HostListModified = false;
    }
    
    private void DeleteHost(PeerHost host) {
        HostList.Remove(host);
        HostListModified = true;
    }

    private async void EditHost(PeerHost host) {
        var modified = await EditHostDialog.EditHost(_dialogService, host);
        if (modified != null) {
            var i = HostList.IndexOf(host);
            if (i >= 0) {
                HostList[i] = modified;
                HostListModified = true;
            }
        }
    }

    public async void AddHost() {
        var modified = await EditHostDialog.NewHost(_dialogService);
        if (modified != null) {
            HostList.Add(modified);
            HostListModified = true;
        }
    }

    public void GotoPlayerPage() {
        if (CurrentHost.Value != null && CanOpenPlayerPage.Value) {
            Save();
            _pageService.GoToPlayerPage();
        }
    }

    private CancellationTokenSource? previousTask = null;
    public void CheckHostAvailability(PeerHost? host) {
        lock (this) {
            if (previousTask != null) {
                previousTask.Cancel();
                previousTask = null;
            }
        }
        IsHostAvailable.Value = false;
        if (host != null) {
            var tcs = new CancellationTokenSource();
            lock (this) {
                previousTask = tcs;
            }
            Task.Run(async () => {
                if (await _clientService.CheckHostAvailabilityAsync(host)) {
                    if (!tcs.IsCancellationRequested) {
                        _mainThreadService.Run(() => {
                            IsHostAvailable.Value = true;
                        });
                    }
                }
            }, tcs.Token);
        }
    }
}
