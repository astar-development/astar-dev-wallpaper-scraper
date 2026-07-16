using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Wallpaper.Scraper.Startup;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStar.Dev.Wallpaper.Scraper.Tests.Unit.Startup;

public sealed class GivenDatabaseMigrator : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");

    [Fact]
    public async Task when_migration_succeeds_then_pending_migrations_are_applied()
    {
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        var dbContextFactory = new TestDbContextFactory(options);

        await DatabaseMigrator.MigrateAsync(dbContextFactory, NullLogger.Instance);

        using var context = dbContextFactory.CreateDbContext();
        await Should.NotThrowAsync(() => context.FileClassificationCategories.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task when_the_db_context_factory_throws_then_the_exception_is_swallowed()
    {
        var dbContextFactory = Substitute.For<IDbContextFactory<AppDbContext>>();
        dbContextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<AppDbContext>(new InvalidOperationException("Simulated connection failure")));

        await Should.NotThrowAsync(() => DatabaseMigrator.MigrateAsync(dbContextFactory, NullLogger.Instance));
    }

    public void Dispose() =>
        connection.Dispose();

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(options));
    }
}
