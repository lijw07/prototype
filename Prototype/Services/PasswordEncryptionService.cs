using System.Security.Cryptography;
using System.Text;
using Prototype.Services.Interfaces;

namespace Prototype.Services;

public class PasswordEncryptionService : IPasswordEncryptionService
{
    private readonly byte[] _key;
    private const string EncryptionPrefix = "ENC:";

    public PasswordEncryptionService(IConfiguration configuration)
    {
        var keyString = Environment.GetEnvironmentVariable("ENCRYPTION_KEY") ?? 
                       configuration["Encryption:Key"];
        
        if (string.IsNullOrEmpty(keyString))
        {
            // In development, generate a warning and use a temporary key
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                // WARNING: No encryption key configured. Using temporary key for development only!
                keyString = GenerateTemporaryKey();
            }
            else
            {
                throw new InvalidOperationException(
                    "Encryption key not configured. Set ENCRYPTION_KEY environment variable or Encryption:Key in configuration.");
            }
        }
            
        _key = Encoding.UTF8.GetBytes(keyString.PadRight(32).Substring(0, 32)); // Ensure 32 bytes for AES-256
    }
    
    private static string GenerateTemporaryKey()
    {
        using var rng = RandomNumberGenerator.Create();
        var keyBytes = new byte[32]; // 256-bit key
        rng.GetBytes(keyBytes);
        return Convert.ToBase64String(keyBytes);
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        if (IsEncrypted(plainText))
            return plainText;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var msEncrypt = new MemoryStream();
        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }

        var iv = aes.IV;
        var encrypted = msEncrypt.ToArray();
        var result = new byte[iv.Length + encrypted.Length];
        Array.Copy(iv, 0, result, 0, iv.Length);
        Array.Copy(encrypted, 0, result, iv.Length, encrypted.Length);

        return EncryptionPrefix + Convert.ToBase64String(result);
    }

    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText) || !IsEncrypted(encryptedText))
            return encryptedText;

        var base64Data = encryptedText.Substring(EncryptionPrefix.Length);
        var fullCipher = Convert.FromBase64String(base64Data);

        using var aes = Aes.Create();
        aes.Key = _key;

        var iv = new byte[aes.BlockSize / 8];
        var cipher = new byte[fullCipher.Length - iv.Length];

        Array.Copy(fullCipher, iv, iv.Length);
        Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        using var msDecrypt = new MemoryStream(cipher);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);

        return srDecrypt.ReadToEnd();
    }

    public bool IsEncrypted(string text)
    {
        return !string.IsNullOrEmpty(text) && text.StartsWith(EncryptionPrefix);
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }
}