using System;

namespace BogChatDesktopClient.ScreenCapture;

public interface IScreenCapture
{
    public Action<byte[]>? ScreenRefreshed { get; set; }

    public void StartCapture();

    public void StopCapture();
}