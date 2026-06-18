namespace BogChatDesktopClient.Models;

public class GifResponse {
    public required string Type { get; set; }
    public required string Id { get; set; }
    public required string Title { get; set; }
    public GifImages? Images { get; set; }
}