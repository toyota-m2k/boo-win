using BooWin.client;
using BooWin.DI.contract;
using BooWin.model;
using BooWin.model.editor;
using BooWin.view.dialog;
using CommunityToolkit.WinUI;
using io.github.toyota32k.BooLib.Utils;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BooWin.view.viewmodel;

public class PlayerPageViewModel
{
    public UtLog Logger = new UtLog("PlayerPage");

    #region DI
    private readonly IAppConfigService _appConfigService;
    private readonly ISettingsService _settingsService;
    private readonly IClientService _clientService;
    private readonly IPageService _pageService;
    private readonly IDialogService _dialogService;
    #endregion

    #region Internal ViewModels

    public PlayerViewModel PlayerModel { get; }
    public PhotoViewerViewModel PhotoViewerModel { get; }
    public FilterViewModel FilterModel { get; }
    public SortViewModel SortViewModel { get; }
    public ItemPropertyViewModel ItemPropertyModel { get; } = new ItemPropertyViewModel();

    #endregion

    #region MediaItems

    // Host / Playlist / Item

    public ReactiveProperty<PeerHost?> CurrentHost { get; } = new();
    public ReactiveProperty<MediaItemList> ItemListSource { get; } = new(MediaItemList.Empty);
    public ReadOnlyReactiveProperty<List<IMediaItem>> ItemList { get; }
    public ReactiveProperty<IMediaItem?> SelectedItem { get; } = new();

    public Visibility IsPhotoViewerVisible(IMediaItem? item) {
        return (item?.IsPhoto == true) ? Visibility.Visible : Visibility.Collapsed;
    }
    public Visibility IsMediaPlayerVisible(IMediaItem? item) {
        return (item?.IsAV == true) ? Visibility.Visible : Visibility.Collapsed;
    }

    #endregion

    #region Slide Show

    public ReactiveProperty<bool> SlideShow { get; } = new(true);
    public ReactiveProperty<TimeSpan> SlideShowInterval { get; } = new(TimeSpan.FromSeconds(5));

    #endregion

    #region Filters

    public ReactiveProperty<bool> IsCategorySupported { get; } = new ReactiveProperty<bool>(false);
    public ReactiveProperty<bool> IsRatingSupported { get; } = new(false);
    public ReactiveProperty<bool> IsMarkSupported { get; } = new(false);

    public ReactiveProperty<bool> CanEditName { get; } = new(false);
    public ReactiveProperty<bool> CanEditLabel { get; } = new(false);
    public ReactiveProperty<bool> CanEditCategory { get; } = new(false);
    public ReactiveProperty<bool> CanEditRating { get; } = new(false);
    public ReactiveProperty<bool> CanEditMark { get; } = new(false);

    #endregion

    #region View Mode
    public ReactiveProperty<bool> IsPlayerOnly { get; } = new(false);
    public ReactiveProperty<bool> IsPropertiesVisible { get; } = new(false);

    public void TogglePlayerOnly() => IsPlayerOnly.Value = !IsPlayerOnly.Value;

    #endregion

    #region Commands

    public ReactiveCommand ReloadListCommand { get; } = new();
    public ReactiveCommand BackToEntryPageCommand { get; } = new();

    #endregion

    #region Construction / Private Event Handlers

    public PlayerPageViewModel(
        IAppConfigService appConfigService,
        ISettingsService settingsService, 
        IClientService clientService,
        IPageService pageService,
        IDialogService dialogService) {
        _appConfigService = appConfigService;
        _settingsService = settingsService;
        _clientService = clientService;
        _pageService = pageService;
        _dialogService = dialogService;

        PlayerModel = new PlayerViewModel(_clientService, _dialogService, _pageService, _settingsService, forEditor: false);
        PhotoViewerModel = new PhotoViewerViewModel(_clientService, _settingsService);
        FilterModel = new FilterViewModel(_clientService);
        SortViewModel = new SortViewModel(_clientService);

        IsPlayerOnly.Subscribe(v => {
            if (v) {
                // Player Only モードになったら、ItemPropertyも非表示にする。
                IsPropertiesVisible.Value = false;
            }
        });

        ItemList = ItemListSource
            .CombineLatest(FilterModel.Filter,SortViewModel.Sorter, (list, filter, sorter) => {
                var currentItem = SelectedItem.Value;
                var position = PlayerModel.PlayerPosition.Value;
                var newList = list.Items.Where(it => filter.Filter(_clientService, it)).SortBy(sorter).ToList();
                ReservePlayingItem(currentItem?.Id, position);
                return newList;
            }).ToReadOnlyReactiveProperty();

        BackToEntryPageCommand.Subscribe(() => {
            _pageService.GoToEntryPage();
        });

        SelectedItem.Subscribe(item => {
            PlayerModel.SetSource(item);
            PhotoViewerModel.SetSource(item);

            var changes = ItemPropertyModel.SetSource(item, _clientService);
            // ItemPropertyModel#SetSourceを呼んだ時に、以前のItemPropertyに変更があれば、IChanges が返ってくる。
            if (changes!=null) {
                OnUpdateItemProperties(changes);
            }
        });

        PlayerModel.NextItemCommand.ObserveOn(_pageService.UIContext).Subscribe((_) => {
            NextItem();
        });
        PlayerModel.PrevItemCommand.ObserveOn(_pageService.UIContext).Subscribe((_) => {
            PrevItem();
        });
        PhotoViewerModel.NextItemCommand.Subscribe(NextItem);
        PhotoViewerModel.PrevItemCommand.Subscribe(PrevItem);

        ReloadListCommand.Subscribe(async () => {
            Save();
            if (!await Prepare()) {
                BackToEntryPageCommand.Execute();
            }
        });
        SlideShow.Subscribe(ss => {
            PlayerModel.ContinuousPlayback.Value = ss;
            PhotoViewerModel.SlideShow.Value = ss;
        });
    }

