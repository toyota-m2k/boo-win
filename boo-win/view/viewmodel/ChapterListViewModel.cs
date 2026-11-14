using BooWin.DI.contract;
using BooWin.model;
using Reactive.Bindings;
using Reactive.Bindings.TinyLinq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BooWin.view.viewmodel;

public class ChapterListViewModel
{
    #region Companion
    public const long MIN_CHAPTER_SPAN = 100L;

    public static IEnumerable<TimeRange> ReverseRange(IEnumerable<TimeRange> ranges) {
        long checking = 0L;
        foreach (var r in ranges) {
            if (checking < r.Start) {
                yield return new TimeRange(checking, r.Start);
            }
            checking = r.End;
            if (checking == 0L) {
                yield break; // 残りは最後まで有効（これ以降、無効領域はない）
            }
        }
        yield return new TimeRange(checking, 0L); // 残りは最後まで有効（これ以降、無効領域はない）
    }
    public static IList<TimeRange> TerminateRanges(IList<TimeRange> ranges, long duration) {
        for (var i = 0; i < ranges.Count; i++) {
            ranges[i] = ranges[i].Terminate(duration);
        }
        return ranges;
    }
    #endregion

    #region Chapter List

    public ReactiveProperty<IList<Chapter>> ChapterList { get; } = new ReactiveProperty<IList<Chapter>>(new List<Chapter>());
    public ReactiveProperty<Chapter?> CurrentChapter { get; } = new();
    public ReadOnlyReactiveProperty<string> ChapterLabel { get; }
    private SortedList<long, Chapter> mChapters = new SortedList<long, Chapter>();
    public ReadOnlyReactiveProperty<bool> CanUndo { get; }
    public ReadOnlyReactiveProperty<bool> CanRedo { get; }
    public ReactiveCommand UndoCommand { get; } = new();
    public ReactiveCommand RedoCommand { get; } = new();

    public ReactiveCommand NextChapterCommand { get; } = new();
    public ReactiveCommand PrevChapterCommand { get; } = new();
    public ReactiveCommand AddChapterCommand { get; } = new();
    public ReactiveCommand RemoveChapterCommand { get; } = new();
    public ReactiveCommand ToggleSkipChapterCommand { get; } = new();
    public ReactiveCommand SetChapterLabelCommand { get; } = new();


    private List<List<Chapter>> mChapterHistory = new List<List<Chapter>>();
    private ReactiveProperty<int> HistoryIndex = new(-1);
    private int mHistoryIndex {
        get => HistoryIndex.Value;
        set => HistoryIndex.Value = value;
    }

    private IDialogService _dialogService;
    public ChapterListViewModel(IDialogService dialogService) {
        _dialogService = dialogService;
        ChapterLabel = CurrentChapter.Select(chapter => chapter?.Label ?? string.Empty).ToReadOnlyReactiveProperty<string>();
        CanUndo = HistoryIndex.Select(i => i > 0).ToReadOnlyReactiveProperty();
        CanRedo = HistoryIndex.Select(i => i < mChapterHistory.Count - 1).ToReadOnlyReactiveProperty();
        UndoCommand.Subscribe(Undo);
        RedoCommand.Subscribe(Redo);
    }

    private void OnUpdateChapterList() {
        var list = mChapters.Values.ToList();
        ClearRangeCache();
        ChapterList.Value = list;
        // 履歴の最後の要素と同じなら、履歴に追加しない
        if (mHistoryIndex < mChapterHistory.Count - 1) {
            mChapterHistory.RemoveRange(mHistoryIndex + 1, mChapterHistory.Count - mHistoryIndex - 1);
        }
        mChapterHistory.Add(list);
        mHistoryIndex = mChapterHistory.Count - 1;
    }

    public void Clear(List<Chapter>? newChapter) {
        mChapters.Clear();
        mChapterHistory.Clear();
        mHistoryIndex = -1;
        lock (mChapters) {
            if (newChapter != null) {
                foreach (var chapter in newChapter) {
                    mChapters.Add(chapter.Position, chapter);
                }
            }
        }
        OnUpdateChapterList();
    }

    public bool AddChapter(Chapter chapter) {
        lock (mChapters) {
            if (!CanAddChapter(chapter.Position)) {
                return false;
            }
            mChapters[chapter.Position] = chapter;
            OnUpdateChapterList();
            return true;
        }
    }
    public bool AddChapter(long position) {
        lock (mChapters) {
            if (!CanAddChapter(position)) {
                return false;
            }
            var chapter = new Chapter() { Position = position };
            mChapters[chapter.Position] = chapter;
            OnUpdateChapterList();
            return true;
        }
    }

