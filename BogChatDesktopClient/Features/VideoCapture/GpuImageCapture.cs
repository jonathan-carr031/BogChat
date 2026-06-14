using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Avalonia.Threading;
using BogChatDesktopClient.Features.VideoCapture.Models;
using BogChatDesktopClient.ScreenCapture;
using LiveKit.Proto;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using ResultCode = SharpDX.DXGI.ResultCode;

namespace BogChatDesktopClient.Features.VideoCapture;

public class GpuImageCapture : IScreenCapture {
    private readonly List<double> _frameTimes = [];
    private Factory1 _factory;
    private bool _running, _initialized;
    private DispatcherTimer _timer;

    public Rectangle CaptureArea { get; set; }
    public Action<VideoInfo>? ScreenRefreshed { get; set; }

    public void StartCapture() {
        _running = true;
        _factory = new Factory1();
        // DisplayAdapters();

        //Get first adapter
        var adapter = _factory.GetAdapter1(0);
        //Get device from adapter
        var device = new Device(adapter);
        //Get front buffer of the adapter
        var output = adapter.GetOutput(0);
        var output1 = output.QueryInterface<Output1>();

        var boundingBox = output.Description.DesktopBounds;

        var width = boundingBox.Right - boundingBox.Left;
        var height = boundingBox.Bottom - boundingBox.Top;

        var textureDesc = new Texture2DDescription {
            CpuAccessFlags = CpuAccessFlags.Read,
            BindFlags = BindFlags.None,
            // Format = Format.B8G8R8A8_UNorm,
            Format = Format.AYUV,
            Width = width,
            Height = height,
            OptionFlags = ResourceOptionFlags.None,
            MipLevels = 1,
            ArraySize = 1,
            SampleDescription = { Count = 1, Quality = 0 },
            Usage = ResourceUsage.Staging
        };

        var screenTexture = new Texture2D(device, textureDesc);
        var stopwatch = new Stopwatch();

        _timer = new DispatcherTimer {
            Interval = TimeSpan.FromMilliseconds(6)
        };

        _frameTimes.Clear();
        _timer.Tick += (sender, args) => {
            stopwatch.Restart();
            using var duplicatedOutput = output1.DuplicateOutput(device);
            try {
                duplicatedOutput.TryAcquireNextFrame(8, out _, out var screenResource);
                if (screenResource == null) {
                    // continue;
                    return;
                }

                using var screenTexture2D = screenResource.QueryInterface<Texture2D>();
                device.ImmediateContext.CopyResource(screenTexture2D, screenTexture);
                device.ImmediateContext.Flush();

                var mapSource =
                    device.ImmediateContext.MapSubresource(screenTexture, 0, MapMode.Read,
                        MapFlags.None);

                using (var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb)) {
                    var boundsRect = new Rectangle(0, 0, width, height);

                    // Copy pixels from screen capture Texture to GDI bitmap
                    var mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
                    var sourcePtr = mapSource.DataPointer;
                    var destPtr = mapDest.Scan0;
                    for (var y = boundingBox.Top; y < boundingBox.Bottom; y++) {
                        // Copy a single line 
                        Utilities.CopyMemory(destPtr, sourcePtr, width * 4);

                        // Advance pointers
                        sourcePtr += mapSource.RowPitch;
                        destPtr += mapDest.Stride;
                    }

                    // Release source and dest locks
                    bitmap.UnlockBits(mapDest);
                    device.ImmediateContext.UnmapSubresource(screenTexture, 0);

                    using (var ms = new MemoryStream()) {
                        bitmap.Save(ms, ImageFormat.Bmp);
                        var videoInfo = new VideoInfo {
                            Width = width,
                            Height = height,
                            Data = ms.ToArray(),
                            FormatType = VideoBufferType.Bgra
                        };
                        ScreenRefreshed?.Invoke(videoInfo);
                        _initialized = true;
                    }
                }

                screenResource.Dispose();
                duplicatedOutput.ReleaseFrame();
            }
            catch (SharpDXException e) {
                if (e.ResultCode.Code != ResultCode.WaitTimeout.Result.Code) {
                    Trace.TraceError(e.Message);
                    Trace.TraceError(e.StackTrace);
                }
            }

            _frameTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
        };

        _timer.Start();
    }

    public void StopCapture() {
        Console.WriteLine("GPU Stop Capture");
        Console.WriteLine($"Captured {_frameTimes.Count} frames");

        var averageCaptureTime = _frameTimes.Average();
        Console.WriteLine($"Average Time to Capture Frames: {averageCaptureTime}");
        Console.WriteLine($"Actual FPS: {1000f / averageCaptureTime}");
        _running = false;
        _timer.Stop();
    }

    private void DisplayAdapters() {
        Console.WriteLine($"Number of Adapters: {_factory.GetAdapterCount1()}");
        foreach (var factoryAdapter in _factory.Adapters.Where(adapter => adapter.Outputs.Length > 0)) {
            Console.WriteLine($"Adapter: {factoryAdapter.Description.Description}");
            foreach (var factoryOutput in factoryAdapter.Outputs) {
                Console.WriteLine($"Output: {factoryOutput.Description.DeviceName}");
                Console.WriteLine($"Output: {factoryOutput.Description.DesktopBounds}");
            }

            Console.WriteLine("/=======================================================/");
        }
    }
}