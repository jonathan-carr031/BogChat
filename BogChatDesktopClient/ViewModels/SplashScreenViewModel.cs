using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading;
using System.Threading.Tasks;
using BogChatDesktopClient.Data;
using BogChatDesktopClient.Services;
using BogChatDesktopClient.Services.ApiServices;
using BogChatDesktopClient.ViewModels.Pages;
using CommunityToolkit.Mvvm.ComponentModel;
using Updatum;

namespace BogChatDesktopClient.ViewModels;

internal partial class SplashScreenViewModel : PageViewModel {
    private static readonly UpdatumManager AppUpdater = new("jonathan-carr031", "BogChat") {
        InstallUpdateWindowsExeType = UpdatumWindowsExeType.Installer,
        InstallUpdateWindowsInstallerArguments = "/qb" // Displays a basic user interface for MSI package
    };

    private readonly ApiService _apiService;
    private readonly IAppSessionService _appSessionService;
    private readonly AuthentikService _authentikService;

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private bool _isUpdateAvailable;

    [ObservableProperty] private string _startUpMessage = string.Empty;

    public SplashScreenViewModel(AuthentikService authentikService, IAppSessionService appSessionService,
        ApiService apiService) {
        _authentikService = authentikService;
        _appSessionService = appSessionService;
        _apiService = apiService;
        PageName = PageName.SplashScreen;
    }

    private CancellationToken CancellationToken => _cancellationTokenSource.Token;

    public async Task<PageName> InitializeApplication() {
        await CheckForUpdates();
        var startingPage = await FetchCredentials();


        return startingPage;
    }

    private async Task CheckForUpdates() {
        StartUpMessage = "Checking For Updates...";
        try {
            _isUpdateAvailable = await AppUpdater.CheckForUpdatesAsync();
            if (!_isUpdateAvailable) return;

            StartUpMessage = "Update Available...";

            var downloadedAsset = await AppUpdater.DownloadUpdateAsync(CancellationToken);

            StartUpMessage = "Downloading Update...";

            if (downloadedAsset == null) return;

            StartUpMessage = "Installing Update...";
            await AppUpdater.InstallUpdateAsync(downloadedAsset);
        }
        catch (Exception ex) {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private async Task<PageName> FetchCredentials() {
        StartUpMessage = "Initializing Application...";
        var accessTokenResponse = await DataSaver.FetchAccessToken();
        var accessToken = accessTokenResponse?.AccessToken;
        if (string.IsNullOrWhiteSpace(accessToken)) {
            return PageName.LoginPage;
        }

        StartUpMessage = "Authenticating...";
        var isTokenExpired = JwtHandler.IsTokenExpired(accessToken);
        if (isTokenExpired) {
            var refreshToken = await DataSaver.FetchRefreshToken();
            if (string.IsNullOrWhiteSpace(refreshToken)) {
                return PageName.LoginPage;
            }

            var newAccessToken = await _authentikService.GetNewToken(refreshToken);
            if (newAccessToken == null) {
                return PageName.LoginPage;
            }

            DataSaver.SaveAccessToken(newAccessToken);
            accessTokenResponse = newAccessToken;
        }

        if (!string.IsNullOrWhiteSpace(accessTokenResponse?.AccessToken)) {
            _appSessionService.JwtToken = accessTokenResponse.AccessToken;
        }

        if (!string.IsNullOrWhiteSpace(accessTokenResponse?.RefreshToken)) {
            _appSessionService.RefreshToken = accessTokenResponse.RefreshToken;
        }

        StartUpMessage = "Fetching User Information...";
        var extractedUser = JwtHandler.ExtractUser(new JwtSecurityToken(accessTokenResponse?.AccessToken));
        if (extractedUser == null) {
            return PageName.LoginPage;
        }

        var user = await _apiService.GetOrCreateUser(extractedUser.Id, extractedUser);
        if (user == null) {
            return PageName.LoginPage;
        }

        _appSessionService.CurrentUser = user;
        return string.IsNullOrWhiteSpace(user.Username)
            ? PageName.LoginPage
            : PageName.HomePage;
    }
}