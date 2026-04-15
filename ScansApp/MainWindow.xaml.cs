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

        var previousEnabled = viewModel.PreviousImageCommand.CanExecute(null);
        var playbackEnabled = (viewModel.IsPlaying ? viewModel.PauseCommand : viewModel.PlayCommand).CanExecute(null);
        var nextEnabled = viewModel.NextImageCommand.CanExecute(null);
        var keyImageEnabled = viewModel.GoToKeyImageCommand.CanExecute(null);
        var speedEnabled = viewModel.ArePlaybackControlsEnabled;
        var loadEnabled = viewModel.LoadScanCommand.CanExecute(null);

        SetSvgSource(PreviousButtonIcon, previousEnabled ? "ms-appx:///logos/Prev.svg" : "ms-appx:///logos/Prev-disabled.svg");
        SetSvgSource(
            PlaybackButtonIcon,
            viewModel.IsPlaying
                ? (playbackEnabled ? "ms-appx:///logos/Pause_default.svg" : "ms-appx:///logos/Pause-Disabled.svg")
                : (playbackEnabled ? "ms-appx:///logos/Play.svg" : "ms-appx:///logos/Play-disabled.svg"));
        SetSvgSource(NextButtonIcon, nextEnabled ? "ms-appx:///logos/Next.svg" : "ms-appx:///logos/Next-disabled.svg");
        SetSvgSource(KeyImageButtonIcon, keyImageEnabled ? "ms-appx:///logos/key.svg" : "ms-appx:///logos/key-disabled.svg");
        SetSvgSource(LoadButtonIcon, loadEnabled ? "ms-appx:///logos/Load.svg" : "ms-appx:///logos/load-disabled.svg");
        SetSvgSource(SpeedIcon, speedEnabled ? "ms-appx:///logos/Speed.svg" : "ms-appx:///logos/Speed-disabled.svg");
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
