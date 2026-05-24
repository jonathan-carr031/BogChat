using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ScreenRecorderLib;

namespace BogChatDesktopClient.Services;

public class ScreenCaptureService
{
    private string _outputFileName;
    private string _outputFolder;
    private Process _processToRecord;

    private Recorder _rec;

    private MemoryStream _videoStream;

    public ScreenCaptureService()
    {
        var processes = Process.GetProcesses().Where(process => !string.IsNullOrEmpty(process.MainWindowTitle))
            .ToList();
        _processToRecord = processes.FirstOrDefault(process => process.ProcessName.Contains("Spotify"));

        _outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ScreenCapture");
        Directory.CreateDirectory(_outputFolder);

        _videoStream = new MemoryStream();

        if (_processToRecord != null) CreateRecording(_processToRecord);

        Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            _rec?.Stop();
        });
    }

    public Recorder CreateRecording(Process process)
    {
        var windowHandle = process.MainWindowHandle;

        var sources = new List<RecordingSourceBase>
        {
            new WindowRecordingSource(windowHandle)
        };

        var options = new RecorderOptions
        {
            AudioOptions = new AudioOptions
            {
                IsAudioEnabled = false
            },
            SourceOptions = new SourceOptions
            {
                RecordingSources = sources
            },
            VideoEncoderOptions = new VideoEncoderOptions
            {
                Bitrate = 8000 * 1000,
                Framerate = 60,
                IsThrottlingDisabled = true
            },
            OutputOptions = new OutputOptions
            {
                RecorderMode = RecorderMode.Video,
                OutputFrameSize = new ScreenSize(2560, 1440),
            }
        };

        _rec = Recorder.CreateRecorder(options);
        _rec.OnRecordingComplete += Rec_OnRecordingComplete;
        _rec.OnRecordingFailed += Rec_OnRecordingFailed;
        _rec.OnStatusChanged += Rec_OnStatusChanged;
        _rec.OnSnapshotSaved += OnRecOnOnSnapshotSaved;
        _rec.OnFrameRecorded += (sender, args) => { _videoStream.Flush(); };
        //Record to a file
        var outputFile = Path.Combine(_outputFolder, $"ScreenCapture_{DateTime.Now:yyy-MM-dd HH-mm-ss}.mp4");
        _rec.Record(_videoStream);

        return _rec;
    }

    private void OnRecOnOnSnapshotSaved(object? sender, SnapshotSavedEventArgs e)
    {
        Console.WriteLine("OnRecOnOnSnapshotSaved");
    }

    private void EndRecording()
    {
        Console.WriteLine("Recording ended");
        _rec.Stop();
    }

    private void Rec_OnRecordingComplete(object sender, RecordingCompleteEventArgs e)
    {
        Console.WriteLine("Recording complete");
        string path = e.FilePath;
    }

    private void Rec_OnRecordingFailed(object sender, RecordingFailedEventArgs e)
    {
        string error = e.Error;
        Console.WriteLine("Rec_OnRecordingFailed");
    }

    private void Rec_OnStatusChanged(object sender, RecordingStatusEventArgs e)
    {
        RecorderStatus status = e.Status;
        Console.WriteLine($"Rec_OnStatusChanged : {e.Status}");
    }
}