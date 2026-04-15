using Microsoft.UI.Xaml;
using ScansApp.ViewModels;

namespace ScansApp;

public sealed partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        Root.DataContext = viewModel;
    }
}
