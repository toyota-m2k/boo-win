using BooWin.DI.contract;
using BooWin.model;
using CommunityToolkit.WinUI;
using io.github.toyota32k.BooLib.Utils;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
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
    public UtLog Logger = new UtLog("PlayerPage");
    private readonly ISettingsService _settingsService;
    private readonly IClientService _clientService;
    private readonly IPageService _pageService;
    private readonly IDialogService _dialogService;

    public PlayerViewModel PlayerModel { get; }
    public FilterViewModel FilterModel { get; }
    public ItemPropertyViewModel ItemPropertyModel { get; } = new ItemPropertyViewModel();


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

        IsPlayerOnly.Subscribe(v => {
            if (v) {
                // Player Only モードになったら、ItemPropertyも非表示にする。
                IsPropertiesVisible.Value = false;
            }
        });

        ItemList = ItemListSource
            .CombineLatest(FilterModel.Filter, (list, filter) => {
                var currentItem = SelectedItem.Value;
                var position = PlayerModel.PlayerPosition.Value;
                var newList = list.Items.Where(it => filter.Filter(it)).ToList();
                ReservePlayingItem(currentItem?.Id, position);
                return newList;
            }).ToReadOnlyReactiveProperty();

        BackToEntryPageCommand.Subscribe(() => {
            _pageService.GoToEntryPage();
        });

        SelectedItem.Subscribe(item => {
            PlayerModel.SetSource(item);
            var changes = ItemPropertyModel.SetSource(item, _clientService);
            // ItemPropertyModel#SetSourceを呼んだ時に、以前のItemPropertyに変更があれば、IChanges が返ってくる。
            if (changes!=null) {
                OnUpdateItemProperties(changes);
            }
        });

        PlayerModel.NextItemCommand.ObserveOn(_pageService.UIContext).Subscribe((_) => {
            var item = SelectedItem.Value;
            if (item == null) return;
            var list = ItemList.Value;
            if (list == null || list.Count == 0) return;
            var index = list.FindIndex(it => it.Id == item.Id);
            if (index>=0 && index+1 < list.Count) {
                SelectedItem.Value = list[index+1];
            }
        });

        PlayerModel.PrevItemCommand.ObserveOn(_pageService.UIContext).Subscribe((_) => {
            var item = SelectedItem.Value;
            if (item == null) return;
            var list = ItemList.Value;
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

        IsCategorySupported.Value = _clientService.CategorySupport.IsSupported;
        IsRatingSupported.Value = _clientService.RatingSupport.IsSupported;
        IsMarkSupported.Value = _clientService.MarkSupport.IsSupported;
        
        CanEditCategory.Value = _clientService.CategorySupport.AllowReadWrite;
        CanEditRating.Value = _clientService.RatingSupport.AllowReadWrite;
        CanEditMark.Value = _clientService.MarkSupport.AllowReadWrite;
        CanEditName.Value = _clientService.Capability.CanEditName;
        CanEditLabel.Value = _clientService.Capability.CanEditLabel;

        FilterModel.ResetOnHostChanged();
        ItemPropertyModel.ResetOnHostChanged(_clientService);
        //FilterModel.CategoryLabel.Value = _clientService.Capability.CategoryLabel;
        //FilterModel.CategoryList.Value = [BuiltInCategory.All, .. _clientService.CategorySupport.Categories, BuiltInCategory.Unspecified];
        //FilterModel.SelectedCategory.Value = FilterModel.CategoryList.Value[0];

        ReservePlayingItem(currentHost.LastPlayingId, currentHost.LastPlayingPosition);
        
        

        //_pageService.DispatcherQueue?.TryEnqueue(() => {
        //    var lastPlayingId = currentHost.LastPlayingId;
        //    if (lastPlayingId != null) {
        //        var item = ItemList.Value.Find(it => it.Id == lastPlayingId);
        //        if (item != null) {
        //            PlayerModel.LastPlayingPosition = currentHost.LastPlayingPosition;
        //            SelectedItem.Value = item;
        //        }
        //    }
        //});
        return true;
    }

    private void ReservePlayingItem(string? lastPlayingId, long position) {
        if (string.IsNullOrEmpty(lastPlayingId)) return;
        _pageService.DispatcherQueue?.EnqueueAsync(async () => {
            await Task.Delay(500);
            var item = ItemList.Value.Find(it => it.Id == lastPlayingId);
            if (item != null) {
                if (item.IsAV) {
                    PlayerModel.ReservedPlayingItemPosition = (item.Id, position);
                }
                SelectedItem.Value = item;
            }
        });
    }


    public ReactiveProperty<PeerHost?> CurrentHost { get; } = new ();
    public ReactiveProperty<MediaItemList> ItemListSource { get; } = new (MediaItemList.Empty);
    public ReadOnlyReactiveProperty<List<MediaItem>> ItemList { get; }
    public ReactiveProperty<MediaItem?> SelectedItem { get; } = new ();

    public ImageSource? GetItemUrl(MediaItem? item) {
        if (item == null) return null;
        var url = _clientService.GetItemUrl(item);
        if (string.IsNullOrEmpty(url)) return null;
        return new BitmapImage(new Uri(url));
    }

    public Visibility IsPhotoViewerVisible(MediaItem? item) {
        return (item?.IsPhoto == true) ? Visibility.Visible : Visibility.Collapsed;
    }
    public Visibility IsMediaPlayerVisible(MediaItem? item) {
        return (item?.IsAV == true) ? Visibility.Visible : Visibility.Collapsed;
    }


    public ReactiveProperty<bool> IsCategorySupported { get; } = new ReactiveProperty<bool>(false);
    public ReactiveProperty<bool> IsRatingSupported { get; } = new(false);
    public ReactiveProperty<bool> IsMarkSupported { get; } = new(false);

    public ReactiveProperty<bool> CanEditName { get; } = new(false);
    public ReactiveProperty<bool> CanEditLabel { get; } = new(false);
    public ReactiveProperty<bool> CanEditCategory { get; } = new(false);
    public ReactiveProperty<bool> CanEditRating { get; } = new(false);
    public ReactiveProperty<bool> CanEditMark { get; } = new(false);


    public ReactiveCommand BackToEntryPageCommand { get; } = new();

    public ReactiveProperty<bool> IsPlayerOnly { get; } = new(false);
    public ReactiveProperty<bool> IsPropertiesVisible { get; } = new(false);

    public void TogglePlayerOnly() => IsPlayerOnly.Value = !IsPlayerOnly.Value;

    public void SaveCurrentStatus() {
        var item = SelectedItem.Value;
        if (item == null) return;
        _settingsService.UpdateCurrentHostStatus(item.Id, 0, save: true);
        SelectedItem.Value = null;
    }

    private void OnUpdateItemProperties(ItemPropertyViewModel.IChanges changes) {
        Task.Run(async () => {
            try {
                // サーバーを更新
                var result = await _clientService.UpdateItemPropertiesAsync(changes.Item.Id, changes.Category, changes.Rating, changes.Mark, changes.Name, changes.Label);
                if (result) {
                    // MediaItemを更新
                    changes.ApplyChanges();
                    // ToDo: mark/category/ratingをリスト上に表示するなら、ItemList をObservableCollectionに変換して、変更を反映する 
                }
                else {
                    await _dialogService.ShowMessage("Server Error", "Failed to update item properties.");
                }
            }
            catch (Exception ex) {
                Logger.Error(ex);
                await _dialogService.ShowMessage("Exception", "Failed to update item properties.");
            }
        });
    }

}
