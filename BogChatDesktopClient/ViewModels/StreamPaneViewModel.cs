using System;
using BogChatDesktopClient.Data;

namespace BogChatDesktopClient.ViewModels;

public class StreamPaneViewModel : ViewModelBase {
    private RoomParticipant _roomParticipant;

    public StreamPaneViewModel() {
        Console.WriteLine($"StreamPaneViewModel: {_roomParticipant?.Username}");
    }

    public StreamPaneViewModel(RoomParticipant roomParticipant) {
        _roomParticipant = roomParticipant;
        Console.WriteLine($"StreamPaneViewModel: {_roomParticipant?.Username}");
    }

    public RoomParticipant RoomParticipant {
        get => _roomParticipant;
        set {
            _roomParticipant = value;
            OnPropertyChanged();
        }
    }
}