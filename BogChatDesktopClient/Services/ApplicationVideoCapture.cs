using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;
using Bitmap = System.Drawing.Bitmap;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace BogChatDesktopClient.Services;

public class ApplicationVideoCapture
{
    private string _outputFolder;

    public ApplicationVideoCapture()
    {
    }

    private static AvaloniaBitmap EmptyBitmap => new WriteableBitmap(
        new PixelSize(200, 200),
        new Vector(96, 96),
        Avalonia.Platform.PixelFormat.Bgra8888,
        AlphaFormat.Premul);

    public async Task<AvaloniaBitmap> CaptureScreen(Rectangle captureArea)
    {
        try
        {
            var captureBitmap = new Bitmap(captureArea.Width, captureArea.Height, PixelFormat.Format32bppArgb);
            var capture = Graphics.FromImage(captureBitmap);
            capture.CopyFromScreen(captureArea.Left, captureArea.Top, 0, 0, captureArea.Size);

            // _outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ScreenCapture");
            // Directory.CreateDirectory(_outputFolder);
            // _outputFileName = Path.Combine(_outputFolder, $"ScreenCapture_{DateTime.Now:yyy-MM-dd HH-mm-ss}.jpg");
            // captureBitmap.Save(_outputFileName, ImageFormat.Jpeg);

            using var memory = new MemoryStream();
            captureBitmap.Save(memory, ImageFormat.Jpeg);
            memory.Position = 0;

            return new AvaloniaBitmap(memory);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message} -  {ex.StackTrace}");
        }

        return EmptyBitmap;
    }

    public async Task<byte[]> CaptureScreenAsBytes(Rectangle captureArea)
    {
        try
        {
            var captureBitmap = new Bitmap(captureArea.Width, captureArea.Height, PixelFormat.Format32bppArgb);
            var capture = Graphics.FromImage(captureBitmap);
            capture.CopyFromScreen(captureArea.Left, captureArea.Top, 0, 0, captureArea.Size);

            return GetRawPixelData(captureBitmap);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message} -  {ex.StackTrace}");
        }

        return [];
    }

    public byte[] GetRawPixelData(Bitmap bmp)
    {
        // 1. Lock the bitmap's bits
        Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);

        // 2. Get the address of the first line
        IntPtr ptr = bmpData.Scan0;

        // 3. Declare an array to hold the bytes of the bitmap
        int bytes = Math.Abs(bmpData.Stride) * bmp.Height;
        byte[] rgbValues = new byte[bytes];

        // 4. Copy the RGB values into the array
        Marshal.Copy(ptr, rgbValues, 0, bytes);

        // 5. Unlock the bits
        bmp.UnlockBits(bmpData);

        return rgbValues;
    }
}