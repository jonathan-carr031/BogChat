using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using BogChatDesktopClient.Models;

namespace BogChatDesktopClient.Services;

public class OAuthService(AuthentikService authentikService) {
    private const string ClientId = "pUeyTht8PcU1wBKBrv7IkxDHtQEmAyviGbTLyoaa";
    private const string AuthorizationEndpoint = "https://auth.whalestargroup.com/application/o/authorize/";
    private const string TokenEndpoint = "https://auth.whalestargroup.com/application/o/token/";
    private const string RedirectUri = "http://localhost:5000/success/";

    private const string ResponseString =
        "<html><body><h2>Authentication successful! You can close this window.</h2></body></html>";


    public async Task<AccessTokenResponse?> StartOAuth() {
        var state = Guid.NewGuid().ToString("N");

        using var listener = new HttpListener();
        listener.Prefixes.Add(RedirectUri);
        listener.Start();
        Console.WriteLine("Listening for browser redirect...");

        var authUrl = $"{AuthorizationEndpoint}?" +
                      $"response_type=code&" +
                      $"client_id={HttpUtility.UrlEncode(ClientId)}&" +
                      $"redirect_uri={HttpUtility.UrlEncode(RedirectUri)}&" +
                      $"scope=profile%20offline_access&" +
                      $"state={state}";

        OpenBrowser(authUrl);

        var context = await listener.GetContextAsync();
        var request = context.Request;

        var code = request.QueryString["code"];
        var incomingState = request.QueryString["state"];

        var response = context.Response;

        var buffer = Encoding.UTF8.GetBytes(ResponseString);
        response.ContentLength64 = buffer.Length;
        await using (var output = response.OutputStream) {
            await output.WriteAsync(buffer);
        }

        listener.Stop();

        if (incomingState != state) {
            Console.WriteLine("Security error: State mismatch!");
            return null;
        }

        if (!string.IsNullOrEmpty(code)) {
            Console.WriteLine($"Authorization code received: {code}");
            return await authentikService.ExchangeCodeForToken(code);
        }


        return null;
    }

    private void OpenBrowser(string url) {
        try {
            Process.Start(url);
        }
        catch {
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
}