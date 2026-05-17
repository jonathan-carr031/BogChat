using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BogChatDesktopClient.Data;

public class RoomParticipant : ObservableObject
{
    private IBrush? _borderColor;

    private Bitmap? _videoStream;
    public string? UserId { get; init; }
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

    private static Bitmap EmptyBitmap => new WriteableBitmap(
        new PixelSize(200, 200),
        new Vector(96, 96),
        PixelFormat.Bgra8888,
        AlphaFormat.Premul);

    public void ClearVideoStream()
    {
        VideoStream = EmptyBitmap;
    }
}