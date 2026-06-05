using System.Runtime.Versioning;
using System.Threading.Tasks;
using BogChatDesktopClient.Data;
using BogChatDesktopClient.Factories;
using BogChatDesktopClient.Messages;
using BogChatDesktopClient.Services;
using CommunityToolkit.Mvvm.Messaging;

namespace BogChatDesktopClient.ViewModels;

[SupportedOSPlatform("windows")]
public class MainWindowViewModel : ViewModelBase {
    private readonly AuthentikService _authentikService;
    private readonly PageFactory _pageFactory;
    private PageViewModel? _currentPage;

    public MainWindowViewModel(IMessenger messenger, PageFactory pageFactory, AuthentikService authentikService) {
        _pageFactory = pageFactory;
        _authentikService = authentikService;

        messenger.Register<MainWindowViewModel, LoginSuccessMessage>(this, (_, message) => {
            var homePage = (HomePageViewModel)_pageFactory.GetPageViewModel(PageNames.HomePage);
            homePage.Username = message.Value;
            CurrentPage = homePage;
        });

        messenger.Register<MainWindowViewModel, LogoutMessage>(this, (_, message) => {
            var loginPage = (LoginPageViewModel)_pageFactory.GetPageViewModel(PageNames.LoginPage);
            CurrentPage = loginPage;
        });

        _ = GetLoginStatus();
    }

    public PageViewModel? CurrentPage {
        get => _currentPage;
        set {
            _currentPage = value;
            OnPropertyChanged();
        }
    }

    private async Task GetLoginStatus() {
        var accessTokenResponse = await DataSaver.FetchAccessToken();

        var accessToken = accessTokenResponse?.AccessToken;
        if (string.IsNullOrWhiteSpace(accessToken)) {
            CurrentPage = _pageFactory.GetPageViewModel(PageNames.LoginPage);
            return;
        }

        var isTokenExpired = JwtHandler.IsTokenExpired(accessToken);
        if (isTokenExpired) {
            var refreshToken = await DataSaver.FetchRefreshToken();
            if (string.IsNullOrWhiteSpace(refreshToken)) {
                CurrentPage = _pageFactory.GetPageViewModel(PageNames.LoginPage);
                return;
            }

            var newAccessToken = await _authentikService.GetNewToken(refreshToken);
            if (newAccessToken == null) {
                CurrentPage = _pageFactory.GetPageViewModel(PageNames.LoginPage);
                return;
            }

            DataSaver.SaveAccessToken(newAccessToken);
        }

        var username = await DataSaver.FetchUserName();
        if (string.IsNullOrWhiteSpace(username)) {
            CurrentPage = _pageFactory.GetPageViewModel(PageNames.LoginPage);
        }
        else {
            var homePage = (HomePageViewModel)_pageFactory.GetPageViewModel(PageNames.HomePage);
            homePage.Username = username;
            CurrentPage = homePage;
        }
    }
}