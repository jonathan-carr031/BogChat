using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Avalonia.Threading;
using BogChatDesktopClient.Features.VideoCapture.Models;
using BogChatDesktopClient.Helpers;
using BogChatDesktopClient.ScreenCapture;
using LiveKit.Proto;

namespace BogChatDesktopClient.Features.VideoCapture;

public class CopyScreenCapture : IScreenCapture {
    private static readonly Rectangle DefaultRectangle = new(0, 0, 1920, 1080);
    private readonly Bitmap _captureBitmap;

    private readonly List<double> _frameTimes = [];

    private readonly string _outputFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ScreenCapture");

    private readonly DispatcherTimer _timer;

    public CopyScreenCapture() : this(Screen.PrimaryScreen != null
        ? Screen.PrimaryScreen.Bounds
        : DefaultRectangle) { }

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
                    Data = ImageProcessor.GetPixelData(_captureBitmap),
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
}