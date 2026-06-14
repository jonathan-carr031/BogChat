using System.Net.Http;

namespace BogChatDesktopClient.Extensions;

public static class HttpClientExtensions {
    public static HttpClient AddAuthorizationHeader(this HttpClient client, string token) {
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }
}