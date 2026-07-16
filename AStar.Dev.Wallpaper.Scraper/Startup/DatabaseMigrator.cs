using System.Diagnostics.CodeAnalysis;
using AStar.Dev.Infrastructure.AppDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LogMessage = AStar.Dev.Logging.Extensions.LogMessage;

namespace AStar.Dev.Wallpaper.Scraper.Startup;

/// <summary>Applies pending Entity Framework Core migrations to the application database at startup.</summary>
[ExcludeFromCodeCoverage]
public static class DatabaseMigrator
{
    /// <summary>Migrates the database, logging and swallowing any failure so that startup can continue.</summary>
    /// <param name="dbContextFactory">The factory used to create the <see cref="AppDbContext" />.</param>
    /// <param name="logger">The logger used to report migration failures.</param>
    public static async Task MigrateAsync(IDbContextFactory<AppDbContext> dbContextFactory, ILogger logger)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            await dbContext.Database.MigrateAsync();
        }
        catch (Exception exception)
        {
            LogMessage.Error(logger, "Database migration failed", exception);
        }
    }
}
