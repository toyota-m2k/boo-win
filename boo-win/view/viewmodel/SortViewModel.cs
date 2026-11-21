using BooWin.DI.contract;
using BooWin.model;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;

namespace BooWin.view.viewmodel;

public class SortViewModel {
    public ReactiveProperty<bool> IsEnabled { get; } = new(false);
    public ReactiveProperty<MediaSorter.Key> SortKey { get; } = new(MediaSorter.Key.Date);
    public ReactiveProperty<bool> Descending { get; } = new(true);
    public ReadOnlyReactiveProperty<bool> Ascending { get; }
    public static readonly List<MediaSorter.Key> AllSortKeys = new List<MediaSorter.Key>(Enum.GetValues<MediaSorter.Key>());
    public ReactiveProperty<List<MediaSorter.Key>> AvailableSortKeys { get; } = new(AllSortKeys);
    private MediaSorter.Key PreviousKey = MediaSorter.Key.Date;
    private MediaSorter.Key SecondaryKey = MediaSorter.Key.Date;


    public ReadOnlyReactiveProperty<IMediaSorter> Sorter { get; }
    private IClientService _clientService;
    public SortViewModel(IClientService clientService) {
        _clientService = clientService;
        Ascending = Descending.Select(d => !d).ToReadOnlyReactiveProperty();
        Sorter = Observable.CombineLatest(
            IsEnabled,
            AvailableSortKeys,
            SortKey,
            Descending,
            (isEnabled, availableKeys, sortKey, descending) => {
                if (!isEnabled) {
                    return MediaSorter.EmptySorter;
                }
                SecondaryKey = PreviousKey;
                if (PreviousKey != sortKey) {
                    PreviousKey = sortKey;
                }
                return new MediaSorter {
                    PrimarySortKey = sortKey,
                    SecondarySortKey = SecondaryKey,
                    Descending = descending
                };
            }
        ).ToReadOnlyReactiveProperty<IMediaSorter>();
    }

    /// <summary>
    /// ListView.SelectedItem への順方向バインド用 (BindBack を使うため関数バインディングにする)。
    /// </summary>
    public object ToSelectedItem(MediaSorter.Key key) => key;

    /// <summary>
    /// ListView.SelectedItem からの BindBack 用。
    /// ItemsSource 差し替え時に SelectedItem が一時的に null になるため、null は無視する。
    /// </summary>
    public void SetSortKey(object? item) {
        if (item is MediaSorter.Key key) {
            SortKey.Value = key;
        }
    }

    public SorterSettings AsSettings() {
        return new SorterSettings {
            SortEnabled = IsEnabled.Value,
            PrimarySortKey = SortKey.Value,
            SecondarySortKey = SecondaryKey,
        };
    }

    public void ResetOnHostChanged(HostAdditionalInfo additionals) {
        AvailableSortKeys.Value = AllSortKeys.Where(k => {
            switch (k) {
                case MediaSorter.Key.Category:
                    return _clientService.CategorySupport.IsSupported;
                case MediaSorter.Key.Rating:
                    return _clientService.RatingSupport.IsSupported;
                case MediaSorter.Key.Mark:
                    return _clientService.MarkSupport.IsSupported;
                default:
                    return true;
            }
        }).ToList();

        PreviousKey = additionals.Sorter.SecondarySortKey;
        SortKey.Value = additionals.Sorter.PrimarySortKey;
        Descending.Value = additionals.Sorter.Descending;
    }
}

