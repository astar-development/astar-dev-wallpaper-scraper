using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scraper.Configuration;
using AStar.Dev.Wallpaper.Scraper.Home;
using AStar.Dev.Wallpaper.Scraper.Services;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using ReactiveUI;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Home;

public sealed class GivenMainWindowViewModel
{
    private readonly IPlaywrightService playwrightService = Substitute.For<IPlaywrightService>();

    static GivenMainWindowViewModel()
    {
        RxApp.MainThreadScheduler = ImmediateScheduler.Instance;
    }

    public GivenMainWindowViewModel() =>
        playwrightService.ConfigurePlaywrightAsync().Returns(Task.FromResult(Exceptional.Success(Substitute.For<IPage>())));

    [Fact]
    public void when_constructed_then_cancel_command_cannot_execute()
    {
        var sut = CreateViewModel();

        var canExecute = sut.CancelCommand.CanExecute.FirstAsync().Wait();

        canExecute.ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_scrape_is_awaiting_confirmation_then_cancel_command_can_execute()
    {
        var sut = CreateViewModel();
        var (confirmationGate, handlerEntered) = RegisterGatedConfirmHandler(sut);

        var execution = sut.ScrapeTopCommand.Execute().ToTask();
        await handlerEntered;

        sut.CancelCommand.CanExecute.FirstAsync().Wait().ShouldBeTrue();

        confirmationGate.SetResult(true);
        await execution;
    }

    [Fact]
    public async Task when_cancel_command_is_invoked_while_awaiting_confirmation_then_the_scrape_is_cancelled_before_playwright_is_configured()
    {
        var sut = CreateViewModel();
        var (confirmationGate, handlerEntered) = RegisterGatedConfirmHandler(sut);

        var execution = sut.ScrapeTopCommand.Execute().ToTask();
        await handlerEntered;

        await sut.CancelCommand.Execute();
        confirmationGate.SetResult(true);
        await execution;

        sut.StatusText.ShouldContain("Cancelled");
        await playwrightService.DidNotReceive().ConfigurePlaywrightAsync();
    }

    [Fact]
    public async Task when_cancel_command_is_invoked_then_the_scrape_command_is_no_longer_executing()
    {
        var sut = CreateViewModel();
        var (confirmationGate, handlerEntered) = RegisterGatedConfirmHandler(sut);

        var execution = sut.ScrapeTopCommand.Execute().ToTask();
        await handlerEntered;

        await sut.CancelCommand.Execute();
        confirmationGate.SetResult(true);
        await execution;

        sut.IsBusy.ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_scrape_completes_without_cancellation_then_playwright_is_configured_and_the_status_reports_success()
    {
        var sut = CreateViewModel();
        sut.ConfirmScrape.RegisterHandler(context => { context.SetOutput(true); return Task.CompletedTask; });

        await sut.ScrapeTopCommand.Execute();

        sut.StatusText.ShouldContain("Playwright page configured successfully.");
        sut.StatusText.ShouldNotContain("Cancelled");
    }

    [Fact]
    public async Task when_disposed_while_a_scrape_is_awaiting_confirmation_then_the_scrape_is_cancelled()
    {
        var sut = CreateViewModel();
        var (confirmationGate, handlerEntered) = RegisterGatedConfirmHandler(sut);

        var execution = sut.ScrapeTopCommand.Execute().ToTask();
        await handlerEntered;

        sut.Dispose();
        confirmationGate.SetResult(true);
        await execution;

        sut.StatusText.ShouldContain("Cancelled");
    }

    private MainWindowViewModel CreateViewModel()
    {
        var scrapeConfiguration = Options.Create(new ScrapeConfiguration { ApplicationName = "Test App" });

        return new MainWindowViewModel(scrapeConfiguration, playwrightService);
    }

    private static (TaskCompletionSource<bool> ConfirmationGate, Task HandlerEntered) RegisterGatedConfirmHandler(MainWindowViewModel sut)
    {
        var confirmationGate = new TaskCompletionSource<bool>();
        var handlerEntered = new TaskCompletionSource();
        sut.ConfirmScrape.RegisterHandler(async context =>
        {
            handlerEntered.TrySetResult();
            var confirmed = await confirmationGate.Task;
            context.SetOutput(confirmed);
        });

        return (confirmationGate, handlerEntered.Task);
    }
}
