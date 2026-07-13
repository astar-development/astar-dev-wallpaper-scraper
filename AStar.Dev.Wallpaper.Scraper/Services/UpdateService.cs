using System;
using System.Threading.Tasks;
using AStar.Dev.Wallpaper.Scraper.Configuration;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using Microsoft.Extensions.Options;
using Velopack;
using Velopack.Sources;

namespace AStar.Dev.Wallpaper.Scraper.Services;

/// <summary>
///     Checks GitHub Releases for a newer Velopack package in the background,
///     downloads it, then prompts the user to restart and apply it.
/// </summary>
public sealed class UpdateService(IOptions<UpdateConfiguration> updateConfiguration)
{
    public async Task CheckForUpdatesAsync(Window owner)
    {
        try
        {
            var manager = new UpdateManager(new GithubSource(updateConfiguration.Value.RepositoryUrl, accessToken: null, prerelease: false));

            if (!manager.IsInstalled)
            {
                return; // Running from the IDE / a plain publish folder, not a Velopack install.
            }

            var update = await manager.CheckForUpdatesAsync();

            if (update is null)
            {
                return;
            }

            await manager.DownloadUpdatesAsync(update);

            var restartNow = await Dispatcher.UIThread.InvokeAsync(() => PromptToRestartAsync(owner, update.TargetFullRelease.Version.ToString()));

            if (restartNow)
            {
                manager.ApplyUpdatesAndRestart(update);
            }
        }
        catch (Exception)
        {
            // The update check must never take the app down; the next launch retries.
        }
    }

    private static async Task<bool> PromptToRestartAsync(Window owner, string version)
    {
        var restartNow = false;

        var dialog = new Window
        {
            Title = "Update available",
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
        };

        var restartButton = new Button { Content = "Restart now" };
        var laterButton = new Button { Content = "Later" };

        restartButton.Click += (_, _) =>
        {
            restartNow = true;
            dialog.Close();
        };

        laterButton.Click += (_, _) => dialog.Close();

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12,
            Children =
            {
                new TextBlock { Text = $"Version {version} has been downloaded. Restart to apply?" },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Children = { laterButton, restartButton },
                },
            },
        };

        await dialog.ShowDialog(owner);

        return restartNow;
    }
}
