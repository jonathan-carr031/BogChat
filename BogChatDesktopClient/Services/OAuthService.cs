using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using BogChatDesktopClient.Models;

namespace BogChatDesktopClient.Services;

public static class OAuthService {
    private const string ClientId = "pUeyTht8PcU1wBKBrv7IkxDHtQEmAyviGbTLyoaa";
    private const string AuthorizationEndpoint = "https://auth.whalestargroup.com/application/o/authorize/";
    private const string TokenEndpoint = "https://auth.whalestargroup.com/application/o/token/";
    private const string RedirectUri = "http://localhost:5000/success/";

    private const string ResponseString =
        "<html><body><h2>Authentication successful! You can close this window.</h2></body></html>";

    public static async Task<AccessTokenResponse?> StartOAuth() {
        // 1. Generate unique state to prevent CSRF
        var state = Guid.NewGuid().ToString("N");

        // 2. Start local HTTP listener
        using var listener = new HttpListener();
        listener.Prefixes.Add(RedirectUri);
        listener.Start();
        Console.WriteLine("Listening for browser redirect...");

        // 3. Build the authorization URL
        var authUrl = $"{AuthorizationEndpoint}?" +
                      $"response_type=code&" +
                      $"client_id={HttpUtility.UrlEncode(ClientId)}&" +
                      $"redirect_uri={HttpUtility.UrlEncode(RedirectUri)}&" +
                      $"scope=profile%20offline_access&" +
                      $"state={state}";

        // 4. Open the system browser safely across Windows/Mac/Linux
        OpenBrowser(authUrl);

        // 5. Wait for the browser to redirect back to localhost
        var context = await listener.GetContextAsync();
        var request = context.Request;

        // Extract authorization parameters
        var code = request.QueryString["code"];
        var incomingState = request.QueryString["state"];

        // 6. Return a friendly HTML response page to the user
        var response = context.Response;

        var buffer = Encoding.UTF8.GetBytes(ResponseString);
        response.ContentLength64 = buffer.Length;
        await using (var output = response.OutputStream) {
            await output.WriteAsync(buffer);
        }

        listener.Stop();

        // 7. Validate state
        if (incomingState != state) {
            Console.WriteLine("Security error: State mismatch!");
            return null;
        }

        if (!string.IsNullOrEmpty(code)) {
            Console.WriteLine($"Authorization code received: {code}");
            // 8. Exchange code for access token via backend POST
            return await ExchangeCodeForTokenAsync(code);
        }


        return null;
    }

    private static void OpenBrowser(string url) {
        try {
            Process.Start(url);
        }
        catch {
            // Fallback framework configuration for .NET Core apps on alternative OS environments
            if (OperatingSystem.IsWindows()) {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (OperatingSystem.IsLinux()) {
                Process.Start("xdg-open", url);
            }
            else if (OperatingSystem.IsMacOS()) {
                Process.Start("open", url);
            }
            else {
                throw;
            }
        }
    }

    private static async Task<AccessTokenResponse?> ExchangeCodeForTokenAsync(string code) {
        using var client = new HttpClient();
        var kvp = new[] {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("client_id", ClientId),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", RedirectUri),
            new KeyValuePair<string, string>("client_secret",
                "UkSXcwDw4QxDUe1lfXLFR8zCdFobvVVvB56qSKF25ylriykXSeTcWFcasXh6AtTDioajFFVo8US8uHWg0En3rYjkGltKU3vM7NQpz8oc2Auo2VLsaodCMVXH0OIj1SNp")
        };

        using var content = new FormUrlEncodedContent(kvp);
        var response = await client.PostAsync(TokenEndpoint, content);

        if (response.IsSuccessStatusCode) {
            var jsonResult = await response.Content.ReadAsStringAsync();
            Console.WriteLine("\nAccess Token Response:");
            Console.WriteLine(jsonResult);

            var accessTokenResponse = JsonSerializer.Deserialize<AccessTokenResponse>(jsonResult);

            return accessTokenResponse;
        }

        Console.WriteLine($"Token exchange failed: {response.StatusCode}");
        return null;
    }
}