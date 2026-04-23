using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using ScansApp.Models;
using ScansApp.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using WinRT.Interop;

namespace ScansApp;

public sealed partial class MainWindow : Window
{
    private const int ImageTransitionDurationMilliseconds = 120;
    private readonly MainViewModel viewModel;

    // Cache of already-decoded BitmapImage objects, keyed by absolute file path.
    // Once a BitmapImage fires ImageOpened it is fully decoded in memory and can
    // be set as Image.Source without any intermediate blank/gray frame.
    private readonly Dictionary<string, BitmapImage> imageCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly PlaneImagePresenter planeAPresenter;
    private readonly PlaneImagePresenter planeBPresenter;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        planeAPresenter = new PlaneImagePresenter(PlaneAImagePrimary, PlaneAImageSecondary, PlaneAPlaceholder);
        planeBPresenter = new PlaneImagePresenter(PlaneBImagePrimary, PlaneBImageSecondary, PlaneBPlaceholder);
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

        // When a new scan is loaded (IsScanLoaded transitions to true) pre-warm the
        // cache so that the first few frames are ready before playback starts.
        if (e.PropertyName is nameof(MainViewModel.IsScanLoaded) && viewModel.IsScanLoaded)
        {
            PrewarmCache();
        }
    }

    private void UpdateDisplayedImages()
    {
        SetImageSource(planeAPresenter, viewModel.CurrentPlaneAImagePath);
        SetImageSource(planeBPresenter, viewModel.CurrentPlaneBImagePath);
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

        SetSvgSource(PreviousButtonIcon, previousEnabled ? "ms-appx:///Assets/Icons/Prev.svg" : "ms-appx:///Assets/Icons/Prev-disabled.svg");
        SetSvgSource(
            PlaybackButtonIcon,
            viewModel.IsPlaying
                ? (playbackEnabled ? "ms-appx:///Assets/Icons/Pause_default.svg" : "ms-appx:///Assets/Icons/Pause-Disabled.svg")
                : (playbackEnabled ? "ms-appx:///Assets/Icons/Play.svg" : "ms-appx:///Assets/Icons/Play-disabled.svg"));
        SetSvgSource(NextButtonIcon, nextEnabled ? "ms-appx:///Assets/Icons/Next.svg" : "ms-appx:///Assets/Icons/Next-disabled.svg");
        SetSvgSource(KeyImageButtonIcon, keyImageEnabled ? "ms-appx:///Assets/Icons/key.svg" : "ms-appx:///Assets/Icons/key-disabled.svg");
        SetSvgSource(LoadButtonIcon, loadEnabled ? "ms-appx:///Assets/Icons/Load.svg" : "ms-appx:///Assets/Icons/load-disabled.svg");
        SetSvgSource(SpeedIcon, speedEnabled ? "ms-appx:///Assets/Icons/Speed.svg" : "ms-appx:///Assets/Icons/Speed-disabled.svg");
    }

    public void Maximize()
    {
        var windowHandle = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        // Use a normal maximized window so the app gets plenty of room while
        // still keeping the standard caption buttons visible.
        if (appWindow.Presenter is OverlappedPresenter overlappedPresenter)
        {
            overlappedPresenter.Maximize();
        }
    }

    // ---------------------------------------------------------------------------
    // Image caching helpers
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Returns a <see cref="BitmapImage"/> for <paramref name="path"/>.
    /// If one already exists in the cache it is returned immediately (already decoded).
    /// Otherwise a new one is created, added to the cache, and returned; the caller
    /// should wait for <see cref="BitmapImage.ImageOpened"/> before using it as a
    /// live <c>Image.Source</c> to avoid a blank frame.
    /// </summary>
    private BitmapImage GetOrCreateBitmap(string path)
    {
        if (imageCache.TryGetValue(path, out var cached))
            return cached;

        var bmp = new BitmapImage(new Uri(path));
        imageCache[path] = bmp;
        return bmp;
    }

    /// <summary>
    /// Pre-load (decode) every image in the loaded scan so that the cache is warm
    /// before the user starts playback.  Done lazily — we just create the
    /// BitmapImage objects; WinUI will decode them in the background.
    /// </summary>
    private void PrewarmCache()
    {
        var scan = viewModel.LoadedScan;
        if (scan is null) return;

        // Drop cached bitmaps from the previous series so both presenters preload
        // only the currently selected scan.
        imageCache.Clear();

        foreach (var path in scan.PlaneAImages)
            GetOrCreateBitmap(path);

        foreach (var path in scan.PlaneBImages)
            GetOrCreateBitmap(path);
    }

    private void SetImageSource(PlaneImagePresenter presenter, string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            presenter.Clear();
            return;
        }

        if (string.Equals(presenter.PendingPath, imagePath, StringComparison.OrdinalIgnoreCase)
            || string.Equals(presenter.CurrentPath, imagePath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        presenter.PendingPath = imagePath;

        var bmp = GetOrCreateBitmap(imagePath);

        presenter.PrepareBitmap(bmp);

        // If the bitmap is already decoded (PixelWidth > 0) assign it directly —
        // no blank frame will appear because the data is already in memory.
        if (bmp.PixelWidth > 0)
        {
            presenter.ShowBitmap(imagePath, bmp, animate: presenter.HasVisibleImage);
            return;
        }

        // Bitmap is still loading. Wait for ImageOpened before swapping the source
        // so the previous frame stays visible until the new one is ready.
        void OnOpened(object? s, RoutedEventArgs _)
        {
            bmp.ImageOpened -= OnOpened;
            if (string.Equals(presenter.PendingPath, imagePath, StringComparison.OrdinalIgnoreCase))
            {
                presenter.ShowBitmap(imagePath, bmp, animate: presenter.HasVisibleImage);
            }
        }

        bmp.ImageOpened += OnOpened;
    }

    private static void SetSvgSource(Image image, string uri)
    {
        if (string.Equals(image.Tag as string, uri, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        image.Source = new SvgImageSource(new Uri(uri));
        image.Tag = uri;
    }

    private sealed class PlaneImagePresenter
    {
        private readonly Image primaryImage;
        private readonly Image secondaryImage;
        private readonly TextBlock placeholder;
        private bool isPrimaryActive = true;

        public PlaneImagePresenter(Image primaryImage, Image secondaryImage, TextBlock placeholder)
        {
            this.primaryImage = primaryImage;
            this.secondaryImage = secondaryImage;
            this.placeholder = placeholder;
        }

        public string? CurrentPath { get; private set; }

        public string? PendingPath { get; set; }

        public bool HasVisibleImage => primaryImage.Visibility == Visibility.Visible || secondaryImage.Visibility == Visibility.Visible;

        public void Clear()
        {
            PendingPath = null;
            CurrentPath = null;
            primaryImage.Source = null;
            secondaryImage.Source = null;
            primaryImage.Opacity = 1;
            secondaryImage.Opacity = 1;
            primaryImage.Visibility = Visibility.Collapsed;
            secondaryImage.Visibility = Visibility.Collapsed;
            placeholder.Visibility = Visibility.Visible;
        }

        public void ShowBitmap(string path, BitmapImage bitmap, bool animate)
        {
            PendingPath = path;

            if (!animate)
            {
                primaryImage.Source = bitmap;
                primaryImage.Opacity = 1;
                primaryImage.Visibility = Visibility.Visible;
                secondaryImage.Source = null;
                secondaryImage.Opacity = 1;
                secondaryImage.Visibility = Visibility.Collapsed;
                isPrimaryActive = true;
                placeholder.Visibility = Visibility.Collapsed;
                CurrentPath = path;
                return;
            }

            var incoming = isPrimaryActive ? secondaryImage : primaryImage;
            var outgoing = isPrimaryActive ? primaryImage : secondaryImage;

            incoming.Source = bitmap;
            incoming.Opacity = 0;
            incoming.Visibility = Visibility.Visible;
            placeholder.Visibility = Visibility.Collapsed;

            StartOpacityAnimation(incoming, 1, completed: null);
            StartOpacityAnimation(outgoing, 0, () =>
            {
                outgoing.Source = null;
                outgoing.Visibility = Visibility.Collapsed;
                outgoing.Opacity = 1;
            });

            isPrimaryActive = !isPrimaryActive;
            CurrentPath = path;
        }

        public void PrepareBitmap(BitmapImage bitmap)
        {
            var incoming = isPrimaryActive ? secondaryImage : primaryImage;

            if (ReferenceEquals(incoming.Source, bitmap))
            {
                return;
            }

            incoming.Source = bitmap;
            incoming.Opacity = 0;
            incoming.Visibility = Visibility.Collapsed;
        }

        private static void StartOpacityAnimation(UIElement element, double to, Action? completed)
        {
            var animation = new DoubleAnimation
            {
                To = to,
                Duration = new Duration(TimeSpan.FromMilliseconds(ImageTransitionDurationMilliseconds))
            };

            if (completed is not null)
            {
                void OnCompleted(object? sender, object e)
                {
                    animation.Completed -= OnCompleted;
                    completed();
                }

                animation.Completed += OnCompleted;
            }

            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            Storyboard.SetTarget(animation, element);
            Storyboard.SetTargetProperty(animation, nameof(UIElement.Opacity));
            storyboard.Begin();
        }
    }
}
