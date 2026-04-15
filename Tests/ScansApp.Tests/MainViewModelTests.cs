using System;
using System.IO;
using System.Linq;
using ScansApp.Services;
using ScansApp.ViewModels;
using Xunit;

namespace ScansApp.Tests;

public sealed class MainViewModelTests : IDisposable
{
    private readonly string scansRoot;

    public MainViewModelTests()
    {
        scansRoot = Path.Combine(Path.GetTempPath(), "ScansApp.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scansRoot);
    }

    [Fact]
    public void LoadScanCommand_LoadsSelectedScanFromScansFolder()
    {
        CreateScan("200002", planeAImageCount: 2, planeBImageCount: 2);
        CreateScan("100001", planeAImageCount: 3, planeBImageCount: 3);

        var repository = new FileSystemScanRepository(scansRoot);
        var viewModel = new MainViewModel(repository);

        Assert.Equal(new[] { "100001", "200002" }, viewModel.AvailableScanIds.ToArray());
        Assert.Equal("100001", viewModel.SelectedScanId);
        Assert.False(viewModel.IsScanLoaded);

        viewModel.LoadScanCommand.Execute(null);

        Assert.True(viewModel.IsScanLoaded);
        Assert.NotNull(viewModel.LoadedScan);
        Assert.Equal("100001", viewModel.LoadedScan!.Id);
        Assert.Equal(3, viewModel.LoadedScan.PlaneAImages.Count);
        Assert.Equal(3, viewModel.LoadedScan.PlaneBImages.Count);
    }

    [Fact]
    public void LoadScanCommand_SelectsKeyImagesForBothPlanes()
    {
        CreateScan("100001", planeAImageCount: 5, planeBImageCount: 5);

        var repository = new FileSystemScanRepository(scansRoot);
        var viewModel = new MainViewModel(repository);

        viewModel.LoadScanCommand.Execute(null);

        Assert.Equal(2, viewModel.CurrentImageIndex);
        Assert.EndsWith(@"Plane-A\image_002.png", viewModel.CurrentPlaneAImagePath);
        Assert.EndsWith(@"Plane-B\image_002.png", viewModel.CurrentPlaneBImagePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(scansRoot))
        {
            Directory.Delete(scansRoot, recursive: true);
        }
    }

    private void CreateScan(string scanId, int planeAImageCount, int planeBImageCount)
    {
        var scanDirectory = Path.Combine(scansRoot, scanId);
        var planeADirectory = Path.Combine(scanDirectory, "Plane-A");
        var planeBDirectory = Path.Combine(scanDirectory, "Plane-B");

        Directory.CreateDirectory(planeADirectory);
        Directory.CreateDirectory(planeBDirectory);

        for (var i = 0; i < planeAImageCount; i++)
        {
            File.WriteAllText(Path.Combine(planeADirectory, $"image_{i:000}.png"), string.Empty);
        }

        for (var i = 0; i < planeBImageCount; i++)
        {
            File.WriteAllText(Path.Combine(planeBDirectory, $"image_{i:000}.png"), string.Empty);
        }
    }
}
