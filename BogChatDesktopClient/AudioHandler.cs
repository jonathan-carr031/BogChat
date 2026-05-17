using System;
using System.IO;
using NAudio.Wave;

namespace BogChatDesktopClient;

public class AudioHandler : IDisposable
{
    private const int SampleRate = 14400;
    private const int Channels = 1;

    private readonly IWaveIn _captureDevice;

    private bool _isMicrophoneOn;
    private string _outputFilename;
    private string _outputFolder;
    private WaveFileWriter? _writer;

    private bool _writeToFile;

    public Action<byte[], int>? OnDataReceived;

    public AudioHandler()
    {
        _captureDevice = InitializeWaveIn();
    }

    public IWaveIn WaveIn => _captureDevice;

    public void Dispose()
    {
        _writer?.Dispose();
        _writer = null;
        _captureDevice.Dispose();
    }

    private void InitializeRecordingOutput()
    {
        _outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "NAudioDemo");
        Directory.CreateDirectory(_outputFolder);
        _outputFilename = Path.Combine(_outputFolder, $"NAudioDemo_{DateTime.Now:yyy-MM-dd HH-mm-ss}.wav");
        _writer = new WaveFileWriter(Path.Combine(_outputFolder, _outputFilename), _captureDevice.WaveFormat);
    }

    private IWaveIn InitializeWaveIn()
    {
        var waveInEvent = new WaveInEvent
        {
            DeviceNumber = -1,
        };

        waveInEvent.WaveFormat = new WaveFormat(SampleRate, Channels);

        waveInEvent.DataAvailable += OnDataAvailable;
        waveInEvent.RecordingStopped += OnRecordingStopped;

        return waveInEvent;
    }

    public void StartMicrophone()
    {
        if (_isMicrophoneOn) return;

        _captureDevice.StartRecording();
        _isMicrophoneOn = true;
    }

    public void StartRecording()
    {
        _writeToFile = true;
    }

    public void StopRecording()
    {
        _writeToFile = false;
        _writer?.Dispose();
        _writer = null;
    }

    public void StopMicrophone()
    {
        if (!_isMicrophoneOn) return;

        _captureDevice.StopRecording();
        _isMicrophoneOn = false;
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (OnDataReceived != null)
        {
            OnDataReceived(e.Buffer, e.BytesRecorded);
        }

        if (_writeToFile)
        {
            if (_writer == null)
            {
                InitializeRecordingOutput();
            }

            _writer?.Write(e.Buffer, 0, e.BytesRecorded);
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (_writeToFile)
        {
            _writer?.Dispose();
        }
    }
}