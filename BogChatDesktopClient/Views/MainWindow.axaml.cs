using Avalonia.Controls;
using Avalonia.Interactivity;
using BogChatDesktopClient.Services;

namespace BogChatDesktopClient.Views;

public partial class MainWindow : Window
{
    private readonly AudioHandler _audioHandler = new();
    private readonly LiveKitService _livekitService = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Mute(object? sender, RoutedEventArgs e)
    {
        _livekitService.ToggleMute();
    }

    private void UnMute(object? sender, RoutedEventArgs e)
    {
        _livekitService.ToggleMute();
    }

    private void RecordMic(object? sender, RoutedEventArgs e)
    {
        _audioHandler.StartRecording();
    }

    private void StopRecording(object? sender, RoutedEventArgs e)
    {
        ApplicationAudioCapture.StopApplicationAudio();
    }

    private void JoinRoom(object? sender, RoutedEventArgs e)
    {
        LeaveRoomButton.IsVisible = true;
        JoinRoomButton.IsVisible = false;
    }

    private void LeaveRoom(object? sender, RoutedEventArgs e)
    {
        JoinRoomButton.IsVisible = true;
        LeaveRoomButton.IsVisible = false;
    }
}