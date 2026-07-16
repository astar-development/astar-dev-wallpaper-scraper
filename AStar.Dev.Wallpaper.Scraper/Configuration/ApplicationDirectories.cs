using AStar.Dev.Utilities;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;
using LogMessage = AStar.Dev.Logging.Extensions.LogMessage;

namespace AStar.Dev.Wallpaper.Scraper.Configuration;

public class ApplicationDirectories(IFileSystem fileSystem, ILogger<ApplicationDirectories> logger) : IApplicationDirectories
{
    private static readonly string root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).CombinePath(ApplicationMetadata.ApplicationFolder);
    
    public void CreateIfRequired()
    {
        LogMessage.Debug(logger, "Ensuring application directories exist at {Root}", root);
        fileSystem.Directory.CreateDirectory(DataDirectory);
        fileSystem.Directory.CreateDirectory(LogsDirectory);
        fileSystem.Directory.CreateDirectory(CacheDirectory);
        fileSystem.Directory.CreateDirectory(DocumentsExportDirectory);
    }

    public static string DataDirectory => root.CombinePath("data");

    public static string CacheDirectory => root.CombinePath("cache");

    public static string LogsDirectory => root.CombinePath("logs");

    /// <summary>The user's documents folder where table Import/Export JSON files are read from and written to.</summary>
    public static string DocumentsExportDirectory => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).CombinePath(ApplicationMetadata.ApplicationFolder);
}
