using BooWin.DI.contract;
using BooWin.model;
using BooWin.view.viewmodel;
using CommunityToolkit.WinUI.Animations;
using io.github.toyota32k.BooLib.Utils;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Reactive.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using Windows.Media.Playback;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BooWin.view.parts;

public sealed partial class PlayerView : UserControl {
    #region ViewModel DependencyProperty
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(PlayerViewModel),
            typeof(PlayerView),
            new PropertyMetadata(null, OnViewModelChanged));

    public PlayerViewModel? ViewModel {
        get => (PlayerViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }
    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is PlayerView view) {
            view.Rebind();
        }
    }
    #endregion

    private readonly IClientService _clientService = App.GetService<IClientService>();

    public PlayerView() {
        InitializeComponent();
    }

    private async void LoadChapter(MediaItem item) {
        var viewModel = ViewModel;
        if (viewModel != null) {
            viewModel.ChapterModel.Clear(null);
            var chapters = await _clientService.LoadChapterAsync(item);
            if (viewModel.SourceMediaItem.Value == item) {
                viewModel.ChapterModel.Clear(chapters);
            }
        }
    }

    private MediaPlayer? mInternalPlayer = null;
    private MediaPlaybackSession? mSession = null;
    private DisposableCollection mBinding = new DisposableCollection();
    private DispatcherTimer? mTrackingTimer = null;

    private void OnLoaded(object sender, RoutedEventArgs e) {
        if (mInternalPlayer == null) {
            mInternalPlayer = new MediaPlayer();
            mSession = mInternalPlayer.PlaybackSession;
            mPlayerElement.SetMediaPlayer(mInternalPlayer);
        }
        Rebind();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) {
        ViewModel?.SaveCurrentStatus();

        Unbind();
        mPlayerElement.SetMediaPlayer(null);
        mInternalPlayer?.Dispose();
        mInternalPlayer = null;
        mSession = null;
    }

    /**
     * MediaPlayerとViewModelの接続を解除する。
     */
    private void Unbind() {
        mTrackingTimer?.Stop();
        mTrackingTimer = null;
        mBinding.Dispose();                    // 全購読・イベントハンドラを解除
        mBinding = new DisposableCollection();
    }

    /**
     * MediaPlayer と ViewModel を接続する。
     */
    private void Rebind() {
        Unbind();
        var vm = ViewModel;
        var player = mInternalPlayer;
        var session = mSession;
        if (vm == null || player == null || session == null) return;

        mTrackingTimer = new DispatcherTimer() {
            Interval = TimeSpan.FromMilliseconds(100),
        };
        mTrackingTimer.Tick += (s, e) => {
            UpdatePlayingPosition();
        };

        var uiContext = SynchronizationContext.Current;
        mBinding
            .Add(GenericDisposable.Create(
            () => {
                session.PlaybackStateChanged += vm.OnPlaybackStateChanged;
                session.NaturalDurationChanged += vm.OnNaturalDurationChanged;
                player.MediaFailed += vm.OnMediaFailed;
                player.MediaEnded += vm.OnMediaEnded;
            },
            () => {
                session.PlaybackStateChanged -= vm.OnPlaybackStateChanged;
                session.NaturalDurationChanged -= vm.OnNaturalDurationChanged;
                player.MediaFailed -= vm.OnMediaFailed;
                player.MediaEnded -= vm.OnMediaEnded;
            }),

            vm.SourceMediaItem.Subscribe(item => {
                if (item != null) {
                    player.AutoPlay = true;
                    player.SetUriSource(new Uri(_clientService.GetItemUrl(item)));
                    LoadChapter(item);
                }
                else {
                    player.Pause();
                    player.Source = null;
                }
            }),
        vm.IsPlaying.ObserveOn(vm.UIContext).Subscribe(isPlaying => {
            if (isPlaying) {
                mTrackingTimer?.Start();
            }
            else {
                mTrackingTimer?.Stop();
            }
        }),
        vm.PlayCommand.Subscribe(_ => {
            player.Play();
        }),
        vm.PauseCommand.Subscribe(_ => {
            player.Pause();
        }),
        vm.PlayerSeekToCommand.Subscribe(pos => {
            if (mSession != null) {
                pos = Math.Max(0, Math.Min(pos, (long)Math.Round(mSession.NaturalDuration.TotalMilliseconds)));
                mSession.Position = TimeSpan.FromMilliseconds(pos);
                vm.PlayerPosition.Value = pos;
                var chapter = vm.ChapterModel.GetChapterAt(pos);
                vm.ChapterModel.CurrentChapter.Value = chapter;
            }
        }),
        vm.ChapterModel.ChapterList.Subscribe((_) => {
            if (mSession != null) {
                var pos = (long)Math.Round(mSession.Position.TotalMilliseconds);
                var chapter = vm.ChapterModel.GetChapterAt(pos);
                vm.ChapterModel.CurrentChapter.Value = null;
                vm.ChapterModel.CurrentChapter.Value = chapter;
            }
        }) );

    }

    private void UpdatePlayingPosition() {
        var viewModel = ViewModel;
        if (viewModel != null && mSession != null && mInternalPlayer != null) {
            var pos = (long)Math.Round(mSession.Position.TotalMilliseconds);
            var chapter = viewModel.ChapterModel.GetChapterAt(pos);
            if (chapter!=null && !chapter.Enabled) {
                chapter = viewModel.ChapterModel.NextEnabledChapter(pos);
                if (chapter != null) {
                    pos = chapter.Position;
                    mSession.Position = TimeSpan.FromMilliseconds(pos);
                }
                else {
                    mInternalPlayer.Pause();
                }
            }
            viewModel.PlayerPosition.Value = pos;
            viewModel.ChapterModel.CurrentChapter.Value = chapter;
        }
    }

    private void OnPlayerTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e) {
        var viewModel = ViewModel;
        if (viewModel == null) return;
        if (viewModel.IsPlaying.Value) {
            viewModel.PauseCommand.Execute();
        }
        else {
            viewModel.PlayCommand.Execute();
        }
    }

    #region Show/Hide Control Panel

    private void OnControlPanelMouseEnter(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {
        ViewModel?.OnControlPanelMouseEnter();
        e.Handled = true;
    }

    private void OnControlPanelMouseLeave(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {
        ViewModel?.OnControlPanelMouseLeave();
        e.Handled = true;
    }

    private void OnPointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {
        ViewModel?.ActivateControlPanel();
        e.Handled = true;
    }

    private void OnControlPanelMouseMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {
        e.Handled = true;
    }

    #endregion
}



