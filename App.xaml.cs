using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using MClauncher.Models;
using MClauncher.Services;
using MClauncher.Views;

namespace MClauncher;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Global exception handling
        DispatcherUnhandledException += (s, ex) =>
        {
            var details = ex.Exception?.ToString() ?? "Unknown UI exception";
            Debug.WriteLine($"[UI Unhandled] {details}");
            Console.Error.WriteLine($"[UI Unhandled] {details}");

            MessageBox.Show(
                $"An unexpected error occurred:\n{ex.Exception?.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );

            ex.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
        {
            var exception = ex.ExceptionObject as Exception;
            var details = exception?.ToString() ?? "Unknown domain exception";
            Debug.WriteLine($"[Domain Unhandled] {details}");
            Console.Error.WriteLine($"[Domain Unhandled] {details}");
        };

        var splash = new SplashScreenWindow();
        splash.Show();

        await Task.Delay(220);
        splash.UpdateStatus("Loading settings...");
        SettingsManager.Load();

        UpdateCheckResult? updateResult = null;
        if (SettingsManager.Current.AutoCheckUpdates)
        {
            splash.UpdateStatus("Checking for updates...");
            updateResult = await LauncherUpdateService.CheckForUpdatesAsync(SettingsManager.Current.UpdateFeedUrl);
        }

        splash.UpdateStatus("Launching Nexus Launcher...");
        await Task.Delay(260);

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;

        splash.Close();
        mainWindow.Show();

        HandleUpdateResult(updateResult);
    }

    private static void HandleUpdateResult(UpdateCheckResult? result)
    {
        if (result == null || !result.Success || !result.IsUpdateAvailable || result.UpdateInfo == null)
            return;

        var info = result.UpdateInfo;
        var currentVersion = LauncherUpdateService.GetCurrentLauncherVersion();

        var message =
            $"New launcher version available!\n\n" +
            $"Current: {currentVersion}\n" +
            $"Latest: {info.Version}\n\n" +
            (!string.IsNullOrWhiteSpace(info.Changelog) ? $"Changelog:\n{info.Changelog}\n\n" : string.Empty) +
            "Open download page now?";

        var buttons = info.Mandatory ? MessageBoxButton.OK : MessageBoxButton.YesNo;
        var response = MessageBox.Show(message, "Launcher Update", buttons, MessageBoxImage.Information);

        var shouldOpen = info.Mandatory || response == MessageBoxResult.Yes;
        if (!shouldOpen)
            return;

        var url = !string.IsNullOrWhiteSpace(info.DownloadUrl)
            ? info.DownloadUrl
            : info.WebsiteUrl;

        if (string.IsNullOrWhiteSpace(url))
            return;

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Update] Failed to open URL: {ex.Message}");
        }
    }
}

