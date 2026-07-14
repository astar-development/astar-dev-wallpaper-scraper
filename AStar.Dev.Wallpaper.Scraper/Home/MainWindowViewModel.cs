using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scraper.Configuration;
using AStar.Dev.Wallpaper.Scraper.Services;
using AStar.Dev.Wallpaper.Scraper.ViewModels;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Options;
using ReactiveUI;
using Unit = System.Reactive.Unit;

namespace AStar.Dev.Wallpaper.Scraper.Home;

public sealed class MainWindowViewModel : ViewModelBase
{
    private string statusText = string.Empty;
    private readonly IPlaywrightService playwrightService;

    public MainWindowViewModel(IOptions<ScrapeConfiguration> scrapeConfiguration, IPlaywrightService playwrightService)
    {
        Title = $"{scrapeConfiguration.Value.ApplicationName} V{ApplicationVersion}";

        ScrapeSearchCategoriesCommand = CreateScrapeCommand("Scrape Search Categories");
        ScrapeTopCommand              = CreateScrapeCommand("Scrape Top Wallpapers");
        ScrapeSubscribedCommand       = CreateScrapeCommand("Scrape Subscribed Wallpapers");
        ScrapeAllCommand              = CreateScrapeCommand("Scrape All Wallpapers");
        this.playwrightService = playwrightService;
    }

    public string Title { get; }

    public Interaction<string, bool> ConfirmScrape { get; } = new();

    public ReactiveCommand<Unit, Unit> ScrapeSearchCategoriesCommand { get; }

    public ReactiveCommand<Unit, Unit> ScrapeTopCommand { get; }

    public ReactiveCommand<Unit, Unit> ScrapeSubscribedCommand { get; }

    public ReactiveCommand<Unit, Unit> ScrapeAllCommand { get; }

    public string StatusText
    {
        get => statusText;
        private set => this.RaiseAndSetIfChanged(ref statusText, value);
    }

    private ReactiveCommand<Unit, Unit> CreateScrapeCommand(string actionName) =>
        ReactiveCommand.CreateFromTask(async () =>
        {
            var confirmed = await ConfirmScrape.Handle($"Are you sure you want to start the '{actionName}'?");

            StatusText += $"{actionName}: {(confirmed ? "Yes" : "No")}{Environment.NewLine}";
            var page = await playwrightService.ConfigurePlaywrightAsync();
            page.Match(
                page => {
                    StatusText += $"Playwright page configured successfully.{Environment.NewLine}";
                    _ = page.GotoAsync("login");

                    return FunctionalParadigm.Unit.Instance;
                },
                error => {
                    StatusText += $"Error configuring Playwright page: {error.Message}{Environment.NewLine}";

                    return FunctionalParadigm.Unit.Instance;
                });
        });

    public ReactiveCommand<Unit, Unit> ExitCommand { get; } = ReactiveCommand.Create(static () =>
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    });

    /// <summary>
    ///     The version CI stamps from the release tag (-p:Version=...), so the title can
    ///     never drift from the Velopack package version. SourceLink appends +sha; strip it.
    /// </summary>
    public static string ApplicationVersion { get; } = typeof(MainWindowViewModel).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion.Split('+')[0] ?? "0.0.0";
}
