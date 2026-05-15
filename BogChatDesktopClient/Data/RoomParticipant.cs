using Avalonia;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BogChatDesktopClient;

public class RoomParticipant : ObservableObject
{
    private Thickness _showBorder = new(0);

    private Bitmap? _videoStream;
    public string? UserId { get; set; }
    public string? Username { get; set; }

    public Thickness ShowBorder
    {
        get => _showBorder;
        set
        {
            _showBorder = value;
            OnPropertyChanged();
        }
    }

    public Bitmap? VideoStream
    {
        get => _videoStream;
        set
        {
            _videoStream = value;
            OnPropertyChanged();
        }
    }
}