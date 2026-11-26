using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Colors = Microsoft.UI.Colors;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
using BooWin.view.viewmodel;
using Microsoft.UI.Dispatching;
using System.Collections.Generic;
using BooWin.model;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BooWin.view.parts;

/**
 * 有効/無効の「区間（TimeRange）」を矩形で表示するビュー。
 *
 * WvvChapterView がチャプター（区切り）位置をアイコンで示すのに対し、
 * こちらは ChapterListViewModel の EnabledRanges / DisabledRanges に従って、
 * 有効領域・無効領域を色や高さを変えた矩形で描画する。
 *
 * さらに、再生位置(Position)の左（再生済み）・右（未再生）でも色を変えるため、
 * 描画される矩形は「有効/無効 × 左/右」の 4 パターンの色・高さを持つ。
 */
public sealed partial class ChapterRangeView : UserControl {
    #region ViewModel DependencyProperty
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(PlayerViewModel),
            typeof(ChapterRangeView),
            new PropertyMetadata(null, OnViewModelChanged));

    public PlayerViewModel? ViewModel {
        get => (PlayerViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }
    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        // 必要ならイベント購読の付け替えなどをここで行う
        if (e.NewValue is PlayerViewModel viewModel && d is ChapterRangeView chapterRangeView) {
            chapterRangeView.OnViewModelChanged(viewModel);
        }
    }

    private void OnViewModelChanged(PlayerViewModel viewModel) {
        viewModel.ChapterModel.ChapterList.Subscribe(list => {
            mChapterList = list;
            invalidate();
        });
        viewModel.NaturalDuration.Subscribe(duration => {
            mTotalRange = duration;
            invalidate();

        });
        viewModel.PlayerPosition.Subscribe(pos => {
            mPosition = pos;
            invalidate();
        });
    }

    public ChapterListViewModel? ChapterModel => ViewModel?.ChapterModel;

    #endregion


    #region Initialization

    public ChapterRangeView() {
        this.InitializeComponent();
        ActualThemeChanged += (_, _) => { applyThemeColors(); invalidate(); };
        applyThemeColors();
    }

    /**
     * Win2D では ThemeResource を直接使えないため、テーマに応じたシステムカラーを取得して設定する。
     */
    private void applyThemeColors() {
        mEnabledLeftColor = ThemeColors.Get(this, "AccentFillColorDefaultBrush", mEnabledLeftColor);
        // *Disabled 系のテーマリソースは半透明色のため、動画上で背景が透けて見えなくなる。不透明な色を使う。
        mEnabledRightColor = ThemeColors.Get(this, ActualTheme == ElementTheme.Light ? "SystemAccentColorLight2" : "SystemAccentColorDark1", mEnabledRightColor);
        mDisabledLeftColor = ThemeColors.Get(this, "ControlStrongFillColorDefaultBrush", mDisabledLeftColor);
        mDisabledRightColor = ActualTheme == ElementTheme.Light ? Color.FromArgb(0xFF, 0xBD, 0xBD, 0xBD) : Color.FromArgb(0xFF, 0x5A, 0x5A, 0x5A);
        mSlitColor = ThemeColors.Get(this, "SolidBackgroundFillColorBaseBrush", mSlitColor);
        mCursorColor = ThemeColors.Get(this, "TextFillColorPrimaryBrush", mCursorColor);
    }

    private IList<Chapter>? mChapterList = null;
    private long mTotalRange = 0;
    private double mPosition = 0;
    private void OnLoaded(object sender, RoutedEventArgs e) {
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) {
        // Win2D の作法: CanvasControl はビジュアルツリーから明示的に取り除かないと
        // リークし、ウィンドウクローズ時（特にPinPなどのサブウィンドウ）にクラッシュすることがある。
        if (null != mCanvas) {
            mCanvas.RemoveFromVisualTree();
            mCanvas = null;
            ViewModel?.Logger.Debug("ChapterRangeView: RemoveFromVisualTree");
        }
    }


    #endregion

    #region Appearance Properties

    // 有効領域の高さ（太め）
    public double EnabledHeight {
        get { return mEnabledHeight; }
        set { if (value != mEnabledHeight) { mEnabledHeight = value; invalidate(); } }
    }
    private double mEnabledHeight = 8;

    // 無効領域の高さ（細め）
    public double DisabledHeight {
        get { return mDisabledHeight; }
        set { if (value != mDisabledHeight) { mDisabledHeight = value; invalidate(); } }
    }
    private double mDisabledHeight = 3;

    // 有効 × 左（再生済み）
    public Color EnabledLeftColor {
        get { return mEnabledLeftColor; }
        set { if (!value.Equals(mEnabledLeftColor)) { mEnabledLeftColor = value; invalidate(); } }
    }
    private Color mEnabledLeftColor = Color.FromArgb(0xFF, 0x21, 0x96, 0xF3);
    private Color mSlitColor = Colors.Black;
    private Color mCursorColor = Colors.White;

    // 有効 × 右（未再生）
    public Color EnabledRightColor {
        get { return mEnabledRightColor; }
        set { if (!value.Equals(mEnabledRightColor)) { mEnabledRightColor = value; invalidate(); } }
    }
    private Color mEnabledRightColor = Color.FromArgb(0xFF, 0x90, 0xCA, 0xF9);

    // 無効 × 左（再生済み）
    public Color DisabledLeftColor {
        get { return mDisabledLeftColor; }
        set { if (!value.Equals(mDisabledLeftColor)) { mDisabledLeftColor = value; invalidate(); } }
    }
    private Color mDisabledLeftColor = Color.FromArgb(0xFF, 0x75, 0x75, 0x75);

    // 無効 × 右（未再生）
    public Color DisabledRightColor {
        get { return mDisabledRightColor; }
        set { if (!value.Equals(mDisabledRightColor)) { mDisabledRightColor = value; invalidate(); } }
    }
    private Color mDisabledRightColor = Color.FromArgb(0xFF, 0xBD, 0xBD, 0xBD);

    #endregion

    #region Event Handlers

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) {
        invalidate();
    }

    #endregion

    #region Rendering

    private void invalidate() {
        mCanvas?.Invalidate();
    }

    /**
     * CanvasControl の描画。EnabledRanges / DisabledRanges を、
     * 再生位置で左右に分割しながら「有効/無効 × 左/右」の 4 パターンで矩形描画する。
     */
    private void OnDraw(CanvasControl sender, CanvasDrawEventArgs args) {
        var vm = ChapterModel;
        if (vm == null || mTotalRange<=0 || mChapterList==null) {
            return;
        }
        var ds = args.DrawingSession;
        double width = sender.Size.Width;
        double height = sender.Size.Height;
        double posX = timeToX(mPosition, width);

        // 無効領域（細め）を先に描画し、有効領域（太め）を上に重ねる。
        foreach (var r in vm.DisabledRanges) {
            drawRange(ds, r, width, height, posX, mDisabledHeight, mDisabledLeftColor, mDisabledRightColor);
        }
        foreach (var r in vm.EnabledRanges) {
            drawRange(ds, r, width, height, posX, mEnabledHeight, mEnabledLeftColor, mEnabledRightColor);
        }
        // チャプター設定位置のスリット
        foreach (var r in mChapterList) {
            drawSlit(ds, r.Position, width, height, mEnabledHeight, 1.5, mSlitColor);
        }
        // スライダーカーソル
        drawSlit(ds, mPosition, width, height, mEnabledHeight, 1.0, mCursorColor);
    }

    /**
     * 1 区間を、再生位置 posX を境に左右で色分けして描画する。
     */
    private void drawRange(Microsoft.Graphics.Canvas.CanvasDrawingSession ds, TimeRange range,
                           double width, double height, double posX,
                           double barHeight, Color leftColor, Color rightColor) {
        long endTime = range.ActualEnd((long)mTotalRange);
        double x0 = timeToX(range.Start, width);
        double x1 = timeToX(endTime, width);
        if (x1 <= x0) {
            return;
        }
        float top = (float)((height - barHeight) / 2);
        float h = (float)barHeight;

        // 左（再生済み）部分と右（未再生）部分に分割する。
        double split = Math.Min(Math.Max(posX, x0), x1);
        if (split > x0) {
            ds.FillRectangle((float)x0, top, (float)(split - x0), h, leftColor);
        }
        if (x1 > split) {
            ds.FillRectangle((float)split, top, (float)(x1 - split), h, rightColor);
        }
    }

    /**
     * 時間（ミリ秒）をコントロール座標の X に変換する。
     */
    private double timeToX(double time, double width) {
        return (mTotalRange > 0) ? width * time / mTotalRange : 0;
    }

    /**
     * チャプター設定位置に、黒いスリットを描画する。
     */
    private void drawSlit(
        Microsoft.Graphics.Canvas.CanvasDrawingSession ds,
        double time, double width, double height, double barHeight,
        double slitWidth,
        Color slitColor
        ) {
        if (time <= 0 || mTotalRange <= time) return;
        float top = (float)((height - barHeight) / 2);
        float h = (float)barHeight;
        double x0 = timeToX(time, width) - slitWidth / 2;
        ds.FillRectangle((float)x0, top, (float)slitWidth, h, slitColor);
    }

    #endregion

}
