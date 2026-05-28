using System;
using System.Threading;
using System.Threading.Tasks;
using Updatum;

namespace BogChatDesktopClient.ViewModels;

internal class SplashScreenViewModel : ViewModelBase {
    private static readonly UpdatumManager AppUpdater = new("jonathan-carr031", "BogChat") {
        InstallUpdateWindowsExeType = UpdatumWindowsExeType.Installer,
        InstallUpdateWindowsInstallerArguments = "/qb" // Displays a basic user interface for MSI package
    };

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private bool _isUpdateAvailable;
    private string _startUpMessage = string.Empty;

    public string StartupMessage {
        get => _startUpMessage;
        set {
            _startUpMessage = value;
            OnPropertyChanged();
        }
    }

    private CancellationToken CancellationToken => _cancellationTokenSource.Token;

    // public void Cancel()
    // {
    //     _cancellationTokenSource.Cancel();
    // }

    public async Task CheckForUpdates() {
        try {
            _isUpdateAvailable = await AppUpdater.CheckForUpdatesAsync();
            if (!_isUpdateAvailable) return;

            StartupMessage = "Update Available...";

            var downloadedAsset = await AppUpdater.DownloadUpdateAsync(CancellationToken);

            StartupMessage = "Downloading Update...";

            if (downloadedAsset == null) return;

            StartupMessage = "Installing Update...";
            await AppUpdater.InstallUpdateAsync(downloadedAsset);
        }
        catch (Exception ex) {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}