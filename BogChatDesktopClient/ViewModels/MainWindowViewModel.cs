using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using BogChatDesktopClient.Services;
using Emgu.CV;
using Emgu.CV.CvEnum;
using LibVLCSharp.Shared;
using LiveKit.Rtc;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using VideoStream = LiveKit.Rtc.VideoStream;


namespace BogChatDesktopClient.ViewModels;

public class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly ApplicationAudioCapture _audioCapture = new();

    private readonly LibVLC _libVlc = new();
    private readonly LiveKitService _livekitService = new();

    private readonly string _outputFolder;
    private readonly VideoConverterService _videoConverter = new();
    private Stack<Media> _mediaList = new();

    private MemoryStream _memoryStream = new();
    private Room? _room = null;
    private StreamMediaInput _streamMediaInput;

    private string _testOuput = "TestOutput";


    private Bitmap _videoFrame;

    public MainWindowViewModel()
    {
        TestOutput = "TestOutput";
        MediaPlayer = new MediaPlayer(_libVlc);

        MediaPlayer.EndReached += (sender, args) =>
        {
            // MediaPlayer.Play(_mediaList.Pop());
            // Console.WriteLine("MediaPlayer.EndReached");
        };

        _streamMediaInput = new StreamMediaInput(_memoryStream);
        Media = new Media(_libVlc, _streamMediaInput);

        Media.AddOption(":input-stream-chunk-size=1024");
        // MovieTest();

        StreamableItems = [];

        GetStreamableItems();

        _outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SampleImages");
        Directory.CreateDirectory(_outputFolder);

        CreateRandomPixelData();
    }

    public Bitmap VideoFrame
    {
        get => _videoFrame;
        set
        {
            _videoFrame = value;
            OnPropertyChanged();
        }
    }

    public string TestOutput
    {
        get => _testOuput;
        set
        {
            _testOuput = value;
            OnPropertyChanged();
        }
    }

    public Media? Media { get; set; }

    public MediaPlayer MediaPlayer { get; }

    public ObservableCollection<StreamableItem> StreamableItems { get; set; }

    public void Dispose()
    {
        MediaPlayer?.Dispose();
        _libVlc?.Dispose();
    }

    private void GetStreamableItems()
    {
        var processes = Process.GetProcesses().Where((process) => !string.IsNullOrEmpty(process.MainWindowTitle));
        var regex = new Regex(@"[^A-Za-z0-9'\s]+");

        foreach (var process in processes)
        {
            Console.WriteLine(
                $"{process.Id} - {process.ProcessName} - {regex.Replace(process.MainWindowTitle, "")}");

            StreamableItems.Add(new StreamableItem(process.Id, regex.Replace(process.MainWindowTitle, "")));
        }
    }

    public async Task StreamableItemClickEvent(StreamableItem item)
    {
        Console.WriteLine($"{item.ProcessId} - {item.WindowTitle} clicked...");

        if (item.ProcessId > 0)
        {
            _ = Task.Run(() => { _audioCapture.CaptureApplicationAudio((uint)item.ProcessId); });

            await Task.Delay(5000);

            _audioCapture.StopApplicationAudio();
        }
    }

    public async Task JoinRoom()
    {
        _room = await _livekitService.JoinRoom("room-name");
        _room.TrackSubscribed += TrackSubscribed;
        await _livekitService.ConnectMicrophone();
    }

    public async Task LeaveRoom()
    {
        await _room?.DisconnectAsync()!;
        await _memoryStream.DisposeAsync();
    }

    public async void TrackSubscribed(object? sender, TrackSubscribedEventArgs e)
    {
        Console.WriteLine("TrackSubscribed");
        //Video Stuff
        var movieUri = new Uri("https://download.blender.org/peach/bigbuckbunny_movies/big_buck_bunny_480p_stereo.avi");
        byte[] movieData;

        using (HttpClient client = new HttpClient())
        {
            // Directly downloads the resource into a byte array
            movieData = await client.GetByteArrayAsync(movieUri);
        }

        if (e.Track is RemoteVideoTrack videoTrack)
        {
            await using var videoStream = new VideoStream(videoTrack);

            await foreach (var frame in videoStream.WithCancellation(CancellationToken.None))
            {
                // Process video frame
                // _memoryStream.Position = _memoryStream.Length;
                // _memoryStream.Write(frame.Frame.DataBytes, 0, frame.Frame.DataBytes.Length);

                Console.WriteLine($"{frame.Frame.Width} x {frame.Frame.Height}");
                Console.WriteLine($"{frame.Frame.DataBytes.Length}");

                VideoFrame =
                    _videoConverter.I420ToBitmap(frame.Frame.DataBytes, frame.Frame.Width, frame.Frame.Height);

                var frameMat = new Mat(1080, 1280, DepthType.Cv8U, 1);
                var rgbMat = new Mat();
                CvInvoke.CvtColor(frameMat, rgbMat, ColorConversion.Yuv420sp2Rgb);
                // frameMat.Save(Path.Combine(_outputFolder, $"FileImage_{DateTime.UtcNow.Ticks}.jpg"));
                CvInvoke.Resize(frameMat, rgbMat, new Size(1920, 1080));

                // VideoFrame = rgbMat.ToBitmap();
                TestOutput = DateTime.Now.ToLongTimeString() + " - " + DateTime.Now.ToLongDateString();

                // var tempData = movieData.Take(frame.Frame.DataBytes.Length).ToArray();
                // movieData = movieData.Skip(frame.Frame.DataBytes.Length).ToArray();

                // _memoryStream.Position = _memoryStream.Length;
                // _memoryStream.Write(frame.Frame.DataBytes, 0, frame.Frame.DataBytes.Length);
                // _streamMediaInput = new StreamMediaInput(_memoryStream);
                // Media = new Media(_libVlc, _streamMediaInput);
                //
                // Media.AddOption(":input-stream-chunk-size=1024");
                //
                // _mediaList.Push(Media);
                //
                // MediaPlayer.SetVideoFormat("YUV", (uint)frame.Frame.Width, (uint)frame.Frame.Height, 32);

                // var frameBitmap = _videoConverter.GetFrameBitmap(frame.Frame.DataBytes);

                // Console.WriteLine(frameBitmap);

                // CreateRandomPixelData();
            }
        }
    }

    public async void MovieTest()
    {
        var movieUri = new Uri("https://download.blender.org/peach/bigbuckbunny_movies/big_buck_bunny_480p_stereo.avi");

        using (HttpClient client = new HttpClient())
        {
            // Directly downloads the resource into a byte array
            var movieBytes = await client.GetByteArrayAsync(movieUri);

            _memoryStream.Position = _memoryStream.Length;

            _memoryStream.Write(movieBytes, 0, movieBytes.Length);
        }

        MediaPlayer.Play(Media);
    }


    public void Play()
    {
        if (Design.IsDesignMode)
        {
            return;
        }

        // using var media = new Media(_libVlc, new Uri("https://download.blender.org/peach/bigbuckbunny_movies/big_buck_bunny_480p_stereo.avi"));
        if (Media != null)
        {
            // Console.WriteLine($"Is Media Parsed? {(Media.IsParsed ? "Yes" : "No")}");
            // Console.WriteLine(Media.Tracks.Length);
            // Console.WriteLine(Media.Tracks);
            // Console.WriteLine(Media.ParsedStatus);


            // _streamMediaInput = new StreamMediaInput(_memoryStream);
            // Media = new Media(_libVlc, _streamMediaInput);
            MediaPlayer.Play(Media);
        }
    }

    public void Stop()
    {
        MediaPlayer.Stop();
    }

    private void CreateRandomPixelData()
    {
        //Create pixel data to put in image, use 2 since it is 16bpp
        var r = new Random(DateTime.Now.Millisecond);
        int width = 100;
        int height = 100;
        byte[] pixelValues = new byte[width * height * 2];
        for (int i = 0; i < pixelValues.Length; ++i)
        {
            // Just creating random pixel values for test
            pixelValues[i] = (byte)r.Next(0, 256);
        }

        var rgbData = Convert16BitGrayScaleToRgb48(pixelValues, width, height);
        var bmp = CreateBitmapFromBytes(rgbData, width, height);

        // display bitmap
        bmp.Save(Path.Combine(_outputFolder, $"FileImage_{DateTime.UtcNow.Ticks}.jpg"));
        bmp.Save(Path.Combine(_outputFolder, "VideoFrame.jpg"));

        // VideoFrame = bmp;
    }

    private void CreateBitmapFromPixelData(byte[] pixelData, int width, int height)
    {
        // var rgbData = Convert16BitGrayScaleToRgb48(pixelData, width, height);
        var bmp = CreateBitmapFromBytes(pixelData, width, height);

        // display bitmap
        bmp.Save(Path.Combine(_outputFolder, $"FileImage_{DateTime.UtcNow.Ticks}.jpg"));
        bmp.Save(Path.Combine(_outputFolder, "VideoFrame.jpg"));

        // VideoFrame = bmp;
    }

    private static byte[] Convert16BitGrayScaleToRgb48(byte[] inBuffer, int width, int height)
    {
        int inBytesPerPixel = 2;
        int outBytesPerPixel = 6;

        byte[] outBuffer = new byte[width * height * outBytesPerPixel];
        int inStride = width * inBytesPerPixel;
        int outStride = width * outBytesPerPixel;

        // Step through the image by row
        for (int y = 0; y < height; y++)
        {
            // Step through the image by column
            for (int x = 0; x < width; x++)
            {
                // Get inbuffer index and outbuffer index
                int inIndex = (y * inStride) + (x * inBytesPerPixel);
                int outIndex = (y * outStride) + (x * outBytesPerPixel);

                byte hibyte = inBuffer[inIndex + 1];
                byte lobyte = inBuffer[inIndex];

                //R
                outBuffer[outIndex] = lobyte;
                outBuffer[outIndex + 1] = hibyte;

                //G
                outBuffer[outIndex + 2] = lobyte;
                outBuffer[outIndex + 3] = hibyte;

                //B
                outBuffer[outIndex + 4] = lobyte;
                outBuffer[outIndex + 5] = hibyte;
            }
        }

        return outBuffer;
    }

    private static System.Drawing.Bitmap CreateBitmapFromBytes(byte[] pixelValues, int width, int height)
    {
        //Create an image that will hold the image data
        System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height, PixelFormat.Format48bppRgb);

        //Get a reference to the images pixel data
        Rectangle dimension = new Rectangle(0, 0, bmp.Width, bmp.Height);
        BitmapData picData = bmp.LockBits(dimension, ImageLockMode.ReadWrite, bmp.PixelFormat);
        IntPtr pixelStartAddress = picData.Scan0;

        //Copy the pixel data into the bitmap structure
        Marshal.Copy(pixelValues, 0, pixelStartAddress, pixelValues.Length);

        bmp.UnlockBits(picData);
        return bmp;
    }
}