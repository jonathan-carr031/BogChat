using System;
using System.Text;
using System.Threading.Tasks;
using LiveKit.Rtc;
using Livekit.Server.Sdk.Dotnet;
using Room = LiveKit.Rtc.Room;

namespace BogChatDesktopClient.Services;

public class LiveKitService
{
    private const string LiveKitUrl = "wss://bogchat-c8y7wswc.livekit.cloud";

    // private const string ApiKey = "devkey";
    private const string ApiKey = "APIGNcD9KetoFXf";

    // private const string ApiSecret = "nfFhkIxwefgFzu50reCSmesvtmuHPTzZ";
    private const string ApiSecret = "grK2qDGUOc4ylMGt2Jx4KYHFHnnzoCsDOXpKSh7nPnJ";

    private readonly AudioHandler _audioHandler;

    private bool _isMuted;
    private AudioSource _microphoneAudioSource;
    private LocalTrackPublication _publication;

    private Room? _room;
    private string _username = "desktop";

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
            .WithIdentity($"{_username}-identity")
            .WithName(_username)
            .WithGrants(new VideoGrants { RoomJoin = true, Room = roomName })
            .WithTtl(TimeSpan.FromHours(24));

        return token.ToJwt();
    }

    public async Task<Room> JoinRoom(string roomName)
    {
        if (_room != null)
        {
            await LeaveRoom();
        }

        var accessToken = GetAccessToken(roomName);

        _room = new Room();
        await _room.ConnectAsync(LiveKitUrl, accessToken,
            new RoomOptions { AutoSubscribe = true });

        Console.WriteLine($"Connected to {_room.Name}");

        // _room.TrackSubscribed += async (sender, e) => { };

        _room.ParticipantConnected += (sender, participant) => { Console.WriteLine($"{participant.Identity} joined"); };

        _room.ActiveSpeakersChanged += (sender, e) => { Console.WriteLine($"Active speakers: {e.Speakers.Count}"); };

        _room.DataReceived += (sender, e) =>
        {
            var message = Encoding.UTF8.GetString(e.Data);
            Console.WriteLine($"Data from {e.Participant?.Identity}: {message}");
        };

        return _room;
    }

    public async Task ConnectMicrophone()
    {
        if (_room == null) return;
        _microphoneAudioSource = InitializeAudioSource();

        var audioTrack = LocalAudioTrack.Create($"{_username}-audio", _microphoneAudioSource);
        _publication = await _room.LocalParticipant!.PublishTrackAsync(audioTrack);
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
        if (_room != null)
        {
            await _room.DisconnectAsync();
            _room = null;
        }
    }

    public void SetUsername(string username)
    {
        _username = username;
    }
}