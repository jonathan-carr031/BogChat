using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BogChatDesktopClient.Features.AudioCapture;
using BogChatDesktopClient.Features.VideoCapture;
using BogChatDesktopClient.Features.VideoCapture.Models;
using BogChatDesktopClient.ScreenCapture;
using LiveKit.Proto;
using LiveKit.Rtc;
using Livekit.Server.Sdk.Dotnet;
using ListParticipantsRequest = Livekit.Server.Sdk.Dotnet.ListParticipantsRequest;
using ParticipantInfo = Livekit.Server.Sdk.Dotnet.ParticipantInfo;
using Room = LiveKit.Rtc.Room;
using RoomOptions = LiveKit.Rtc.RoomOptions;

namespace BogChatDesktopClient.Services;

public class LiveKitService {
    private const string LiveKitUrl = "wss://bogchat-c8y7wswc.livekit.cloud";

    // private const string ApiKey = "devkey";
    private const string ApiKey = "APIGNcD9KetoFXf";

    // ReSharper disable once CommentTypo
    // private const string ApiSecret = "nfFhkIxwefgFzu50reCSmesvtmuHPTzZ";
    private const string ApiSecret = "grK2qDGUOc4ylMGt2Jx4KYHFHnnzoCsDOXpKSh7nPnJ";

    private readonly AudioHandler _audioHandler;
    private readonly RoomServiceClient _roomServiceClient = new(LiveKitUrl, ApiKey, ApiSecret);
    private string? _applicationAudioPublicationSid;

    private AudioSource? _applicationAudioSource;

    private bool _isMuted;
    private LocalTrackPublication? _localApplicationAudioTrackPublication;
    private AudioSource? _microphoneAudioSource;
    private LocalTrackPublication? _microphonePublication;

    private Room? _room;
    private IScreenCapture _screenCapture;
    private string _username = "desktop";
    private VideoSource? _videoSource;

    public LiveKitService(IScreenCapture screenCapture) {
        _audioHandler = new AudioHandler();
        _screenCapture = screenCapture;

        _screenCapture = new GpuImageCapture();
    }

    private void OnAudioReceived(byte[] buffer, int bytes) {
        if (_isMuted) return;

        var audioFrame = new AudioFrame(buffer, _audioHandler.WaveIn.WaveFormat.SampleRate,
            _audioHandler.WaveIn.WaveFormat.Channels, 1440);

        _ = _microphoneAudioSource?.CaptureFrameAsync(audioFrame);
    }

    private string GetAccessToken(string roomName) {
        var token = new AccessToken(ApiKey, ApiSecret)
            .WithIdentity($"{_username}-identity")
            .WithName(_username)
            .WithGrants(new VideoGrants { RoomJoin = true, Room = roomName })
            .WithTtl(TimeSpan.FromHours(24));

        return token.ToJwt();
    }

    public async Task<Room> JoinRoom(string roomName) {
        if (_room != null) {
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

        _room.DataReceived += (_, e) => {
            var message = Encoding.UTF8.GetString(e.Data);
            Console.WriteLine($"Data from {e.Participant?.Identity}: {message}");
        };

        return _room;
    }

    public async Task ConnectMicrophone() {
        if (_room == null) return;
        _microphoneAudioSource = InitializeAudioSource();

        var audioTrack = LocalAudioTrack.Create($"{_username}-audio", _microphoneAudioSource);
        _microphonePublication = await _room.LocalParticipant!.PublishTrackAsync(audioTrack);
    }

    public async Task InitializeVideoSource() {
        _screenCapture = new CopyScreenCapture {
            ScreenRefreshed = OnFrameReceived
        };

        var captureArea = _screenCapture.CaptureArea;

        _videoSource = new VideoSource(captureArea.Width, captureArea.Height);
        var videoTrack = _videoSource.CreateTrack($"{_username}-video");
        var videoPublication = await _room!.LocalParticipant!.PublishTrackAsync(videoTrack);
        Console.WriteLine($"Published video track: {videoPublication.Sid}\n");
    }

    private void StartStreaming() {
        _screenCapture.StartCapture();
    }

    public async Task StreamDesktop() {
        await InitializeVideoSource();
        StartStreaming();
    }

    public async Task StopStreaming() {
        _screenCapture.StopCapture();
        ApplicationAudioCapture.StopApplicationAudio();

        if (_applicationAudioPublicationSid != null)
            await _room.LocalParticipant.UnpublishTrackAsync(_applicationAudioPublicationSid);
    }

    private void OnFrameReceived(VideoInfo videoInfo) {
        if (videoInfo.Data.Length == 0) return;

        var videoFrame = new VideoFrame(videoInfo.Width, videoInfo.Height, VideoBufferType.Bgra, videoInfo.Data);
        OnFrameCaptured?.Invoke(videoFrame);
        _videoSource?.CaptureFrame(videoFrame);
    }

    public async Task ToggleMute() {
        _isMuted = !_isMuted;
        if (_isMuted) {
            if (_microphonePublication?.Sid != null)
                await _room.LocalParticipant.UnpublishTrackAsync(_microphonePublication.Sid);
        }
        else {
            var audioTrack = LocalAudioTrack.Create($"{_username}-audio", _microphoneAudioSource);
            _microphonePublication = await _room.LocalParticipant!.PublishTrackAsync(audioTrack);
        }
    }

    private AudioSource InitializeAudioSource() {
        _audioHandler.OnDataReceived = OnAudioReceived;
        _audioHandler.StartMicrophone();
        return new AudioSource(_audioHandler.WaveIn.WaveFormat.SampleRate,
            _audioHandler.WaveIn.WaveFormat.Channels);
    }

    public async Task LeaveRoom() {
        if (_room != null) {
            await _room.DisconnectAsync();
            _room = null;
        }
    }

    public void SetUsername(string username) {
        _username = username;
    }

    public async Task<List<ParticipantInfo>> GetRoomParticipants(string roomName) {
        var request = new ListParticipantsRequest {
            Room = roomName
        };
        var response = await _roomServiceClient.ListParticipants(request);
        var peopleList = response.Participants.ToList();

        peopleList.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        return peopleList;
    }

    public event Action<VideoFrame>? OnFrameCaptured;

    public void StreamApplication(uint processId) {
        //Stream Video
        _ = StreamApplicationVideo(processId);

        //Stream Audio
        _ = StreamApplicationAudio(processId);
    }

    private async Task StreamApplicationVideo(uint processId) {
        await InitializeVideoSource();
        StartStreaming();
    }

    private async Task StreamApplicationAudio(uint processId) {
        //Initialize Audio Source
        ApplicationAudioCapture.OnAudioDataReceived = ((bytes, i) => {
            var audioFrame = new AudioFrame(bytes, ApplicationAudioCapture.SampleRate,
                ApplicationAudioCapture.Channels, 441);

            _ = _applicationAudioSource?.CaptureFrameAsync(audioFrame);
        });
        _ = Task.Run(() => { ApplicationAudioCapture.CaptureApplicationAudio(processId); });
        _applicationAudioSource = new AudioSource(ApplicationAudioCapture.SampleRate,
            ApplicationAudioCapture.Channels);

        //Publish Track
        var audioTrack = LocalAudioTrack.Create($"{_username}-application-audio", _applicationAudioSource);
        _localApplicationAudioTrackPublication = await _room.LocalParticipant!.PublishTrackAsync(audioTrack);
    }
}