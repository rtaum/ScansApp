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
        UpdateControlState();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.CurrentPlaneAImagePath)
            or nameof(MainViewModel.CurrentPlaneBImagePath))
        {
            UpdateDisplayedImages();
        }

        if (e.PropertyName is nameof(MainViewModel.IsScanLoaded)
            or nameof(MainViewModel.IsPlaying)
            or nameof(MainViewModel.ArePlaybackControlsEnabled)
            or nameof(MainViewModel.CurrentImageIndex)
            or nameof(MainViewModel.SelectedScanId))
        {
            UpdateControlState();
        }
    }

    private void UpdateDisplayedImages()
    {
        SetImageSource(PlaneAImage, PlaneAPlaceholder, viewModel.CurrentPlaneAImagePath);
        SetImageSource(PlaneBImage, PlaneBPlaceholder, viewModel.CurrentPlaneBImagePath);
    }

    private void UpdateControlState()
    {
        PlaybackButton.Command = viewModel.IsPlaying ? viewModel.PauseCommand : viewModel.PlayCommand;

        SetSvgSource(PreviousButtonIcon, PreviousButton.IsEnabled ? "ms-appx:///logos/Prev.svg" : "ms-appx:///logos/Prev-disabled.svg");
        SetSvgSource(
            PlaybackButtonIcon,
            viewModel.IsPlaying
                ? (PlaybackButton.IsEnabled ? "ms-appx:///logos/Pause_default.svg" : "ms-appx:///logos/Pause-Disabled.svg")
                : (PlaybackButton.IsEnabled ? "ms-appx:///logos/Play.svg" : "ms-appx:///logos/Play-disabled.svg"));
        SetSvgSource(NextButtonIcon, NextButton.IsEnabled ? "ms-appx:///logos/Next.svg" : "ms-appx:///logos/Next-disabled.svg");
        SetSvgSource(KeyImageButtonIcon, KeyImageButton.IsEnabled ? "ms-appx:///logos/key.svg" : "ms-appx:///logos/key-disabled.svg");
        SetSvgSource(LoadButtonIcon, LoadButton.IsEnabled ? "ms-appx:///logos/Load.svg" : "ms-appx:///logos/load-disabled.svg");
        SetSvgSource(SpeedIcon, SpeedComboBox.IsEnabled ? "ms-appx:///logos/Speed.svg" : "ms-appx:///logos/Speed-disabled.svg");
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

    private static void SetSvgSource(Microsoft.UI.Xaml.Controls.Image image, string uri)
    {
        if (string.Equals(image.Tag as string, uri, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        image.Source = new SvgImageSource(new Uri(uri));
        image.Tag = uri;
    }
}
