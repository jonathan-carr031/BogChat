using LiveKit.Proto;

namespace BogChatDesktopClient.Features.VideoCapture.Models;

public struct VideoInfo {
    public string Name { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public byte[] Data { get; set; }
    public VideoBufferType FormatType { get; set; }
}