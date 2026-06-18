using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using BogChatDesktopClient.ViewModels.Controls;

namespace BogChatDesktopClient.Views.Controls;

public partial class TextChannelView : UserControl {
    public TextChannelView() {
        InitializeComponent();

        TextScrollViewer.ScrollToEnd();
    }

    private void GifSearchText_OnKeyUp(object? sender, KeyEventArgs e) {
        if (DataContext is TextChannelViewModel textChannelViewModel) {
            _ = textChannelViewModel.SearchGifs();
        }
    }

    private void GifButton_OnClick(object? sender, RoutedEventArgs e) {
        GifSearchSection.IsVisible ^= true;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e) {
        if (sender is ScrollViewer scrollViewer && e.Delta.Y != 0) {
            scrollViewer.Offset = scrollViewer.Offset.WithX(scrollViewer.Offset.X - e.Delta.Y * 50);
            e.Handled = true;
        }
    }
}