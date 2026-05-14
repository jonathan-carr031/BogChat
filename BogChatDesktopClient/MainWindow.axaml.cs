using Avalonia.Controls;
using Avalonia.Interactivity;
using BogChatDesktopClient.Services;

namespace BogChatDesktopClient;

public partial class MainWindow : Window
{
    private readonly ApplicationAudioCapture _audioCapture = new();

    private readonly AudioHandler _audioHandler = new();
    private readonly LiveKitService _livekitService = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Mute(object? sender, RoutedEventArgs e)
    {
        _livekitService.ToggleMute();
        // _audioHandler.StopMicrophone();
    }

    private void UnMute(object? sender, RoutedEventArgs e)
    {
        _livekitService.ToggleMute();
        // _audioHandler.StartMicrophone();
    }

    private void RecordMic(object? sender, RoutedEventArgs e)
    {
        _audioHandler.StartRecording();
    }

    private void StopRecording(object? sender, RoutedEventArgs e)
    {
        _audioCapture.StopApplicationAudio();
    }
}