using BooWin.DI.contract;
using BooWin.DI.implement;
using BooWin.model;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Text;

namespace BooWin.view.viewmodel; 
public class PhotoViewerViewModel {
    private ReactiveProperty<MediaItem?> mSourceMediaItem = new();
    private MediaItem? mCurrentItem = null;
    public IReadOnlyReactiveProperty<MediaItem?> SourceMediaItem => mSourceMediaItem;

    public ReactiveProperty<bool> SlideShow { get; } = new(true);
    public ReactiveCommand NextItemCommand { get; } = new();
    public ReactiveCommand PrevItemCommand { get; } = new();

    public TimeSpan SlideShowInterval { get; set; } = TimeSpan.FromSeconds(3);


    public void SetSource(MediaItem? item) {
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
    }

    public ImageSource? GetItemUrl(MediaItem? item) {
        if (item == null) return null;
        var url = _clientService.GetItemUrl(item);
        if (string.IsNullOrEmpty(url)) return null;
        return new BitmapImage(new Uri(url));
    }


}
