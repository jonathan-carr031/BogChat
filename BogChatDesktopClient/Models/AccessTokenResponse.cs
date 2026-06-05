using System.Text.Json.Serialization;

namespace BogChatDesktopClient.Models;

public class AccessTokenResponse {
    [JsonPropertyName("access_token")] public string? AccessToken { get; init; }
    [JsonPropertyName("refresh_token")] public string? RefreshToken { get; init; }
}