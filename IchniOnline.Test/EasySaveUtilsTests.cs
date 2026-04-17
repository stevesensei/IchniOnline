using IchniOnline.Server.Models;
using IchniOnline.Server.Models.Game;
using IchniOnline.Server.Utilities;
using System.Security.Cryptography;
using System.Text.Json;

namespace IchniOnline.Test;

public class EasySaveUtilsTests
{
    private const string TestKey = "testPassword123";

    [Test]
    public void Encrypt_WithoutGzip_WithoutKey_ReturnsOriginalData()
    {
        // Arrange
        var plainText = "Hello, World!";
        var plainTextHex = Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(plainText));

        // Act
        var result = EasySaveUtils.Encrypt(key: "", plainTextHex, enableGzip: false);

        // Assert
        var decrypted = System.Text.Encoding.UTF8.GetString(result.Data);
        Assert.That(decrypted, Is.EqualTo(plainText));
        Assert.That(result.WasGunzipped, Is.False);
    }

    [Test]
    public void Encrypt_WithGzip_WithoutKey_ReturnsGzippedData()
    {
        // Arrange
        var plainText = "Hello, World!";
        var plainTextHex = Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(plainText));

        // Act
        var result = EasySaveUtils.Encrypt(key: "", plainTextHex, enableGzip: true);

        // Assert
        Assert.That(result.Data[0], Is.EqualTo(0x1F));
        Assert.That(result.Data[1], Is.EqualTo(0x8B));
        Assert.That(result.WasGunzipped, Is.True);
    }

    [Test]
    public void Encrypt_WithoutGzip_WithKey_ReturnsEncryptedData()
    {
        // Arrange
        var plainText = "Hello, World!";
        var plainTextHex = Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(plainText));

        // Act
        var result = EasySaveUtils.Encrypt(key: TestKey, plainTextHex, enableGzip: false);

        // Assert
        Assert.That(result.Data.Length, Is.GreaterThan(16));
        Assert.That(result.Data[..16], Is.Not.EqualTo(System.Text.Encoding.UTF8.GetBytes(plainText)));
        Assert.That(result.WasGunzipped, Is.False);
    }

    [Test]
    public void Encrypt_WithGzip_WithKey_ReturnsEncryptedGzippedData()
    {
        // Arrange
        var plainText = "Hello, World!";
        var plainTextHex = Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(plainText));

        // Act
        var result = EasySaveUtils.Encrypt(key: TestKey, plainTextHex, enableGzip: true);

        // Assert
        Assert.That(result.Data.Length, Is.GreaterThan(16));
        Assert.That(result.Data[0], Is.Not.EqualTo(0x1F));
        Assert.That(result.WasGunzipped, Is.True);
    }

    [Test]
    public void Decrypt_WithoutGzip_WithoutKey_ReturnsOriginalData()
    {
        // Arrange
        var plainText = "Hello, World!";
        var plainTextHex = Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(plainText));

        // Act
        var encrypted = EasySaveUtils.Encrypt(key: "", plainTextHex, enableGzip: false);
        var decryptedHex = Convert.ToHexString(encrypted.Data);
        var result = EasySaveUtils.Decrypt(key: "", encryptedHex: decryptedHex, isGzipped: false);

        // Assert
        var decrypted = System.Text.Encoding.UTF8.GetString(result.Data);
        Assert.That(decrypted, Is.EqualTo(plainText));
        Assert.That(result.WasGunzipped, Is.False);
    }

    [Test]
    public void Decrypt_WithoutGzip_WithKey_ReturnsOriginalData()
    {
        // Arrange
        var plainText = "Hello, World!";
        var plainTextHex = Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(plainText));

        // Act
        var encrypted = EasySaveUtils.Encrypt(key: TestKey, plainTextHex, enableGzip: false);
        var encryptedHex = Convert.ToHexString(encrypted.Data);
        var result = EasySaveUtils.Decrypt(key: TestKey, encryptedHex: encryptedHex, isGzipped: false);

        // Assert
        var decrypted = System.Text.Encoding.UTF8.GetString(result.Data);
        Assert.That(decrypted, Is.EqualTo(plainText));
        Assert.That(result.WasGunzipped, Is.False);
    }

    [Test]
    public void Decrypt_WithGzip_WithKey_ReturnsOriginalData()
    {
        // Arrange
        var plainText = "Hello, World!";
        var plainTextHex = Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(plainText));

        // Act
        var encrypted = EasySaveUtils.Encrypt(key: TestKey, plainTextHex, enableGzip: true);
        var encryptedHex = Convert.ToHexString(encrypted.Data);
        var result = EasySaveUtils.Decrypt(key: TestKey, encryptedHex: encryptedHex, isGzipped: true);

        // Assert
        var decrypted = System.Text.Encoding.UTF8.GetString(result.Data);
        Assert.That(decrypted, Is.EqualTo(plainText));
        Assert.That(result.WasGunzipped, Is.True);
    }

    [Test]
    public void Decrypt_WrongKey_ThrowsException()
    {
        // Arrange
        var plainText = "Hello, World!";
        var plainTextHex = Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(plainText));
        var encrypted = EasySaveUtils.Encrypt(key: TestKey, plainTextHex, enableGzip: false);
        var encryptedHex = Convert.ToHexString(encrypted.Data);

        // Act & Assert
        Assert.Throws<CryptographicException>(() => EasySaveUtils.Decrypt(key: "wrongKey", encryptedHex: encryptedHex, isGzipped: false));
    }

    [Test]
    public void EncryptDecrypt_RoundTrip_PreservesData()
    {
        // Arrange
        var originalJson = """
        {
            "Beatmap": {
                "__type": "Ichni.RhythmGame.Beatmap.BeatmapContainer_BM,Assembly-CSharp",
                "value": {
                    "elementList": [
                        {
                            "__type": "Ichni.RhythmGame.Beatmap.Stay_BM,Assembly-CSharp",
                            "exactJudgeTime": 16.17,
                            "elementName": "Stay (16.17)",
                            "tags": [],
                            "elementGuid": {
                                "value": "c0a7832d-9d10-4d32-abf6-ca17409aab69"
                            },
                            "attachedElementGuid": {
                                "value": "63ac2902-f22d-4461-9f5c-73b57974d301"
                            }
                        }
                    ]
                }
            }
        }
        """;
        var plainTextHex = Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(originalJson));

        // Act
        var encrypted = EasySaveUtils.Encrypt(key: TestKey, plainTextHex, enableGzip: true);
        var encryptedHex = Convert.ToHexString(encrypted.Data);
        var decrypted = EasySaveUtils.Decrypt(key: TestKey, encryptedHex: encryptedHex, isGzipped: true);
        var decryptedJson = System.Text.Encoding.UTF8.GetString(decrypted.Data);

        // Assert
        Assert.That(decryptedJson, Is.EqualTo(originalJson));
    }
}
