using BooWin.DI.contract;
using BooWin.model;
using Microsoft.Windows.System.Power;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BooWin.view.viewmodel;

public class PlayerPageViewModel
{
    private readonly ISettingsService _settingsService;
    private readonly IClientService _clientService;
    private readonly IPageService _pageService;
    private readonly IDialogService _dialogService;

    public PlayerViewModel PlayerModel { get; }
    public FilterViewModel FilterModel { get; }


    public PlayerPageViewModel(
        ISettingsService settingsService, 
        IClientService clientService,
        IPageService pageService,
        IDialogService dialogService) {
        _settingsService = settingsService;
        _clientService = clientService;
        _pageService = pageService;
        _dialogService = dialogService;

        PlayerModel = new PlayerViewModel(_clientService, _dialogService, _pageService, _settingsService);
        FilterModel = new FilterViewModel(_clientService);

        ItemList = ItemListSource
            .CombineLatest(FilterModel.Filter, (list, filter) => {
                return list.Items.Where(it => filter.Filter(it)).ToList();
            }).ToReadOnlyReactiveProperty();

        BackToEntryPageCommand.Subscribe(() => {
            _pageService.GoToEntryPage();
        });

        SelectedItem.Subscribe(item => {
            PlayerModel.SetSource(item);
        });

        PlayerModel.NextItemCommand.ObserveOn(_pageService.UIContext).Subscribe((_) => {
            var item = SelectedItem.Value;
            if (item == null) return;
            var list = ItemListSource.Value.Items;
            if (list == null || list.Count == 0) return;
            var index = list.FindIndex(it => it.Id == item.Id);
            if (index>=0 && index+1 < list.Count) {
                SelectedItem.Value = list[index+1];
            }
        });

        PlayerModel.PrevItemCommand.ObserveOn(_pageService.UIContext).Subscribe((_) => {
            var item = SelectedItem.Value;
            if (item == null) return;
            var list = ItemListSource.Value.Items;
            if (list == null || list.Count == 0) return;
            var index = list.FindIndex(it => it.Id == item.Id);
            if (index > 0) {
                SelectedItem.Value = list[index - 1];
            }
        });

    }


    public async Task<bool> Prepare() {
        PlayerModel.SetSource(null);
        ItemListSource.Value = MediaItemList.Empty;
        var currentHost = _settingsService.CurrentHost;
        if (currentHost == null) {
            return false;
        }
        CurrentHost.Value = currentHost;
        await _clientService.SetCurrentHostAsync(currentHost);
        ItemListSource.Value = await _clientService.GetMediaItemListAsync();

        var lastPlayingId = currentHost.LastPlayingId;
        if (lastPlayingId!=null) {
            var item = ItemListSource.Value.Items.Find(it => it.Id == lastPlayingId);
            if (item != null) {
                SelectedItem.Value = item;
                PlayerModel.LastPlayingPosition = currentHost.LastPlayingPosition;
            }
        }
        FilterModel.CategoryLabel.Value = _clientService.Capability.CategoryLabel;
        FilterModel.CategoryList.Value = [BuiltInCategory.All, .. _clientService.CategorySupport.Categories, BuiltInCategory.Unspecified];
        FilterModel.IsCategorySupported.Value = _clientService.CategorySupport.IsSupported;
        FilterModel.IsRatingSupported.Value = _clientService.RatingSupport.IsSupported;
        FilterModel.IsMarkSupported.Value = _clientService.MarkSupport.IsSupported;

        return true;
    }
    public ReactiveProperty<PeerHost?> CurrentHost { get; } = new ();
    public ReactiveProperty<MediaItemList> ItemListSource { get; } = new (MediaItemList.Empty);
    public ReadOnlyReactiveProperty<List<MediaItem>> ItemList { get; }

    public ReactiveProperty<MediaItem?> SelectedItem { get; } = new ();

    public ReactiveCommand BackToEntryPageCommand { get; } = new();

    public ReactiveProperty<bool> IsPlayerOnly { get; } = new(false);
    public void TogglePlayerOnly() => IsPlayerOnly.Value = !IsPlayerOnly.Value;

    public void SaveCurrentStatus() {
        var item = SelectedItem.Value;
        if (item == null) return;
        _settingsService.UpdateCurrentHostStatus(item.Id, 0);
    }
}
