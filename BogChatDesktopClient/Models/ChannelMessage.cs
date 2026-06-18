using System;
using System.Text.RegularExpressions;

namespace BogChatDesktopClient.Models;

public class ChannelMessage {
    private readonly Regex _gifRegex = new("(\\S)+(.gif)");
    public int Id { get; init; }

    public Guid UserId { get; init; }
    public string? Username { get; set; }
    public string? Message { get; init; }
    public Guid ChannelId { get; set; }

    public DateTime CreatedAt { get; init; }
    public DateTime LocalCreatedAt => CreatedAt.ToLocalTime();
    public DateTime UpdatedAt { get; init; }

    public bool IsSelf { get; set; }

    public bool HasGif => Message != null && _gifRegex.Match(Message).Success;

    public string? SharedGif {
        get {
            if (Message != null) {
                var match = _gifRegex.Match(Message);
                return match.Success ? match.Value : null;
            }

            return null;
        }
    }

    public bool HasText => Message != null && !string.IsNullOrEmpty(_gifRegex.Replace(Message, string.Empty));

    public string? MessageText => Message != null ? _gifRegex.Replace(Message, string.Empty) : null;
}