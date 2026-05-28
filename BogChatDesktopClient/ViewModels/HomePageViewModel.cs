using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using BogChatDesktopClient.Data;
using BogChatDesktopClient.Services;
using LiveKit.Rtc;
using NAudio.Wave;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using Brush = Avalonia.Media.Brush;
using Brushes = Avalonia.Media.Brushes;
using Room = LiveKit.Rtc.Room;
using VideoStream = LiveKit.Rtc.VideoStream;

namespace BogChatDesktopClient.ViewModels;

public class HomePageViewModel : PageViewModel, IDisposable {
    private readonly LiveKitService _livekitService;

    private readonly MemoryStream _memoryStream = new();

    private readonly DispatcherTimer _timer;

    private Room? _room;

    private Bitmap? _streamPreview;

    private string? _username;

    public HomePageViewModel(LiveKitService livekitService) {
        _livekitService = livekitService;
        _livekitService.OnFrameCaptured += OnFrameCaptured;
        PageName = PageNames.HomePage;

        RoomParticipants = [];

        StreamableItems = [];

        GetStreamableItems();

        _ = GetRoomParticipants("room-name");

        _timer = new DispatcherTimer {
            Interval = TimeSpan.FromSeconds(5)
        };
        _timer.Tick += (sender, e) => { _ = GetRoomParticipants("room-name"); };
        _timer.Start();
    }

    public ObservableCollection<StreamableItem> StreamableItems { get; set; }
    public ObservableCollection<RoomParticipant> RoomParticipants { get; set; }
    public ObservableCollection<string?> RoomPeople { get; set; } = [];

    public Bitmap? StreamPreview {
        get => _streamPreview;
        set {
            _streamPreview = value;
            OnPropertyChanged();
        }
    }

    public string? Username {
        get => _username;
        set {
            _username = value;
            OnPropertyChanged();
        }
    }

    public void Dispose() {
        _timer.Stop();
    }

    public async Task JoinRoom() {
        if (Username != null) _livekitService.SetUsername(Username);
        _room = await _livekitService.JoinRoom("room-name");
        _room.TrackSubscribed += TrackSubscribed;
        _room.ActiveSpeakersChanged += SpeakerChanged;
        _room.TrackMuted += OnTrackMuted;
        _ = GetRoomParticipants("room-name");
        await _livekitService.ConnectMicrophone();
    }

    public async Task LeaveRoom() {
        await _livekitService.LeaveRoom();
        await _memoryStream.DisposeAsync();

        RoomParticipants.Clear();
        _ = GetRoomParticipants("room-name");
    }

    private void OnTrackMuted(object? sender, TrackMutedEventArgs e) {
        var roomParticipant =
            RoomParticipants.FirstOrDefault(roomParticipant => roomParticipant.UserId == e.Participant.Identity);

        if (roomParticipant == null) {
            roomParticipant = new RoomParticipant {
                UserId = e.Participant.Identity,
                Username = e.Participant.Name
            };
            RoomParticipants.Add(roomParticipant);
        }

        if (e.Publication.Track is RemoteVideoTrack _) {
            Task.Run(async () => {
                await Task.Delay(250);
                roomParticipant.ClearVideoStream();
            });
        }
    }

    private void GetStreamableItems() {
        var processes = Process.GetProcesses().Where(process => !string.IsNullOrEmpty(process.MainWindowTitle));
        var regex = new Regex(@"[^A-Za-z0-9'\s]+");

        foreach (var process in processes) {
            Console.WriteLine(
                $"{process.Id} - {process.ProcessName} - {regex.Replace(process.MainWindowTitle, "")}");

            StreamableItems.Add(new StreamableItem(process.Id, regex.Replace(process.MainWindowTitle, "")));
        }
    }

    private async Task GetRoomParticipants(string roomName) {
        var participants = await _livekitService.GetRoomParticipants(roomName);

        RoomPeople.Clear();

        foreach (var participantInfo in participants) {
            RoomPeople.Add(participantInfo.Name);
        }
    }

    public async Task RecordApplication() {
        await _livekitService.InitializeVideoSource();
    }

    public async Task StopStreaming() {
        await _livekitService.StopStreaming();
        StreamPreview = null;
    }

    public async Task StreamableItemClickEvent(StreamableItem item) {
        Console.WriteLine($"{item.ProcessId} - {item.WindowTitle} clicked...");

        if (item.ProcessId > 0) {
            _ = Task.Run(() => { ApplicationAudioCapture.CaptureApplicationAudio((uint)item.ProcessId); });

            await Task.Delay(5000);

            ApplicationAudioCapture.StopApplicationAudio();
        }
    }

    private void OnFrameCaptured(VideoFrame videoFrame) {
        var videoFrameBitmap = VideoConverterService.ConvertToBitmap(videoFrame);
        StreamPreview = videoFrameBitmap;
    }

    private async void TrackSubscribed(object? sender, TrackSubscribedEventArgs eventArgs) {
        try {
            var roomParticipant =
                RoomParticipants.FirstOrDefault(roomParticipant =>
                    roomParticipant.UserId == eventArgs.Participant.Identity);

            if (roomParticipant == null) {
                roomParticipant = new RoomParticipant {
                    UserId = eventArgs.Participant.Identity,
                    Username = eventArgs.Participant.Name
                };
                RoomParticipants.Add(roomParticipant);
            }

            //TODO: Check for user stop streaming
            switch (eventArgs.Track) {
                case RemoteVideoTrack: {
                    Console.WriteLine("Is a Video Track");
                    await using var videoStream = new VideoStream(eventArgs.Track);

                    await foreach (var frame in videoStream.WithCancellation(CancellationToken.None)) {
                        roomParticipant.VideoStream = VideoConverterService.ConvertToBitmap(frame.Frame);
                    }

                    break;
                }
                case RemoteAudioTrack: {
                    Console.WriteLine("Is an Audio Track");
                    await using var audioStream = new AudioStream(eventArgs.Track);

                    var sampleRate = (int)audioStream.SampleRate;
                    var channels = (int)audioStream.NumChannels;
                    var waveFormat = new WaveFormat(sampleRate, channels);
                    var bufferedWaveProvider = new BufferedWaveProvider(waveFormat) {
                        DiscardOnBufferOverflow = true
                    };

                    var waveOut = new WaveOutEvent();
                    waveOut.Init(bufferedWaveProvider);

                    waveOut.Play();

                    await foreach (var frame in audioStream.WithCancellation(CancellationToken.None)) {
                        bufferedWaveProvider.AddSamples(frame.Frame.DataBytes, 0, frame.Frame.DataBytes.Length);
                    }

                    break;
                }
            }
        }
        catch (Exception e) {
            Console.WriteLine($"{e.Message} - {e.StackTrace}");
        }
    }

    private void SpeakerChanged(object? sender, ActiveSpeakersChangedEventArgs e) {
        foreach (var participant in RoomParticipants) {
            var isSpeaking = e.Speakers.FirstOrDefault(speaker => speaker.Identity == participant.UserId) != null;
            participant.BorderColor = isSpeaking ? Brush.Parse("#44FF33") : Brushes.Transparent;
        }
    }

    public void MuteVoice() {
        _livekitService.ToggleMute();
    }

    public void UnmuteVoice() {
        _livekitService.ToggleMute();
    }
}