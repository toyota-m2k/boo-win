using BooWin.DI.contract;
using BooWin.DI.implement;
using BooWin.model;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using Windows.Media.Playback;

namespace BooWin.view.viewmodel; 
public class PhotoViewerViewModel {
    private ReactiveProperty<IMediaItem?> mSourceMediaItem = new();
    private IMediaItem? mCurrentItem = null;
    public IReadOnlyReactiveProperty<IMediaItem?> SourceMediaItem => mSourceMediaItem;

    public ReactiveProperty<bool> SlideShow { get; } = new(true);
    public ReactiveCommand NextItemCommand { get; } = new();
    public ReactiveCommand PrevItemCommand { get; } = new();

    public TimeSpan SlideShowInterval { get; set; } = TimeSpan.FromSeconds(3);


    public void SetSource(IMediaItem? item) {
        mSourceMediaItem.Value = item?.IsPhoto == true ? item : null;
        if (mSourceMediaItem.Value!=null) {
            _settingsService.UpdateCurrentHostStatus(mSourceMediaItem.Value.Id, 0);
        }
        KickSlideShow();
    }

    private DispatcherTimer mTimer;
    public void KickSlideShow() {
        mTimer.Stop();
        if (SlideShow.Value && mSourceMediaItem.Value != null) {
            mCurrentItem = mSourceMediaItem.Value;
            mTimer.Start();
        }
    }

    public ISettingsService _settingsService;
    public IClientService _clientService;

    public PhotoViewerViewModel(IClientService clientService, ISettingsService settingsService) {
        _clientService = clientService;
        _settingsService = settingsService;
        mTimer = new DispatcherTimer { Interval = SlideShowInterval };
        mTimer.Tick += (s, e) => {
            mTimer.Stop();
            if (SlideShow.Value && mCurrentItem!=null && mCurrentItem == mSourceMediaItem.Value) {
                mCurrentItem = null;
                NextItemCommand.Execute();
            }
        };
        SlideShow.Subscribe(ss => {
            if (ss) {
                KickSlideShow();
            }
            else {
                mTimer.Stop();
            }
        });

        // Control Panel Visibility
        IsControlPanelVisible = Observable.CombineLatest(
            ControlPanelPinned, ControlPanelActivatedByMouse, (pinned, activated) => pinned || activated
        ).ToReadOnlyReactiveProperty();

        mControlPanelTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        mControlPanelTimer.Tick += OnControlPanelTimerTick;

    }

    public ImageSource? GetItemUrl(IMediaItem? item) {
        if (item == null) return null;
        var url = _clientService.GetItemUrl(item);
        if (string.IsNullOrEmpty(url)) return null;
        return new BitmapImage(new Uri(url));
    }

    #region Show/Hide Control Panel

    // ピン止め有効・無効
    public ReactiveProperty<bool> ControlPanelPinned { get; } = new(false);

    // マウス操作によるアクティブ化
    //  - ControlPanel上にMouseが乗っている間は true
    //  - MouseMoved から３秒間 true
    public ReactiveProperty<bool> ControlPanelActivatedByMouse { get; } = new(false);
    public ReadOnlyReactiveProperty<bool> IsControlPanelVisible { get; }
    private DispatcherTimer mControlPanelTimer;
    private bool mMouseInControlPanel = false;

    public void OnControlPanelMouseEnter() {
        mControlPanelTimer.Stop();
        mMouseInControlPanel = true;
        ControlPanelActivatedByMouse.Value = true;
    }
    public void ActivateControlPanel() {
        mControlPanelTimer.Stop();
        ControlPanelActivatedByMouse.Value = true;
        if (!mMouseInControlPanel) {
            mControlPanelTimer.Start();
        }
    }
    public void OnControlPanelMouseLeave() {
        mMouseInControlPanel = false;
        mControlPanelTimer.Start();
    }
    private void OnControlPanelTimerTick(object? sender, object e) {
        mControlPanelTimer.Stop();
        ControlPanelActivatedByMouse.Value = false;
    }
    #endregion

}
