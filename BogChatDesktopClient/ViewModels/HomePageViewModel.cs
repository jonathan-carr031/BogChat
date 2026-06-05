using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using BogChatDesktopClient.Data;
using BogChatDesktopClient.Extensions;
using BogChatDesktopClient.Helpers;
using BogChatDesktopClient.Messages;
using BogChatDesktopClient.Services;
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
    private readonly LiveKitService _livekitService;
    private readonly MemoryStream _memoryStream = new();
    private readonly IMessenger _messenger;
    private readonly DispatcherTimer _timer;

    private bool _isStreaming;

    private RoomParticipant? _maximizedParticipant;
    private Room? _room;
    private Bitmap? _streamPreview;
    private string? _username;

    public HomePageViewModel(IMessenger messenger) {
        _messenger = messenger;
        Username = "Test UserName";
    }

    public HomePageViewModel(LiveKitService livekitService, IMessenger messenger) {
        PageName = PageNames.HomePage;
        _livekitService = livekitService;
        _messenger = messenger;
        _livekitService.OnFrameCaptured += OnFrameCaptured;

        RoomParticipants = [];

        StreamableItems = [];

        GetStreamableItems();

        _ = GetRoomParticipants("room-name");

        _timer = new DispatcherTimer {
            Interval = TimeSpan.FromSeconds(5)
        };
        _timer.Tick += (sender, e) => { _ = GetRoomParticipants("room-name"); };
        _timer.Start();

        var self = new RoomParticipant {
            Username = "self"
        };
        var trash = new RoomParticipant {
            Username = "trash"
        };
        var azytzeen = new RoomParticipant {
            Username = "azytzeen"
        };
        var ahr102 = new RoomParticipant {
            Username = "ahr102"
        };
        var koldmilk = new RoomParticipant {
            Username = "koldmilk"
        };

        RoomParticipants.Add(self);
        RoomParticipants.Add(trash);
        RoomParticipants.Add(azytzeen);
        RoomParticipants.Add(ahr102);
        RoomParticipants.Add(koldmilk);
        var timer = new DispatcherTimer {
            Interval = TimeSpan.FromSeconds(5)
        };

        timer.Tick += OnTimerOnTick;

        timer.Start();
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

    private void OnTimerOnTick(object? sender, EventArgs e) {
        var user = RoomParticipants.First();
        if (RoomParticipants.Count == 5) {
            RoomParticipants.RemoveAt(RoomParticipants.Count - 1);
            user.VideoStream = new Bitmap(@"C:\Users\jonat\Desktop\ScreenCapture\test_picture.jpeg");
        }
        else {
            RoomParticipants.Add(new RoomParticipant {
                Username = "koldmilk"
            });

            user.VideoStream = new WriteableBitmap(
                new PixelSize(200, 200),
                new Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Premul);
        }
    }

    public async Task MaximizePane(RoomParticipant? roomParticipant) {
        MaximizedParticipant = roomParticipant;
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
        var processes = WindowsProcessManager.GetStreamableProcesses();

        foreach (var process in processes) {
            Console.WriteLine(
                $"{process.Id} - {process.ProcessName} - {process.MainWindowTitle.RemoveNonStandardCharacters()}");

            StreamableItems.Add(new StreamableItem(process.Id, process.MainWindowTitle.RemoveNonStandardCharacters()));
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
                        roomParticipant.VideoStream = ImageProcessor.ConvertToBitmap(frame.Frame);
                    }

                    break;
                }
                case RemoteAudioTrack: {
                    Console.WriteLine("Is an Audio Track");
                    await using var audioStream = new AudioStream(eventArgs.Track);

                    var sampleRate = (int)audioStream.SampleRate;
                    // var sampleRate = (int)44100;
                    var channels = (int)audioStream.NumChannels;
                    // var channels = (int)1;
                    var waveFormat = new WaveFormat(sampleRate, channels);
                    var bufferedWaveProvider = new BufferedWaveProvider(waveFormat) {
                        DiscardOnBufferOverflow = true
                    };

                    var waveOut = new WaveOutEvent();
                    waveOut.Init(bufferedWaveProvider);

                    waveOut.Play();

                    await foreach (var frame in audioStream.WithCancellation(CancellationToken.None)) {
                        // Console.WriteLine(@"\=====================================/");
                        // Console.WriteLine($"Track: {eventArgs.Track.Name}");
                        // Console.WriteLine($"Audio Info: {audioStream.SampleRate} - {audioStream.NumChannels}");
                        // Console.WriteLine($"Bytes Received: {frame.Frame.DataBytes.Length}");
                        // Console.WriteLine(@"/=====================================\");
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

    public async Task AddParticipants() {
        RoomParticipants.Add(new RoomParticipant {
            Username = "User1"
        });
        RoomParticipants.Add(new RoomParticipant {
            Username = "User2"
        });
        RoomParticipants.Add(new RoomParticipant {
            Username = "User3"
        });
        RoomParticipants.Add(new RoomParticipant {
            Username = "User4"
        });
        RoomParticipants.Add(new RoomParticipant {
            Username = "User5"
        });
    }

    public void Logout() {
        _messenger.Send(new LogoutMessage(true));
    }
}