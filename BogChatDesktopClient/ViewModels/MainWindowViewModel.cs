using System;
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
    private readonly PageFactory _pageFactory;
    private PageViewModel? _currentPage;

    public MainWindowViewModel(IMessenger messenger, PageFactory pageFactory) {
        _pageFactory = pageFactory;

        messenger.Register<MainWindowViewModel, LoginSuccessMessage>(this, (_, message) => {
            var homePage = (HomePageViewModel)_pageFactory.GetPageViewModel(PageNames.HomePage);
            homePage.Username = message.Value;
            CurrentPage = homePage;
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
        var username = await DataSaver.FetchUserName();

        Console.WriteLine($"Refresh Token: {await DataSaver.FetchRefreshToken()}");

        var accessTokenResponse = await DataSaver.FetchAccessToken();

        var accessToken = accessTokenResponse?.AccessToken;
        if (accessToken != null) {
            var token = JwtHandler.Decode(accessToken);
            Console.WriteLine(token);
            Console.WriteLine($"Is Token Expired? {JwtHandler.IsTokenExpired(accessToken)}");
        }

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