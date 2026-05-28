using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Avalonia.Threading;
using BogChatDesktopClient.Features.VideoCapture.Models;
using BogChatDesktopClient.ScreenCapture;
using LiveKit.Proto;

namespace BogChatDesktopClient.Features.ScreenCapture;

public class CopyScreenCapture : IScreenCapture {
    private readonly Bitmap _captureBitmap;

    private readonly List<double> _frameTimes = [];

    private readonly string _outputFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ScreenCapture");

    private readonly DispatcherTimer _timer;

    public CopyScreenCapture() : this(Screen.PrimaryScreen != null
        ? Screen.PrimaryScreen.Bounds
        : new Rectangle(0, 0, 1920, 1080)) { }

    private CopyScreenCapture(Rectangle captureArea, uint targetFps = 120) {
        CaptureArea = captureArea;
        _captureBitmap = new Bitmap(CaptureArea.Width, CaptureArea.Height, PixelFormat.Format32bppArgb);

        if (targetFps <= 0)
            throw new ArgumentOutOfRangeException(nameof(targetFps), $"{nameof(targetFps)} must be greater than zero");

        var timerInterval = 1000f / targetFps;
        Console.WriteLine($"Target FPS: {targetFps}");
        Console.WriteLine($"Take a screen capture every {timerInterval} milliseconds");
        _timer = new DispatcherTimer {
            Interval = TimeSpan.FromMilliseconds(timerInterval)
        };

        Directory.CreateDirectory(_outputFolder);
    }

    public Rectangle CaptureArea { get; set; }
    public Action<VideoInfo>? ScreenRefreshed { get; set; }

    public void StartCapture() {
        Console.WriteLine("StartCapture");
        var captureGraphics = Graphics.FromImage(_captureBitmap);


        if (ScreenRefreshed != null) {
            var stopwatch = new Stopwatch();
            _timer.Tick += (_, _) => {
                stopwatch.Restart();
                captureGraphics.CopyFromScreen(CaptureArea.Left, CaptureArea.Top, 0, 0, CaptureArea.Size);
                var videoInfo = new VideoInfo {
                    Width = CaptureArea.Width,
                    Height = CaptureArea.Height,
                    Data = GetPixelData(_captureBitmap),
                    FormatType = VideoBufferType.Bgra
                };
                ScreenRefreshed(videoInfo);
                _frameTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
            };
        }

        _frameTimes.Clear();
        _timer.Start();
    }

    public void StopCapture() {
        Console.WriteLine("Copy Screen Stop Capture");
        Console.WriteLine($"Captured {_frameTimes.Count} frames");

        var averageCaptureTime = _frameTimes.Average();
        Console.WriteLine($"Average Time to Capture Frames: {averageCaptureTime}");
        Console.WriteLine($"Actual FPS: {1000f / averageCaptureTime}");
        _timer.Stop();
    }

    private byte[] GetPixelData(Bitmap bitmap) {
        var bitmapBounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var bitmapData = bitmap.LockBits(bitmapBounds, ImageLockMode.WriteOnly, bitmap.PixelFormat);
        var length = bitmapData.Stride * bitmapData.Height;

        var bytes = new byte[length];

        Marshal.Copy(bitmapData.Scan0, bytes, 0, length);
        bitmap.UnlockBits(bitmapData);

        return bytes;
    }
}