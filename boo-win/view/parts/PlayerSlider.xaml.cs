using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using BooWin.view.viewmodel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BooWin.view.parts;

public sealed partial class PlayerSlider : UserControl
{
    #region ViewModel DependencyProperty
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(PlayerViewModel),
            typeof(PlayerSlider),
            new PropertyMetadata(null, OnViewModelChanged));

    public PlayerViewModel? ViewModel {
        get => (PlayerViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }
    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        // 必要ならイベント購読の付け替えなどをここで行う
        //if (e.NewValue is PlayerViewModel viewModel && d is PlayerView playerView) {
        //    playerView.OnViewModelChanged(viewModel);
        //}
    }
    #endregion

    private ChapterListViewModel ChapterModel => ViewModel?.ChapterModel ?? throw new InvalidOperationException("ViewModel is not set.");


    public PlayerSlider() {
        this.InitializeComponent();
    }

    #region 内部 Slider へのブリッジ（プロパティ／イベント）

    /**
     * 内部 Slider の Value をブリッジする。
     * mChapterSlider.Value = v のように、Slider と同じ感覚で読み書きできる。
     */
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(PlayerSlider),
            new PropertyMetadata(0.0, OnValuePropertyChanged));

    public double Value {
        get { return (double)GetValue(ValueProperty); }
        set { SetValue(ValueProperty, value); }
    }

    private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        var self = (PlayerSlider)d;
        if (self.mSlider.Value != (double)e.NewValue) {
            self.mSlider.Value = (double)e.NewValue;
        }
    }

    /**
     * 内部 Slider の Minimum をブリッジする。
     */
    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(PlayerSlider),
            new PropertyMetadata(0.0, (d, e) => ((PlayerSlider)d).mSlider.Minimum = (double)e.NewValue));

    public double Minimum {
        get { return (double)GetValue(MinimumProperty); }
        set { SetValue(MinimumProperty, value); }
    }

    /**
     * 内部 Slider の Maximum をブリッジする。
     * チャプター系ビューの TotalRange（総時間）としても利用する。
     */
    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(PlayerSlider),
            new PropertyMetadata(100.0, OnMaximumPropertyChanged));

    public double Maximum {
        get { return (double)GetValue(MaximumProperty); }
        set { SetValue(MaximumProperty, value); }
    }

    private static void OnMaximumPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        var self = (PlayerSlider)d;
        self.mSlider.Maximum = (double)e.NewValue;
        //self.ApplyTotalRange();
    }

    /**
     * 内部 Slider の SmallChange をブリッジする。
     */
    public static readonly DependencyProperty SmallChangeProperty =
        DependencyProperty.Register(nameof(SmallChange), typeof(double), typeof(PlayerSlider),
            new PropertyMetadata(1.0, (d, e) => ((PlayerSlider)d).mSlider.SmallChange = (double)e.NewValue));

    public double SmallChange {
        get { return (double)GetValue(SmallChangeProperty); }
        set { SetValue(SmallChangeProperty, value); }
    }

    /**
     * 内部 Slider の LargeChange をブリッジする。
     */
    public static readonly DependencyProperty LargeChangeProperty =
        DependencyProperty.Register(nameof(LargeChange), typeof(double), typeof(PlayerSlider),
            new PropertyMetadata(1.0, (d, e) => ((PlayerSlider)d).mSlider.LargeChange = (double)e.NewValue));

    public double LargeChange {
        get { return (double)GetValue(LargeChangeProperty); }
        set { SetValue(LargeChangeProperty, value); }
    }

    /**
     * 内部 Slider の ValueChanged を中継する。
     * mLegacySlider.ValueChanged と同じ感覚でハンドラを登録できる。
     */
    public event RangeBaseValueChangedEventHandler? ValueChanged;

    #endregion

    private void OnSliderChanged(object sender, RangeBaseValueChangedEventArgs e) {
        // 依存プロパティ Value を内部 Slider に同期する
        if (Value != mSlider.Value) {
            Value = mSlider.Value;
        }
        // 外部へ ValueChanged を中継する
        ValueChanged?.Invoke(this, e);
    }

    private void OnSliderWheelChanged(object sender, PointerRoutedEventArgs e) {
        PointerWheelChanged?.Invoke(this, e);
    }

    /**
     * 内部 Slider のホイール操作を中継する。
     */
    public new event PointerEventHandler? PointerWheelChanged;

    /**
     * 内部 Slider のタップを中継する。
     */
    //public new event TappedEventHandler Tapped;

    public event DragStartedEventHandler? DragStarted;
    private void OnSliderDragStarted(object sender, DragStartedEventArgs e) {
        DragStarted?.Invoke(this, e);
    }

    public event DragCompletedEventHandler? DragCompleted;
    private void OnSliderDragCompleted(object sender, DragCompletedEventArgs e) {
        DragCompleted?.Invoke(this, e);
    }
}
