using System.Runtime.Versioning;
using BogChatDesktopClient.Data;
using BogChatDesktopClient.Factories;
using BogChatDesktopClient.Messages;
using BogChatDesktopClient.ViewModels.Pages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace BogChatDesktopClient.ViewModels;

[SupportedOSPlatform("windows")]
public partial class MainWindowViewModel : ViewModelBase {
    private readonly PageFactory _pageFactory;

    [ObservableProperty] private PageViewModel? _currentPage;

    public MainWindowViewModel(IMessenger messenger, PageFactory pageFactory) {
        _pageFactory = pageFactory;

        messenger.Register<MainWindowViewModel, LoginSuccessMessage>(this,
            (_, message) => { CurrentPage = (HomePageViewModel)_pageFactory.GetPageViewModel(PageName.HomePage); });

        messenger.Register<MainWindowViewModel, LogoutMessage>(this,
            (_, message) => { CurrentPage = (LoginPageViewModel)_pageFactory.GetPageViewModel(PageName.LoginPage); });
    }

    public void SetCurrentPage(PageName pageName) {
        CurrentPage = _pageFactory.GetPageViewModel(pageName);
    }
}