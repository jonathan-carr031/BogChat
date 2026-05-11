using System;
using System.Diagnostics;
using System.IO;
using NAudio.Wave;

namespace BogChatDesktopClient;

//TODO: Implement IDisposable?
public class AudioHandler : IDisposable
{
    private WaveFileWriter? _writer;

    private readonly IWaveIn _captureDevice;
    private string _outputFilename;
    private string _outputFolder;
    private readonly WaveOutEvent? _waveOutEvent;

    private bool _writeToFile = false;

    private bool _isMicrophoneOn = false;

    public int BytesRecorded;
    public byte[] BytesBuffer;

    public AudioHandler()
    {
        _captureDevice = InitializeWaveIn();

        // var waveInProvider = new WaveInProvider(_captureDevice);
        // _waveOutEvent = new WaveOutEvent();
        // _waveOutEvent.Init(waveInProvider);
    }
    
    public Action<byte[], int>? OnDataReceived;

    public IWaveIn WaveIn => _captureDevice;

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
            DeviceNumber = 1,
        };
        var sampleRate = (int)14400;
        var channels = 1;
        waveInEvent.WaveFormat = new WaveFormat(sampleRate, channels);

        waveInEvent.DataAvailable += OnDataAvailable;
        waveInEvent.RecordingStopped += OnRecordingStopped;

        return waveInEvent;
    }

    public void StartMicrophone()
    {
        if (_isMicrophoneOn) return;

        _captureDevice.StartRecording();
        // _waveOutEvent.Play();
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
        // _waveOutEvent.Stop();
        _isMicrophoneOn = false;
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        BytesRecorded = e.BytesRecorded;
        BytesBuffer = e.Buffer;

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
        Console.WriteLine(sender);
        Console.WriteLine(e);

        if (_writeToFile)
        {
            _writer?.Dispose();
        }
    }

    public void Dispose()
    {
        _writer?.Dispose();
        _writer = null;
        _captureDevice.Dispose();
        _waveOutEvent?.Dispose();
    }
}