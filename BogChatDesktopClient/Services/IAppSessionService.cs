using System;
using BogChatDesktopClient.Models;

namespace BogChatDesktopClient.Services;

public interface IAppSessionService {
    public User CurrentUser { get; set; }
    Guid Id { get; set; }
    string Username { get; set; }
    string? Email { get; set; }
    string JwtToken { get; set; }
    string RefreshToken { get; set; }
}