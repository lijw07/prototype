namespace Prototype.Services.Interfaces;

/// <summary>
/// Provides password hashing and data encryption/decryption services
/// </summary>
public interface IPasswordEncryptionService
{
    /// <summary>
    /// Encrypts plain text using AES encryption
    /// </summary>
    /// <param name="plainText">The text to encrypt</param>
    /// <returns>Encrypted text with encryption prefix</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts encrypted text using AES decryption
    /// </summary>
    /// <param name="encryptedText">The encrypted text to decrypt</param>
    /// <returns>Decrypted plain text</returns>
    string Decrypt(string encryptedText);

    /// <summary>
    /// Checks if the given text is encrypted
    /// </summary>
    /// <param name="text">The text to check</param>
    /// <returns>True if the text is encrypted, false otherwise</returns>
    bool IsEncrypted(string text);

    /// <summary>
    /// Hashes a password using BCrypt
    /// </summary>
    /// <param name="password">The plain text password to hash</param>
    /// <returns>BCrypt hashed password</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a password against its BCrypt hash
    /// </summary>
    /// <param name="password">The plain text password to verify</param>
    /// <param name="hashedPassword">The BCrypt hashed password to verify against</param>
    /// <returns>True if the password matches the hash, false otherwise</returns>
    bool VerifyPassword(string password, string hashedPassword);
}