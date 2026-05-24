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

namespace BogChatDesktopClient.ScreenCapture;

public class ScreenCapture : IScreenCapture
{
    private readonly Rectangle _captureArea;
    private readonly Bitmap _captureBitmap;


    private readonly string _outputFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ScreenCapture");

    private List<double> _frameTimes = [];

    private DispatcherTimer _timer;


    public ScreenCapture() : this(Screen.PrimaryScreen != null
        ? Screen.PrimaryScreen.Bounds
        : new Rectangle(0, 0, 1920, 1080))
    {
    }

    public ScreenCapture(Rectangle captureArea, uint targetFps = 120)
    {
        _captureArea = captureArea;
        _captureBitmap = new Bitmap(_captureArea.Width, _captureArea.Height, PixelFormat.Format32bppArgb);

        if (targetFps <= 0)
            throw new ArgumentOutOfRangeException(nameof(targetFps), $"{nameof(targetFps)} must be greater than zero");

        var timerInterval = 1000f / targetFps;
        Console.WriteLine($"Target FPS: {targetFps}");
        Console.WriteLine($"Take a screen capture every {timerInterval} milliseconds");
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(timerInterval)
        };

        Directory.CreateDirectory(_outputFolder);
    }

    public Action<byte[]>? ScreenRefreshed { get; set; }

    public void StartCapture()
    {
        Console.WriteLine("StartCapture");
        var captureGraphics = Graphics.FromImage(_captureBitmap);

        var stopwatch = new Stopwatch();
        if (ScreenRefreshed != null)
        {
            _timer.Tick += (s, e) =>
            {
                stopwatch.Restart();
                //TODO: COPY FROM SCREEN IS WAY TOO SLOW
                captureGraphics.CopyFromScreen(_captureArea.Left, _captureArea.Top, 0, 0, _captureArea.Size);
                // var fileName = $"ScreenCapture_{DateTime.UtcNow:yyyyMMdd_HHmmss}.jpeg";
                // _captureBitmap.Save(Path.Combine(_outputFolder, fileName), ImageFormat.Jpeg);
                // ScreenRefreshed(GetPixelData(_captureBitmap));
                _frameTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
            };
        }

        _frameTimes.Clear();
        _timer.Start();
    }

    public void StopCapture()
    {
        Console.WriteLine("Copy Screen Stop Capture");
        Console.WriteLine($"Captured {_frameTimes.Count} frames");

        var averageCaptureTime = _frameTimes.Average();
        Console.WriteLine($"Average Time to Capture Frames: {averageCaptureTime}");
        Console.WriteLine($"Actual FPS: {1000f / averageCaptureTime}");
        _timer.Stop();
    }

    private byte[] GetPixelData(Bitmap bitmap)
    {
        var bitmapBounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var bitmapData = bitmap.LockBits(bitmapBounds, ImageLockMode.WriteOnly, bitmap.PixelFormat);
        var length = bitmapData.Stride * bitmapData.Height;

        var bytes = new byte[length];

        Marshal.Copy(bitmapData.Scan0, bytes, 0, length);
        bitmap.UnlockBits(bitmapData);

        return bytes;
    }
}