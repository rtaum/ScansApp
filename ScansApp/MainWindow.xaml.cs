using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using ScansApp.ViewModels;
using System;
using System.ComponentModel;

namespace ScansApp;

public sealed partial class MainWindow : Window
{
    private readonly MainViewModel viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        Root.DataContext = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        UpdateDisplayedImages();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.CurrentPlaneAImagePath)
            or nameof(MainViewModel.CurrentPlaneBImagePath)
            or nameof(MainViewModel.IsScanLoaded))
        {
            UpdateDisplayedImages();
        }
    }

    private void UpdateDisplayedImages()
    {
        SetImageSource(PlaneAImage, PlaneAPlaceholder, viewModel.CurrentPlaneAImagePath);
        SetImageSource(PlaneBImage, PlaneBPlaceholder, viewModel.CurrentPlaneBImagePath);
    }

    private static void SetImageSource(Microsoft.UI.Xaml.Controls.Image image, Microsoft.UI.Xaml.Controls.TextBlock placeholder, string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            image.Source = null;
            image.Visibility = Visibility.Collapsed;
            placeholder.Visibility = Visibility.Visible;
            return;
        }

        image.Source = new BitmapImage(new Uri(imagePath));
        image.Visibility = Visibility.Visible;
        placeholder.Visibility = Visibility.Collapsed;
    }
}
