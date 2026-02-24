// Copyright (c) 2026 songkemail-commits
// Licensed under the MIT License (MIT)

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace CaptureScreenService;

public class EncryptionService
{
    private byte[] _entropy;
    private const string EncryptionVersion = "v1";
    private const int MinEntropyLength = 16;

    public EncryptionService(string? base64Entropy = null, ILogger<EncryptionService>? logger = null)
    {
        if (!string.IsNullOrEmpty(base64Entropy))
        {
            try
            {
                _entropy = Convert.FromBase64String(base64Entropy);

                // Validate entropy length
                if (_entropy.Length < MinEntropyLength)
                {
                    logger?.LogWarning("Entropy length insufficient, generating new entropy");
                    _entropy = GenerateEntropy();
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to parse entropy, generating new entropy");
                _entropy = GenerateEntropy();
            }
        }
        else
        {
            _entropy = GenerateEntropy();
        }
    }

    public static byte[] GenerateEntropy()
    {
        // Use 32 bytes (256 bits) of entropy for better security
        byte[] entropy = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(entropy);
        return entropy;
    }

    public string GetEntropyBase64()
    {
        return Convert.ToBase64String(_entropy);
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        try
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = ProtectedData.Protect(plainBytes, _entropy, DataProtectionScope.CurrentUser);

            // Add version prefix for future compatibility
            string encryptedWithVersion = $"{EncryptionVersion}:{Convert.ToBase64String(encryptedBytes)}";
            return encryptedWithVersion;
        }
        catch (CryptographicException)
        {
            // Log cryptographic errors specifically
            return string.Empty;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return string.Empty;

        try
        {
            // Handle versioned encryption
            string actualEncryptedText = encryptedText;
            if (encryptedText.StartsWith($"{EncryptionVersion}:"))
            {
                actualEncryptedText = encryptedText.Substring(EncryptionVersion.Length + 1);
            }

            byte[] encryptedBytes = Convert.FromBase64String(actualEncryptedText);

            try
            {
                byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, _entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (CryptographicException)
            {
                // Fallback to LocalMachine scope if CurrentUser fails
                try
                {
                    byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, _entropy, DataProtectionScope.LocalMachine);
                    return Encoding.UTF8.GetString(plainBytes);
                }
                catch (CryptographicException)
                {
                    return string.Empty;
                }
            }
        }
        catch (FormatException)
        {
            // Invalid base64 format
            return string.Empty;
        }
        catch (CryptographicException)
        {
            // Cryptographic operation failed
            return string.Empty;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    public static string EncryptStatic(string plainText, string base64Entropy)
    {
        return new EncryptionService(base64Entropy).Encrypt(plainText);
    }

    public static string DecryptStatic(string encryptedText, string base64Entropy)
    {
        return new EncryptionService(base64Entropy).Decrypt(encryptedText);
    }

    // Validate encryption key strength
    public bool IsEncryptionStrong()
    {
        return _entropy.Length >= MinEntropyLength;
    }
}
