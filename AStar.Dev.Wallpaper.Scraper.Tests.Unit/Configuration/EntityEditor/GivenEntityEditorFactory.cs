using System.Reactive.Linq;
using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using AStar.Dev.Wallpaper.Scraper.Configuration.EntityEditor;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using Testably.Abstractions.Testing;
using RxUnit = System.Reactive.Unit;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Configuration.EntityEditor;

public sealed class GivenEntityEditorFactory : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"entity-editor-factory-{Guid.NewGuid():N}.db");
    private readonly DbContextOptions<AppDbContext> options;
    private readonly EntityEditorFactory sut;

    public GivenEntityEditorFactory()
    {
        options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        using (var migrationContext = new AppDbContext(options))
        {
            migrationContext.Database.Migrate();
        }

        sut = new EntityEditorFactory(new TestDbContextFactory(options), new MockFileSystem(), "/exports");
    }

    [Fact]
    public void when_creating_the_connection_strings_editor_then_audit_columns_are_hidden_and_add_remove_is_disabled()
    {
        var editor = sut.CreateConnectionStringsEditor();

        editor.CanAddRemove.ShouldBeFalse();
        editor.IsColumnVisible(nameof(AuditableEntity.CreatedAt)).ShouldBeFalse();
        editor.IsColumnVisible(nameof(AuditableEntity.UpdatedAt)).ShouldBeFalse();
        editor.IsColumnVisible(nameof(ConnectionStringsEntity.Sqlite)).ShouldBeTrue();
        editor.IsColumnReadOnly(nameof(ConnectionStringsEntity.Id)).ShouldBeTrue();
    }

    [Fact]
    public void when_creating_the_file_classification_categories_editor_then_the_parent_navigation_is_hidden_and_add_remove_is_enabled()
    {
        var editor = sut.CreateFileClassificationCategoriesEditor();

        editor.CanAddRemove.ShouldBeTrue();
        editor.IsColumnVisible(nameof(FileClassificationCategoryEntity.Parent)).ShouldBeFalse();
        editor.IsColumnVisible(nameof(FileClassificationCategoryEntity.Name)).ShouldBeTrue();
        editor.IsColumnReadOnly(nameof(FileClassificationCategoryEntity.Id)).ShouldBeTrue();
    }

    [Fact]
    public void when_creating_the_search_configuration_editor_then_the_search_categories_collection_is_hidden_and_add_remove_is_disabled()
    {
        var editor = sut.CreateSearchConfigurationEditor();

        editor.CanAddRemove.ShouldBeFalse();
        editor.IsColumnVisible(nameof(SearchConfigurationEntity.SearchCategories)).ShouldBeFalse();
        editor.IsColumnVisible(nameof(AuditableEntity.CreatedAt)).ShouldBeFalse();
    }

    [Fact]
    public void when_creating_the_model_to_ignore_editor_then_add_remove_is_enabled()
    {
        var editor = sut.CreateModelToIgnoreEditor();

        editor.CanAddRemove.ShouldBeTrue();
        editor.IsColumnReadOnly(nameof(ModelToIgnoreEntity.Id)).ShouldBeTrue();
    }

    [Fact]
    public void when_creating_the_scrape_directories_editor_then_add_remove_is_disabled()
    {
        var editor = sut.CreateScrapeDirectoriesEditor();

        editor.CanAddRemove.ShouldBeFalse();
    }

    [Fact]
    public void when_creating_the_tag_to_ignore_editor_then_add_remove_is_enabled()
    {
        var editor = sut.CreateTagToIgnoreEditor();

        editor.CanAddRemove.ShouldBeTrue();
        editor.IsColumnReadOnly(nameof(TagToIgnoreEntity.Id)).ShouldBeTrue();
    }

    [Fact]
    public void when_creating_the_user_configuration_editor_then_add_remove_is_disabled()
    {
        var editor = sut.CreateUserConfigurationEditor();

        editor.CanAddRemove.ShouldBeFalse();
    }

    [Fact]
    public void when_creating_the_search_categories_editor_then_the_foreign_key_column_is_hidden_but_add_remove_is_enabled()
    {
        var editor = sut.CreateSearchCategoriesEditor();

        editor.CanAddRemove.ShouldBeTrue();
        editor.IsColumnVisible(nameof(SearchCategoryEntity.SearchConfiguration)).ShouldBeFalse();
        editor.IsColumnVisible(nameof(SearchCategoryEntity.SearchConfigurationId)).ShouldBeFalse();
    }

    [Fact]
    public async Task when_a_new_search_category_row_is_added_and_saved_then_it_is_stamped_with_the_single_search_configurations_id()
    {
        int searchConfigurationId;
        await using (var seedContext = new AppDbContext(options))
        {
            var searchConfiguration = new SearchConfigurationEntity();
            seedContext.Add(searchConfiguration);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            searchConfigurationId = searchConfiguration.Id;
        }

        var editor = sut.CreateSearchCategoriesEditor();

        await ((ReactiveCommand<RxUnit, RxUnit>)editor.AddCommand).Execute();
        await ((ReactiveCommand<RxUnit, RxUnit>)editor.SaveCommand).Execute();

        await using var context = new AppDbContext(options);
        var row = await context.Set<SearchCategoryEntity>().SingleAsync(TestContext.Current.CancellationToken);
        row.SearchConfigurationId.ShouldBe(searchConfigurationId);
    }

    public void Dispose()
    {
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() =>
            new(options);
    }
}
