using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BogChatDesktopClient.Models;

namespace BogChatDesktopClient.Services;

public class AuthentikService {
    private const string ClientId = "pUeyTht8PcU1wBKBrv7IkxDHtQEmAyviGbTLyoaa";

    private const string ClientSecret =
        "UkSXcwDw4QxDUe1lfXLFR8zCdFobvVVvB56qSKF25ylriykXSeTcWFcasXh6AtTDioajFFVo8US8uHWg0En3rYjkGltKU3vM7NQpz8oc2Auo2VLsaodCMVXH0OIj1SNp";

    private const string AuthorizationEndpoint = "https://auth.whalestargroup.com/application/o/authorize/";
    private const string TokenEndpoint = "https://auth.whalestargroup.com/application/o/token/";
    private const string RedirectUri = "http://localhost:5000/success/";

    private readonly HttpClient _httpClient;

    public AuthentikService(HttpClient httpClient) {
        _httpClient = httpClient;
    }

    public async Task<AccessTokenResponse?> ExchangeCodeForToken(string accessCode) {
        var kvp = new[] {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("client_id", ClientId),
            new KeyValuePair<string, string>("code", accessCode),
            new KeyValuePair<string, string>("redirect_uri", RedirectUri),
            new KeyValuePair<string, string>("client_secret", ClientSecret)
        };

        using var content = new FormUrlEncodedContent(kvp);
        var response = await _httpClient.PostAsync(TokenEndpoint, content);

        if (response.IsSuccessStatusCode) {
            var jsonResult = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Access Token Response:");
            Console.WriteLine(jsonResult);

            var accessTokenResponse = JsonSerializer.Deserialize<AccessTokenResponse>(jsonResult);

            return accessTokenResponse;
        }

        Console.WriteLine($"Token exchange failed: {response.StatusCode}");
        return null;
    }

    public async Task<AccessTokenResponse?> GetNewToken(string refreshToken) {
        var kvp = new[] {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("client_id", ClientId),
            new KeyValuePair<string, string>("refresh_token", refreshToken),
            new KeyValuePair<string, string>("client_secret", ClientSecret),
            new KeyValuePair<string, string>("scope", "profile%20offline_access")
        };

        using var content = new FormUrlEncodedContent(kvp);
        var response = await _httpClient.PostAsync(TokenEndpoint, content);

        if (!response.IsSuccessStatusCode) return null;

        var jsonResult = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AccessTokenResponse>(jsonResult);
    }
}