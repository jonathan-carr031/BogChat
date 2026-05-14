using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.CV.CvEnum;

namespace BogChatDesktopClient.Services;

public class VideoConverterService
{
    public static string fileName = "TestFile.mp4";


    private readonly string _outputFolder;

    private List<Bitmap> _videoFrames = [];

    public string filePath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "VideoConverterTest");

    public int h264Codec = VideoWriter.Fourcc('H', '2', '6', '4');
    public int iyuvCodec = VideoWriter.Fourcc('I', 'Y', 'U', 'V');
    public int mjpgCodec = VideoWriter.Fourcc('M', 'J', 'P', 'G');

    public int mp4vCodec = VideoWriter.Fourcc('M', 'P', '4', 'V');

    public int videoHeight;

    public Size videoSize = new(1920, 1080);
    public int videoWidth;
    public VideoWriter videoWriter;
    public int xvidCodec = VideoWriter.Fourcc('X', 'V', 'I', 'D');


    public VideoConverterService()
    {
        var backends = GetBackends();

        foreach (var backend in backends)
        {
            Console.WriteLine(backend.Name);
        }

        _outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SampleImages");
        Directory.CreateDirectory(_outputFolder);
    }


    public Backend[] GetBackends()
    {
        return CvInvoke.WriterBackends;
    }

    public Bitmap GetFrameBitmap(byte[] data)
    {
        var frame = new Mat();
        var resizedFrame = new Mat();

        CvInvoke.Imdecode(data, ImreadModes.ColorRgb, frame);
        CvInvoke.Resize(frame, resizedFrame, videoSize);

        return resizedFrame.ToBitmap();
    }

    public Bitmap GetFrameFromData(byte[] data, int width, int height)
    {
        var bitmap = new Bitmap(width, height);
        var rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var pictureData = bitmap.LockBits(rectangle, ImageLockMode.ReadWrite, bitmap.PixelFormat);
        var pixelStartAddress = pictureData.Scan0;

        Marshal.Copy(data, 0, pixelStartAddress, data.Length);
        bitmap.UnlockBits(pictureData);

        bitmap.Save(Path.Combine(_outputFolder, $"FileImage_{DateTime.UtcNow.Ticks}.jpg"));
        bitmap.Save(Path.Combine(_outputFolder, "VideoFrame.jpg"));

        return bitmap;
    }

    // public void GetVideo()
    // {
    //     var outputFile = Path.Combine(_outputFolder, fileName);
    //     videoWriter = new VideoWriter(outputFile, GetBackends()[0].ID, mjpgCodec, 30, videoSize, true);
    //
    //     foreach (var frame in _videoFrames)
    //     {
    //         videoWriter.Write(frame.ToMat());
    //     }
    //
    //     videoWriter.Dispose();
    //     _videoFrames?.Clear();
    // }

    public Avalonia.Media.Imaging.Bitmap I420ToBitmap(byte[] data, int width, int height)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        var bitmapData =
            bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bitmap.PixelFormat);

        unsafe
        {
            var ptr = (byte*)bitmapData.Scan0;
            var ySize = width * height;
            var uvSize = ySize / 4;


            var yPlane = 0;
            var uPlane = ySize;
            var vPlane = ySize + uvSize;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var yIdx = y * width + x;
                    var uvIdx = (y / 2) * (width / 2) + (x / 2);

                    var Y = data[yPlane + yIdx];
                    var U = data[uPlane + uvIdx];
                    var V = data[vPlane + uvIdx];

                    var c = Y - 16;
                    var d = U - 128;
                    var e = V - 128;

                    var r = (byte)Math.Max(0, Math.Min(255, (298 * c + 409 * e + 128) >> 8));
                    var g = (byte)Math.Max(0, Math.Min(255, (298 * c - 100 * d - 208 * e + 128) >> 8));
                    var b = (byte)Math.Max(0, Math.Min(255, (298 * c + 516 * d + 128) >> 8));

                    var pixelIdx = (y * bitmapData.Stride) + (x * 4);
                    ptr[pixelIdx] = b;
                    ptr[pixelIdx + 1] = g;
                    ptr[pixelIdx + 2] = r;
                    ptr[pixelIdx + 3] = 255;
                }
            }
        }

        bitmap.UnlockBits(bitmapData);
        // bitmap.Save(Path.Combine(_outputFolder, $"FileImage_{DateTime.UtcNow.Ticks}.jpg"));
        // bitmap.Save(Path.Combine(_outputFolder, "VideoFrame.jpg"));

        // _videoFrames.Add(bitmap);


        if (_videoFrames.Count > 500)
        {
            // GetVideo();
        }


        using var memory = new MemoryStream();
        bitmap.Save(memory, ImageFormat.Jpeg);
        memory.Position = 0;

        //AvIrBitmap is our new Avalonia compatible image. You can pass this to your view
        Avalonia.Media.Imaging.Bitmap AvIrBitmap = new Avalonia.Media.Imaging.Bitmap(memory);


        return AvIrBitmap;
    }
}