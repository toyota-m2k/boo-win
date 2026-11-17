using BooWin.DI.contract;
using BooWin.model;
using FFMpegCore;
using io.github.toyota32k.BooLib.Utils;
using Microsoft.UI.Xaml;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Windows.Media.Playback;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BooWin.view.viewmodel;

public class PlayerViewModel {
    public UtLog Logger = new UtLog("Player");
    public ReactiveProperty<bool> Editable { get; set; } = new(true); // player: false / editor: true

    public ChapterListViewModel ChapterModel { get; }

    public ReactiveProperty<long> PlayerPosition { get; } = new(0);
    public ReactiveProperty<double> SliderPosition { get; } = new(0.0);
    public ReadOnlyReactiveProperty<string> PositionString { get; }
    public ReadOnlyReactiveProperty<string> DurationString { get; }


    /**
     * VideoPlayerの状態
     */
    private enum MediaPlayerState {
        NONE,           // unloaded
        PAUSED,         // loaded and not-playing
        PLAYING,        // playing and loaded, of cource.
    }
    private ReactiveProperty<MediaPlayerState> PlayerState { get; } = new (MediaPlayerState.NONE);
    public ReactiveProperty<long> NaturalDuration { get; } = new(0);

    public ReadOnlyReactiveProperty<bool> IsPlaying { get; }
    public ReadOnlyReactiveProperty<bool> MediaReady { get; }
    public ReadOnlyReactiveProperty<bool> Seekable { get; }
    public ReactiveProperty<bool> MediaEnded { get; } = new(false);
    public ReactiveProperty<bool> MediaError { get; } = new(false);
    public ReactiveProperty<bool> ContinuousPlayback { get; } = new(true);

    public ReactiveCommand PlayCommand { get; } = new();
    public ReactiveCommand PauseCommand { get; } = new();
    public ReactiveCommand NextItemCommand { get; } = new();
    public ReactiveCommand PrevItemCommand { get; } = new();
    public ReactiveCommand<long> PlayerSeekToCommand { get; } = new();

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

    public (string? Id, long Position) ReservedPlayingItemPosition { get; set; } = (null,0);

    private ReactiveProperty<MediaItem?> mSourceMediaItem = new();
    public IReadOnlyReactiveProperty<MediaItem?> SourceMediaItem => mSourceMediaItem;
    public async void SetSource(MediaItem? item) {
        PlayerState.Value = MediaPlayerState.NONE;
        MediaEnded.Value = false;
        MediaError.Value = false;
        mSourceMediaItem.Value = item?.IsAV == true ? item : null;
        //if (item!=null) {
        //    var chapters = await _clientService.LoadChapterAsync(item);
        //    if (item ==mSourceMediaItem.Value) {
        //        ChapterModel.Clear(chapters);
        //    }
        //}
    }

    private IClientService _clientService;
    private IDialogService _dialogService;
    public IPageService _pageService;
    public ISettingsService _settingsService;

    public SynchronizationContext UIContext => _pageService.UIContext;

    public PlayerViewModel(
        IClientService clientService, 
        IDialogService dialogService, 
        IPageService pageService,
        ISettingsService settingsService
        ) {
        _clientService = clientService;
        _dialogService = dialogService;
        _pageService = pageService;
        _settingsService = settingsService;

        ChapterModel = new ChapterListViewModel(dialogService);
        IsPlaying = PlayerState.Select(s => s == MediaPlayerState.PLAYING).ToReadOnlyReactiveProperty();
        MediaReady = PlayerState.Select(s => s != MediaPlayerState.NONE).ToReadOnlyReactiveProperty();
        Seekable = PlayerState.CombineLatest(NaturalDuration, (state, duration) => (state != MediaPlayerState.NONE && duration > 0)).ToReadOnlyReactiveProperty();
        PositionString = PlayerPosition.CombineLatest(NaturalDuration, (pos, dur) => {
            var duration = TimeSpan.FromMilliseconds(dur);
                var current = TimeSpan.FromMilliseconds(pos);
            return (duration.Hours>0) ? $"{current.Hours:D2}:{current.Minutes:D2}:{current.Seconds:D2}" : $"{current.Minutes:D2}:{current.Seconds:D2}";
        }).ToReadOnlyReactiveProperty<string>();
        DurationString = NaturalDuration.Select(dur => {
            var duration = TimeSpan.FromMilliseconds(dur);
            return (duration.Hours > 0) ? $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}" : $"{duration.Minutes:D2}:{duration.Seconds:D2}";
        }).ToReadOnlyReactiveProperty<string>();