    public bool RemoveChapter(long position) {
        lock (mChapters) {
            var chapter = GetChapterAt(position);
            if (chapter!=null) {
                return RemoveChapter(chapter);
            }
            return false;
        }
    }
    public bool RemoveChapter(Chapter chapter) {
        lock (mChapters) {
            if (chapter.Position != 0) {
                if (mChapters.Remove(chapter.Position)) {
                    OnUpdateChapterList();
                    return true;
                }
            }
            return false;
        }
    }

    public bool UpdateChapter(long position, string? label, bool? skip) {
        lock (mChapters) {
            var chapter = GetChapterAt(position);
            if (chapter != null) {
                return UpdateChapter(chapter, label, skip);
            }
            return false;
        }
    }


    public bool UpdateChapter(Chapter chapter, string? label, bool? skip) {
        lock (mChapters) {
            if (!mChapters.ContainsKey(chapter.Position)) {
                return false;
            }
            Chapter? newChapter = chapter.CopyChapterIfModifed(label, skip);
            if (newChapter!=null) {
                mChapters[chapter.Position] = newChapter;
                OnUpdateChapterList();
                return true;
            }
            return false;
        }
    }


    private bool CanAddChapter(long position) {
        if (0L < position && position < MIN_CHAPTER_SPAN) {
            // 0Lは許可する（pseudo chapter）が、0～100msは許可しない
            return false;
        }

        int prevIndex = -1;
        int nextIndex = -1;
        int hit = GetNeighbourIndexOf(mChapters, position, out prevIndex, out nextIndex);
        if (hit >= 0) {
            return false;
        }
        else {
            if (prevIndex >= 0) {
                if (Math.Abs(mChapters.Values[prevIndex].Position - position) < MIN_CHAPTER_SPAN) {
                    return false;
                }
            }
            if (nextIndex >= 0) {
                if (Math.Abs(mChapters.Values[nextIndex].Position - position) < MIN_CHAPTER_SPAN) {
                    return false;
                }
            }
        }
        return true;
    }

    public void Undo() {
        lock (mChapters) {
            if (mHistoryIndex > 0) {
                mHistoryIndex--;
                var list = mChapterHistory[mHistoryIndex];
                mChapters.Clear();
                foreach (var chapter in list) {
                    mChapters.Add(chapter.Position, chapter);
                }
                ClearRangeCache();  // Skip属性が変化したかチェックすべきだが、そこまで厳密にやることもないだろう。
                ChapterList.Value = list;
            }
        }
    }

    public void Redo() {
        lock (mChapters) {
            if (mHistoryIndex < mChapterHistory.Count - 1) {
                mHistoryIndex++;
                var list = mChapterHistory[mHistoryIndex];
                mChapters.Clear();
                foreach (var chapter in list) {
                    mChapters.Add(chapter.Position, chapter);
                }
                ClearRangeCache();
                ChapterList.Value = list;
            }
        }
    }

    public async void RenameChapter(Chapter chapter) {
        var name = await _dialogService.ShowInputText(title:"Chapter Name", message:"Enter new chapter name:", initialText:chapter.Label ?? "", placeholder:"Chapter Name", acceptEmptyString:true);
        if (name != null) {
            UpdateChapter(chapter.Position, name, null);
        }
    }
    public async void RenameChapter(long position) {
        var chapter = GetChapterAt(position);
        if (chapter != null) {
            RenameChapter(chapter);
        }
    }

    public void ToggleSkipChapter(Chapter chapter) {
        UpdateChapter(chapter.Position, null, !chapter.Skip);
    }
    public void ToggleSkipChapter(long position) {
        var chapter = GetChapterAt(position);
        if (chapter != null) {
            UpdateChapter(chapter.Position, null, !chapter.Skip);
        }
    }

    #endregion

    #region Find Chapter


    /**
     * 指定位置(current)近傍のインデックスを取得
     * 
     * @param prev (out) currentの前（なければ-1）
     * @param next (out) currentの次（なければ-1）
     * @return true: currentがヒット（prevとnextにひとつ前/後のindexがセットされる）
     *         false: ヒットしていない
     */
    public static int GetNeighbourIndexOf<T>(SortedList<long, T> list, long current, out int prev, out int next) {
        prev = next = -1;
        int count = list.Count, s = 0, e = count - 1, m;
        if (e < 0) {
            return -1;
        }

        var markers = list.Keys;
        if (markers[e] < current) {
            prev = e;
            return -1;
        }

        while (s < e) {
            m = (s + e) / 2;
            long v = markers[m];
            if (v == current) {
                prev = m - 1;
                if (m < count - 1) {
                    next = m + 1;
                }
                return m;
            }
            else if (v < current) {
                s = m + 1;
            }
            else // current < markers[m]
            {
                e = m;
            }
        }
        next = s;
        prev = s - 1;
        return -1;
    }

