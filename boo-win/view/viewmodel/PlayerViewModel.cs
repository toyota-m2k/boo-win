using BooWin.DI.contract;
using BooWin.model;
using io.github.toyota32k.BooLib.Utils;
using Microsoft.UI.Xaml;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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


    private ReactiveProperty<MediaItem?> mSourceMediaItem = new();
    public IReadOnlyReactiveProperty<MediaItem?> SourceMediaItem => mSourceMediaItem;
    public void SetSource(MediaItem? item) {
        PlayerState.Value = MediaPlayerState.NONE;
        MediaEnded.Value = false;
        MediaError.Value = false;
        mSourceMediaItem.Value = item;
    }

    private IClientService _clientService;
    private IDialogService _dialogService;
    public IPageService _pageService;

    public SynchronizationContext UIContext => _pageService.UIContext;

    public PlayerViewModel(
        IClientService clientService, 
        IDialogService dialogService, 
        IPageService pageService
        ) {
        _clientService = clientService;
        _dialogService = dialogService;
        _pageService = pageService;

        ChapterModel = new ChapterListViewModel(dialogService);
        IsPlaying = PlayerState.Select(s => s == MediaPlayerState.PLAYING).ToReadOnlyReactiveProperty();
        MediaReady = PlayerState.Select(s => s != MediaPlayerState.NONE).ToReadOnlyReactiveProperty();
        Seekable = PlayerState.CombineLatest(NaturalDuration, (state, duration) => (state != MediaPlayerState.NONE && duration > 0)).ToReadOnlyReactiveProperty();
        PositionString = PlayerPosition.CombineLatest(NaturalDuration, (pos, dur) => {
            var duration = TimeSpan.FromTicks(dur);
            var current = TimeSpan.FromTicks(pos);
            return (duration.Hours>0) ? $"{current.Hours:D2}:{current.Minutes:D2}:{current.Seconds:D2}" : $"{current.Minutes:D2}:{current.Seconds:D2}";
        }).ToReadOnlyReactiveProperty<string>();
        DurationString = NaturalDuration.Select(dur => {
            var duration = TimeSpan.FromTicks(dur);
            return (duration.Hours > 0) ? $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}" : $"{duration.Minutes:D2}:{duration.Seconds:D2}";
        }).ToReadOnlyReactiveProperty<string>();

        ChapterModel.NextChapterCommand.Subscribe(() => {
            var chapter = ChapterModel.NextChapter(PlayerPosition.Value);
            if (chapter!=null) {
                PlayerSeekToCommand.Execute(chapter.Position);
            }
        });
        ChapterModel.PrevChapterCommand.Subscribe(() => {
            var chapter = ChapterModel.PrevChapter(PlayerPosition.Value);
            if (chapter != null) {
                PlayerSeekToCommand.Execute(chapter.Position);
            }
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
        
    }


    public void OnNaturalDurationChanged(MediaPlaybackSession sender, object args) {
        var dur = sender.NaturalDuration;
        NaturalDuration.Value = dur.Ticks;
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
}
