using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Versioning;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace BogChatDesktopClient.Services;

[SupportedOSPlatform("windows")]
public class VideoConverterService
{
    public Bitmap I420ToBitmap(byte[] data, int width, int height)
    {
        var bitmap = new System.Drawing.Bitmap(width, height, PixelFormat.Format32bppArgb);
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

        using var memory = new MemoryStream();
        bitmap.Save(memory, ImageFormat.Jpeg);
        memory.Position = 0;

        //AvIrBitmap is our new Avalonia compatible image. You can pass this to your view
        var avaloniaBitmap = new Bitmap(memory);

        return avaloniaBitmap;
    }
}