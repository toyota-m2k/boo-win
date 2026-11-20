using BooWin.DI.contract;
using BooWin.view.viewmodel;
using Microsoft.UI.Xaml.Controls;
using Reactive.Bindings;
using System;
using System.Threading.Tasks;

namespace BooWin.view.dialog;

public sealed partial class DateRangeDialog : ContentDialog {
    ReactiveProperty<bool> StartDateEnabled { get; }
    ReactiveProperty<DateTimeOffset?> StartDate { get; }
    ReactiveProperty<bool> EndDateEnabled { get; }
    ReactiveProperty<DateTimeOffset?> EndDate { get; }

    private DateRangeDialog(FilterViewModel filter) {
        StartDateEnabled = new(filter.StartDateEnabled.Value);
        StartDate = new(filter.StartDate.Value);
        EndDateEnabled = new(filter.EndDateEnabled.Value);
        EndDate = new(filter.EndDate.Value);
        InitializeComponent();
    }

    private void ApplyTo(FilterViewModel filter) {
        if (StartDate.Value is DateTimeOffset start) {
            filter.StartDate.Value = start;
        }
        if (EndDate.Value is DateTimeOffset end) {
            filter.EndDate.Value = end;
        }
        filter.StartDateEnabled.Value = StartDateEnabled.Value && StartDate.Value != null;
        filter.EndDateEnabled.Value = EndDateEnabled.Value && EndDate.Value != null;
    }

    public static async Task<bool> Show(IDialogService dialogService, FilterViewModel filter) {
        var dialog = new DateRangeDialog(filter);
        var result = await dialogService
            .Builder(dialog)
            .ShowOkCancel();
        if (result) {
            dialog.ApplyTo(filter);
        }
        return result;
    }
}
