using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using AStar.Dev.FunctionalParadigm;
using AStar.Dev.Wallpaper.Scraper.Configuration;
using AStar.Dev.Wallpaper.Scraper.Configuration.EntityEditor;
using AStar.Dev.Wallpaper.Scraper.Home;
using AStar.Dev.Wallpaper.Scraper.Scraping;
using AStar.Dev.Wallpaper.Scraper.Services;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using ReactiveUI;
using RxUnit = System.Reactive.Unit;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Home;

public sealed class GivenMainWindowViewModel
{
    private readonly IPlaywrightService playwrightService = Substitute.For<IPlaywrightService>();
    private readonly IScrapeAction searchCategoryScrapeAction = Substitute.For<IScrapeAction>();
    private readonly IEntityEditorFactory entityEditorFactory = Substitute.For<IEntityEditorFactory>();

    static GivenMainWindowViewModel()
    {
        RxApp.MainThreadScheduler = ImmediateScheduler.Instance;
    }

    public GivenMainWindowViewModel()
    {
        playwrightService.ConfigurePlaywrightAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(Exceptional.Success(Substitute.For<IPage>())));
        searchCategoryScrapeAction.ExecuteAsync(Arg.Any<IPage>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Exceptional.Success(FunctionalParadigm.Unit.Instance)));
    }

    [Fact]
    public async Task when_search_categories_scrape_completes_then_the_injected_action_executes_against_the_configured_page()
    {
        var page = Substitute.For<IPage>();
        playwrightService.ConfigurePlaywrightAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(Exceptional.Success(page)));
        var sut = CreateViewModel();
        sut.ConfirmScrape.RegisterHandler(context => { context.SetOutput(true); return Task.CompletedTask; });

        await sut.ScrapeSearchCategoriesCommand.Execute();

        await searchCategoryScrapeAction.Received().ExecuteAsync(page, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_search_categories_action_fails_then_the_status_reports_the_failure_message()
    {
        searchCategoryScrapeAction.ExecuteAsync(Arg.Any<IPage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Exceptional<FunctionalParadigm.Unit>>(Exceptional.Failure<FunctionalParadigm.Unit>(new InvalidOperationException("boom"))));
        var sut = CreateViewModel();
        sut.ConfirmScrape.RegisterHandler(context => { context.SetOutput(true); return Task.CompletedTask; });

        await sut.ScrapeSearchCategoriesCommand.Execute();

        sut.StatusText.ShouldContain("boom");
    }

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
        await playwrightService.DidNotReceive().ConfigurePlaywrightAsync(Arg.Any<CancellationToken>());
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

    [Fact]
    public async Task when_open_connection_strings_command_executes_then_the_factory_supplied_editor_is_shown()
    {
        var editor = Substitute.For<EntityEditorViewModelBase>();
        entityEditorFactory.CreateConnectionStringsEditor().Returns(editor);
        var sut = CreateViewModel();

        var handled = await ExecuteAndCaptureOpenedEditor(sut, sut.OpenConnectionStringsCommand);

        handled.ShouldBe(editor);
    }

    [Fact]
    public async Task when_open_file_classification_categories_command_executes_then_the_factory_supplied_editor_is_shown()
    {
        var editor = Substitute.For<EntityEditorViewModelBase>();
        entityEditorFactory.CreateFileClassificationCategoriesEditor().Returns(editor);
        var sut = CreateViewModel();

        var handled = await ExecuteAndCaptureOpenedEditor(sut, sut.OpenFileClassificationCategoriesCommand);

        handled.ShouldBe(editor);
    }

    [Fact]
    public async Task when_open_search_configuration_command_executes_then_the_factory_supplied_editor_is_shown()
    {
        var editor = Substitute.For<EntityEditorViewModelBase>();
        entityEditorFactory.CreateSearchConfigurationEditor().Returns(editor);
        var sut = CreateViewModel();

        var handled = await ExecuteAndCaptureOpenedEditor(sut, sut.OpenSearchConfigurationCommand);

        handled.ShouldBe(editor);
    }

    [Fact]
    public async Task when_open_model_to_ignore_command_executes_then_the_factory_supplied_editor_is_shown()
    {
        var editor = Substitute.For<EntityEditorViewModelBase>();
        entityEditorFactory.CreateModelToIgnoreEditor().Returns(editor);
        var sut = CreateViewModel();

        var handled = await ExecuteAndCaptureOpenedEditor(sut, sut.OpenModelToIgnoreCommand);

        handled.ShouldBe(editor);
    }

    [Fact]
    public async Task when_open_scrape_directories_command_executes_then_the_factory_supplied_editor_is_shown()
    {
        var editor = Substitute.For<EntityEditorViewModelBase>();
        entityEditorFactory.CreateScrapeDirectoriesEditor().Returns(editor);
        var sut = CreateViewModel();

        var handled = await ExecuteAndCaptureOpenedEditor(sut, sut.OpenScrapeDirectoriesCommand);

        handled.ShouldBe(editor);
    }

    [Fact]
    public async Task when_open_search_categories_command_executes_then_the_factory_supplied_editor_is_shown()
    {
        var editor = Substitute.For<EntityEditorViewModelBase>();
        entityEditorFactory.CreateSearchCategoriesEditor().Returns(editor);
        var sut = CreateViewModel();

        var handled = await ExecuteAndCaptureOpenedEditor(sut, sut.OpenSearchCategoriesCommand);

        handled.ShouldBe(editor);
    }

    [Fact]
    public async Task when_open_tag_to_ignore_command_executes_then_the_factory_supplied_editor_is_shown()
    {
        var editor = Substitute.For<EntityEditorViewModelBase>();
        entityEditorFactory.CreateTagToIgnoreEditor().Returns(editor);
        var sut = CreateViewModel();

        var handled = await ExecuteAndCaptureOpenedEditor(sut, sut.OpenTagToIgnoreCommand);

        handled.ShouldBe(editor);
    }

    [Fact]
    public async Task when_open_user_configuration_command_executes_then_the_factory_supplied_editor_is_shown()
    {
        var editor = Substitute.For<EntityEditorViewModelBase>();
        entityEditorFactory.CreateUserConfigurationEditor().Returns(editor);
        var sut = CreateViewModel();

        var handled = await ExecuteAndCaptureOpenedEditor(sut, sut.OpenUserConfigurationCommand);

        handled.ShouldBe(editor);
    }

    private MainWindowViewModel CreateViewModel()
    {
        var scrapeConfiguration = Options.Create(new ScrapeConfiguration { ApplicationName = "Test App" });

        return new MainWindowViewModel(scrapeConfiguration, playwrightService, searchCategoryScrapeAction, entityEditorFactory);
    }

    private static async Task<EntityEditorViewModelBase?> ExecuteAndCaptureOpenedEditor(MainWindowViewModel sut, ReactiveCommand<RxUnit, RxUnit> command)
    {
        EntityEditorViewModelBase? handled = null;
        sut.OpenEditor.RegisterHandler(context =>
        {
            handled = context.Input;
            context.SetOutput(RxUnit.Default);

            return Task.CompletedTask;
        });

        await command.Execute();

        return handled;
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
