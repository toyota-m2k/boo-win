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
public sealed partial class PhotoViewer : UserControl {
    #region ViewModel DependencyProperty
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(PhotoViewerViewModel),
            typeof(PhotoViewer),
            new PropertyMetadata(null, OnViewModelChanged));

    public PhotoViewerViewModel? ViewModel {
        get => (PhotoViewerViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }
    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        //if (d is PhotoViewer view) {
        //}
    }
    #endregion


    public PhotoViewer() {
        InitializeComponent();
    }

    #region Show/Hide Control Panel

    private void OnControlPanelMouseEnter(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {
        ViewModel?.OnControlPanelMouseEnter();
        e.Handled = true;
    }

    private void OnControlPanelMouseLeave(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {
        ViewModel?.OnControlPanelMouseLeave();
        e.Handled = true;
    }

    private void OnPointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {
        ViewModel?.ActivateControlPanel();
        e.Handled = true;
    }

    private void OnControlPanelMouseMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) {
        e.Handled = true;
    }

    #endregion

}
