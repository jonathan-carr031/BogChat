using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BogChatDesktopClient.Data;

public class RoomParticipant : ObservableObject
{
    private IBrush? _borderColor;

    private string _speakingIndicator;
    private Bitmap? _videoStream;
    public string? UserId { get; set; }
    public string? Username { get; set; }

    public Bitmap? VideoStream
    {
        get => _videoStream;
        set
        {
            _videoStream = value;
            OnPropertyChanged();
        }
    }

    public IBrush? BorderColor
    {
        get => _borderColor;
        set
        {
            _borderColor = value;
            OnPropertyChanged();
        }
    }
}