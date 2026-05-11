using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace BogChatDesktopClient.Services;

public class AuthentikService
{
    private readonly HttpClient _httpClient = new();
    private const string AccessToken = "ucxszsMuVHVj5mxO0y8VqKymHfAPkomiiUcoLxERLJt9rShqcPal9vxza8FC";
    private readonly Uri _baseUri = new("https://auth.whalestargroup.com");

    public async Task<string> GetAuthentikStuff()
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

        var apiUri = new Uri(_baseUri, "api/v3/core/authenticated_sessions/");
        var response = await _httpClient.GetAsync(apiUri);
        var content = await response.Content.ReadAsStringAsync();

        Console.WriteLine(content);

        return content;
    }
}