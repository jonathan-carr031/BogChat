using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BogChatDesktopClient;

public static class DataSaver {
    public static void TestEncryptionAndDecryption(string dataToSave) {
        var usernameBytes = UnicodeEncoding.ASCII.GetBytes(dataToSave);
        var fStream = new FileStream("Data.dat", FileMode.OpenOrCreate);

        var entropy = CreateRandomEntropy();
        var duplicateEntropy = CreateRandomEntropy();

        Console.WriteLine(entropy == duplicateEntropy);

        Console.WriteLine();
        Console.WriteLine($"Original data: {UnicodeEncoding.ASCII.GetString(usernameBytes)}");
        Console.WriteLine("Encrypting and writing to disk...");

        var bytesWritten = EncryptDataToStream(usernameBytes, entropy, DataProtectionScope.CurrentUser, fStream);

        fStream.Close();

        Console.WriteLine("Reading data from disk and decrypting...");

        // Open the file.
        fStream = new FileStream("Data.dat", FileMode.Open);

        // Read from the stream and decrypt the data.
        var decryptData = DecryptDataFromStream(entropy, DataProtectionScope.CurrentUser, fStream, bytesWritten);

        fStream.Close();

        Console.WriteLine($"Decrypted data: {UnicodeEncoding.ASCII.GetString(decryptData)}");
    }

    public static bool SaveData(string dataToSave) {
        var usernameBytes = UnicodeEncoding.ASCII.GetBytes(dataToSave);
        using var fStream = new FileStream("Data.dat", FileMode.OpenOrCreate);

        var entropy = CreateRandomEntropy();

        Console.WriteLine();
        Console.WriteLine($"Original data: {UnicodeEncoding.ASCII.GetString(usernameBytes)}");
        Console.WriteLine("Encrypting and writing to disk...");

        var bytesWritten = EncryptDataToStream(usernameBytes, entropy, DataProtectionScope.CurrentUser, fStream);

        fStream.Close();

        return bytesWritten > 0;
    }

    public static string FetchData() {
        Console.WriteLine("Reading data from disk and decrypting...");

        if (!File.Exists("Data.dat")) return "";

        // Open the file.
        using var fStream = new FileStream("Data.dat", FileMode.Open);

        var entropy = CreateRandomEntropy();

        // Read from the stream and decrypt the data.
        var decryptData = DecryptDataFromStream(entropy, DataProtectionScope.CurrentUser, fStream, (int)fStream.Length);

        fStream.Close();

        Console.WriteLine($"Decrypted data: {UnicodeEncoding.ASCII.GetString(decryptData)}");

        return UnicodeEncoding.ASCII.GetString(decryptData);
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
}