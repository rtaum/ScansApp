using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScansApp.Models;
using ScansApp.Services;

namespace ScansApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IScanRepository scanRepository;
    private string? selectedScanId;
    private Scan? loadedScan;
    private int currentImageIndex = -1;

    public MainViewModel(IScanRepository scanRepository)
    {
        this.scanRepository = scanRepository ?? throw new ArgumentNullException(nameof(scanRepository));

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
                OnPropertyChanged(nameof(CurrentPlaneAImagePath));
                OnPropertyChanged(nameof(CurrentPlaneBImagePath));
            }
        }
    }

    public string? CurrentPlaneAImagePath => GetCurrentImagePath(LoadedScan?.PlaneAImages);

    public string? CurrentPlaneBImagePath => GetCurrentImagePath(LoadedScan?.PlaneBImages);

    [RelayCommand(CanExecute = nameof(CanLoadScan))]
    private void LoadScan()
    {
        LoadedScan = scanRepository.LoadScan(SelectedScanId!);
        CurrentImageIndex = LoadedScan.KeyImageIndex;
    }

    private bool CanLoadScan() => !string.IsNullOrWhiteSpace(SelectedScanId);

    private string? GetCurrentImagePath(IReadOnlyList<string>? images)
    {
        if (images is null || CurrentImageIndex < 0 || CurrentImageIndex >= images.Count)
        {
            return null;
        }

        return images[CurrentImageIndex];
    }
}
