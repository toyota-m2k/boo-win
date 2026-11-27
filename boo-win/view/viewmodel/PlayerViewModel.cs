using BooWin.DI.contract;
using BooWin.model;
using io.github.toyota32k.BooLib.Utils;
using Microsoft.UI.Xaml;
using Reactive.Bindings;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Management;
using Windows.Media.Playback;

namespace BooWin.view.viewmodel;

public class PlayerViewModel {
    public UtLog Logger = new UtLog("Player");

    #region DI

    private IItemService _itemService;
    private IDialogService _dialogService;
    public IPageService _pageService;
    public ISettingsService _settingsService;

    #endregion

    public SynchronizationContext UIContext => _pageService.UIContext;



    #region Chapters

    public ReactiveProperty<bool> ChapterEditable { get; set; } = new(true); // player: false / editor: true
    public ChapterListViewModel ChapterModel { get; }

    private void SaveChapter(IMediaItem? item) {
        if (item == null) return;
        if (!ChapterModel.IsDirty) return;
        var chapters = ChapterModel.ChapterList.Value.ToList();
        Task.Run(async () => {
            await _itemService.SaveChapterAsync(item, chapters);
        });
    }

    public async void LoadChapter(IMediaItem? item) {
        ChapterModel.Clear(null);
        if (item != null) {
            var chapters = await _itemService.LoadChapterAsync(item);
            if (SourceMediaItem.Value == item) {
                ChapterModel.Clear(chapters);
            }
        }
    }

    public Uri? GetItemUrl(IMediaItem item) {
        return _itemService.GetItemUrl(item);
    }

    #endregion

    #region Playing Status / Seek

    public ReactiveProperty<long> PlayerPosition { get; } = new(0);         // MediaPlayerのPosition
    //public ReactiveProperty<double> SliderPosition { get; } = new(0.0);     // PlayerSliderのPosition
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

    #endregion

    #region Modes

    public bool ForEditor { get; }

    public ReactiveProperty<bool> ContinuousPlayback { get; } = new(true);      // 連続再生 (<-- PlayerPageViewModel.SlideShow)

    #endregion

    #region Commands

    public ReactiveCommand PlayCommand { get; } = new();
    public ReactiveCommand PauseCommand { get; } = new();
    public ReactiveCommand NextItemCommand { get; } = new();
    public ReactiveCommand PrevItemCommand { get; } = new();
    public ReactiveCommand<long> PlayerSeekToCommand { get; } = new();
    public ReactiveCommand<long> RelativePlayerSeekCommand { get; } = new();

    #endregion

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

    #region Media Source

    private ReactiveProperty<IMediaItem?> mSourceMediaItem = new();
    public IReadOnlyReactiveProperty<IMediaItem?> SourceMediaItem => mSourceMediaItem;
    public async void SetSource(IMediaItem? item) {
        SaveChapter(mSourceMediaItem.Value);

        PlayerPosition.Value = 0;
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

    public (string? Id, long Position) ReservedPlayingItemPosition { get; set; } = (null, 0);   // 再生位置復元予約用

    public bool SaveCurrentStatus() {
        var item = SourceMediaItem.Value;
        if (item == null) return false;
        _settingsService.UpdateCurrentHostStatus(item.Id, PlayerPosition.Value, save: true);
        return true;
    }

    #endregion

    #region Construction / Event Handling

    public PlayerViewModel(
        IItemService chapterSource, 
        IDialogService dialogService, 
        IPageService pageService,
        ISettingsService settingsService,
        bool forEditor
        ) {
        ForEditor = forEditor;
        _itemService = chapterSource;
        _dialogService = dialogService;
        _pageService = pageService;
        _settingsService = settingsService;

        ChapterModel = new ChapterListViewModel(dialogService);
        ControlPanelPinned.Value = ForEditor;    // エディタでは常にピン止め表示
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
            PlayerState, ControlPanelPinned, ControlPanelActivatedByMouse, (state, pinned, activated) => ForEditor || state!=MediaPlayerState.NONE && (state== MediaPlayerState.PAUSED || pinned || activated)
        ).ToReadOnlyReactiveProperty();

        mControlPanelTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        mControlPanelTimer.Tick += OnControlPanelTimerTick;
    }


    public void OnNaturalDurationChanged(MediaPlaybackSession sender, object args) {
        var dur = sender.NaturalDuration;
        Logger.Debug($"NaturalDuration={dur}");
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
        Logger.Debug($"PlaybackState={state}");
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
        Logger.Error("On Media Ended");
        MediaEnded.Value = true;
        if (ContinuousPlayback.Value) {
            NextItemCommand.Execute();
        }
    }
    #endregion

    #region for Key Command

    public void TogglePlay() {
        if (IsPlaying.Value) {
            PauseCommand.Execute();
        }
        else {
            PlayCommand.Execute();
        }
    }
    public void SeekRelative(long delta) {
        RelativePlayerSeekCommand.Execute(delta);
    }

    public ReactiveCommand<double> PlaySpeedCommand { get; } = new();



    private DispatcherTimer? mHighSpeedTimer = null;
    public void StartSuperHighSpeedPlay() {
        if (mHighSpeedTimer != null) return;
        mHighSpeedTimer = new DispatcherTimer() {
            Interval = TimeSpan.FromMilliseconds(100),
        };
        mHighSpeedTimer.Tick += (s, e) => {
                RelativePlayerSeekCommand.Execute(1000);   // 1秒進める
        };
        mHighSpeedTimer.Start();
    }

    public void StopSuperHighSpeedPlay() {
        if (mHighSpeedTimer == null) return;
        mHighSpeedTimer.Stop();
        mHighSpeedTimer = null;
    }

    public void ToggleSuperHighSpeedMode() {
        if (mHighSpeedTimer==null) {
            StartSuperHighSpeedPlay();
        } else {
            StopSuperHighSpeedPlay();
        }
    }

    #endregion

    #region // Cutting Range by Chapter

    private long mCuttingStartPosition = -1L;
    private IDisposable? mCuttingObserver = null;

    public void BeginCuttingRange() {
        if (mCuttingObserver!=null) return;
        mCuttingStartPosition = PlayerPosition.Value;
        ChapterModel.CuttingRange.Value = TimeRange.Empty;
        mCuttingObserver = PlayerPosition.Subscribe(pos => {
            if (mCuttingStartPosition >= 0) {
                var start = Math.Min(mCuttingStartPosition, pos);
                var end = Math.Max(mCuttingStartPosition, pos);
                ChapterModel.CuttingRange.Value = new TimeRange(start, end);
            }
        });
    }

    private void DisposeCuttingObserver() {
        ChapterModel.CuttingRange.Value = TimeRange.Empty;
        mCuttingStartPosition = -1L;
        mCuttingObserver?.Dispose();
        mCuttingObserver = null;
    }

    public void CancelCuttingRange() {
        var pos = mCuttingStartPosition;
        DisposeCuttingObserver();
        PlayerSeekToCommand.Execute(pos);
    }

    public void CommitCuttingRange() {
        var range = ChapterModel.CuttingRange.Value;
        if (range.IsEmpty) return;
        ChapterModel.CutRange(range.Start, range.End, NaturalDuration.Value);
        DisposeCuttingObserver();
    }

    public void CutToStart() {
        ChapterModel.CutToStart(PlayerPosition.Value);
    }
    public void CutToEnd() {
        ChapterModel.CutToEnd(PlayerPosition.Value);
    }

    #endregion
}
