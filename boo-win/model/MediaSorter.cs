using BooWin.view.viewmodel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BooWin.model;

public interface IMediaSorter {

    public IEnumerable<MediaItem> Sort(IEnumerable<MediaItem> source);
}

public class MediaSorter : IMediaSorter {
    public enum Key {
        Name,
        Size,
        Duration,
        Label,
        Date,
        MediaType,
        Category,
        Rating,
        Mark,
    }


    public Key PrimarySortKey { get; set; } = Key.Date;
    public Key SecondarySortKey { get; set; } = Key.Date;
    public bool Descending { get; set; } = true;

    public MediaSorter() { }
    public MediaSorter(IMediaSorter sorter) {
        if (sorter is MediaSorter s) {
            PrimarySortKey = s.PrimarySortKey;
            SecondarySortKey = s.SecondarySortKey;
            Descending = s.Descending;
        }
    }

    IEnumerable<MediaItem> SortWithKey(IEnumerable<MediaItem> source, Key key, bool descending) {
        switch(key) {
            case Key.Name:
                return descending ? source.OrderByDescending(item => item.Name) : source.OrderBy(item => item.Name);
            case Key.Size:
                return descending ? source.OrderByDescending(item => item.Size) : source.OrderBy(item => item.Size);
            case Key.Duration:
                return descending ? source.OrderByDescending(item => item.Duration) : source.OrderBy(item => item.Duration);
            case Key.Label:
                return descending ? source.OrderByDescending(item => item.Label) : source.OrderBy(item => item.Label);
            case Key.Date:
                return descending ? source.OrderByDescending(item => item.Date) : source.OrderBy(item => item.Date);
            case Key.MediaType:
                return descending ? source.OrderByDescending(item => item.MediaType) : source.OrderBy(item => item.MediaType);
            case Key.Category:
                return descending ? source.OrderByDescending(item => item.CategoryName) : source.OrderBy(item => item.CategoryName);
            case Key.Rating:
                return descending ? source.OrderByDescending(item => item.Rating) : source.OrderBy(item => item.Rating);
            case Key.Mark:
                return descending ? source.OrderByDescending(item => item.Mark) : source.OrderBy(item => item.Mark);
            default:
                throw new ArgumentException($"Invalid sort key: {key}");
        }
    }

    public IEnumerable<MediaItem> Sort(IEnumerable<MediaItem> source) {
        var sorted = source;
        if (SecondarySortKey != PrimarySortKey) {
            sorted = SortWithKey(sorted, SecondarySortKey, Descending);
        }
        sorted = SortWithKey(sorted, PrimarySortKey, Descending);
        return sorted;
    }
    public class EmptySorterImpl : IMediaSorter {
        public IEnumerable<MediaItem> Sort(IEnumerable<MediaItem> source) {
            return source;
        }
    }
    public static readonly IMediaSorter EmptySorter = new EmptySorterImpl();

    public override bool Equals(object? obj) {
        if (base.Equals(obj)) return true;
        if (obj is not MediaSorter other) return false;
        return PrimarySortKey == other.PrimarySortKey &&
               SecondarySortKey == other.SecondarySortKey &&
               Descending == other.Descending;
    }

    public override int GetHashCode() {
        return HashCode.Combine(PrimarySortKey, SecondarySortKey, Descending);
    }
}

public static class SortViewModelExtensions {
    public static IEnumerable<MediaItem> SortBy(this IEnumerable<MediaItem> source, IMediaSorter sorter) {
        return sorter.Sort(source);
    }
}
