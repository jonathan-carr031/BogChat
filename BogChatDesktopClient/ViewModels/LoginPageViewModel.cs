using System.Threading.Tasks;
using BogChatDesktopClient.Data;
using BogChatDesktopClient.Messages;
using BogChatDesktopClient.Services;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace BogChatDesktopClient.ViewModels;

public partial class LoginPageViewModel : PageViewModel {
    private IMessenger _messenger;

    public LoginPageViewModel(IMessenger messenger) {
        PageName = PageNames.LoginPage;

        _messenger = messenger;
    }

    public string? ErrorMessage { get; set; }

    [RelayCommand]
    public async Task LoginCommand() {
        ErrorMessage = null;
        var accessTokenResponse = await OAuthService.StartOAuth();
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

        var token = JwtHandler.Decode(accessToken);
        if (token != null) {
            var username = JwtHandler.ExtractUsername(token);
            if (string.IsNullOrEmpty(username)) {
                ErrorMessage = "Unable to retrieve username";
                return;
            }

            _messenger.Send(new LoginSuccessMessage(username));
        }

        ErrorMessage = "Unable to Login";
    }
}