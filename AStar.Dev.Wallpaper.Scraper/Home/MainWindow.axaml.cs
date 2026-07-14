using AStar.Dev.Wallpaper.Scraper.Services;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AStar.Dev.Wallpaper.Scraper.Home;

public partial class MainWindow : Window, IDisposable
{
    private CancellationTokenSource cts = new();
    private bool disposedValue;

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

    private void OnScrapeClicked(object? sender, RoutedEventArgs e)
    {
        cts = new CancellationTokenSource();
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => cts?.Cancel();

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                cts.Cancel();
                cts.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method. Do NOT remove this comment!
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}