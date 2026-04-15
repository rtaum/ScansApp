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
    private readonly FakePlaybackScheduler playbackScheduler = new();

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
        var viewModel = new MainViewModel(repository, playbackScheduler);

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
        var viewModel = new MainViewModel(repository, playbackScheduler);

        viewModel.LoadScanCommand.Execute(null);

        Assert.Equal(2, viewModel.CurrentImageIndex);
        Assert.EndsWith(@"Plane-A\image_002.png", viewModel.CurrentPlaneAImagePath);
        Assert.EndsWith(@"Plane-B\image_002.png", viewModel.CurrentPlaneBImagePath);
    }

    [Fact]
    public void NextAndPreviousCommands_MoveBothPlanesTogether()
    {
        CreateScan("100001", planeAImageCount: 5, planeBImageCount: 5);

        var repository = new FileSystemScanRepository(scansRoot);
        var viewModel = new MainViewModel(repository, playbackScheduler);

        viewModel.LoadScanCommand.Execute(null);

        viewModel.NextImageCommand.Execute(null);

        Assert.Equal(3, viewModel.CurrentImageIndex);
        Assert.EndsWith(@"Plane-A\image_003.png", viewModel.CurrentPlaneAImagePath);
        Assert.EndsWith(@"Plane-B\image_003.png", viewModel.CurrentPlaneBImagePath);

        viewModel.PreviousImageCommand.Execute(null);

        Assert.Equal(2, viewModel.CurrentImageIndex);
        Assert.EndsWith(@"Plane-A\image_002.png", viewModel.CurrentPlaneAImagePath);
        Assert.EndsWith(@"Plane-B\image_002.png", viewModel.CurrentPlaneBImagePath);
    }

    [Fact]
    public void NextAndPreviousCommands_StopAtScanBounds()
    {
        CreateScan("100001", planeAImageCount: 3, planeBImageCount: 3);

        var repository = new FileSystemScanRepository(scansRoot);
        var viewModel = new MainViewModel(repository, playbackScheduler);

        viewModel.LoadScanCommand.Execute(null);

        Assert.True(viewModel.PreviousImageCommand.CanExecute(null));
        Assert.True(viewModel.NextImageCommand.CanExecute(null));

        viewModel.NextImageCommand.Execute(null);

        Assert.Equal(2, viewModel.CurrentImageIndex);
        Assert.False(viewModel.NextImageCommand.CanExecute(null));

        viewModel.NextImageCommand.Execute(null);

        Assert.Equal(2, viewModel.CurrentImageIndex);

        viewModel.PreviousImageCommand.Execute(null);
        viewModel.PreviousImageCommand.Execute(null);

        Assert.Equal(0, viewModel.CurrentImageIndex);
        Assert.False(viewModel.PreviousImageCommand.CanExecute(null));
    }

    [Fact]
    public void KeyImageCommand_ReturnsToMiddleImageFromAnotherFrame()
    {
        CreateScan("100001", planeAImageCount: 5, planeBImageCount: 5);

        var repository = new FileSystemScanRepository(scansRoot);
        var viewModel = new MainViewModel(repository, playbackScheduler);

        viewModel.LoadScanCommand.Execute(null);
        Assert.False(viewModel.GoToKeyImageCommand.CanExecute(null));

        viewModel.PlayCommand.Execute(null);
        viewModel.PauseCommand.Execute(null);
        viewModel.NextImageCommand.Execute(null);

        Assert.Equal(1, viewModel.CurrentImageIndex);
        Assert.True(viewModel.GoToKeyImageCommand.CanExecute(null));

        viewModel.GoToKeyImageCommand.Execute(null);

        Assert.Equal(2, viewModel.CurrentImageIndex);
        Assert.EndsWith(@"Plane-A\image_002.png", viewModel.CurrentPlaneAImagePath);
        Assert.EndsWith(@"Plane-B\image_002.png", viewModel.CurrentPlaneBImagePath);
        Assert.False(viewModel.GoToKeyImageCommand.CanExecute(null));
    }

    [Fact]
    public void PlayCommand_StartsFromFirstImage_UsesNormalSpeed_AndLoops()
    {
        CreateScan("100001", planeAImageCount: 3, planeBImageCount: 3);

        var repository = new FileSystemScanRepository(scansRoot);
        var viewModel = new MainViewModel(repository, playbackScheduler);

        viewModel.LoadScanCommand.Execute(null);

        Assert.True(viewModel.PlayCommand.CanExecute(null));
        Assert.False(viewModel.PauseCommand.CanExecute(null));
        Assert.False(viewModel.ArePlaybackControlsEnabled);

        viewModel.PlayCommand.Execute(null);

        Assert.True(viewModel.IsPlaying);
        Assert.Equal(0, viewModel.CurrentImageIndex);
        Assert.Equal(TimeSpan.FromMilliseconds(200), playbackScheduler.Interval);
        Assert.False(viewModel.PlayCommand.CanExecute(null));
        Assert.True(viewModel.PauseCommand.CanExecute(null));
        Assert.False(viewModel.NextImageCommand.CanExecute(null));
        Assert.False(viewModel.PreviousImageCommand.CanExecute(null));
        Assert.True(viewModel.ArePlaybackControlsEnabled);

        playbackScheduler.Tick();
        Assert.Equal(1, viewModel.CurrentImageIndex);

        playbackScheduler.Tick();
        Assert.Equal(2, viewModel.CurrentImageIndex);

        playbackScheduler.Tick();
        Assert.Equal(0, viewModel.CurrentImageIndex);
    }

    [Fact]
    public void PauseCommand_StopsPlayback_AndReenablesManualNavigation()
    {
        CreateScan("100001", planeAImageCount: 4, planeBImageCount: 4);

        var repository = new FileSystemScanRepository(scansRoot);
        var viewModel = new MainViewModel(repository, playbackScheduler);

        viewModel.LoadScanCommand.Execute(null);
        viewModel.PlayCommand.Execute(null);
        playbackScheduler.Tick();

        Assert.Equal(1, viewModel.CurrentImageIndex);

        viewModel.PauseCommand.Execute(null);

        Assert.False(viewModel.IsPlaying);
        Assert.True(viewModel.PlayCommand.CanExecute(null));
        Assert.False(viewModel.PauseCommand.CanExecute(null));
        Assert.True(viewModel.NextImageCommand.CanExecute(null));
        Assert.True(viewModel.PreviousImageCommand.CanExecute(null));

        playbackScheduler.Tick();

        Assert.Equal(1, viewModel.CurrentImageIndex);
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

    private sealed class FakePlaybackScheduler : IPlaybackScheduler
    {
        private Action? tick;

        public TimeSpan? Interval { get; private set; }

        public bool IsRunning => tick is not null;

        public void Start(TimeSpan interval, Action tick)
        {
            Interval = interval;
            this.tick = tick;
        }

        public void Stop()
        {
            tick = null;
        }

        public void Tick()
        {
            tick?.Invoke();
        }
    }
}
