using System.Reactive.Linq;
using System.Reflection;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scraper.Configuration;
using AStar.Dev.Wallpaper.Scraper.Configuration.EntityEditor;
using AStar.Dev.Wallpaper.Scraper.Scraping;
using AStar.Dev.Wallpaper.Scraper.Services;
using AStar.Dev.Wallpaper.Scraper.ViewModels;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Options;
using ReactiveUI;
using Unit = System.Reactive.Unit;

namespace AStar.Dev.Wallpaper.Scraper.Home;

public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    private const int MaxStatusLines = 1000;

    private string statusText = string.Empty;
    private bool isBusy;
    private CancellationTokenSource? scrapeCancellationSource;
    private readonly IPlaywrightService playwrightService;
    private readonly Queue<string> statusLines = new();

    public MainWindowViewModel(IOptions<ScrapeConfiguration> scrapeConfiguration, IPlaywrightService playwrightService, IScrapeAction searchCategoryScrapeAction,
        IEntityEditorFactory entityEditorFactory)
    {
        Title = $"{scrapeConfiguration.Value.ApplicationName} V{ApplicationVersion}";
        this.playwrightService = playwrightService;

        ScrapeSearchCategoriesCommand = CreateScrapeCommand("Scrape Search Categories", searchCategoryScrapeAction);
        ScrapeTopCommand = CreateScrapeCommand("Scrape Top Wallpapers");
        ScrapeSubscribedCommand = CreateScrapeCommand("Scrape Subscribed Wallpapers");
        ScrapeAllCommand = CreateScrapeCommand("Scrape All Wallpapers");
        CancelCommand = ReactiveCommand.Create(CancelRunningScrape, this.WhenAnyValue(vm => vm.IsBusy));

        OpenConnectionStringsCommand = CreateOpenEditorCommand(entityEditorFactory.CreateConnectionStringsEditor);
        OpenFileClassificationCategoriesCommand = CreateOpenEditorCommand(entityEditorFactory.CreateFileClassificationCategoriesEditor);
        OpenSearchConfigurationCommand = CreateOpenEditorCommand(entityEditorFactory.CreateSearchConfigurationEditor);
        OpenModelToIgnoreCommand = CreateOpenEditorCommand(entityEditorFactory.CreateModelToIgnoreEditor);
        OpenScrapeDirectoriesCommand = CreateOpenEditorCommand(entityEditorFactory.CreateScrapeDirectoriesEditor);
        OpenSearchCategoriesCommand = CreateOpenEditorCommand(entityEditorFactory.CreateSearchCategoriesEditor);
        OpenTagToIgnoreCommand = CreateOpenEditorCommand(entityEditorFactory.CreateTagToIgnoreEditor);
        OpenUserConfigurationCommand = CreateOpenEditorCommand(entityEditorFactory.CreateUserConfigurationEditor);
    }

    public string Title { get; }

    public Interaction<string, bool> ConfirmScrape { get; } = new();

    public ReactiveCommand<Unit, Unit> ScrapeSearchCategoriesCommand { get; }

    public ReactiveCommand<Unit, Unit> ScrapeTopCommand { get; }

    public ReactiveCommand<Unit, Unit> ScrapeSubscribedCommand { get; }

    public ReactiveCommand<Unit, Unit> ScrapeAllCommand { get; }

    /// <summary>
    ///     Gets the command that cancels whichever scrape command is currently running.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    /// <summary>Raised by an Open* command to ask the view to display an <see cref="EntityEditorViewModelBase" /> as a modal dialog.</summary>
    public Interaction<EntityEditorViewModelBase, Unit> OpenEditor { get; } = new();

    /// <summary>Opens the Connection Strings Configuration editor.</summary>
    public ReactiveCommand<Unit, Unit> OpenConnectionStringsCommand { get; }

    /// <summary>Opens the File Classification Categories Configuration editor.</summary>
    public ReactiveCommand<Unit, Unit> OpenFileClassificationCategoriesCommand { get; }

    /// <summary>Opens the Search Configuration editor.</summary>
    public ReactiveCommand<Unit, Unit> OpenSearchConfigurationCommand { get; }

    /// <summary>Opens the Model to Ignore editor.</summary>
    public ReactiveCommand<Unit, Unit> OpenModelToIgnoreCommand { get; }

    /// <summary>Opens the Scrape Directories editor.</summary>
    public ReactiveCommand<Unit, Unit> OpenScrapeDirectoriesCommand { get; }

    /// <summary>Opens the Search Categories editor.</summary>
    public ReactiveCommand<Unit, Unit> OpenSearchCategoriesCommand { get; }

    /// <summary>Opens the Tag to Ignore editor.</summary>
    public ReactiveCommand<Unit, Unit> OpenTagToIgnoreCommand { get; }

    /// <summary>Opens the User Configuration editor.</summary>
    public ReactiveCommand<Unit, Unit> OpenUserConfigurationCommand { get; }

    /// <summary>
    ///     Gets a value indicating whether a scrape command is currently running. Drives whether
    ///     <see cref="CancelCommand" /> can execute.
    /// </summary>
    public bool IsBusy
    {
        get => isBusy;
        private set => this.RaiseAndSetIfChanged(ref isBusy, value);
    }

    public string StatusText
    {
        get => statusText;
        private set => this.RaiseAndSetIfChanged(ref statusText, value);
    }

    private void CancelRunningScrape() => scrapeCancellationSource?.Cancel();

    private void AppendStatusLine(string line)
    {
        statusLines.Enqueue(line);

        while (statusLines.Count > MaxStatusLines)
        {
            statusLines.Dequeue();
        }

        StatusText = string.Join(Environment.NewLine, statusLines) + Environment.NewLine;
    }

    private ReactiveCommand<Unit, Unit> CreateOpenEditorCommand(Func<EntityEditorViewModelBase> createEditor) =>
        ReactiveCommand.CreateFromTask(async () => await OpenEditor.Handle(createEditor()));

    private ReactiveCommand<Unit, Unit> CreateScrapeCommand(string actionName, IScrapeAction? action = null)
    {
        var canExecute = this.WhenAnyValue(vm => vm.IsBusy).Select(busy => !busy);

        var command = ReactiveCommand.CreateFromTask(async () =>
        {
            IsBusy = true;
            using var cancellationSource = new CancellationTokenSource();
            scrapeCancellationSource = cancellationSource;

            try
            {
                var confirmed = await ConfirmScrape.Handle($"Are you sure you want to start the '{actionName}'?");
                cancellationSource.Token.ThrowIfCancellationRequested();

                AppendStatusLine($"{actionName}: {(confirmed ? "Yes" : "No")}");
                var page = await playwrightService.ConfigurePlaywrightAsync(cancellationSource.Token);
                cancellationSource.Token.ThrowIfCancellationRequested();

                await page.MatchAsync(
                    async page =>
                    {
                        AppendStatusLine("Playwright page configured successfully.");

                        if (action is null)
                        {
                            _ = page.GotoAsync("login");
                        }
                        else
                        {
                            _ = page.GotoAsync("/");
                            var progress = new Progress<string>(AppendStatusLine);
                            var result = await action.ExecuteAsync(page, progress, cancellationSource.Token);
                            result.Match(
                                _ => FunctionalParadigm.Unit.Instance,
                                error =>
                                {
                                    AppendStatusLine($"{actionName}: {error.Message}");

                                    return FunctionalParadigm.Unit.Instance;
                                });
                        }

                        return FunctionalParadigm.Unit.Instance;
                    },
                    error =>
                    {
                        AppendStatusLine($"Error configuring Playwright page: {error.Message}");

                        return FunctionalParadigm.Unit.Instance;
                    });
            }
            catch (OperationCanceledException)
            {
                AppendStatusLine($"{actionName}: Cancelled");
            }
            finally
            {
                scrapeCancellationSource = null;
                IsBusy = false;
            }
        }, canExecute);

        command.ThrownExceptions.Subscribe(exception =>
            AppendStatusLine($"{actionName}: Unexpected error - {exception.Message}"));

        return command;
    }

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

    /// <summary>
    ///     Cancels any in-flight scrape operation so it does not continue running after the view model is torn down.
    /// </summary>
    public void Dispose() => scrapeCancellationSource?.Cancel();
}