        ChapterModel.NextChapterCommand.Subscribe(() => {
            long position = 0;
            if (IsPlaying.Value) {
                var chapter = ChapterModel.NextEnabledChapter(PlayerPosition.Value);
                position = chapter?.Position ?? ChapterModel.LastEnabledPosition(NaturalDuration.Value);
            } else {
                var chapter = ChapterModel.NextChapter(PlayerPosition.Value);
                position = chapter?.Position ?? NaturalDuration.Value;
            }
            position = Math.Clamp(position, 0, NaturalDuration.Value);
            PlayerSeekToCommand.Execute(position);
        });
        ChapterModel.PrevChapterCommand.Subscribe(() => {
            long position = 0;
            if (IsPlaying.Value) {
                var chapter = ChapterModel.PrevEnabledChapter(PlayerPosition.Value, true);
                position = chapter?.Position ?? ChapterModel.FirstEnabledPosition();
            }
            else {
                var chapter = ChapterModel.PrevChapter(PlayerPosition.Value);
                position = chapter?.Position ?? 0;
            }
            position = Math.Clamp(position, 0, NaturalDuration.Value);
            PlayerSeekToCommand.Execute(position);
        });

        ChapterModel.AddChapterCommand.Subscribe(() => {
            var pos = PlayerPosition.Value;
            ChapterModel.AddChapter(pos);
        });
        ChapterModel.RemoveChapterCommand.Subscribe(() => {
            var pos = PlayerPosition.Value;
            ChapterModel.RemoveChapter(pos);
        });
        ChapterModel.ToggleSkipChapterCommand.Subscribe(() => {
            var pos = PlayerPosition.Value;
            ChapterModel.ToggleSkipChapter(pos);
        });
        ChapterModel.SetChapterLabelCommand.Subscribe(() => {
            var pos = PlayerPosition.Value;
            ChapterModel.RenameChapter(pos);
        });

        IsControlPanelVisible = Observable.CombineLatest(
            PlayerState, ControlPanelPinned, ControlPanelActivatedByMouse, (state, pinned, activated) => state!=MediaPlayerState.NONE && (state== MediaPlayerState.PAUSED || pinned || activated)
        ).ToReadOnlyReactiveProperty();

        mControlPanelTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        mControlPanelTimer.Tick += OnControlPanelTimerTick;
    }


    public void OnNaturalDurationChanged(MediaPlaybackSession sender, object args) {
        var dur = sender.NaturalDuration;
        NaturalDuration.Value = (long)Math.Round(dur.TotalMilliseconds);
        _pageService.DispatcherQueue?.TryEnqueue(() => {
            if (ReservedPlayingItemPosition.Id == SourceMediaItem.Value?.Id && ReservedPlayingItemPosition.Position > 0) {
                PlayerSeekToCommand.Execute(ReservedPlayingItemPosition.Position);
            }
            ReservedPlayingItemPosition = (null, 0);
        });
    }

    /**
     * MediaPlayerの状態監視
     */
    public void OnPlaybackStateChanged(MediaPlaybackSession sender, object args) {
        var state = sender.PlaybackState;
        switch (state) {
            case MediaPlaybackState.Buffering:
            default:
                return;
            case MediaPlaybackState.None:
            case MediaPlaybackState.Opening:
                PlayerState.Value = MediaPlayerState.NONE;
                return;
            case MediaPlaybackState.Paused:
                PlayerState.Value = MediaPlayerState.PAUSED;
                return;
            case MediaPlaybackState.Playing:
                PlayerState.Value = MediaPlayerState.PLAYING;
                return;
        }
    }

    public void OnMediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args) {
        MediaError.Value = true;
        Logger.Error("Load Error");
        if (!string.IsNullOrWhiteSpace(args.ErrorMessage)) {
            Logger.Debug(args.ErrorMessage);
        }
        if (null != args.ExtendedErrorCode) {
            Logger.Error(args.ExtendedErrorCode);
        }
    }

    public void OnMediaEnded(MediaPlayer sender, object args) {
        MediaEnded.Value = true;
        if (ContinuousPlayback.Value) {
            NextItemCommand.Execute();
        }
    }

    public void SaveCurrentStatus() {
        var item = SourceMediaItem.Value;
        if (item == null) return;
        _settingsService.UpdateCurrentHostStatus(item.Id, PlayerPosition.Value, save: true);
    }
}
