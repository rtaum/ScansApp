using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScansApp.Models;
using ScansApp.Services;

namespace ScansApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const string SlowSpeed = "Slow";
    private const string NormalSpeed = "Normal";
    private const string FastSpeed = "Fast";

    private static readonly TimeSpan SlowPlaybackInterval = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan NormalPlaybackInterval = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan FastPlaybackInterval = TimeSpan.FromMilliseconds(100);

    private readonly IScanRepository scanRepository;
    private readonly IPlaybackScheduler playbackScheduler;
    private string? selectedScanId;
    private string selectedSpeed = NormalSpeed;
    private Scan? loadedScan;
    private int currentImageIndex = -1;
    private bool isPlaying;
    private bool hasPlaybackControlsEnabled;
    private double sliderValue;

    public MainViewModel(IScanRepository scanRepository, IPlaybackScheduler playbackScheduler)
    {
        this.scanRepository = scanRepository ?? throw new ArgumentNullException(nameof(scanRepository));
        this.playbackScheduler = playbackScheduler ?? throw new ArgumentNullException(nameof(playbackScheduler));

        foreach (var scanId in scanRepository.GetAvailableScanIds())
        {
            AvailableScanIds.Add(scanId);
        }

        if (AvailableScanIds.Count > 0)
        {
            SelectedScanId = AvailableScanIds[0];
        }
    }

    public ObservableCollection<string> AvailableScanIds { get; } = new();

    public ObservableCollection<string> SpeedOptions { get; } = new()
    {
        SlowSpeed,
        NormalSpeed,
        FastSpeed
    };

    public bool IsPlaying
    {
        get => isPlaying;
        private set
        {
            if (SetProperty(ref isPlaying, value))
            {
                PlayCommand.NotifyCanExecuteChanged();
                PauseCommand.NotifyCanExecuteChanged();
                NextImageCommand.NotifyCanExecuteChanged();
                PreviousImageCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool ArePlaybackControlsEnabled
    {
        get => hasPlaybackControlsEnabled;
        private set
        {
            if (SetProperty(ref hasPlaybackControlsEnabled, value))
            {
                GoToKeyImageCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string SelectedSpeed
    {
        get => selectedSpeed;
        set
        {
            if (SetProperty(ref selectedSpeed, value))
            {
                ApplyPlaybackSpeed();
            }
        }
    }

    public double SliderMaximum => Math.Max(0, (LoadedScan?.ImageCount ?? 0) - 1);

    public double SliderValue
    {
        get => sliderValue;
        set
        {
            var clampedValue = Math.Clamp(value, 0, SliderMaximum);
            if (SetProperty(ref sliderValue, clampedValue))
            {
                SetCurrentImageIndex((int)Math.Round(clampedValue, MidpointRounding.AwayFromZero));
            }
        }
    }

    public string? SelectedScanId
    {
        get => selectedScanId;
        set
        {
            if (SetProperty(ref selectedScanId, value))
            {
                LoadScanCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public Scan? LoadedScan
    {
        get => loadedScan;
        private set
        {
            if (SetProperty(ref loadedScan, value))
            {
                OnPropertyChanged(nameof(IsScanLoaded));
                OnPropertyChanged(nameof(CurrentPlaneAImagePath));
                OnPropertyChanged(nameof(CurrentPlaneBImagePath));
                OnPropertyChanged(nameof(SliderMaximum));
                PlayCommand.NotifyCanExecuteChanged();
                PauseCommand.NotifyCanExecuteChanged();
                GoToKeyImageCommand.NotifyCanExecuteChanged();
                NextImageCommand.NotifyCanExecuteChanged();
                PreviousImageCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsScanLoaded => LoadedScan is not null;

    public int CurrentImageIndex
    {
        get => currentImageIndex;
        private set
        {
            if (SetProperty(ref currentImageIndex, value))
            {
                SetProperty(ref sliderValue, value, nameof(SliderValue));
                OnPropertyChanged(nameof(CurrentPlaneAImagePath));
                OnPropertyChanged(nameof(CurrentPlaneBImagePath));
                GoToKeyImageCommand.NotifyCanExecuteChanged();
                NextImageCommand.NotifyCanExecuteChanged();
                PreviousImageCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string? CurrentPlaneAImagePath => GetCurrentImagePath(LoadedScan?.PlaneAImages);

    public string? CurrentPlaneBImagePath => GetCurrentImagePath(LoadedScan?.PlaneBImages);

    [RelayCommand(CanExecute = nameof(CanLoadScan))]
    private void LoadScan()
    {
        StopPlayback();
        LoadedScan = scanRepository.LoadScan(SelectedScanId!);
        CurrentImageIndex = LoadedScan.KeyImageIndex;
        ArePlaybackControlsEnabled = false;
    }

    [RelayCommand(CanExecute = nameof(CanPlay))]
    private void Play()
    {
        if (!CanPlay())
        {
            return;
        }

        if (!ArePlaybackControlsEnabled)
        {
            CurrentImageIndex = 0;
        }

        ArePlaybackControlsEnabled = true;
        IsPlaying = true;
        playbackScheduler.Start(GetPlaybackInterval(), AdvancePlayback);
    }

    [RelayCommand(CanExecute = nameof(CanPause))]
    private void Pause()
    {
        if (!CanPause())
        {
            return;
        }

        StopPlayback();
    }

    [RelayCommand(CanExecute = nameof(CanMoveNext))]
    private void NextImage()
    {
        if (!CanMoveNext())
        {
            return;
        }

        CurrentImageIndex++;
    }

    [RelayCommand(CanExecute = nameof(CanMovePrevious))]
    private void PreviousImage()
    {
        if (!CanMovePrevious())
        {
            return;
        }

        CurrentImageIndex--;
    }

    [RelayCommand(CanExecute = nameof(CanGoToKeyImage))]
    private void GoToKeyImage()
    {
        if (!CanGoToKeyImage())
        {
            return;
        }

        CurrentImageIndex = LoadedScan!.KeyImageIndex;
    }

    private bool CanPlay() => LoadedScan is not null && !IsPlaying;

    private bool CanLoadScan() => !string.IsNullOrWhiteSpace(SelectedScanId);

    private bool CanPause() => IsPlaying;

    private bool CanGoToKeyImage() => ArePlaybackControlsEnabled && LoadedScan is not null && CurrentImageIndex != LoadedScan.KeyImageIndex;

    private bool CanMoveNext() => !IsPlaying && LoadedScan is not null && CurrentImageIndex < LoadedScan.ImageCount - 1;

    private bool CanMovePrevious() => !IsPlaying && LoadedScan is not null && CurrentImageIndex > 0;

    private void AdvancePlayback()
    {
        if (LoadedScan is null || LoadedScan.ImageCount == 0)
        {
            return;
        }

        CurrentImageIndex = (CurrentImageIndex + 1) % LoadedScan.ImageCount;
    }

    private void StopPlayback()
    {
        playbackScheduler.Stop();
        IsPlaying = false;
    }

    private void ApplyPlaybackSpeed()
    {
        if (IsPlaying)
        {
            playbackScheduler.Start(GetPlaybackInterval(), AdvancePlayback);
        }
    }

    private TimeSpan GetPlaybackInterval()
    {
        return SelectedSpeed switch
        {
            SlowSpeed => SlowPlaybackInterval,
            FastSpeed => FastPlaybackInterval,
            _ => NormalPlaybackInterval
        };
    }

    private void SetCurrentImageIndex(int imageIndex)
    {
        if (LoadedScan is null || LoadedScan.ImageCount == 0)
        {
            return;
        }

        CurrentImageIndex = Math.Clamp(imageIndex, 0, LoadedScan.ImageCount - 1);
    }

    private string? GetCurrentImagePath(IReadOnlyList<string>? images)
    {
        if (images is null || CurrentImageIndex < 0 || CurrentImageIndex >= images.Count)
        {
            return null;
        }

        return images[CurrentImageIndex];
    }
}
