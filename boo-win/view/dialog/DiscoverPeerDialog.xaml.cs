using BooWin.DI.contract;
using BooWin.model;
using BooWin.view.viewmodel;
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
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BooWin.view.dialog;

public sealed partial class DiscoverPeerDialog : ContentDialog {
    DiscoverPeerDialogViewModel ViewModel { get; }
    public DiscoverPeerDialog() {
        ViewModel = App.GetService<DiscoverPeerDialogViewModel>();
        InitializeComponent();
        ViewModel.Discover();
    }

    public static async Task<PeerHost?> Discover(IDialogService dialogService) {
        var dialog = new DiscoverPeerDialog();
        try {
            var result = await dialogService
                .Builder(dialog)
                .ShowAsync();
            if (result == ContentDialogResult.Primary) {
                return dialog.ViewModel.SelectedPeer.Value;
            }
            return null;
        } finally {
            dialog.ViewModel.Dispose(); // MDNS Browser を終了
        }
    }
}
