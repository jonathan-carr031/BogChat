using System;
using BogChatDesktopClient.Features.ScreenCapture.Models;

namespace BogChatDesktopClient.ScreenCapture;

public interface IScreenCapture {
    public Action<VideoInfo>? ScreenRefreshed { get; set; }

    public void StartCapture();

    public void StopCapture();
}