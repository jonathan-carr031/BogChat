namespace BogChatDesktopClient.Data;

public class StreamableItem(int processId, string windowTitle)
{
    public int ProcessId { get; set; } = processId;
    public string WindowTitle { get; set; } = windowTitle;
}