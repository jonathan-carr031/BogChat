using BogChatDesktopClient.Data;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BogChatDesktopClient.ViewModels.Pages;

public partial class PageViewModel : ViewModelBase {
    [ObservableProperty] private PageName _pageName;
}