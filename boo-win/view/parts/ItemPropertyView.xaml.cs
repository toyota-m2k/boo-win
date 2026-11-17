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
public sealed partial class ItemPropertyView : UserControl {
    #region ViewModel DependencyProperty
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(PlayerPageViewModel),
            typeof(ItemPropertyView),
            new PropertyMetadata(null, OnViewModelChanged));

    public PlayerPageViewModel? ViewModel {
        get => (PlayerPageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }
    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        // 必要ならイベント購読の付け替えなどをここで行う
        //if (e.NewValue is PlayerPageViewModel viewModel && d is ItemPropertyView propertyView) {
        //    propertyView.OnViewModelChanged(viewModel);
        //}
    }
    #endregion


    public ItemPropertyView() {
        InitializeComponent();
    }

    private void OnCategorySelectionChanged(object sender, SelectionChangedEventArgs e) {
        categoryFlyout.Hide();
    }
}
