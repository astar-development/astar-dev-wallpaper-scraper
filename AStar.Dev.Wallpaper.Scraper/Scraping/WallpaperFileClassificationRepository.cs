using AStar.Dev.Infrastructure.AppDb;
using AStar.Dev.Infrastructure.AppDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.Wallpaper.Scraper.Scraping;

/// <summary>
///     Persists downloaded wallpapers and their tag classifications.
/// </summary>
public sealed class WallpaperFileClassificationRepository(IDbContextFactory<AppDbContext> dbContextFactory) : IWallpaperFileClassificationRepository
{
    /// <inheritdoc />
    public async Task<bool> IsAlreadyDownloadedAsync(string directoryPath, string fileName, CancellationToken token)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(token);

        return await context.Files.AnyAsync(file => file.FileName.Value == fileName && file.DirectoryName.Value == directoryPath, token);
    }

    /// <inheritdoc />
    public async Task RecordAsync(IReadOnlyList<TagData> tags, string imageUrl, string directoryPath, long sizeBytes, ImageDimensions dimensions, CancellationToken token)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(token);

        foreach (var tag in tags)
        {
            var category = await context.FileClassificationCategories.FirstAsync(c => c.Name == tag.Tag, token);

            context.FileClassifications.Add(new FileClassificationEntity
            {
                FileDetailId = new FileId(Guid.CreateVersion7()),
                CategoryId = category.Id,
                FileDetail = new FileDetailEntity
                {
                    FileName = new FileName(Path.GetFileName(imageUrl)),
                    DirectoryName = new DirectoryName(directoryPath),
                    FileHandle = new FileHandle(Guid.NewGuid().ToString()),
                    FileSize = sizeBytes,
                    IsImage = true,
                    ImageDetail = new ImageDetailEntity
                    {
                        Width = dimensions.Width,
                        Height = dimensions.Height,
                    },
                },
            });
        }

        await context.SaveChangesAsync(token);
    }
}
