using System;
using System.Threading.Tasks;
using BogChatDesktopClient.Data;
using BogChatDesktopClient.Messages;
using BogChatDesktopClient.Services;
using BogChatDesktopClient.Services.ApiServices;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace BogChatDesktopClient.ViewModels.Pages;

public partial class LoginPageViewModel : PageViewModel {
    private readonly ApiService _apiService;
    private readonly IAppSessionService _appSessionService;
    private readonly IMessenger _messenger;
    private readonly OAuthService _oAuthService;

    public LoginPageViewModel(IMessenger messenger, IAppSessionService appSessionService, ApiService apiService,
        OAuthService oAuthService) {
        PageName = PageName.LoginPage;

        _messenger = messenger;
        _appSessionService = appSessionService;
        _apiService = apiService;
        _oAuthService = oAuthService;
    }

    public string? ErrorMessage { get; set; }

    [RelayCommand]
    public async Task LoginCommand() {
        ErrorMessage = null;
        var accessTokenResponse = await _oAuthService.StartOAuth();
        if (accessTokenResponse == null) {
            ErrorMessage = "Failed to Login";
            return;
        }

        DataSaver.SaveAccessToken(accessTokenResponse);

        var accessToken = accessTokenResponse.AccessToken;
        if (string.IsNullOrEmpty(accessToken)) {
            ErrorMessage = "Invalid Access Token";
            return;
        }

        if (!string.IsNullOrWhiteSpace(accessTokenResponse.AccessToken)) {
            _appSessionService.JwtToken = accessTokenResponse.AccessToken;
        }

        if (!string.IsNullOrWhiteSpace(accessTokenResponse.RefreshToken)) {
            _appSessionService.RefreshToken = accessTokenResponse.RefreshToken;
        }

        var token = JwtHandler.Decode(accessToken);


        if (token != null) {
            var tempUser = JwtHandler.ExtractUser(token);
            if (tempUser == null) {
                ErrorMessage = "Invalid Token Format";
                return;
            }

            var user = await _apiService.GetOrCreateUser(Guid.Parse(token.Subject), tempUser);
            if (user == null) {
                ErrorMessage = "Unable to fetch user data";
                return;
            }

            _appSessionService.CurrentUser = user;
            _messenger.Send(new LoginSuccessMessage(""));
        }

        ErrorMessage = "Unable to Login";
    }
}