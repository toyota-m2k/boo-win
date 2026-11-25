using BooWin.DI.contract;
using BooWin.model;
using io.github.toyota32k.BooLib.Utils;
using Reactive.Bindings;
using System.Collections.Generic;

namespace BooWin.view.viewmodel; 
public class ItemPropertyViewModel {
    public ReactiveProperty<bool> IsRatingEditable { get; } = new(false);
    public ReactiveProperty<bool> IsMarkEditable { get; } = new(false);
    public ReactiveProperty<bool> IsCategoryEditable { get; } = new(false);

    public ReactiveProperty<int> Rating { get; } = new (3);
    public ReactiveProperty<int> Mark { get; } = new(0);
    public ReactiveProperty<ICategory> Category { get; } = new(model.Category.Unspecified);
    public ReactiveProperty<string> Name { get; } = new(string.Empty);
    public ReactiveProperty<string> Label { get; } = new(string.Empty);

    public ReactiveProperty<List<ICategory>> CategoryList { get; } = new(new List<ICategory>());

    private MediaItem? mEditingItem = null;

    public interface IChanges {
        MediaItem Item { get; }
        int? Rating { get; }
        int? Mark { get; }
        string? Category { get; }
        string? Name { get; }
        string? Label { get; }

        void ApplyChanges();
    }
    public class Changes : IChanges {
        public MediaItem Item { get; set; }
        public int? Rating { get; set; }
        public int? Mark { get; set; }
        public string? Category { get; set; }
        public string? Name { get; set; }
        public string? Label { get; set; }

        public bool HasChanges => Rating != null || Mark != null || Category != null || Name != null || Label != null;

        public Changes(MediaItem item) {
            Item = item;
        }

        public void ApplyChanges() {
            if (Rating != null) {
                Item.Rating = Rating.Value;
            }
            if (Mark != null) {
                Item.Mark = Mark.Value;
            }
            if (Category != null) {
                Item.CategoryName = Category;
            }
            if (Name != null) {
                Item.Name = Name;
            }
            if (Label != null) {
                Item.Label = Label;
            }
        }
    }


    public IChanges? SetSource(IMediaItem? item_, IClientService clientService) {
        var changes = GetChanges(mEditingItem, clientService);
        var item = item_ as MediaItem;
        mEditingItem = item;
        if (item != null) {
            Rating.Value = clientService.RatingSupport.ComplementalRating(item);
            Mark.Value = item.Mark;
            Category.Value = clientService.CategorySupport.GetCategoryByName(item.CategoryName);
            Name.Value = item.Name;
            Label.Value = item.Label;
        }
        return changes;
    }

    

    public void ResetOnHostChanged(IClientService clientService) {
        IsRatingEditable.Value = clientService.RatingSupport.AllowReadWrite;
        IsMarkEditable.Value = clientService.MarkSupport.AllowReadWrite;
        IsCategoryEditable.Value = clientService.CategorySupport.AllowReadWrite;
        Rating.Value = clientService.RatingSupport.DefaultRating;
        Mark.Value = 0;

        CategoryList.Value = clientService.CategorySupport.GetCategoryList(withAll: false, withUnspecified: true);
        Category.Value = model.Category.Unspecified;
    }

    public Changes? GetChanges(MediaItem? item, IClientService clientService) {
        if (item != null) {
            int? rating = null;
            int? mark = null;
            string? category = null;
            string? name = null;
            string? label = null;

            if (IsRatingEditable.Value && Rating.Value != clientService.RatingSupport.ComplementalRating(item)) {
                rating = Rating.Value;
            }
            if (IsMarkEditable.Value && Mark.Value != item.Mark) {
                mark = Mark.Value;
            }
            if (IsCategoryEditable.Value && Category.Value!=null && !Category.Value.Match(item.CategoryName)) {
                category = Category.Value.Name;
            }
            if (Name.Value.IsNotEmpty() &&Name.Value != item.Name) {
                name = Name.Value;
            }
            if (Label.Value != item.Label) {
                label = Label.Value;
            }
            if (rating!=null || mark != null || category != null || name != null || label != null) {
                return new Changes(item) {
                    Rating = rating,
                    Mark = mark,
                    Category = category,
                    Name = name,
                    Label = label
                };
            }
        }
        return null;
    }
}
