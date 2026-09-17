using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;
using OSTB.ViewModels;
using OSTB.Views;

namespace OSTB;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = new MainViewModel();
            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel
            };
        }

        base.OnFrameworkInitializationCompleted();

        // Fire off background update check after UI initialization
        _ = CheckForUpdatesAsync();
    }

    private static async Task CheckForUpdatesAsync()
    {
        try
        {
            // Point to your public GitHub repository releases
            var mgr = new UpdateManager(new GithubSource("https://github.com/rowanross417-cmyk/OSTB", string.Empty, false));

            // Check if a newer version tag exists on GitHub
            var newVersion = await mgr.CheckForUpdatesAsync();
            if (newVersion == null)
                return;

            // Download the update package in the background
            await mgr.DownloadUpdatesAsync(newVersion);

            // Apply updates and prompt to restart the application
            mgr.ApplyUpdatesAndRestart(newVersion);
        }
        catch
        {
            // Fails silently if offline, unreleased, or rate-limited
        }
    }
}