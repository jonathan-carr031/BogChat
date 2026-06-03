using System;
using System.IO;
using System.Runtime.InteropServices;
using BogChatDesktopClient.Helpers;

namespace BogChatDesktopClient.Features.AudioCapture;

public static class ApplicationAudioCapture {
    public const ushort Channels = 1;
    public const int SampleRate = 44100;
    private const int BitsPerSample = 16;
    private static MemoryStream _memoryStream = new();

    public static Action<byte[], int>? OnAudioDataReceived;

    [DllImport("ApplicationLoopback.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern void SetAudioCallback(AudioCallback callback);

    [DllImport("ApplicationLoopback.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern IntPtr StartCaptureAsync(uint processId, bool includeProcessTree, ushort channel,
        uint sampleRate, ushort bitsPerSample);

    [DllImport("ApplicationLoopback.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern int StopCaptureAsync();

    private static void OnAudioReceived(IntPtr data, int length) {
        var memoryStream = new MemoryStream();
        var buffer = new byte[length];
        Marshal.Copy(data, buffer, 0, length);

        memoryStream.Write(buffer, 0, buffer.Length);
        // _memoryStream.Write(buffer, 0, buffer.Length);

        OnAudioDataReceived?.Invoke(buffer, length);
    }

    public static void CaptureApplicationAudio(uint processId) {
        _memoryStream = new MemoryStream();

        SetAudioCallback(OnAudioReceived);

        StartCaptureAsync(processId, true, Channels, SampleRate, BitsPerSample);
    }

    public static void CaptureDesktopAudio() {
        _memoryStream = new MemoryStream();

        SetAudioCallback(OnAudioReceived);

        var processes = WindowsProcessManager.GetOwnProcess();
        try {
            StartCaptureAsync(processes, false, Channels, SampleRate, BitsPerSample);
        }
        catch (Exception e) {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
        }
    }

    public static void StopApplicationAudio() {
        StopCaptureAsync();
        // var outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "NAudioDemo");
        // WavConverter.WriteWavFile(_memoryStream, Path.Combine(outputPath, "Audio.wav"), SampleRate, (short)Channels,
        //     BitsPerSample); // We are converting PCM format to WAV.

        // _memoryStream.Close();
        // _memoryStream.Flush();
        // _memoryStream.Dispose();
    }

    delegate void AudioCallback(IntPtr data, int length);

    private static class WavConverter {
        public static void WriteWavFile(MemoryStream pcmStream, string outputPath, int sampleRate, short channels,
            short bitDepth) {
            var pcmData = pcmStream.ToArray();

            using FileStream fs = new FileStream(outputPath, FileMode.Create);
            WriteWavHeader(fs, pcmData.Length, sampleRate, channels, bitDepth);
            fs.Write(pcmData, 0, pcmData.Length);
        }

        private static void WriteWavHeader(FileStream fs, int pcmDataLength, int sampleRate, short channels,
            short bitDepth) {
            var blockAlign = channels * (bitDepth / 8);
            var byteRate = sampleRate * blockAlign;
            var dataChunkSize = pcmDataLength;
            var chunkSize = 36 + dataChunkSize;

            // "RIFF" header
            fs.Write("RIFF"u8.ToArray(), 0, 4);
            fs.Write(BitConverter.GetBytes(chunkSize), 0, 4);
            fs.Write("WAVE"u8.ToArray(), 0, 4);

            // "fmt " subchunk
            fs.Write("fmt "u8.ToArray(), 0, 4);
            fs.Write(BitConverter.GetBytes(16), 0, 4); // Subchunk1Size (16 for PCM)
            fs.Write(BitConverter.GetBytes((short)1), 0, 2); // AudioFormat (1 for PCM)
            fs.Write(BitConverter.GetBytes(channels), 0, 2); // NumChannels
            fs.Write(BitConverter.GetBytes(sampleRate), 0, 4); // SampleRate
            fs.Write(BitConverter.GetBytes(byteRate), 0, 4); // ByteRate
            fs.Write(BitConverter.GetBytes(blockAlign), 0, 2); // BlockAlign
            fs.Write(BitConverter.GetBytes(bitDepth), 0, 2); // BitsPerSample

            // "data" subchunk
            fs.Write("data"u8.ToArray(), 0, 4);
            fs.Write(BitConverter.GetBytes(dataChunkSize), 0, 4); // DataSize
        }
    }
}