using System;
using BogChatDesktopClient.Data;
using BogChatDesktopClient.ViewModels;

namespace BogChatDesktopClient.Factories;

public class PageFactory(Func<PageNames, PageViewModel> pageViewModelFactory) {
    public PageViewModel GetPageViewModel(PageNames pageName) => pageViewModelFactory.Invoke(pageName);
}