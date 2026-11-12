using BooWin.DI.contract;
using BooWin.model;
using Microsoft.Windows.System.Power;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BooWin.view.viewmodel;

public class PlayerPageViewModel
{
    private readonly ISettingsService _settingsService;
    private readonly IClientService _clientService;
    private readonly IPageService _pageService;

    public PlayerPageViewModel(
        ISettingsService settingsService, 
        IClientService clientService,
        IPageService pageService) {
        _settingsService = settingsService;
        _clientService = clientService;
        _pageService = pageService;

        ItemList = ItemListSource.Select(it => it.Items).ToReadOnlyReactiveProperty();
        BackToEntryPageCommand.Subscribe(() => {
            _pageService.GoToEntryPage();
        });
    }

    public async Task<bool> Prepare() {
        var currentHost = _settingsService.CurrentHost;
        if (currentHost == null) {
            return false;
        }
        CurrentHost.Value = currentHost;
        await _clientService.SetCurrentHostAsync(currentHost);
        ItemListSource.Value = await _clientService.GetMediaItemListAsync();

        return true;
    }
    public ReactiveProperty<PeerHost?> CurrentHost { get; } = new ();
    public ReactiveProperty<MediaItemList> ItemListSource { get; } = new (MediaItemList.Empty);
    public ReadOnlyReactiveProperty<List<MediaItem>> ItemList { get; }

    public ReactiveProperty<MediaItem?> SelectedItem { get; } = new ();

    public ReactiveCommand BackToEntryPageCommand { get; } = new();

    public void SaveCurrentStatus() {
        var item = SelectedItem.Value;
        if (item == null) return;
        _settingsService.UpdateCurrentHostStatus(item.Id, 0);
    }
}
