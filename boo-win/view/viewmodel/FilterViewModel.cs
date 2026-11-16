using BooWin.DI.contract;
using BooWin.model;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;

namespace BooWin.view.viewmodel; 
public class FilterViewModel {
    #region Category

    public ReactiveProperty<bool> IsCategorySupported { get; } = new ReactiveProperty<bool>(false);
    public ReactiveProperty<string> CategoryLabel { get; } = new ReactiveProperty<string>("Category");
    public ReactiveProperty<List<ICategory>> CategoryList { get; } = new ReactiveProperty<List<ICategory>>(new List<ICategory>());
    public ReactiveProperty<ICategory> SelectedCategory { get; } = new ReactiveProperty<ICategory>(BuiltInCategory.All);

    #endregion

    #region Types

    public ReactiveProperty<bool> TypeFilterEnabled { get; } = new(false);
    public ReactiveProperty<bool> TypeVideoChecked { get; } = new(true);
    public ReactiveProperty<bool> TypeAudioChecked { get; } = new(true);
    public ReactiveProperty<bool> TypePhotoChecked { get; } = new(true);

    #endregion

    #region Ratings

    public ReactiveProperty<bool> IsRatingSupported { get; } = new(false);
    public ReactiveProperty<bool> RatingFilterEnabled { get; } = new(false);
    public ReactiveProperty<bool> Rating1Checked { get; } = new(true);
    public ReactiveProperty<bool> Rating2Checked { get; } = new(true);
    public ReactiveProperty<bool> Rating3Checked { get; } = new(true);
    public ReactiveProperty<bool> Rating4Checked { get; } = new(true);
    public ReactiveProperty<bool> Rating5Checked { get; } = new(true);


    #endregion

    #region Marks
    public ReactiveProperty<bool> IsMarkSupported { get; } = new(false);
    public ReactiveProperty<bool> MarkFilterEnabled { get; } = new(false);
    public ReactiveProperty<bool> Mark0Checked { get; } = new(true);
    public ReactiveProperty<bool> Mark1Checked { get; } = new(true);
    public ReactiveProperty<bool> Mark2Checked { get; } = new(true);
    public ReactiveProperty<bool> Mark3Checked { get; } = new(true);

    #endregion

    #region Date Range
    public ReactiveProperty<bool> StartDateEnabled { get; } = new(false);
    public ReactiveProperty<DateTimeOffset> StartDate { get; } = new(DateTime.Now.AddDays(-7));
    public ReactiveProperty<bool> EndDateEnabled { get; } = new(false);
    public ReactiveProperty<DateTimeOffset> EndDate { get; } = new(DateTime.Now);
    #endregion

    #region Text Search
    public ReactiveProperty<string> SearchText { get; } = new(string.Empty);
    #endregion

    public ReadOnlyReactiveProperty<IMediaFilter> Filter { get; }
    private IClientService _clientService;

    public FilterViewModel(IClientService clientService) {
        _clientService = clientService;

        ReadOnlyReactiveProperty<List<int>?> marks = Observable.CombineLatest(
            IsMarkSupported, MarkFilterEnabled, 
            Mark0Checked, Mark1Checked, Mark2Checked, Mark3Checked,
            (isSupported, filterEnabled, m0, m1, m2, m3) => {
                if (isSupported && filterEnabled) {
                    List<int> list = new(capacity: 4);
                    if (m0) list.Add(0);
                    if (m1) list.Add(1);
                    if (m2) list.Add(2);
                    if (m3) list.Add(3);
                    return list;
                }
                else {
                    return null;
                }
            }).ToReadOnlyReactiveProperty();

        ReadOnlyReactiveProperty<List<int>?> ratings = Observable.CombineLatest(
            IsRatingSupported, RatingFilterEnabled,
            Rating1Checked, Rating2Checked, Rating3Checked, Rating4Checked, Rating5Checked,
            (isSupported, filterEnabled, r1, r2, r3, r4, r5) => {
                if (isSupported && filterEnabled) {
                    List<int> list = new(capacity: 5);
                    if (r1) list.Add(1);
                    if (r2) list.Add(2);
                    if (r3) list.Add(3);
                    if (r4) list.Add(4);
                    if (r5) list.Add(5);
                    return list;
                }
                else {
                    return null;
                }
            }).ToReadOnlyReactiveProperty();



        ReadOnlyReactiveProperty<List<MediaType>?> types = Observable.CombineLatest(
            TypeFilterEnabled, TypeVideoChecked, TypeAudioChecked, TypePhotoChecked,
            (filterEnabled, video, audio, photo) => {
                if (filterEnabled) {
                    List<MediaType> list = new();
                    if (video) list.Add(MediaType.VIDEO);
                    if (audio) list.Add(MediaType.AUDIO);
                    if (photo) list.Add(MediaType.PHOTO);
                    return list;
                }
                return null;
            }).ToReadOnlyReactiveProperty();

        Filter = Observable.CombineLatest(
            SelectedCategory, types, ratings, marks, SearchText, StartDateEnabled, StartDate, EndDateEnabled, EndDate,
            (category, typeList, ratingList, markList, searchText, startDateEnabled, startDate, endDateEnabled, endDate) => {
                return new MediaFilter(_clientService) {
                    Category = category,
                    TargetTypes = typeList,
                    Ratings = ratingList,
                    Marks = markList,
                    SearchText = searchText,
                    EnableStartDate = startDateEnabled,
                    StartDate = startDate,
                    EnableEndDate = endDateEnabled,
                    EndDate = endDate
                };
            }).ToReadOnlyReactiveProperty<IMediaFilter>();
    }
}
