using System.Runtime.Versioning;
using System.Threading.Tasks;
using BogChatDesktopClient.Data;
using BogChatDesktopClient.Factories;
using BogChatDesktopClient.Messages;
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
        var username = await DataSaver.FetchData();

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