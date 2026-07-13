using System.Reflection;
using AStar.Dev.Wallpaper.Scraper.Configuration;
using Microsoft.Extensions.Options;

namespace AStar.Dev.Wallpaper.Scraper.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(IOptions<ScrapeConfiguration> scrapeConfiguration)
    {
        Title = $"{scrapeConfiguration.Value.ApplicationName} V{ApplicationVersion}";
    }

    public string Title { get; }

    /// <summary>
    ///     The version CI stamps from the release tag (-p:Version=...), so the title can
    ///     never drift from the Velopack package version. SourceLink appends +sha; strip it.
    /// </summary>
    public static string ApplicationVersion { get; } = typeof(MainWindowViewModel).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion.Split('+')[0] ?? "0.0.0";
}
