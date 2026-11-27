using io.github.toyota32k.BooLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BooWin.model; 
public class ChapterListEditor {
    private SortedList<long, Chapter> mChapters = new SortedList<long, Chapter>();
    public const long MIN_CHAPTER_SPAN = 100L;

    public List<Chapter> ToList() {
        return mChapters.Values.ToList();
    }   

    public int Count => mChapters.Count;
    public Chapter this[int index] => mChapters.Values[index];

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

    #region CRUD

    public bool Clear() {
        if (mChapters.IsEmpty()) return false;
        mChapters.Clear();
        return true;

    }
    public bool Set(List<Chapter>? newChapter) {
        var cleared = Clear();
        if (newChapter == null || newChapter.Count==0) {
            if (mChapters.IsEmpty()) {
                return false;   // 変化なし
            }
            mChapters.Clear();
            return true;
        }
        foreach (var chapter in newChapter) {
            mChapters.Add(chapter.Position, chapter);
        }
        return true;
    }

    public bool AddChapter(Chapter chapter) {
        if (!CanAddChapter(chapter.Position)) {
            return false;
        }
        mChapters[chapter.Position] = chapter;
        return true;
    }

    public bool RemoveChapter(Chapter chapter) {
        if (mChapters.Remove(chapter.Position)) {
            return true;
        }
        return false;
    }

    public bool UpdateChapter(Chapter chapter, string? label, bool? skip) {
        if (!mChapters.ContainsKey(chapter.Position)) {
            return false;   // not found
        }
        Chapter? newChapter = chapter.CopyChapterIfModifed(label, skip);
        if (newChapter != null) {
            mChapters[chapter.Position] = newChapter;
            return true;
        }
        return false;   // no difference
    }

    public bool AddOrUpdateChapter(Chapter chapter) {
        if (mChapters.ContainsKey(chapter.Position)) {
            return UpdateChapter(chapter, chapter.Label, chapter.Skip);
        }
        else {
            return AddChapter(chapter);
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
    //public bool RenameChapter(Chapter chapter, string newName) {
    //    return UpdateChapter(chapter.Position, newName, null);
    //}
    //public bool RenameChapter(long position, string newName) {
    //    var chapter = GetChapterAt(position);
    //    if (chapter != null) {
    //        return RenameChapter(chapter, newName);
    //    }
    //    return false;
    //}

    //public bool ToggleSkipChapter(Chapter chapter) {
    //    return UpdateChapter(chapter.Position, null, !chapter.Skip);
    //}
    //public bool ToggleSkipChapter(long position) {
    //    var chapter = GetChapterAt(position);
    //    if (chapter != null) {
    //        return UpdateChapter(chapter.Position, null, !chapter.Skip);
    //    }
    //    return false;
    //}

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
    private static int GetNeighbourIndexOf<T>(SortedList<long, T> list, long current, out int prev, out int next) {
        prev = next = -1;
        int count = list.Count, s = 0, e = count - 1, m;
        if (e < 0) {
            return -1;
        }

        var markers = list.Keys;
        if (markers[e] < current) {
            prev = e;
            return -1;
        } else if (current < markers[s]) {
            next = s;
            return -1;
        }

        // markers[e] にヒットするケースを特別扱い
        if (markers[e] == current) {
            return e;   // hit
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
        return GetNeighbourIndexOf(mChapters, posMs, out prev, out next);
    }

    public Chapter? NextChapter(long current) {
        FindChapter(current, out var prev, out var next);
        if (next < 0) {
            return null;
        }
        return mChapters.Values[next];
    }

    public Chapter? PrevChapter(long current) {
        FindChapter(current, out var prev, out var next);
        if (prev < 0) {
            return null;
        }
        return mChapters.Values[prev];
    }

    public Chapter? NextEnabledChapter(long current) {
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

    public Chapter? PrevEnabledChapter(long current, bool isPlaying) {
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

    public Chapter? GetChapterAt(long current) {
        var hit = FindChapter(current, out var prev, out var next);
        if (hit >= 0) {
            return mChapters.Values[hit];
        }
        if (prev >= 0) {
            return mChapters.Values[prev];
        }
        return null;
    }

    public Chapter? GetJustChapterAt(long current) {
        var hit = FindChapter(current, out var prev, out var next);
        if (hit >= 0) {
            return mChapters.Values[hit];
        }
        return null;
    }

    //public long FirstEnabledPosition() {
    //    return (EnabledRanges.Count > 0) ? EnabledRanges.First().Start : 0L;
    //}
    //public long LastEnabledPosition(long duration) {
    //    return (EnabledRanges.Count > 0) ? EnabledRanges.Last().ActualEnd(duration) : duration;
    //}
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

    public IEnumerable<TimeRange> GetEnabledRanges() {
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

    //private IEnumerable<TimeRange> GetDisabledRanges() {
    //    return ReverseRange(GetEnabledRanges());
    //}

    //private List<TimeRange>? mEnabledRangeCache = null;
    //private List<TimeRange>? mDisabledRangeCache = null;
    //private void ClearRangeCache() {
    //    mEnabledRangeCache = null;
    //    mDisabledRangeCache = null;
    //}

    //public IList<TimeRange> EnabledRanges {
    //    get {
    //        lock (mChapters) {
    //            if (mEnabledRangeCache != null) {
    //                return mEnabledRangeCache;
    //            }
    //            mEnabledRangeCache = GetEnabledRanges().ToList();
    //            return mEnabledRangeCache;
    //        }
    //    }
    //}

    //public IList<TimeRange> DisabledRanges {
    //    get {
    //        lock (mChapters) {
    //            if (mDisabledRangeCache != null) {
    //                return mDisabledRangeCache;
    //            }
    //            mDisabledRangeCache = GetDisabledRanges().ToList();
    //            return mDisabledRangeCache;
    //        }
    //    }
    //}

    #endregion

    private static bool checkStartEndDuration(long start, long end) {
        if (start < 0 || end < 0) {
            return false;
        }
        if (end > 0) {
            if (start >= end) {
                return false;
            }
        }
        return true;
    }

    /**
     * item毎に start/end を指定する旧型式を、chapters に一本化する。
     */
    public static List<Chapter> MergeChaptersAndStartEnd(List<Chapter> chapters, long start, long end) {
        if (!checkStartEndDuration(start, end)) {
            return chapters;
        }
        var editor = new ChapterListEditor();
        editor.Set(chapters);

        if (start>0) {
            var orgChapterAtStart = editor.GetChapterAt(start);
            editor.AddOrUpdateChapter(new Chapter { Position = 0, Skip = true });
            for (int i = 1; i < editor.Count; i++) {
                var chapter = editor[i];
                if (start < chapter.Position) {
                    break;
                }
                editor.UpdateChapter(chapter, null, true);
            }
            editor.AddChapter(new Chapter { Position = start, Skip = orgChapterAtStart?.Skip ?? false });
        }

        if (end > 0) {
            for (int i = editor.Count - 1; i >= 0; i--) {
                var chapter = editor[i];
                if (chapter.Position < end) {
                    break;
                }
                editor.UpdateChapter(chapter, null, true); 
            }
            editor.AddChapter(new Chapter { Position = end, Skip = true });
        }

        return editor.ToList();
    }

}
