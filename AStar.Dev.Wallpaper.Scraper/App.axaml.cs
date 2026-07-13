using System;
using AStar.Dev.Wallpaper.Scraper.Services;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.Wallpaper.Scraper;

public partial class App : Application
{
    public IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Services = new ServiceCollection()
            .AddApplicationServices()
            .BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = Services.GetRequiredService<MainWindow>();

            // Fire-and-forget: the check runs in the background and prompts via a
            // dialog only when an update has already been downloaded.
            _ = Services.GetRequiredService<UpdateService>().CheckForUpdatesAsync(desktop.MainWindow);
        }

        base.OnFrameworkInitializationCompleted();
    }
}