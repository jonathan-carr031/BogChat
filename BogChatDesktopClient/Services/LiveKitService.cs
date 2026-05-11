using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input.Raw;
using Livekit.Server.Sdk.Dotnet;
using LiveKit.Rtc;
using NAudio.Wave;
using Room = LiveKit.Rtc.Room;

namespace BogChatDesktopClient.Services;

public class LiveKitService
{
    private const string LiveKitUrl = "wss://bogchat-c8y7wswc.livekit.cloud";

    // private const string ApiKey = "devkey";
    private const string ApiKey = "APIGNcD9KetoFXf";

    // private const string ApiSecret = "nfFhkIxwefgFzu50reCSmesvtmuHPTzZ";
    private const string ApiSecret = "grK2qDGUOc4ylMGt2Jx4KYHFHnnzoCsDOXpKSh7nPnJ";
    private const string Username = "desktop";
    private const string UserIdentity = "desktop-identity";

    private readonly AudioHandler _audioHandler;

    private Room _room;
    private LocalTrackPublication _publication;
    private AudioSource _microphoneAudioSource;

    private bool _isMuted;

    public LiveKitService()
    {
        _audioHandler = new AudioHandler();
    }

    private void OnDataReceived(byte[] buffer, int bytes)
    {
        if (_isMuted) return;
        
        var audioFrame = new AudioFrame(buffer, _audioHandler.WaveIn.WaveFormat.SampleRate,
            _audioHandler.WaveIn.WaveFormat.Channels, 1440);
        _ = _microphoneAudioSource.CaptureFrameAsync(audioFrame);
    }

    private string GetAccessToken(string roomName)
    {
        var token = new AccessToken(ApiKey, ApiSecret)
            .WithIdentity(UserIdentity)
            .WithName(Username)
            .WithGrants(new VideoGrants { RoomJoin = true, Room = roomName })
            .WithTtl(TimeSpan.FromHours(24));

        return token.ToJwt();
    }

    public async Task JoinRoom(string roomName)
    {
        var accessToken = GetAccessToken(roomName);

        _room = new Room();
        await _room.ConnectAsync(LiveKitUrl, accessToken,
            new RoomOptions { AutoSubscribe = true });

        Console.WriteLine($"Connected to {_room.Name}");

        _room.TrackSubscribed += async (sender, e) =>
        {
            Console.WriteLine($"Subscribed to track: {e.Track.Sid}");
            if (e.Track is RemoteVideoTrack videoTrack)
            {
                using var videoStream = new VideoStream(videoTrack);
                await foreach (var frame in videoStream.WithCancellation(CancellationToken.None))
                {
                    // Process video frame
                    Console.WriteLine($"Frame: {frame.Frame.Width}x{frame.Frame.Height}");
                }
            }
        };

        _room.ParticipantConnected += (sender, participant) => { Console.WriteLine($"{participant.Identity} joined"); };

        _room.ActiveSpeakersChanged += (sender, e) => { Console.WriteLine($"Active speakers: {e.Speakers.Count}"); };

        _room.DataReceived += (sender, e) =>
        {
            var message = Encoding.UTF8.GetString(e.Data);
            Console.WriteLine($"Data from {e.Participant?.Identity}: {message}");
        };
    }

    public async Task ConnectMicrophone()
    {
        _microphoneAudioSource = InitializeAudioSource();

        var audioTrack = LocalAudioTrack.Create($"{Username}-audio", _microphoneAudioSource);
        _publication = await _room.LocalParticipant!.PublishTrackAsync(audioTrack);

        Console.WriteLine(_publication);
    }

    public void ToggleMute()
    {
        _isMuted = !_isMuted;
    }

    private AudioSource InitializeAudioSource()
    {
        _audioHandler.OnDataReceived = OnDataReceived;
        _audioHandler.StartMicrophone();
        return new AudioSource(_audioHandler.WaveIn.WaveFormat.SampleRate,
            _audioHandler.WaveIn.WaveFormat.Channels);
    }

    public async Task LeaveRoom()
    {
        await _room.DisconnectAsync();
    }
}