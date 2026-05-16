using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using BogChatDesktopClient.Data;
using BogChatDesktopClient.Services;
using LibVLCSharp.Shared;
using LiveKit.Rtc;
using NAudio.Wave;
using Room = LiveKit.Rtc.Room;
using VideoStream = LiveKit.Rtc.VideoStream;


namespace BogChatDesktopClient.ViewModels;

public class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly ApplicationAudioCapture _audioCapture = new();

    private readonly LibVLC _libVlc = new();
    private readonly LiveKitService _livekitService = new();

    private readonly string _outputFolder;
    private readonly VideoConverterService _videoConverter = new();

    private MemoryStream _memoryStream = new();
    private Room? _room;

    private string _username;
    // private StreamMediaInput _streamMediaInput;

    private WaveOutEvent _waveOut;

    public MainWindowViewModel()
    {
        RoomParticipants = [];
        MediaPlayer = new MediaPlayer(_libVlc);

        StreamableItems = [];

        GetStreamableItems();

        _outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SampleImages");
        Directory.CreateDirectory(_outputFolder);
    }

    public Media? Media { get; set; }

    public MediaPlayer MediaPlayer { get; }

    public ObservableCollection<StreamableItem> StreamableItems { get; set; }

    public string Username
    {
        get => _username;
        set
        {
            _username = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<RoomParticipant> RoomParticipants { get; set; }

    public void Dispose()
    {
        MediaPlayer?.Dispose();
        _libVlc?.Dispose();
    }

    private void GetStreamableItems()
    {
        var processes = Process.GetProcesses().Where((process) => !string.IsNullOrEmpty(process.MainWindowTitle));
        var regex = new Regex(@"[^A-Za-z0-9'\s]+");

        foreach (var process in processes)
        {
            Console.WriteLine(
                $"{process.Id} - {process.ProcessName} - {regex.Replace(process.MainWindowTitle, "")}");

            StreamableItems.Add(new StreamableItem(process.Id, regex.Replace(process.MainWindowTitle, "")));
        }
    }

    public async Task StreamableItemClickEvent(StreamableItem item)
    {
        Console.WriteLine($"{item.ProcessId} - {item.WindowTitle} clicked...");

        if (item.ProcessId > 0)
        {
            _ = Task.Run(() => { ApplicationAudioCapture.CaptureApplicationAudio((uint)item.ProcessId); });

            await Task.Delay(5000);

            ApplicationAudioCapture.StopApplicationAudio();
        }
    }

    public async Task JoinRoom()
    {
        _livekitService.SetUsername(Username);
        _room = await _livekitService.JoinRoom("room-name");
        _room.TrackSubscribed += TrackSubscribed;
        _room.ActiveSpeakersChanged += SpeakerChanged;
        await _livekitService.ConnectMicrophone();
    }

    public async Task LeaveRoom()
    {
        await _livekitService.LeaveRoom();
        await _memoryStream.DisposeAsync();

        RoomParticipants.Clear();
    }

    private async void TrackSubscribed(object? sender, TrackSubscribedEventArgs e)
    {
        Console.WriteLine("TrackSubscribed");

        var roomParticipant =
            RoomParticipants.FirstOrDefault(roomParticipant => roomParticipant.UserId == e.Participant.Identity);

        if (roomParticipant == null)
        {
            roomParticipant = new RoomParticipant
            {
                UserId = e.Participant.Identity,
                Username = e.Participant.Name
            };
            RoomParticipants.Add(roomParticipant);
        }

        //TODO: Check for user stop streaming

        if (e.Track is RemoteVideoTrack videoTrack)
        {
            await using var videoStream = new VideoStream(videoTrack);

            await foreach (var frame in videoStream.WithCancellation(CancellationToken.None))
            {
                roomParticipant.VideoStream =
                    _videoConverter.I420ToBitmap(frame.Frame.DataBytes, frame.Frame.Width, frame.Frame.Height);
            }
        }

        if (e.Track is RemoteAudioTrack audioTrack)
        {
            await using var audioStream = new AudioStream(audioTrack);

            var sampleRate = (int)48000;
            var channels = 1;
            var waveFormat = new WaveFormat(sampleRate, channels);
            var bufferedWaveProvider = new BufferedWaveProvider(waveFormat)
            {
                DiscardOnBufferOverflow = true
            };

            var waveOut = new WaveOutEvent();
            waveOut.Init(bufferedWaveProvider);

            waveOut.Play();

            await foreach (var frame in audioStream.WithCancellation(CancellationToken.None))
            {
                bufferedWaveProvider.AddSamples(frame.Frame.DataBytes, 0, frame.Frame.DataBytes.Length);
            }
        }
    }

    private void SpeakerChanged(object? sender, ActiveSpeakersChangedEventArgs e)
    {
        foreach (var participant in RoomParticipants)
        {
            var isSpeaking = e.Speakers.FirstOrDefault(speaker => speaker.Identity == participant.UserId) != null;
            participant.BorderColor = isSpeaking ? Brush.Parse("#44FF33") : Brushes.Transparent;
        }
    }

    public void Play()
    {
        if (Design.IsDesignMode)
        {
            return;
        }

        if (Media != null)
        {
            MediaPlayer.Play(Media);
        }
    }

    public void Stop()
    {
        MediaPlayer.Stop();
    }
}