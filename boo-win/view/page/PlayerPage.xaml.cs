using BooWin.model;
using BooWin.view.viewmodel;
using io.github.toyota32k.BooLib.Utils;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BooWin.view.page;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class PlayerPage : Page {
    public PlayerPageViewModel ViewModel { get; }
    public PlayerPage() {
        ViewModel = App.GetService<PlayerPageViewModel>();
        InitializeComponent();
    }

    private void OnEscapeInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args) {
        if (ViewModel.IsPlayerOnly.Value) {
            ViewModel.IsPlayerOnly.Value = false;
            args.Handled = true;
        }
    }
    private DisposableCollection Disposer = new DisposableCollection();

    protected override void OnNavigatedFrom(NavigationEventArgs e) {
        base.OnNavigatedFrom(e);
        ViewModel.Save();
    }
    protected override void OnNavigatedTo(NavigationEventArgs e) {
        base.OnNavigatedTo(e);
        _ = ViewModel.Prepare();

        Disposer.Add(
            ViewModel.SelectedItem.Subscribe(item => {
                if (item!=null) { 
                    PlaylistView.ScrollIntoView(item);
                }
            }));
        
    }

    private async void OnDateRangeClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) {
        await dialog.DateRangeDialog.Show(App.GetService<DI.contract.IDialogService>(), ViewModel.FilterModel);
    }

    private void OnCategorySelectionChanged(object sender, SelectionChangedEventArgs e) {
        categoryFlyout.Hide();
    }

    private void OnSortKeySelectionChanged(object sender, SelectionChangedEventArgs e) {
        sortKeyFlyout.Hide();
    }

    private void OnSlideShowRightTapped(object sender, RightTappedRoutedEventArgs e) {
        e.Handled = true;
        _ = ShowSlideShowIntervalDialog();
    }

    private void OnSlideShowHolding(object sender, HoldingRoutedEventArgs e) {
        // 長押し開始時のみ反応する
        if (e.HoldingState != Microsoft.UI.Input.HoldingState.Started) return;
        e.Handled = true;
        _ = ShowSlideShowIntervalDialog();
    }

    private bool _isIntervalDialogShowing = false;

    private async System.Threading.Tasks.Task ShowSlideShowIntervalDialog() {
        if (_isIntervalDialogShowing) return;
        _isIntervalDialogShowing = true;
        try {
            var numberBox = new NumberBox {
                Header = "Interval (seconds)",
                Minimum = 1,
                Maximum = 3600,
                SmallChange = 1,
                LargeChange = 10,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                Value = Math.Max(1, Math.Round(ViewModel.SlideShowInterval.Value.TotalSeconds)),
            };
            var ok = await App.GetService<DI.contract.IDialogService>()
                .Builder()
                .Title("Slide Show Interval")
                .Content(numberBox)
                .ShowOkCancel();
            if (ok && !double.IsNaN(numberBox.Value)) {
                var seconds = Math.Clamp(numberBox.Value, numberBox.Minimum, numberBox.Maximum);
                ViewModel.SlideShowInterval.Value = TimeSpan.FromSeconds(seconds);
            }
        }
        finally {
            _isIntervalDialogShowing = false;
        }
    }
}