    private void NextItem() {
        var item = SelectedItem.Value;
        if (item == null) return;
        var list = ItemList.Value;
        if (list == null || list.Count == 0) return;
        var index = list.FindIndex(it => it.Id == item.Id);
        if (index >= 0 && index + 1 < list.Count) {
            SelectedItem.Value = list[index + 1];
        }
    }
    private void PrevItem() {
        var item = SelectedItem.Value;
        if (item == null) return;
        var list = ItemList.Value;
        if (list == null || list.Count == 0) return;
        var index = list.FindIndex(it => it.Id == item.Id);
        if (index > 0) {
            SelectedItem.Value = list[index - 1];
        }
    }
    #endregion

    #region Methods

    /**
     * EntryPageからPlayerPageに遷移してきたとき（＝PeerHostが切り替わったとき）に、ビューモデルの状態を初期化する。
     * ReloadListCommand（プレイリストの再読み込み）にも利用する。
     * @return true: 正常に初期化できた。false: PeerHostが存在しない
     */

    public async Task<bool> Prepare() {
        PlayerModel.SetSource(null);
        ItemListSource.Value = MediaItemList.Empty;
        var currentHost = _settingsService.CurrentHost;
        if (currentHost == null) {
            return false;
        }
        CurrentHost.Value = currentHost;
        await _clientService.SetCurrentHostAsync(currentHost);
        PlayerModel.ChapterEditable.Value = _clientService.Capability.CanEditChapter;
        ItemListSource.Value = await _clientService.GetMediaItemListAsync();

        IsCategorySupported.Value = _clientService.CategorySupport.IsSupported;
        IsRatingSupported.Value = _clientService.RatingSupport.IsSupported;
        IsMarkSupported.Value = _clientService.MarkSupport.IsSupported;
        
        CanEditCategory.Value = _clientService.CategorySupport.AllowReadWrite;
        CanEditRating.Value = _clientService.RatingSupport.AllowReadWrite;
        CanEditMark.Value = _clientService.MarkSupport.AllowReadWrite;
        CanEditName.Value = _clientService.Capability.CanEditName;
        CanEditLabel.Value = _clientService.Capability.CanEditLabel;

        var additionals = _settingsService.Additionals[currentHost];
        FilterModel.ResetOnHostChanged(additionals);
        SortViewModel.ResetOnHostChanged(additionals);
        ItemPropertyModel.ResetOnHostChanged(_clientService);

        SlideShow.Value = additionals.SlideShow.Enabled;
        SlideShowInterval.Value = additionals.SlideShow.Interval;

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

    /**
     * 前回の再生アイテムを選択し、
     * MediaPlayer にソースがロードされたタイミング（=NaturalDurationChanged）での、再生位置復元を予約する。
     */
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

    /**
     * モデルの状態を保存する。
     */
    public void Save() {
        // 再生アイテム、再生位置を保存
        PlayerModel.SaveCurrentStatus();

        // Filter, Sorter, SlideShowの状態を保存
        SaveAdditionals();
        
        // SelectItemが変更されるタイミングで、直前のアイテムの ItemProperties が保存されるので、null をセットして、保存を強制する。
        SelectedItem.Value = null;
    }

    /**
     * Filter, Sorter, SlideShowの状態を保存する。
     */
    private void SaveAdditionals() {
        var currentHost = CurrentHost.Value;
        if (currentHost == null) return;
        var additionals = _settingsService.Additionals[currentHost];
        additionals.Filter = FilterModel.AsSettings();
        additionals.Sorter = SortViewModel.AsSettings();
        additionals.SlideShow.Enabled = SlideShow.Value;
        additionals.SlideShow.Interval = SlideShowInterval.Value;
        _settingsService.SaveAdditionals();
    }

    /**
     * ItemProperties (Category/Mark/Rating/Chapters) が変化したときに、サーバーに変更を反映する。
     */
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
    #endregion

    public async void OpenEditorWindow(IMediaItem item) {
        var host = _clientService.CurrentHost;
        var hash = HashBuilder.MD5.Append(host.Address).Build().AsHexString;
        var name = $"{hash}-{item.Id}.{item.Type}";
        var path = Path.Combine(_appConfigService.AppDataPath, name);
        var chapters = await _clientService.LoadChapterAsync(item);
        var result = await DownloadProgressDialog.ShowAsync(_dialogService, "Download", item.Name,
            (token, progress) => _clientService.DownloadItemAsync(item, path, token, progress));
        if (result) {
            App.OpenEditorWindow(CacheMediaItem.Create(path, item), chapters);
        }
    }
}
