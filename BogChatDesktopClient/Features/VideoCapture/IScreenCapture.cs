using System;
using System.Drawing;
using BogChatDesktopClient.Features.VideoCapture.Models;

namespace BogChatDesktopClient.ScreenCapture;

public interface IScreenCapture {
    public Rectangle CaptureArea { get; set; }
    public Action<VideoInfo>? ScreenRefreshed { get; set; }

    public void StartCapture();

    public void StopCapture();
}