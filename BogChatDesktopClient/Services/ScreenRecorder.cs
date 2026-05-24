using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.UI;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using Resource = SharpDX.DXGI.Resource;
using ResultCode = SharpDX.DXGI.ResultCode;

namespace BogChatDesktopClient.Services;

public class ScreenRecorder
{
    private const int targetFramesPerSecond = 1;
    private GraphicsCaptureItem _captureItem;
    private bool _closed;
    private ManualResetEvent _closedEvent;
    private RenderTargetView _composeRenderTargetView;
    private Texture2D _composeTexture;
    private Direct3D11CaptureFrame _currentFrame;


    private IDirect3DDevice? _device;
    private MediaEncodingProfile _encodingProfile;
    private ManualResetEvent[] _events;
    private ManualResetEvent _frameEvent;
    private Direct3D11CaptureFramePool _framePool;
    private bool _isRecording;
    private MediaStreamSource _mediaStreamSource;
    private int _millisecondsPerFrame;
    private Multithread _multithread;

    private byte[] _previousScreen;
    private bool _run, _init;
    private GraphicsCaptureSession _session;
    private Device? _sharpDxD3dDevice;
    private MediaTranscoder _transcoder;
    private VideoStreamDescriptor _videoDescriptor;

    public EventHandler<byte[]> ScreenRefreshed;

    public ScreenRecorder()
    {
        _millisecondsPerFrame = 1000 / targetFramesPerSecond;
    }

