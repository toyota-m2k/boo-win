using BooWin.view.viewmodel;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Colors = Microsoft.UI.Colors;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
using BooWin.model;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BooWin.view.parts;

/**
 * チャプター（ChapterInfo）位置を表示するビュー。
 * WvvMarkerView が有効/無効の区別のない double 配列を表示するのに対し、
 * こちらは有効/無効を区別できる ChapterInfo の配列を、ベクター画像（Path）で表示する。
 */
public sealed partial class ChapterView : UserControl {

    #region ViewModel DependencyProperty
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(PlayerViewModel),
            typeof(ChapterView),
            new PropertyMetadata(null, OnViewModelChanged));

    public PlayerViewModel? ViewModel {
        get => (PlayerViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }
    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        // 必要ならイベント購読の付け替えなどをここで行う
        if (e.NewValue is PlayerViewModel viewModel && d is ChapterView view) {
            view.OnViewModelChanged(viewModel);
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
    }

    private PlayerViewModel viewModel => ViewModel ?? throw new InvalidOperationException("ViewModel is not set.");
    #endregion

    #region Constants

    // Drawable の viewport サイズ（18 x 36）
    const double VP_WIDTH = 18;
    const double VP_HEIGHT = 36;
    // 実際の表示サイズ（有効: 12dp x 24dp を基準に統一）
    const double ICON_WIDTH = 12;
    const double ICON_HEIGHT = 24;
    // 右端でアイコンが見切れないようにするためのマージン
    const double POS_HORZ_MARGIN = 8;

    #endregion

    #region Fields

    // Win2D の CanvasControl 上に全チャプターアイコンを直接描画する（Android の onDraw 相当）。
    // Geometry / StrokeStyle は device 依存リソースとして 1 度だけ生成し、全アイコンで共有する。
    // CanvasGeometry は Path.Data のような親付けを持たないため、複数アイコンで安全に共有できる。
    private CanvasGeometry? mHeadGeometry;
    private CanvasGeometry? mStemGeometry;
    private CanvasStrokeStyle? mStrokeStyle;

    // 色（ピン本体の有効/無効、枠線、脚）
    private static readonly Color COLOR_FILL_ENABLED = Colors.White;
    private static readonly Color COLOR_FILL_DISABLED = Color.FromArgb(0xFF, 0x80, 0x80, 0x80); // 無効は gray50 相当（要調整）
    private static readonly Color COLOR_FILL_SELECTED = Color.FromArgb(0xFF, 0x40, 0x80, 0xFF); // 無効は gray50 相当（要調整）
    private static readonly Color COLOR_HEAD_STROKE = Color.FromArgb(0xFF, 0xC0, 0xC0, 0xC0);
    private static readonly Color COLOR_STEM_STROKE = Colors.White;

    private long mSelectedChapterTimeInMs = -1L;
    #endregion

    #region Initialization

    public ChapterView() {
        this.InitializeComponent();
    }

    private IList<Chapter>? mChapterList = null;
    private long mTotalRange = 0L;
    private void OnLoaded(object sender, RoutedEventArgs e) {
    }
    private void OnUnloaded(object sender, RoutedEventArgs e) {
        if (null != mCanvas) {
            mCanvas.RemoveFromVisualTree();
            mCanvas = null;
        }
    }


    /**
     * CanvasControl の device 依存リソース（共有ジオメトリ）を生成する。
     * device ロスト時にも再生成されるよう CreateResources で構築する。
     */
    private void OnCreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args) {
        // ピン本体: M1,1 L17,1 L17,10 L9,18 L1,10 Z（viewport 18x36 座標）
        using (var head = new CanvasPathBuilder(sender)) {
            head.BeginFigure(1, 1);
            head.AddLine(17, 1);
            head.AddLine(17, 10);
            head.AddLine(9, 18);
            head.AddLine(1, 10);
            head.EndFigure(CanvasFigureLoop.Closed);
            mHeadGeometry?.Dispose();
            mHeadGeometry = CanvasGeometry.CreatePath(head);
        }

        // ピンの脚: M9,16 L9,36
        using (var stem = new CanvasPathBuilder(sender)) {
            stem.BeginFigure(9, 16);
            stem.AddLine(9, 36);
            stem.EndFigure(CanvasFigureLoop.Open);
            mStemGeometry?.Dispose();
            mStemGeometry = CanvasGeometry.CreatePath(stem);
        }

        mStrokeStyle = new CanvasStrokeStyle { LineJoin = CanvasLineJoin.Round };
    }

    #endregion

    #region Properties


    #endregion

    #region Event Handlers

    /**
     * ビューサイズが変更されたら再描画する（アイコン位置は描画時に計算）。
     */
    private void OnSizeChanged(object sender, SizeChangedEventArgs e) {
        invalidate();
    }

    /**
     * クリック/タップ: 位置に対応するチャプターへジャンプ（シーク）する。
     */
    private void OnTapped(object sender, TappedRoutedEventArgs e) {
        var handler = ChapterSelected;
        if (handler == null) return;

        var pt = e.GetPosition(mCanvas);
        if (hitTestChapter(pt.X, out var chapter) && chapter != null) {
            handler.Invoke(chapter);
        }
        e.Handled = true;
    }

    private void requestContextMenu(Point pt) {
        var handler = ChapterContextMenuRequested;
        if (handler == null) return;

        if (hitTestChapter(pt.X, out var chapter) && chapter != null) {
            var left = chapterIconLeft(chapter.Position);
            var rect = new Rect(left, 0, ICON_WIDTH, ActualHeight);
            var pinRect = TransformToVisual(null).TransformBounds(rect);
            SelectChapter(chapter);
            handler.Invoke(chapter, mCanvas, pinRect);
        }
    }

    /**
     * 右クリック: 位置に対応するチャプターのコンテキストメニューを要求する。
     */
    private void OnRightTapped(object sender, RightTappedRoutedEventArgs e) {
        var pt = e.GetPosition(mCanvas);
        requestContextMenu(pt);
        e.Handled = true;
    }

    /**
     * ロングタップ: 位置に対応するチャプターのコンテキストメニューを要求する。
     */
    private void OnHolding(object sender, HoldingRoutedEventArgs e) {
        if (e.HoldingState != HoldingState.Started) {
            return;
        }
        var pt = e.GetPosition(mCanvas);
        requestContextMenu(pt);
        e.Handled = true;
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e) {
        var pointer = e.GetCurrentPoint(mCanvas);
        if (pointer.Properties.IsLeftButtonPressed) {
            var pt = pointer.Position;
            if (hitTestChapter(pt.X, out var chapter) && chapter != null) {
                SelectChapter(chapter);
            }
        }
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e) {
        var pointer = e.GetCurrentPoint(mCanvas);
        if (pointer.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased) {
            UnselectChapter(duration: 100);
        }
    }

    #endregion

    #region Events

    /**
     * チャプターが選択（タップ）されたときのイベント。ホストはシークに使う。
     */
    public delegate void ChapterSelectHandler(Chapter chapter);
    public event ChapterSelectHandler? ChapterSelected;

    /**
     * チャプター上でコンテキストメニューが要求されたときのイベント。
     * ホストは anchor / position を使って MenuFlyout を表示する。
     */
    public delegate void ChapterContextHandler(Chapter chapter, FrameworkElement anchor, Rect pinRect);
    public event ChapterContextHandler? ChapterContextMenuRequested;

    #endregion

    #region Private Methods - Rendering

    /**
     * 再描画を要求する。
     */
    private void invalidate() {
        mCanvas?.Invalidate();
    }

    /**
     * CanvasControl の描画。ChapterList の全チャプターアイコンを直接描画する。
     * viewport(18x36) 座標のジオメトリを ICON_WIDTH x ICON_HEIGHT にスケールし、
     * 各チャプターの時間位置へ平行移動して描く。
     *
     * 有効: ピン本体を白で塗る
     * 無効: ピン本体をグレーで塗る
     */
    private void OnDraw(CanvasControl sender, CanvasDrawEventArgs args) {
        if (mHeadGeometry == null || mStemGeometry == null || mChapterList == null) {
            return;
        }
        var ds = args.DrawingSession;
        float scale = (float)(ICON_WIDTH / VP_WIDTH);   // 12/18 == 24/36（等倍スケール）
        var baseTransform = ds.Transform;

        foreach (var chapter in mChapterList) {
            if (chapter.Position != mSelectedChapterTimeInMs) {
                var fill = chapter.Enabled ? COLOR_FILL_ENABLED : COLOR_FILL_DISABLED;
                drawOne(ds, chapter.Position, fill, scale);
            }
        }
        if (mSelectedChapterTimeInMs >= 0) {
            drawOne(ds, mSelectedChapterTimeInMs, COLOR_FILL_SELECTED, scale);
        }
        ds.Transform = baseTransform;
    }

    private void drawOne(CanvasDrawingSession ds, long timeInMs, Color fill, float scale) {
        double left = chapterIconLeft(timeInMs);
        // viewport 座標 -> スケール -> (left, top=0) へ平行移動
        ds.Transform = Matrix3x2.CreateScale(scale) * Matrix3x2.CreateTranslation((float)left, 0f);
        ds.FillGeometry(mHeadGeometry, fill);
        ds.DrawGeometry(mHeadGeometry, COLOR_HEAD_STROKE, 1f, mStrokeStyle);
        ds.DrawGeometry(mStemGeometry, COLOR_STEM_STROKE, 1f, mStrokeStyle);
    }

    /**
     * 指定時間のアイコン左端 X 座標（コントロール座標）を返す。
     * viewport 中央(x=9) が時間位置に一致するよう、中央揃えで配置する。
     */
    private double chapterIconLeft(long time) {
        return POS_HORZ_MARGIN + chapterCenterOffset(time) - ICON_WIDTH / 2;
    }

    /**
     * 指定時間の中央位置（左マージンからのオフセット）を返す。
     */
    private double chapterCenterOffset(long time) {
        double max = ActualWidth - POS_HORZ_MARGIN * 2;
        return (mTotalRange > 0) ? max * time / mTotalRange : 0;
    }

    /**
     * X 座標（コントロール座標）に対応するチャプターをヒットテストで探す。
     * 各アイコンの左右端 [left, left+ICON_WIDTH] で判定し、
     * 複数該当時は中央が最も近いものを選ぶ。
     */
    private bool hitTestChapter(double x, out Chapter? result) {
        result = null;
        double bestDist = double.MaxValue;
        bool found = false;
        var chapterList = mChapterList;
        if (chapterList == null) {
            return false;
        }
        foreach (var chapter in chapterList) {
            double left = chapterIconLeft(chapter.Position);
            double right = left + ICON_WIDTH;
            if (x >= left && x <= right) {
                double center = left + ICON_WIDTH / 2;
                double dist = Math.Abs(x - center);
                if (dist < bestDist) {
                    bestDist = dist;
                    result = chapter;
                    found = true;
                }
            }
        }
        return found;
    }

    #endregion

    #region Chapter Selection

    private void SelectChapter(Chapter chapter) {
        if (mSelectedChapterTimeInMs == chapter.Position) {
            return;
        }
        mSelectedChapterTimeInMs = chapter.Position;
        invalidate();
    }

    public async void UnselectChapter(int duration = 0) {
        if (mSelectedChapterTimeInMs >= 0) {
            mSelectedChapterTimeInMs = -1L;
            if (duration > 0) {
                await Task.Delay(duration);
            }
            invalidate();
        }
    }

    #endregion

}
