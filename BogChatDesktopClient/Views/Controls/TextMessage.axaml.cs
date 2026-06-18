using Avalonia;
using Avalonia.Controls;
using BogChatDesktopClient.Models;

namespace BogChatDesktopClient.Views.Controls;

public partial class TextMessage : UserControl {
    public static readonly StyledProperty<ChannelMessage> ChannelMessageProperty =
        AvaloniaProperty.Register<TextMessage, ChannelMessage>(
            nameof(ChannelMessage));

    public TextMessage() {
        InitializeComponent();
    }

    public ChannelMessage ChannelMessage {
        get => GetValue(ChannelMessageProperty);
        set => SetValue(ChannelMessageProperty, value);
    }
}