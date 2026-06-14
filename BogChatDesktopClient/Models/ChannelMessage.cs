using System;

namespace BogChatDesktopClient.Models;

public class ChannelMessage {
    public int Id { get; init; }

    public Guid UserId { get; init; }
    public string? Message { get; init; }
    public Guid ChannelId { get; set; }

    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public bool IsSelf { get; set; } = false;
}