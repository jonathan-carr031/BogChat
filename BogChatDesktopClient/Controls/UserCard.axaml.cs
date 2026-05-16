using Avalonia;
using Avalonia.Controls;
using BogChatDesktopClient.Data;

namespace BogChatDesktopClient.Controls;

public partial class UserCard : UserControl
{
    public static readonly StyledProperty<RoomParticipant> RoomParticipantProperty =
        AvaloniaProperty.Register<UserCard, RoomParticipant>(
            nameof(RoomParticipant));

    public UserCard()
    {
        InitializeComponent();
    }

    public RoomParticipant RoomParticipant
    {
        get => GetValue(RoomParticipantProperty);
        set => SetValue(RoomParticipantProperty, value);
    }
}