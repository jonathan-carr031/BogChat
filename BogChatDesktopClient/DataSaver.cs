using System;
using System.IO;
using System.Security.Cryptography;
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
        catch (Exception _) {
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
        catch (Exception _) {
            return "";
        }
    }

    public static int EncryptDataToStream(byte[] Buffer, byte[] Entropy, DataProtectionScope Scope, Stream S) {
        if (Buffer == null)
            throw new ArgumentNullException(nameof(Buffer));
        if (Buffer.Length <= 0)
            throw new ArgumentException("The buffer length was 0.", nameof(Buffer));
        if (Entropy == null)
            throw new ArgumentNullException(nameof(Entropy));
        if (Entropy.Length <= 0)
            throw new ArgumentException("The entropy length was 0.", nameof(Entropy));
        if (S == null)
            throw new ArgumentNullException(nameof(S));

        int length = 0;

        // Encrypt the data and store the result in a new byte array. The original data remains unchanged.
        byte[] encryptedData = ProtectedData.Protect(Buffer, Entropy, Scope);

        // Write the encrypted data to a stream.
        if (S.CanWrite && encryptedData != null) {
            S.Write(encryptedData, 0, encryptedData.Length);

            length = encryptedData.Length;
        }

        // Return the length that was written to the stream.
        return length;
    }

    public static byte[] CreateRandomEntropy() {
        // Create a byte array to hold the random value.
        byte[] entropy = new byte[16];

        // Create a new instance of the RNGCryptoServiceProvider.
        // Fill the array with a random value.
        new RNGCryptoServiceProvider().GetBytes(entropy);

        // Return the array.
        return entropy;
    }

    public static byte[] DecryptDataFromStream(byte[] Entropy, DataProtectionScope Scope, Stream S, int Length) {
        if (S == null)
            throw new ArgumentNullException(nameof(S));
        if (Length <= 0)
            throw new ArgumentException("The given length was 0.", nameof(Length));
        if (Entropy == null)
            throw new ArgumentNullException(nameof(Entropy));
        if (Entropy.Length <= 0)
            throw new ArgumentException("The entropy length was 0.", nameof(Entropy));

        byte[] inBuffer = new byte[Length];
        byte[] outBuffer;

        // Read the encrypted data from a stream.
        if (S.CanRead) {
            S.Read(inBuffer, 0, Length);

            outBuffer = ProtectedData.Unprotect(inBuffer, Entropy, Scope);
        }
        else {
            throw new IOException("Could not read the stream.");
        }

        // Return the decrypted data
        return outBuffer;
    }

    public static void DeleteCredentials() {
        var vault = new PasswordVault();
        var storedCredentials = vault.Retrieve(ApplicationResourceName, "CurrentUser");
        vault.Remove(storedCredentials);
    }
}