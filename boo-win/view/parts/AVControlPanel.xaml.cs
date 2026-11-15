using BooWin.model;
using BooWin.view.viewmodel;
using io.github.toyota32k.BooLib.Utils;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
//using Windows.Foundation;
//using Windows.Foundation.Collections;
//using Windows.UI.Popups;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BooWin.view.parts;

public sealed partial class AVControlPanel : UserControl
{
    #region ViewModel DependencyProperty
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(PlayerViewModel),
            typeof(AVControlPanel),
            new PropertyMetadata(null, OnViewModelChanged));

    public PlayerViewModel? ViewModel {
        get => (PlayerViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }
    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is AVControlPanel view) {
            view.Bind();
        }

    }

    private void OnViewModelChanged(PlayerViewModel vm) {

    }

    //private PlayerViewModel viewModel => ViewModel ?? throw new InvalidOperationException("ViewModel is not set.");

    #endregion

    public AVControlPanel()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) {
        Bind();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) {
        Unbind();
    }

    private DisposableCollection mBinding = new DisposableCollection();
    private void Bind() {
        Unbind();
        var vm = ViewModel;
        if (vm!=null) {
            mBinding.Add(
                vm.PlayerPosition.Subscribe(pos => {
                    mUpdatingSliderFromPlayer = true;
                    try {
                        mChapterSlider.Value = pos;
                    } finally {
                        mUpdatingSliderFromPlayer = false;
                    }
                })
            );
        }
    }
    private void Unbind() {
        mBinding.Dispose();
        mBinding = new DisposableCollection();
    }


    private async void CV_ChapterContextMenu(Chapter chapter, FrameworkElement sender, Rect pinRect) {
        var menu = new MenuFlyout();
        var item = new MenuFlyoutItem { Text = "Remove" };
        item.Click += (s, e) => {
            ViewModel?.ChapterModel.RemoveChapter(chapter.Position);
        };
        menu.Items.Add(item);
        item = new MenuFlyoutItem { Text = "Chapter Name" };
        item.Click += (s, e) => {
            ViewModel?.ChapterModel.RenameChapter(chapter);
        };
        menu.Items.Add(item);
        item = new MenuFlyoutItem { Text = chapter.Skip ? "Enable" : "Disable" };
        item.Click += (s, e) => {
            ViewModel?.ChapterModel.ToggleSkipChapter(chapter);
        };
        menu.Items.Add(item);

        menu.ShowAt(sender, new FlyoutShowOptions {
            Position = new Point((pinRect.X+pinRect.Width/2), pinRect.Y),   // targetElement 基準の座標
            Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft,
        });
    }

    private void CV_ChapterSelected(Chapter chapter) {
        if (ViewModel?.Seekable.Value == true) { 
            ViewModel.PlayerSeekToCommand.Execute(chapter.Position);
        }
    }

    // Stopper
    // コントロールパネル上のクリックが、下のPlayerViewに伝播するのを防ぐ
    private void OnContainerTapped(object sender, TappedRoutedEventArgs e) {
        e.Handled = true;
    }

    private bool mUpdatingSliderFromPlayer = false;

    private void OnSliderChanged(object sender, RangeBaseValueChangedEventArgs e) {
        var viewModel = ViewModel;
        if (viewModel==null || mUpdatingSliderFromPlayer) {
            return;
        }

        //var slider = sender as Slider;

        if (viewModel.MediaReady.Value) {
            var seekPos = (long)Math.Round(mChapterSlider.Value);
            if (viewModel.PlayerPosition.Value != seekPos) {
                viewModel.PlayerSeekToCommand.Execute(seekPos);
            }
        }
    }

    private void OnSliderWheelChanged(object sender, PointerRoutedEventArgs e) {
        var viewModel = ViewModel;
        if (viewModel == null) return;
        var elem = sender as UIElement;
        if (null != elem) {
            int delta = e.GetCurrentPoint(elem).Properties.MouseWheelDelta;
            if (0 != delta) {
                //Debug.WriteLine("Pointer: WheelChanged", delta);
                delta = (delta > 0) ? 1 : -1;
                double v = mChapterSlider.Value + delta * 1000; // 1秒単位でシーク
                viewModel.PlayerSeekToCommand.Execute((long)Math.Round(v));
            }
        }
        e.Handled = true;
    }

    private void OnSliderDragStarted(object sender, DragStartedEventArgs e) {
        pauseOnStartTracking();
    }

    private void OnSliderDragCompleted(object sender, DragCompletedEventArgs e) {
        restartOnEndTracking();
    }

    private bool mPauseTemporary = false;
    /**
     * スライダー操作中に動画の再生を一時的に停止する
     */
    private void pauseOnStartTracking() {
        var viewModel = ViewModel;
        if (viewModel == null) return;
        mPauseTemporary = false;
        if (viewModel.MediaReady.Value) {
            mPauseTemporary = viewModel.IsPlaying.Value;
            if (mPauseTemporary) {
                viewModel.PauseCommand.Execute();
            }
        }
    }

    /**
     * スライダー操作が終わったときに（必要なら）動画の再生を再開する。
     */
    private void restartOnEndTracking() {
        var viewModel = ViewModel;
        if (viewModel == null) return;
        if (viewModel.MediaReady.Value) {
            if (mPauseTemporary) {
                viewModel.PlayCommand.Execute();
            }
        }
        mPauseTemporary = false;
    }
}
