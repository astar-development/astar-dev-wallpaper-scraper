using Avalonia.Controls;

namespace AStar.Dev.Wallpaper.Scraper.Home;

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

        viewModel.ConfirmScrape.RegisterHandler(async context =>
            context.SetOutput(await new ConfirmDialog(context.Input).ShowDialog<bool?>(this) ?? false));
    }
}