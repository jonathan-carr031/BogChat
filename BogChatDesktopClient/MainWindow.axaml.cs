using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BogChatDesktopClient.Services;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace BogChatDesktopClient;

public partial class MainWindow : Window
{
    private readonly LiveKitService _livekitService = new();

    private readonly AudioHandler _audioHandler = new();

    private readonly ApplicationAudioCapture _audioCapture = new();

    public MainWindow()
    {
        InitializeComponent();

        // _audioHandler.StartMicrophone();

        // _ = _livekitService.JoinRoom("room-name");
    }

    private async Task GetApplicationAudioTest()
    {
        var deviceEnumerator = new MMDeviceEnumerator();
        var device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

        // Access all active audio sessions (apps currently making sound)
        var sessions = device.AudioSessionManager.Sessions;

        var processes = Process.GetProcesses();
        foreach (var process in processes)
        {
            Console.WriteLine(process.ProcessName);
        }

        for (int i = 0; i < sessions.Count; i++)
        {
            var session = sessions[i];

            // Check for a specific application by process name
            if (session.GetProcessID != 0)
            {
                var process = Process.GetProcessById((int)session.GetProcessID);
                if (process.ProcessName.Contains("Spotify"))
                {
                    // Set volume (0.0 to 1.0)
                    session.SimpleAudioVolume.Volume = 0.4f;

                    _ = Task.Run(() => { _audioCapture.CaptureApplicationAudio((uint)process.Id); });
                }
            }
        }
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // await _livekitService.JoinRoom("room-name");
        // await _livekitService.ConnectMicrophone();

        _ = GetApplicationAudioTest();
    }

    private async void JoinRoom(object? sender, RoutedEventArgs routedEventArgs)
    {
        await _livekitService.JoinRoom("room-name");
        await _livekitService.ConnectMicrophone();
    }

    private async void LeaveRoom(object? sender, RoutedEventArgs routedEventArgs)
    {
        await _livekitService.LeaveRoom();
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