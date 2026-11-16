using BooWin.DI.contract;
using BooWin.model;
using io.github.toyota32k.BooLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BooWin.model;

public interface IMediaFilter {
    public bool IsEmpty { get; }
    public bool Filter(IMediaItem item);
}


public class MediaFilter : IMediaFilter {
    private IClientService ClientService { get; }
    public ICategory Category = BuiltInCategory.All;
    public IList<int>? Ratings = null; // null: all / ratings to allow
    public IList<int>? Marks = null;   // 

    public IList<MediaType>? TargetTypes = null;
    public string SearchText = string.Empty;

    public bool EnableStartDate = false;
    public bool EnableEndDate = false;
    public DateTimeOffset StartDate = DateTimeOffset.MinValue;
    public DateTimeOffset EndDate = DateTimeOffset.MaxValue;

    public MediaFilter(IClientService clientService) {
        ClientService = clientService;
    }


    public bool Filter(IMediaItem item) {
        if (Category != BuiltInCategory.All) {
            if (Category== BuiltInCategory.Unspecified && !string.IsNullOrWhiteSpace(item.CategoryName) ||
                Category != BuiltInCategory.Unspecified && item.CategoryName != Category.Name) {
                return false;
            }   
        }
        if (Ratings != null) {
            if (!Ratings.Any(it=>it == ClientService.RatingSupport.ComplementalRating(item))) {
                return false;
            }
        }

        if (Marks != null) {
            if (!Marks.Any(it => it == item.Mark)) {
                return false;
            }
        }

        if (TargetTypes != null && !TargetTypes.IsEmpty()) {
            if (!TargetTypes.Contains(item.MediaType)) {
                return false;
            }
        }
        if (EnableStartDate) {
            if (item.Date < StartDate) {
                return false;
            }
        }
        if (EnableEndDate) {
            if (item.Date > EndDate) {
                return false;
            }
        }
        if (!string.IsNullOrWhiteSpace(SearchText)) {
            if (!item.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) &&
                !item.Label.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) {
                return false;
            }
        }
        return true;
    }

    public bool IsEmpty =>
        Category == BuiltInCategory.All && (Ratings == null || Ratings.IsEmpty()) && (Marks == null || Marks.IsEmpty()) && (TargetTypes == null||TargetTypes.IsEmpty()) && !EnableStartDate && !EnableEndDate && string.IsNullOrWhiteSpace(SearchText);

    class EmptyFilter : IMediaFilter {
        public bool IsEmpty => true;
        public bool Filter(IMediaItem item) => true;
    }
    public static IMediaFilter Empty { get; } = new EmptyFilter();


    private static bool IsEqualList<T>(IList<T>? a, IList<T>? b) {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        if (a.Count != b.Count) return false;
        for (var i = 0; i < a.Count; i++) {
            if (!EqualityComparer<T>.Default.Equals(a[i], b[i])) {
                return false;
            }
        }
        return true;
    }
    public static bool IsEqual(IMediaFilter a, IMediaFilter b) {
        //if (a == null && b == null) return true;
        //if (a == null || b == null) return false;
        if (a.IsEmpty && b.IsEmpty) return true;
        if (a is MediaFilter ma && b is MediaFilter mb) {
            if (ma.Category != mb.Category) return false;
            if (ma.EnableStartDate != mb.EnableStartDate) return false;
            if (ma.EnableEndDate != mb.EnableEndDate) return false;
            if (ma.EnableStartDate && ma.StartDate != mb.StartDate) return false;
            if (ma.EnableEndDate && ma.EndDate != mb.EndDate) return false;
            if (!IsEqualList(ma.Ratings, mb.Ratings)) return false;
            if (!IsEqualList(ma.Marks, mb.Marks)) return false;
            if (!IsEqualList(ma.TargetTypes, mb.TargetTypes)) return false;
            if (!string.Equals(ma.SearchText, mb.SearchText, StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }
        return false;
    }
}

