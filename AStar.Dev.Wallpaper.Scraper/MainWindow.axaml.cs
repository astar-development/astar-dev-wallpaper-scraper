using AStar.Dev.Wallpaper.Scraper.ViewModels;
using Avalonia.Controls;

namespace AStar.Dev.Wallpaper.Scraper;

public partial class MainWindow : Window
{
    // Parameterless constructor is required by the XAML previewer only; the app resolves the DI constructor.
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}