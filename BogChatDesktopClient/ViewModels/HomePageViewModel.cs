using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using BogChatDesktopClient.Data;
using BogChatDesktopClient.Extensions;
using BogChatDesktopClient.Helpers;
using BogChatDesktopClient.Messages;
using BogChatDesktopClient.Models;
using BogChatDesktopClient.Services;
using BogChatDesktopClient.Services.ApiServices;
using CommunityToolkit.Mvvm.Messaging;
using LiveKit.Rtc;
using NAudio.Wave;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using Brush = Avalonia.Media.Brush;
using Brushes = Avalonia.Media.Brushes;
using Room = LiveKit.Rtc.Room;
using VideoStream = LiveKit.Rtc.VideoStream;

namespace BogChatDesktopClient.ViewModels;

public class HomePageViewModel : PageViewModel, IDisposable {
    private readonly ApiService _apiService;
    private readonly LiveKitService _livekitService;
    private readonly MemoryStream _memoryStream = new();
    private readonly IMessenger _messenger;
    private readonly DispatcherTimer _timer;

    private bool _isStreaming;

    private RoomParticipant? _maximizedParticipant;
    private Room? _room;
    private Bitmap? _streamPreview;
    private string? _username;
    private RoomParticipant? LocalRoomParticipant;


    public HomePageViewModel() {
        Username = "Test UserName";
    }

    public HomePageViewModel(LiveKitService livekitService, IMessenger messenger, ApiService apiService) {
        PageName = PageNames.HomePage;
        _livekitService = livekitService;
        _messenger = messenger;
        _apiService = apiService;
        _livekitService.OnFrameCaptured += OnFrameCaptured;

        RoomParticipants = [];

        StreamableItems = [];

        GetStreamableItems();

        _timer = new DispatcherTimer {
            Interval = TimeSpan.FromSeconds(5)
        };
        _timer.Tick += (sender, e) => { CheckRoomParticipants(); };
        _timer.Start();

        _ = GetChannels();
    }

    public RoomParticipant? MaximizedParticipant {
        get => _maximizedParticipant;
        set {
            _maximizedParticipant = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasMaximizedParticipant));
        }
    }

    public bool HasMaximizedParticipant => MaximizedParticipant != null;


    public ObservableCollection<StreamableItem> StreamableItems { get; set; }
    public ObservableCollection<RoomParticipant> RoomParticipants { get; set; } = [];
    public ObservableCollection<string?> RoomPeople { get; set; } = [];
    public ObservableCollection<Channel> Channels { get; set; } = [];

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

    public bool IsStreaming {
        get => _isStreaming;
        set {
            _isStreaming = value;
            OnPropertyChanged();
        }
    }


    public void Dispose() {
        _timer.Stop();
    }

    public async Task GetChannels() {
        var channels = (await _apiService.GetChannels()).OrderBy(channel => channel.ChannelType)
            .ThenBy(channel => channel.Name);

        foreach (var channel in channels) {
            Channels.Add(channel);
        }
    }

    public async Task MaximizePane(RoomParticipant? roomParticipant) {
        MaximizedParticipant = roomParticipant;
    }

    public async Task JoinRoom(Channel channel) {
        switch (channel.ChannelType) {
            case ChannelType.Voice: {
                if (Username != null) _livekitService.SetUsername(Username);
                if (_room is not null && _room.Name == channel.Id.ToString()) {
                    return;
                }

                _room = await _livekitService.JoinRoom(channel.Id.ToString());
                _room.TrackSubscribed += TrackSubscribed;

                // _room.TrackUnsubscribed += TrackUnsubscribed;
                _room.ActiveSpeakersChanged += SpeakerChanged;
                _room.TrackMuted += OnTrackMuted;
                _ = GetRoomParticipants(channel);
                await _livekitService.ConnectMicrophone();

                LocalRoomParticipant = new RoomParticipant {
                    Username = Username,
                    UserId = Username
                };

                RoomParticipants.Add(LocalRoomParticipant);

                break;
            }
            case ChannelType.Afk: {
                //Make this a database read to not worry about livekit bs
                break;
            }
            case ChannelType.Text: {
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public async Task LeaveRoom() {
        await _livekitService.LeaveRoom();
        await _memoryStream.DisposeAsync();
        _room = null;

        RoomParticipants.Clear();
        CheckRoomParticipants();
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
        var processes = WindowsProcessManager.GetStreamableProcesses();

        foreach (var process in processes) {
            Console.WriteLine(
                $"{process.Id} - {process.ProcessName} - {process.MainWindowTitle.RemoveNonStandardCharacters()}");

            StreamableItems.Add(new StreamableItem(process.Id, process.MainWindowTitle.RemoveNonStandardCharacters()));
        }
    }

    private void CheckRoomParticipants() {
        var filteredChannels = Channels.Where(channel => channel.ChannelType == ChannelType.Voice);
        foreach (var channel in filteredChannels) {
            _ = GetRoomParticipants(channel);
        }
    }

    private async Task GetRoomParticipants(Channel channel) {
        var participants = await _livekitService.GetRoomParticipants(channel.Id.ToString());

        channel.Participants.Clear();

        foreach (var participantInfo in participants) {
            channel.Participants.Add(participantInfo.Name);
        }
    }

    public async Task RecordApplication() {
        await _livekitService.StreamDesktop();
        IsStreaming = true;
    }

    public void StopStreaming() {
        _ = _livekitService.StopStreaming();
        StreamPreview = null;
        IsStreaming = false;
    }

    public async Task StreamableItemClickEvent(StreamableItem item) {
        Console.WriteLine($"{item.ProcessId} - {item.WindowTitle} clicked...");

        if (item.ProcessId > 0) {
            _livekitService.StreamApplication((uint)item.ProcessId);
            IsStreaming = true;
        }
    }

    private void OnFrameCaptured(VideoFrame videoFrame) {
        var videoFrameBitmap = ImageProcessor.ConvertToBitmap(videoFrame);
        if (LocalRoomParticipant != null) {
            LocalRoomParticipant.VideoStream = videoFrameBitmap;
        }
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

            switch (eventArgs.Track) {
                case RemoteVideoTrack: {
                    await using var videoStream = new VideoStream(eventArgs.Track);

                    await foreach (var frame in videoStream.WithCancellation(CancellationToken.None)) {
                        roomParticipant.VideoStream = ImageProcessor.ConvertToBitmap(frame.Frame);
                    }

                    break;
                }
                case RemoteAudioTrack: {
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

    public void Logout() {
        _messenger.Send(new LogoutMessage(true));
    }
}