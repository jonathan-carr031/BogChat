using System;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using BogChatDesktopClient.Models;
using BogChatDesktopClient.Services;

namespace BogChatDesktopClient;

public static class DataSaver {
    private const string ApplicationResourceName = "BogChatApp_OAuth";

    public static void SaveData(string dataToSave) {
        var vault = new PasswordVault();
        var credentials = new PasswordCredential(ApplicationResourceName, "CurrentUser", dataToSave);

        vault.Add(credentials);
    }

    public static void SaveAccessToken(AccessTokenResponse accessToken) {
        var vault = new PasswordVault();
        var credentials =
            new PasswordCredential(ApplicationResourceName, "CurrentUser", JsonSerializer.Serialize(accessToken));

        vault.Add(credentials);
    }

    public static async Task<AccessTokenResponse?> FetchAccessToken() {
        try {
            var vault = new PasswordVault();
            var storedCredentials = vault.Retrieve(ApplicationResourceName, "CurrentUser");
            await Task.Delay(TimeSpan.FromMilliseconds(1));
            return JsonSerializer.Deserialize<AccessTokenResponse>(storedCredentials.Password);
        }
        catch (Exception) {
            Console.WriteLine("Could not read the access token.");
            return null;
        }
    }

    public static async Task<string?> FetchUserName() {
        var accessTokenResponse = await FetchAccessToken();

        var accessToken = accessTokenResponse?.AccessToken;
        if (accessToken == null) return null;
        var token = JwtHandler.Decode(accessToken);
        if (token == null) return null;
        var username = JwtHandler.ExtractUsername(token);
        return string.IsNullOrEmpty(username) ? null : username;
    }

    public static async Task<string?> FetchRefreshToken() {
        var accessTokenResponse = await FetchAccessToken();

        var refreshToken = accessTokenResponse?.RefreshToken;
        return refreshToken;
    }

    public static async Task<string> FetchData() {
        try {
            var vault = new PasswordVault();
            var storedCredentials = vault.Retrieve(ApplicationResourceName, "CurrentUser");
            await Task.Delay(TimeSpan.FromMilliseconds(1));
            return JwtHandler.ExtractUsername(storedCredentials.Password) ?? string.Empty;
        }
        catch (Exception) {
            return "";
        }
    }

    public static void DeleteCredentials() {
        var vault = new PasswordVault();
        var storedCredentials = vault.Retrieve(ApplicationResourceName, "CurrentUser");
        vault.Remove(storedCredentials);
    }
}