using System.Security.Cryptography;
using System.Text;

namespace CaptureScreenService;

public class EncryptionService
{
    private byte[] _entropy;

    public EncryptionService(string? base64Entropy = null)
    {
        if (!string.IsNullOrEmpty(base64Entropy))
        {
            try
            {
                _entropy = Convert.FromBase64String(base64Entropy);
            }
            catch
            {
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
        byte[] entropy = new byte[16];
        RandomNumberGenerator.Fill(entropy);
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
            return Convert.ToBase64String(encryptedBytes);
        }
        catch
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
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
            
            try
            {
                byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, _entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, _entropy, DataProtectionScope.LocalMachine);
                return Encoding.UTF8.GetString(plainBytes);
            }
        }
        catch
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
}
