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
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BooWin.view.parts; 

public sealed partial class EditorPanel : UserControl {
    #region ViewModel DependencyProperty
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(EditorWindowViewModel),
            typeof(EditorPanel),
            new PropertyMetadata(null, OnViewModelChanged));

    public EditorWindowViewModel? ViewModel {
        get => (EditorWindowViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }
    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        //if (d is EditorPanel view) {
        //}
    }
    #endregion

    public EditorPanel() {
        InitializeComponent();
    }

    private void OnBitRateTextSubmitted(ComboBox sender, ComboBoxTextSubmittedEventArgs args) {
        args.Handled = true;
        var model = ViewModel?.ConvertModel;
        if (model == null) return;
        var text = args.Text.Trim().ToLowerInvariant().Replace("mbps", "").Replace("m", "").Trim();
        if (double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) && v > 0) {
            // 1000未満はMbps、それ以上はbpsとみなす
            var bps = v < 1000 ? (int)Math.Round(v * 1_000_000) : (int)Math.Round(v);
            model.MaxBitRate.Value = bps;
        }
        sender.SelectedItem = model.SelectedBitRate.Value;
        sender.Text = model.SelectedBitRate.Value?.Label ?? string.Empty;
    }
}
