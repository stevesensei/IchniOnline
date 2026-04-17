using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace IchniOnline.Server.Utilities;

/// <summary>
/// Utility class for encrypting and decrypting EasySave3 save files.
/// </summary>
public static class EasySaveUtils
{
    private const int GzipMagic1 = 0x1F;
    private const int GzipMagic2 = 0x8B;
    private const int Pbkdf2Iterations = 100;
    private const int KeySize = 16; // 128 bits
    private const int IvSize = 16; // 128 bits

    /// <summary>
    /// Result of a crypt operation containing the processed data and whether gzip was applied.
    /// </summary>
    /// <param name="Data">The encrypted or decrypted data.</param>
    /// <param name="WasGunzipped">Indicates whether gzip decompression was applied during decryption.</param>
    public record CryptResult(byte[] Data, bool WasGunzipped);

    /// <summary>
    /// Checks if the data is gzip compressed by examining the magic bytes.
    /// </summary>
    private static bool IsGzip(byte[] data)
    {
        return data.Length >= 2 && data[0] == GzipMagic1 && data[1] == GzipMagic2;
    }

    /// <summary>
    /// Derives a key from a password using PBKDF2 with SHA1.
    /// </summary>
    private static byte[] DeriveKey(string password, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA1, KeySize);
    }

    /// <summary>
    /// Encrypts data using AES-128-CBC with PBKDF2 key derivation.
    /// </summary>
    /// <param name="key">The password/key to use for encryption.</param>
    /// <param name="plainTextHex">The original file content as a hex string.</param>
    /// <param name="enableGzip">Whether to gzip compress the data before encryption.</param>
    /// <returns>A CryptResult containing the encrypted data and a flag indicating gzip was applied.</returns>
    public static CryptResult Encrypt(string key, string plainTextHex, bool enableGzip)
    {
        var data = Convert.FromHexString(plainTextHex);
        var wasGzipped = false;

        if (enableGzip)
        {
            data = GZipCompress(data);
            wasGzipped = true;
        }

        if (!string.IsNullOrEmpty(key))
        {
            var iv = RandomNumberGenerator.GetBytes(IvSize);
            var derivedKey = DeriveKey(key, iv);

            using var cipher = Aes.Create();
            cipher.Mode = CipherMode.CBC;
            cipher.Padding = PaddingMode.PKCS7;
            cipher.Key = derivedKey;
            cipher.IV = iv;

            using var cipherStream = new MemoryStream();
            using (var cryptoStream = new CryptoStream(cipherStream, cipher.CreateEncryptor(), CryptoStreamMode.Write))
            {
                cryptoStream.Write(data);
            }
            data = iv.Concat(cipherStream.ToArray()).ToArray();
        }

        return new CryptResult(data, wasGzipped);
    }

    /// <summary>
    /// Decrypts data encrypted with EasySave3 format.
    /// </summary>
    /// <param name="key">The password/key to use for decryption.</param>
    /// <param name="encryptedHex">The encrypted file content as a hex string.</param>
    /// <param name="isGzipped">Whether the data is expected to be gzip compressed after decryption.</param>
    /// <returns>A CryptResult containing the decrypted data and a flag indicating if the data was gunzipped.</returns>
    public static CryptResult Decrypt(string key, string encryptedHex, bool isGzipped)
    {
        var data = Convert.FromHexString(encryptedHex);
        var wasGunzipped = false;

        if (!string.IsNullOrEmpty(key))
        {
            var iv = data[..IvSize];
            var cipherText = data[IvSize..];
            var derivedKey = DeriveKey(key, iv);

            using var cipher = Aes.Create();
            cipher.Mode = CipherMode.CBC;
            cipher.Padding = PaddingMode.PKCS7;
            cipher.Key = derivedKey;
            cipher.IV = iv;

            using var cryptoStream = new CryptoStream(new MemoryStream(cipherText), cipher.CreateDecryptor(), CryptoStreamMode.Read);
            using var plainStream = new MemoryStream();
            cryptoStream.CopyTo(plainStream);
            data = plainStream.ToArray();
        }

        if (isGzipped && IsGzip(data))
        {
            wasGunzipped = true;
            data = GZipDecompress(data);
        }

        return new CryptResult(data, wasGunzipped);
    }

    /// <summary>
    /// Compresses data using GZip (equivalent to gzip format).
    /// </summary>
    private static byte[] GZipCompress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
        {
            gzip.Write(data);
        }
        return output.ToArray();
    }

    /// <summary>
    /// Decompresses GZip compressed data.
    /// </summary>
    private static byte[] GZipDecompress(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }

    /// <summary>
    /// Decrypts data and deserializes the result as JSON.
    /// </summary>
    /// <param name="key">The password/key to use for decryption.</param>
    /// <param name="encryptedHex">The encrypted file content as a hex string.</param>
    /// <param name="isGzipped">Whether the data is expected to be gzip compressed after decryption.</param>
    /// <returns>A tuple containing the deserialized object and a flag indicating if the data was gunzipped.</returns>
    public static (T? Data, bool WasGunzipped) DecryptJson<T>(string key, string encryptedHex, bool isGzipped)
    {
        var result = Decrypt(key, encryptedHex, isGzipped);
        var json = Encoding.UTF8.GetString(result.Data);
        var obj = JsonSerializer.Deserialize<T>(json);
        return (obj, result.WasGunzipped);
    }
}
