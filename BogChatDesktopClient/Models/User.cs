using System;

namespace BogChatDesktopClient.Models;

public class User {
    public required Guid Id { get; init; }
    public required string Username { get; init; }
    public string? Email { get; init; }
    public string? DisplayName { get; init; }
}