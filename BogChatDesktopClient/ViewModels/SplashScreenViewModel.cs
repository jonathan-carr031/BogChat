using System;
using System.Threading;
using System.Threading.Tasks;
using Updatum;

namespace BogChatDesktopClient.ViewModels;

internal partial class SplashScreenViewModel : ViewModelBase
{
    private static readonly UpdatumManager AppUpdater = new("jonathan-carr031", "BogChat")
    {
        InstallUpdateWindowsExeType = UpdatumWindowsExeType.Installer,
        InstallUpdateWindowsInstallerArguments = "/qb" // Displays a basic user interface for MSI package
    };

    private CancellationTokenSource _cancellationTokenSource = new();

    private bool _isUpdateAvailable;
    private string _startUpMessage = string.Empty;

    public string StartupMessage
    {
        get { return _startUpMessage; }
        set
        {
            _startUpMessage = value;
            OnPropertyChanged();
        }
    }

    public CancellationToken CancellationToken => _cancellationTokenSource.Token;

    public void Cancel()
    {
        _cancellationTokenSource.Cancel();
    }

    public async Task CheckForUpdates()
    {
        try
        {
            _isUpdateAvailable = await AppUpdater.CheckForUpdatesAsync();
            if (!_isUpdateAvailable) return;

            var downloadedAsset = await AppUpdater.DownloadUpdateAsync();

            if (downloadedAsset == null) return;

            //await AppUpdater.InstallUpdateAsync(downloadedAsset);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}