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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BooWin.view.dialog;

public sealed partial class EditHostDialog : ContentDialog
{
    EditHostDialogViewModel ViewModel { get; }
    public EditHostDialog(EditHostDialogViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    private static async Task<PeerHost?> Show(IDialogService dialogService, PeerHost? host) {
        var viewModel = new EditHostDialogViewModel(host);
        var dialog = new EditHostDialog(viewModel);
        var result = await dialogService
            .Builder(dialog)
            .ShowOkCancel();
        if (result) {
            return viewModel.GetResult();
        }
        else {
            return null;
        }
    }


    public static async Task<PeerHost?> NewHost(IDialogService dialogService) {
        return await Show(dialogService, null);
    }

    public static async Task<PeerHost?> EditHost(IDialogService dialogService, PeerHost host) {
        return await Show(dialogService, host);
    }

    private void CheckAddress(TextBox sender, TextBoxBeforeTextChangingEventArgs args) {
        args.Cancel = args.NewText.Any(c => !char.IsAsciiDigit(c) && c != '.');
    }
}
