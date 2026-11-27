using BooWin.DI.contract;
using BooWin.view.viewmodel;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BooWin; 
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window {
    public Frame RootFrame => _rootFrame;
    private MainWindowViewModel ViewModel { get; }

    public MainWindow() {
        ViewModel = App.GetService<MainWindowViewModel>();
        InitializeComponent();
        ViewModel.WindowTitle.Subscribe(title => {
            this.Title = title;
        });
        AppWindow.Closing += AppWindow_Closing;
    }

    private bool _closeConfirmed = false;
    private async void ConfirmClose() {
        var r = await App.GetService<IDialogService>()
            .ShowYesNo("Confirm Exit", "Are you sure you want to exit?", yesAsDefault: true);
        if (r == true) {
            _closeConfirmed = true;
            Close();
        }
    }

    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args) {
        App.GetService<EntryPageViewModel>().Save();
        App.GetService<PlayerPageViewModel>().Save();
        App.GetService<ISettingsService>().Save();

        if (App.GetService<IAppConfigService>().NeedsConfirmOnExit && !_closeConfirmed) {
            args.Cancel = true;
            ConfirmClose();
        }
    }

    #region Drag & Drop

    private void OnDragOver(object sender, DragEventArgs e) {
        if (e.DataView.Contains(StandardDataFormats.StorageItems)) {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "Open in Editor";
        } else {
            e.AcceptedOperation = DataPackageOperation.None;
        }
    }

    private async void OnDrop(object sender, DragEventArgs e) {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;
        var deferral = e.GetDeferral();
        List<string> paths;
        try {
            var items = await e.DataView.GetStorageItemsAsync();
            paths = items
                .OfType<IStorageFile>()
                .Select(it => it.Path)
                .Where(it => !string.IsNullOrEmpty(it) && App.IsEditableFile(it))
                .ToList();
        } finally {
            deferral.Complete();
        }
        if (paths.Count == 0) {
            await App.GetService<IDialogService>().ShowMessage("Open in Editor", "No supported media file was dropped.");
            return;
        }
        await App.OpenMediaFilesAsync(paths);
    }

    #endregion
}
