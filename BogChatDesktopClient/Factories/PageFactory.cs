using System;
using BogChatDesktopClient.Data;
using PageViewModel = BogChatDesktopClient.ViewModels.Pages.PageViewModel;

namespace BogChatDesktopClient.Factories;

public class PageFactory(Func<PageName, PageViewModel> pageViewModelFactory) {
    public PageViewModel GetPageViewModel(PageName pageName) => pageViewModelFactory.Invoke(pageName);
}