    public int Size { get; private set; }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("windows.ui.dll", ExactSpelling = true)]
    private static extern int GetWindowIdFromWindow(IntPtr hwnd, out WindowId id);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

    public RECT GetProcessWindowBounds(string processName)
    {
        // 1. Find the process by name
        // Process[] processes = Process.GetProcessesByName(processName);
        // if (processes.Length == 0) throw new Exception("Process not found.");

        var processToRecord = GetSpotifyProcess();
        if (processToRecord == null) return new RECT();

        // 2. Get the handle to the main window
        IntPtr hWnd = processToRecord.MainWindowHandle;
        if (hWnd == IntPtr.Zero) throw new Exception("Process has no main window.");

        // 3. Retrieve the bounds
        RECT rect = new RECT();
        if (GetWindowRect(hWnd, ref rect))
        {
            return rect;
        }

        throw new Exception("Could not get window bounds.");
    }

    public static void ListAndCaptureWindows()
    {
        EnumWindows((hWnd, lParam) =>
        {
            if (IsWindowVisible(hWnd))
            {
                var sb = new StringBuilder(256);
                GetWindowText(hWnd, sb, sb.Capacity);
                string windowTitle = sb.ToString();

                if (!string.IsNullOrWhiteSpace(windowTitle))
                {
                    // Convert HWND to WindowId
                    GetWindowIdFromWindow(hWnd, out WindowId windowId);

                    Console.WriteLine(
                        $"Title: {windowTitle} | HWND: 0x{hWnd.ToInt64():X} | WindowId: {windowId.Value}");
                }
            }

            return true;
        }, IntPtr.Zero);
    }

    public Process? GetSpotifyProcess()
    {
        var processes = Process.GetProcesses().Where(process => !string.IsNullOrEmpty(process.MainWindowTitle))
            .ToList();
        return processes.FirstOrDefault(process => process.ProcessName.Contains("Spotify"));
    }

    public async Task SetupEncoding()
    {
        if (!GraphicsCaptureSession.IsSupported())
        {
            Console.WriteLine("Screen recorder is not supported.");
            return;
        }

        if (_device == null)
        {
            _device = Direct3D11Helpers.CreateD3DDevice();
        }

        if (_sharpDxD3dDevice == null)
        {
            _sharpDxD3dDevice = Direct3D11Helpers.CreateSharpDXDevice(_device);
        }

        try
        {
            var processToRecord = GetSpotifyProcess();
            if (processToRecord == null) return;

            GetWindowByProcess(processToRecord);

            if (_captureItem == null) return;

            _framePool = Direct3D11CaptureFramePool.Create(
                _device, // D3D device
                DirectXPixelFormat.B8G8R8A8UIntNormalized, // Pixel format
                2, // Number of frames
                _captureItem.Size); // Size of the buffers

            _framePool.FrameArrived += (s, a) =>
            {
                Console.WriteLine("FrameArrived");
                // The FrameArrived event fires for every frame on the thread that
                // created the Direct3D11CaptureFramePool. This means we don't have to
                // do a null-check here, as we know we're the only one  
                // dequeueing frames in our application.  

                // NOTE: Disposing the frame retires it and returns  
                // the buffer to the pool.
                using (var frame = _framePool.TryGetNextFrame())
                {
                    // We'll define this method later in the document.
                    // ProcessFrame(frame);
                    Console.WriteLine($"Frame: {frame}");
                }
            };


            _session = _framePool.CreateCaptureSession(_captureItem);

            _session.StartCapture();
        }
        catch (Exception ex)
        {
            return;
        }
    }

    public void Start()
    {
        _run = true;
        var factory = new Factory1();

        Console.WriteLine($"Number of Adapters: {factory.GetAdapterCount1()}");
        foreach (var factoryAdapter in factory.Adapters.Where(adapter => adapter.Outputs.Length > 0))
        {
            Console.WriteLine($"Adapter: {factoryAdapter.Description.Description}");
            foreach (var factoryOutput in factoryAdapter.Outputs)
            {
                Console.WriteLine($"Output: {factoryOutput.Description.DeviceName}");
                Console.WriteLine($"Output: {factoryOutput.Description.DesktopBounds}");
            }

            Console.WriteLine("/=======================================================/");
        }

        //Get first adapter
        var adapter = factory.GetAdapter1(0);
        //Get device from adapter
        var device = new Device(adapter);
        //Get front buffer of the adapter
        var output = adapter.GetOutput(0);
        var output1 = output.QueryInterface<Output1>();

        // var boundingBox = GetProcessWindowBounds("");
        var boundingBox = output.Description.DesktopBounds;

        // Width/Height of desktop to capture
        var width = boundingBox.Right - boundingBox.Left;
        var height = boundingBox.Bottom - boundingBox.Top;

        Console.WriteLine($"Width: {width}, Height: {height}");

        // Create Staging texture CPU-accessible
        var textureDesc = new Texture2DDescription
        {
            CpuAccessFlags = CpuAccessFlags.Read,
            BindFlags = BindFlags.None,
            Format = Format.B8G8R8A8_UNorm,
            Width = width,
            Height = height,
            OptionFlags = ResourceOptionFlags.None,
            MipLevels = 1,
            ArraySize = 1,
            SampleDescription = { Count = 1, Quality = 0 },
            Usage = ResourceUsage.Staging
        };
        var screenTexture = new Texture2D(device, textureDesc);

        Task.Factory.StartNew(() =>
        {
            // Duplicate the output
            using var duplicatedOutput = output1.DuplicateOutput(device);
            while (_run)
            {
                try
                {
                    Resource screenResource;
                    OutputDuplicateFrameInformation duplicateFrameInformation;

                    duplicatedOutput.TryAcquireNextFrame(_millisecondsPerFrame, out duplicateFrameInformation,
                        out screenResource);
                    if (screenResource == null)
                    {
                        continue;
                    }

                    // copy resource into memory that can be accessed by the CPU
                    using (var screenTexture2D = screenResource.QueryInterface<Texture2D>())
                        device.ImmediateContext.CopyResource(screenTexture2D, screenTexture);

                    // Get the desktop capture texture
                    var mapSource = device.ImmediateContext.MapSubresource(screenTexture, 0, MapMode.Read,
                        MapFlags.None);

                    // Create Drawing.Bitmap
                    using (var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                    {
                        var boundsRect = new Rectangle(0, 0, width, height);

                        // Copy pixels from screen capture Texture to GDI bitmap
                        var mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
                        var sourcePtr = mapSource.DataPointer;
                        var destPtr = mapDest.Scan0;
                        for (var y = boundingBox.Top; y < boundingBox.Bottom; y++)
                        {
                            // Copy a single line 
                            Utilities.CopyMemory(destPtr, sourcePtr, width * 4);

                            // Advance pointers
                            sourcePtr += mapSource.RowPitch;
                            destPtr += mapDest.Stride;
                        }

                        // Release source and dest locks
                        bitmap.UnlockBits(mapDest);
                        device.ImmediateContext.UnmapSubresource(screenTexture, 0);

                        using (var ms = new MemoryStream())
                        {
                            bitmap.Save(ms, ImageFormat.Bmp);
                            bitmap.Save(
                                @$"C:\Users\jonat\Desktop\ScreenCapture\screencap_{DateTime.UtcNow:yyyy-MM-dd-hhmmss}.jpg",
                                ImageFormat.Bmp);
                            ScreenRefreshed?.Invoke(this, ms.ToArray());
                            _init = true;
                        }
                    }

                    screenResource.Dispose();
                    duplicatedOutput.ReleaseFrame();
                }
                catch (SharpDXException e)
                {
                    if (e.ResultCode.Code != ResultCode.WaitTimeout.Result.Code)
                    {
                        Trace.TraceError(e.Message);
                        Trace.TraceError(e.StackTrace);
                    }
                }
            }
        });
        while (!_init) ;
    }

    public void Stop()
    {
        Console.WriteLine("Stopping screen recorder");
        _run = false;
    }

    public void GetWindowByProcess(Process process)
    {
        GetWindowIdFromWindow(process.MainWindowHandle, out var windowId);
        _captureItem = GraphicsCaptureItem.TryCreateFromWindowId(windowId);
        Console.WriteLine($"Capture Item: {_captureItem.DisplayName}");
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left; // X coordinate of top-left corner
        public int Top; // Y coordinate of top-left corner
        public int Right; // X coordinate of bottom-right corner
        public int Bottom; // Y coordinate of bottom-right corner
    }
}