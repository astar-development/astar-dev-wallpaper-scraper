using AStar.Dev.Wallpaper.Scraper.Configuration.EntityEditor;
using Avalonia.Controls;
using Avalonia.Threading;
using ReactiveUI;
using Unit = System.Reactive.Unit;

namespace AStar.Dev.Wallpaper.Scraper.Home;

public partial class MainWindow : Window
{
    // Parameterless constructor is required by the XAML previewer only; the app resolves the DI constructor.
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel) : this()
    {
        DataContext = viewModel;

        viewModel.ConfirmScrape.RegisterHandler(async context =>
            context.SetOutput(await new ConfirmDialog(context.Input).ShowDialog<bool?>(this) ?? false));

        viewModel.OpenEditor.RegisterHandler(async context =>
        {
            await new EntityEditorWindow { DataContext = context.Input }.ShowDialog(this);
            context.SetOutput(Unit.Default);
        });

        viewModel.WhenAnyValue(vm => vm.StatusText)
            .Subscribe(_ => Dispatcher.UIThread.Post(StatusScroller.ScrollToEnd, DispatcherPriority.Background));
    }
}