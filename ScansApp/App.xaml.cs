using Microsoft.UI.Xaml;
using System;
using System.IO;
using ScansApp.Services;
using ScansApp.ViewModels;

namespace ScansApp;

public partial class App : Application
{
    private Window? window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        var scansRoot = Path.Combine(AppContext.BaseDirectory, "scans");
        var repository = new FileSystemScanRepository(scansRoot);
        var viewModel = new MainViewModel(repository);

        window = new MainWindow(viewModel);
        window.Activate();
    }
}
