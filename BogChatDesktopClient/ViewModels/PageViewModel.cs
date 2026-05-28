using BogChatDesktopClient.Data;

namespace BogChatDesktopClient.ViewModels;

public class PageViewModel : ViewModelBase {
    private PageNames _pageName;

    public PageNames PageName {
        get => _pageName;
        set {
            _pageName = value;
            OnPropertyChanged();
        }
    }
}