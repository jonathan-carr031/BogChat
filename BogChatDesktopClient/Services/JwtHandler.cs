using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using BogChatDesktopClient.Models;

namespace BogChatDesktopClient.Services;

public static class JwtHandler {
    public static JwtSecurityToken? Decode(string token) {
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadToken(token);
        return jsonToken as JwtSecurityToken;
    }

    public static string? ExtractUsername(string token) {
        var decodedToken = Decode(token);
        var usernameClaim = decodedToken?.Claims.FirstOrDefault(claim => claim.Type == "preferred_username");

        return usernameClaim?.Value;
    }

    public static string? ExtractUsername(JwtSecurityToken token) {
        var usernameClaim = token.Claims.FirstOrDefault(claim => claim.Type == "preferred_username");
        return usernameClaim?.Value;
    }

    public static User? ExtractUser(JwtSecurityToken token) {
        var userIdClaim = token.Claims.FirstOrDefault(claim => claim.Type == "sub");
        var usernameClaim = token.Claims.FirstOrDefault(claim => claim.Type == "preferred_username");
        var email = token.Claims.FirstOrDefault(claim => claim.Type == "email");

        if (userIdClaim == null || usernameClaim == null) return null;

        return new User {
            Id = Guid.Parse(userIdClaim.Value),
            Username = usernameClaim.Value,
            Email = email?.Value,
            DisplayName = usernameClaim?.Value
        };
    }

    public static bool IsTokenExpired(string token) {
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadToken(token);

        return DateTime.UtcNow > jsonToken.ValidTo;
    }

    public static bool IsTokenExpired(JwtSecurityToken token) {
        return DateTime.UtcNow > token.ValidTo;
    }
}