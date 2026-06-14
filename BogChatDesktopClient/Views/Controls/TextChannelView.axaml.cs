using Avalonia.Controls;

namespace BogChatDesktopClient.Views.Controls;

public partial class TextChannelView : UserControl {
    public TextChannelView() {
        InitializeComponent();

        TextScrollViewer.ScrollToEnd();
    }
}