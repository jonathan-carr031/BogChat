using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BogChatDesktopClient.ScreenCapture;
using LiveKit.Proto;
using LiveKit.Rtc;
using Livekit.Server.Sdk.Dotnet;
using ListParticipantsRequest = Livekit.Server.Sdk.Dotnet.ListParticipantsRequest;
using ParticipantInfo = Livekit.Server.Sdk.Dotnet.ParticipantInfo;
using Room = LiveKit.Rtc.Room;
using RoomOptions = LiveKit.Rtc.RoomOptions;

namespace BogChatDesktopClient.Services;

public class LiveKitService
{
    private const string LiveKitUrl = "wss://bogchat-c8y7wswc.livekit.cloud";

    // private const string ApiKey = "devkey";
    private const string ApiKey = "APIGNcD9KetoFXf";

    // private const string ApiSecret = "nfFhkIxwefgFzu50reCSmesvtmuHPTzZ";
    private const string ApiSecret = "grK2qDGUOc4ylMGt2Jx4KYHFHnnzoCsDOXpKSh7nPnJ";

    private readonly AudioHandler _audioHandler;
    private readonly RoomServiceClient _roomServiceClient = new(LiveKitUrl, ApiKey, ApiSecret);

    private readonly ApplicationVideoCapture _videoCapture;
    // private LocalTrackPublication? _publication;

    private byte[] _data = [];

    private bool _isMuted;
    private AudioSource? _microphoneAudioSource;
    private string _outputFileName;

    private string _outputFolder;

    private Room? _room;
    private IScreenCapture _screenCapture;
    private string _username = "desktop";
    private VideoSource? _videoSource;

    public LiveKitService()
    {
        _audioHandler = new AudioHandler();

        _outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ScreenCapture");
        Directory.CreateDirectory(_outputFolder);
        _outputFileName = Path.Combine(_outputFolder, $"ScreenCapture_{DateTime.Now:yyy-MM-dd HH-mm-ss}.mp4");

        GenerateByteArray();

        _screenCapture = new GpuImageCapture();
    }

    private void GenerateByteArray()
    {
        var width = 2560;
        var height = 1440;
        var size = (int)(2560 * 1440 * 1.5);
        _data = new byte[size];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                _data[y * width + x] = 125;
            }
        }
    }

    private void OnAudioReceived(byte[] buffer, int bytes)
    {
        if (_isMuted) return;

        var audioFrame = new AudioFrame(buffer, _audioHandler.WaveIn.WaveFormat.SampleRate,
            _audioHandler.WaveIn.WaveFormat.Channels, 1440);
        _ = _microphoneAudioSource?.CaptureFrameAsync(audioFrame);
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

        _room.ParticipantConnected += (_, participant) => { Console.WriteLine($"{participant.Identity} joined"); };

        // _room.ActiveSpeakersChanged += (sender, e) => { Console.WriteLine($"Active speakers: {e.Speakers.Count}"); };

        _room.DataReceived += (_, e) =>
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
        // _publication = await _room.LocalParticipant!.PublishTrackAsync(audioTrack);
        _ = await _room.LocalParticipant!.PublishTrackAsync(audioTrack);
    }

    public async Task InitializeVideoSource()
    {
        _videoSource = new VideoSource(2560, 1440);
        var videoTrack = _videoSource.CreateTrack($"{_username}-video");
        var videoPublication = await _room.LocalParticipant.PublishTrackAsync(videoTrack);
        Console.WriteLine($"Published video track: {videoPublication.Sid}\n");
        _screenCapture = new GpuImageCapture()
        {
            ScreenRefreshed = (data) =>
            {
                // Console.WriteLine($"Frame Captured: {data.Length}");
                // File.WriteAllBytes(
                //     @$"C:\Users\jonat\Desktop\ScreenCapture\screencap_{DateTime.UtcNow:yyyy-MM-dd-hhmmss}.jpg", data);

                using var memoryStream = new MemoryStream();
                memoryStream.Write(data, 0, data.Length);
                OnFrameReceived(memoryStream, 2560, 1440);
            }
        };

        _screenCapture.StartCapture();
    }

    public async Task StopStreaming()
    {
        _screenCapture.StopCapture();
    }

    private void OnFrameReceived(MemoryStream memoryStream, int width, int height)
    {
        var frames = memoryStream.GetBuffer();
        if (frames.Length != 0)
        {
            Console.WriteLine($"Buffer length: {frames.Length}");
            var videoFrame = new VideoFrame(width, height, VideoBufferType.Rgba, frames);
            _videoSource?.CaptureFrame(videoFrame);
            memoryStream.Seek(0, SeekOrigin.Begin);
        }
    }

    public void ToggleMute()
    {
        _isMuted = !_isMuted;
    }

    private AudioSource InitializeAudioSource()
    {
        _audioHandler.OnDataReceived = OnAudioReceived;
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

    public async Task<List<ParticipantInfo>> GetRoomParticipants(string roomName)
    {
        var request = new ListParticipantsRequest
        {
            Room = roomName
        };
        var response = await _roomServiceClient.ListParticipants(request);
        var peopleList = response.Participants.ToList();

        peopleList.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        return peopleList;
    }
}