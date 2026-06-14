using System;
using BogChatDesktopClient.Models;

namespace BogChatDesktopClient.Services;

public class AppSessionService : IAppSessionService {
    public User CurrentUser { get; set; }
    public Guid Id { get; set; }
    public required string Username { get; set; }
    public string? Email { get; set; }
    public string JwtToken { get; set; }
    public string RefreshToken { get; set; }
}