using Avalonia;
using Avalonia.Controls;
using BogChatDesktopClient.Data;
using BogChatDesktopClient.ViewModels;

namespace BogChatDesktopClient.Controls;

public partial class StreamPane : UserControl {
    public static readonly StyledProperty<RoomParticipant> RoomParticipantProperty =
        AvaloniaProperty.Register<StreamPane, RoomParticipant>(
            nameof(RoomParticipant));

    public StreamPane(RoomParticipant roomParticipant) {
        RoomParticipant = roomParticipant;
        InitializeComponent();

        DataContext = new StreamPaneViewModel(roomParticipant);

        if (DataContext is StreamPaneViewModel streamPaneViewModel) {
            streamPaneViewModel.RoomParticipant = RoomParticipant;
        }
    }

    public RoomParticipant RoomParticipant {
        get => GetValue(RoomParticipantProperty);
        init => SetValue(RoomParticipantProperty, value);
    }
}