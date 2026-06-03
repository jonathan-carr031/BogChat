using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

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

    public static bool IsTokenExpired(string token) {
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadToken(token);

        Console.WriteLine($"Token Expiry Time: {jsonToken.ValidTo}");
        Console.WriteLine($"Current Time: {DateTime.UtcNow}");

        return DateTime.UtcNow > jsonToken.ValidTo;
    }

    public static bool IsTokenExpired(JwtSecurityToken token) {
        return DateTime.UtcNow > token.ValidTo;
    }
}