using System.Text.Json.Serialization;

namespace BogChatDesktopClient.Models;

[JsonSerializable(typeof(AccessTokenResponse))]
public class AccessTokenResponse {
    [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
    [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
}