using System;
using System.IO;
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
    private static readonly string LogPath = Path.Combine(Path.GetTempPath(), "astar-dev-wallpaper-scraper-update.log");

    public async Task CheckForUpdatesAsync(Window owner)
    {
        try
        {
            Log($"Update check starting; repository {updateConfiguration.Value.RepositoryUrl}");

            var manager = new UpdateManager(new GithubSource(updateConfiguration.Value.RepositoryUrl, accessToken: null, prerelease: false));

            if (!manager.IsInstalled)
            {
                Log("Not a Velopack install (IDE / plain publish folder run); skipping");
                return;
            }

            Log($"Installed version {manager.CurrentVersion}; checking for updates");

            var update = await manager.CheckForUpdatesAsync();

            if (update is null)
            {
                Log("Already on the latest version");
                return;
            }

            Log($"Update {update.TargetFullRelease.Version} found; downloading");

            var lastLoggedProgress = 0;
            await manager.DownloadUpdatesAsync(update, progress =>
            {
                if (progress >= lastLoggedProgress + 25)
                {
                    lastLoggedProgress = progress;
                    Log($"Download progress {progress}%");
                }
            });

            Log("Download complete; prompting to restart");

            var restartNow = await Dispatcher.UIThread.InvokeAsync(() => PromptToRestartAsync(owner, update.TargetFullRelease.Version.ToString()));

            Log($"Prompt answered; restart now: {restartNow}");

            if (restartNow)
            {
                manager.ApplyUpdatesAndRestart(update);
            }
        }
        catch (Exception exception)
        {
            // The update check must never take the app down; the next launch retries.
            Log($"Update check failed: {exception}");
        }
    }

    private static void Log(string message)
    {
        try
        {
            File.AppendAllText(LogPath, $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
        }
        catch (IOException)
        {
            // Diagnostics only - never let logging break the update flow.
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