    public int FindChapter(long posMs, out int prev, out int next) {
        lock (mChapters) {
            return GetNeighbourIndexOf(mChapters, posMs, out prev, out next);
        }
    }

    public Chapter? NextChapter(long current) {
        lock (mChapters) {
            FindChapter(current, out var prev, out var next);
            if (next < 0) {
                return null;
            }
            return mChapters.Values[next];
        }
    }

    public Chapter? PrevChapter(long current) {
        lock (mChapters) {
            FindChapter(current, out var prev, out var next);
            if (prev < 0) {
                return null;
            }
            return mChapters.Values[prev];
        }
    }

    public Chapter? NextEnabledChapter(long current) {
        lock (mChapters) {
            FindChapter(current, out var prev, out var next);
            if (next < 0) {
                return null;
            }
            for (int i = next; i < mChapters.Count; i++) {
                var nextChapter = mChapters.Values[i];
                if (nextChapter.Enabled) {
                    return nextChapter;
                }
            }
            return null;
        }
    }

    public Chapter? PrevEnabledChapter(long current, bool isPlaying) {
        lock (mChapters) {
            FindChapter(current, out var prev, out var next);
            if (prev < 0) {
                return null;
            }
            if (isPlaying && Math.Abs(mChapters.Values[prev].Position - current) < 300L) {
                // 再生中に戻るボタンが押され、且つ、prev chapter の先頭からのオフセットが300ms未満なら、prev chapterの先頭を基準にprevChapterを返す
                return PrevEnabledChapter(mChapters.Values[prev].Position, false);
            }
            for (int i = prev; i >= 0; i--) {
                var prevChapter = mChapters.Values[i];
                if (prevChapter.Enabled) {
                    return prevChapter;
                }
            }
            return null;
        }
    }

    public Chapter? GetChapterAt(long current) {
        lock (mChapters) {
            var hit = FindChapter(current, out var prev, out var next);
            if (hit >= 0) {
                return mChapters.Values[hit];
            }
            if (prev >= 0) {
                return mChapters.Values[prev];
            }
            return null;
        }
    }

    public long FirstEnabledPosition() {
        lock (mChapters) {
            return (EnabledRanges.Count > 0) ? EnabledRanges.First().Start : 0L;
        }
    }
    public long LastEnabledPosition(long duration) {
        lock (mChapters) {
            return (EnabledRanges.Count > 0) ? EnabledRanges.Last().ActualEnd(duration) : duration;
        }
    }
    #endregion

    #region Chapter Ranges

    private IEnumerable<Chapter> ChapterListWithHeadChapter() {
        if (mChapters.Count == 0 || mChapters.Values.First().Position > 0L) {
            yield return new Chapter { Position = 0L, Skip = false };
        }
        foreach (var chapter in mChapters.Values) {
            yield return chapter;
        }
    }

    private IEnumerable<TimeRange> GetEnabledRanges() {
        var skipping = false;
        var checking = 0L;
        foreach (var r in ChapterListWithHeadChapter()) {
            if (skipping != r.Skip) {
                if (r.Skip) {
                    // enabled --> disabled at r.position
                    skipping = true;
                    if (checking < r.Position) {
                        yield return new TimeRange(checking, r.Position);
                        checking = r.Position;
                    }
                }
                else {
                    // disabled --> enabled at r.position
                    skipping = false;
                    checking = r.Position;
                }
            }
        }
        if (!skipping) {
            yield return new TimeRange(checking);
        }
    }

    private IEnumerable<TimeRange> GetDisabledRanges() {
        return ReverseRange(EnabledRanges);
    }

    private List<TimeRange>? mEnabledRangeCache = null;
    private List<TimeRange>? mDisabledRangeCache = null;
    private void ClearRangeCache() {
        mEnabledRangeCache = null;
        mDisabledRangeCache = null;
    }

    public IList<TimeRange> EnabledRanges {
        get {
            lock (mChapters) {
                if (mEnabledRangeCache != null) {
                    return mEnabledRangeCache;
                }
                mEnabledRangeCache = GetEnabledRanges().ToList();
                return mEnabledRangeCache;
            }
        }
    }

    public IList<TimeRange> DisabledRanges {
        get {
            lock (mChapters) {
                if (mDisabledRangeCache != null) {
                    return mDisabledRangeCache;
                }
                mDisabledRangeCache = GetDisabledRanges().ToList();
                return mDisabledRangeCache;
            }
        }
    }

    #endregion
}
