using System.Security.Cryptography;
using System.Text;

namespace CaptureScreenService;

public class EncryptionService
{
    private static readonly byte[] AdditionalEntropy = { 0x53, 0x63, 0x72, 0x65, 0x65, 0x6E, 0x43, 0x61, 0x70 };

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        try
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedBytes = ProtectedData.Protect(plainBytes, AdditionalEntropy, DataProtectionScope.CurrentUser);
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
                byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, AdditionalEntropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, AdditionalEntropy, DataProtectionScope.LocalMachine);
                return Encoding.UTF8.GetString(plainBytes);
            }
        }
        catch
        {
            return string.Empty;
        }
    }

    public static string EncryptStatic(string plainText)
    {
        return new EncryptionService().Encrypt(plainText);
    }

    public static string DecryptStatic(string encryptedText)
    {
        return new EncryptionService().Decrypt(encryptedText);
    }
}
