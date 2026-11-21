using BooWin.DI.contract;
using BooWin.view.viewmodel;
using io.github.toyota32k.BooLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BooWin.model;

public interface IMediaFilter {
    public bool IsEmpty { get; }
    public bool Filter(IClientService clientService, IMediaItem item);
}


public class MediaFilter : IMediaFilter {
    public ICategory? Category { get; set; } = BuiltInCategory.All;
    public IList<int>? Ratings { get; set; } = null; // null: all / ratings to allow
    public IList<int>? Marks { get; set; } = null;   // 

    public IList<MediaType>? TargetTypes { get; set; } = null;
    public string SearchText { get; set; } = string.Empty;

    public bool EnableStartDate { get; set; } = false;
    public bool EnableEndDate { get; set; } = false;
    public DateTimeOffset StartDate { get; set; } = DateTimeOffset.MinValue;
    public DateTimeOffset EndDate { get; set; } = DateTimeOffset.MaxValue;


    //private IClientService _clientService { get; }
    public MediaFilter() {
    }
    public MediaFilter(IMediaFilter filter) {
        if (filter is MediaFilter f) {
            Category = f.Category;
            Ratings = f.Ratings;
            Marks = f.Marks;
            TargetTypes = f.TargetTypes;
            SearchText = f.SearchText;
            EnableStartDate = f.EnableStartDate;
            EnableEndDate = f.EnableEndDate;
            StartDate = f.StartDate;
            EndDate = f.EndDate;
        }
    }


    public bool Filter(IClientService clientService, IMediaItem item) {
        if (clientService.CategorySupport.IsSupported && Category != null && Category != BuiltInCategory.All) {
            if (Category== BuiltInCategory.Unspecified && !string.IsNullOrWhiteSpace(item.CategoryName) ||
                Category != BuiltInCategory.Unspecified && item.CategoryName != Category.Name) {
                return false;
            }   
        }
        if (clientService.RatingSupport.IsSupported && Ratings != null) {
            if (!Ratings.Any(it=>it == clientService.RatingSupport.ComplementalRating(item))) {
                return false;
            }
        }

        if (clientService.MarkSupport.IsSupported && Marks != null) {
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

    //class EmptyFilter : IMediaFilter {
    //    public bool IsEmpty => true;
    //    public bool Filter(IMediaItem item) => true;
    //}
    //public static IMediaFilter Empty { get; } = new EmptyFilter();


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

    public override bool Equals(object? obj) {
        if (obj is IMediaFilter filter) {
            return IsEqual(this, filter);
        } else {
            return false;
        }
    }
    public override int GetHashCode() {
        var hash = new HashCode();
        if (Category != null) {
            hash.Add(Category.Name);
        }
        Ratings?.Aggregate(hash, (acc, r) => {
            acc.Add(r);
            return acc;
        });
        Marks?.Aggregate(hash, (acc, m) => {
                acc.Add(m);
                return acc;
            });
        TargetTypes?.Aggregate(hash, (acc, t) => {
                acc.Add(t);
                return acc;
            });
        hash.Add(SearchText??"");
        hash.Add(EnableStartDate);
        if (EnableStartDate) {
            hash.Add(StartDate);
        }
        hash.Add(EnableEndDate);
        if (EnableEndDate) {
            hash.Add(EndDate);
        }
        return hash.ToHashCode();
    }
}


