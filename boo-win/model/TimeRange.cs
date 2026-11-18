using System;

namespace BooWin.model;

public struct TimeRange {
    private readonly long mStart;
    private readonly long mEnd;

    public long Start => mStart;
    public long End => mEnd;

    public TimeRange(long startTime, long endTime = 0L) {
        mStart = startTime;
        mEnd = endTime;
    }

    public TimeRange Terminate(long videoNaturalDuration) {
        return End <= 0 ? new TimeRange(Start, videoNaturalDuration) : this;
    }

    /**
     * pos が start-end 内に収まるようクリップする
     */
    public long Clamp(long pos) {
        if (Start < End) {
            return Math.Min(Math.Max(Start, pos), End);
        }
        else {
            // Endが与えられていない
            return Math.Max(Start, pos);
        }
    }

    public bool Contains(long pos) {
        if (Start < End) {
            return Start <= pos && pos < End;
        }
        else {
            return Start <= pos;
        }
    }

    public long ActualEnd(long videoNaturalDuration) {
        if (0 < End) {
            return End;
        }
        else {
            // Endが与えられていない
            return videoNaturalDuration;
        }
    }

    public bool IsEmpty => Start == 0 && End == 0;
    public long Span => End - Start;
    public bool IsTerminated => 0 < End;

    public static TimeRange Empty = new TimeRange(0, 0);
    public static long Clamp(TimeRange? clip, long pos) {
        return clip?.Clamp(pos) ?? pos;
    }
}
