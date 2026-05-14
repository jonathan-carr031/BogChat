namespace BogChatDesktopClient;

public class StreamableItem
{
    public StreamableItem(int processId, string windowTitle)
    {
        ProcessId = processId;
        WindowTitle = windowTitle;
    }

    public int ProcessId { get; set; }
    public string WindowTitle { get; set; }
}