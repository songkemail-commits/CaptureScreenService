using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Encoder = System.Drawing.Imaging.Encoder;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace CaptureScreenService;

public sealed class ScreenCapService
{
    private readonly ILogger<ScreenCapService> _logger;
    private readonly AppConfig _config;
    private readonly EncryptionService _encryptionService;

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    private static extern int BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    private const int SRCCOPY = 0x00CC0020;
    private const int DESKTOPHORZRES = 118;
    private const int DESKTOPVERTRES = 117;

    public ScreenCapService(ILogger<ScreenCapService> logger, AppConfig config, EncryptionService encryptionService)
    {
        _logger = logger;
        _config = config;
        _encryptionService = encryptionService;
    }

    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrEmpty(email))
            return "***";

        if (email.Contains('@'))
        {
            var parts = email.Split('@');
            if (parts[0].Length <= 2)
                return "***@" + parts[1];
            return parts[0][0] + "***" + parts[0][^1] + "@" + parts[1];
        }

        return email.Length <= 2 ? "***" : email[0] + "***" + email[^1];
    }

    public void CaptureMainScreen()
    {
        _logger.LogInformation("=== Config diagnostics start ===");
        _logger.LogInformation("StorageMode: {StorageMode}", _config.StorageMode);
        _logger.LogInformation("CaptureIntervalMinutes: {CaptureIntervalMinutes}", _config.CaptureIntervalMinutes);
        _logger.LogInformation("Local.SavePath: {SavePath}", _config.Local?.SavePath ?? "NULL");
        _logger.LogInformation("Email object is null: {IsNull}", _config.Email == null);
        if (_config.Email != null)
        {
            _logger.LogInformation("Email.Provider: {Provider}", _config.Email.Provider);
            _logger.LogInformation("Email.SmtpServer: {SmtpServer}", _config.Email.SmtpServer ?? "NULL");
            _logger.LogInformation("Email.SmtpPort: {SmtpPort}", _config.Email.SmtpPort);
            _logger.LogInformation("Email.EmailAddress: {EmailAddress}", MaskEmail(_config.Email.EmailAddress));
            _logger.LogInformation("Email.EncryptedAuthCode length: {Length}", _config.Email.EncryptedAuthCode?.Length ?? 0);
        }
        _logger.LogInformation("=== Config diagnostics end ===");

        string saveDirectory = _config.StorageMode == StorageMode.Local
            ? _config.Local?.SavePath ?? @"C:\temp\TempPics"
            : @"C:\temp\TempPics";

        try
        {
            Directory.CreateDirectory(saveDirectory);
            _logger.LogInformation("Save directory: {SaveDirectory}", saveDirectory);

            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string fileName = $"screenshot_{timestamp}.png";
            string filePath = Path.Combine(saveDirectory, fileName);

            IntPtr hdcScreen = GetDC(IntPtr.Zero);
            if (hdcScreen == IntPtr.Zero)
            {
                _logger.LogError("GetDC failed: {Error}", Marshal.GetLastWin32Error());
                return;
            }

            IntPtr hdcCompatible = CreateCompatibleDC(hdcScreen);
            if (hdcCompatible == IntPtr.Zero)
            {
                _logger.LogError("CreateCompatibleDC failed: {Error}", Marshal.GetLastWin32Error());
                ReleaseDC(IntPtr.Zero, hdcScreen);
                return;
            }

            int screenWidth = GetDeviceCaps(hdcScreen, DESKTOPHORZRES);
            int screenHeight = GetDeviceCaps(hdcScreen, DESKTOPVERTRES);

            IntPtr hBitmap = CreateCompatibleBitmap(hdcScreen, screenWidth, screenHeight);
            if (hBitmap == IntPtr.Zero)
            {
                _logger.LogError("CreateCompatibleBitmap failed: {Error}", Marshal.GetLastWin32Error());
                ReleaseDC(IntPtr.Zero, hdcScreen);
                DeleteObject(hdcCompatible);
                return;
            }

            IntPtr hOld = SelectObject(hdcCompatible, hBitmap);
            if (hOld == IntPtr.Zero)
            {
                _logger.LogError("SelectObject failed: {Error}", Marshal.GetLastWin32Error());
                ReleaseDC(IntPtr.Zero, hdcScreen);
                DeleteObject(hdcCompatible);
                DeleteObject(hBitmap);
                return;
            }

            if (BitBlt(hdcCompatible, 0, 0, screenWidth, screenHeight, hdcScreen, 0, 0, SRCCOPY) == 0)
            {
                int error = Marshal.GetLastWin32Error();
                _logger.LogError("BitBlt failed: {Error}, Message: {Message}", error, new System.ComponentModel.Win32Exception(error).Message);
                ReleaseDC(IntPtr.Zero, hdcScreen);
                DeleteObject(hdcCompatible);
                DeleteObject(hBitmap);
                return;
            }

            using (Bitmap bmp = Bitmap.FromHbitmap(hBitmap))
            {
                SelectObject(hdcCompatible, hOld);
                DeleteObject(hBitmap);
                DeleteObject(hdcCompatible);
                ReleaseDC(IntPtr.Zero, hdcScreen);

                bmp.Save(filePath, ImageFormat.Png);
                _logger.LogInformation("Screenshot saved to: {FilePath}", filePath);

                if (_config.StorageMode == StorageMode.Email)
                {
                    string jpgPath = Path.ChangeExtension(filePath, "jpg");
                    ConvertToJpgWithSizeLimit(bmp, jpgPath, 100);
                    SendEmailWithAttachment(jpgPath);
                }
                else
                {
                    _logger.LogInformation("Local storage mode - screenshot saved successfully");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred: {Message}", ex.Message);
        }
    }

    private void ConvertToJpgWithSizeLimit(Bitmap bitmap, string outputPath, int maxSizeKB)
    {
        try
        {
            var encoderParams = new EncoderParameters(1);
            ImageCodecInfo encoderInfo = GetEncoderInfo("image/jpeg");
            long targetSize = maxSizeKB * 1024;

            long srcSize;
            using (var tempMs = new MemoryStream())
            {
                encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);
                bitmap.Save(tempMs, encoderInfo, encoderParams);
                srcSize = tempMs.Length;
            }

            long quality = (long)Math.Round(100.0 * targetSize / srcSize);
            quality = Math.Clamp(quality, 5, 90);

            encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);
            bitmap.Save(outputPath, encoderInfo, encoderParams);
            _logger.LogInformation("JPG converted: {OutputPath}", outputPath);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "JPG conversion failed");
        }
    }

    private void SendEmailWithAttachment(string attachmentPath)
    {
        var emailConfig = _config.Email;
        _logger.LogInformation("Starting email send process - Target: {EmailAddress}", MaskEmail(emailConfig.EmailAddress));
        _logger.LogInformation("SMTP server: {SmtpServer}, Port: {SmtpPort}", emailConfig.SmtpServer, emailConfig.SmtpPort);
        _logger.LogInformation("Email provider: {Provider}", emailConfig.Provider);
        _logger.LogInformation("Encrypted auth code length: {Length}", emailConfig.EncryptedAuthCode?.Length ?? 0);

        string authCode = _encryptionService.Decrypt(emailConfig.EncryptedAuthCode ?? "");
        _logger.LogInformation("Auth code decryption completed, length: {Length}", authCode?.Length ?? 0);

        if (string.IsNullOrEmpty(emailConfig.EmailAddress))
        {
            _logger.LogError("Email config incomplete: email address is empty");
            File.Delete(attachmentPath);
            _logger.LogInformation("Temp file deleted: {AttachmentPath}", attachmentPath);
            return;
        }

        if (string.IsNullOrEmpty(authCode))
        {
            _logger.LogError("Email config incomplete: auth code is empty or decryption failed");
            File.Delete(attachmentPath);
            _logger.LogInformation("Temp file deleted: {AttachmentPath}", attachmentPath);
            return;
        }

        _logger.LogInformation("Email config validated, preparing to send");

        try
        {
            _logger.LogInformation("Sending email to: {EmailAddress}", MaskEmail(emailConfig.EmailAddress));
            _logger.LogInformation("Email subject: Screen Monitor");
            _logger.LogInformation("Email attachment: {AttachmentPath}", attachmentPath);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("", emailConfig.EmailAddress));
            message.To.Add(new MailboxAddress("", emailConfig.EmailAddress));
            message.Subject = "Screen Monitor";

            var bodyBuilder = new BodyBuilder
            {
                TextBody = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss monitor screenshot")
            };
            bodyBuilder.Attachments.Add(attachmentPath);
            message.Body = bodyBuilder.ToMessageBody();

            _logger.LogInformation("Email message created, sending...");

            using (var smtpClient = new SmtpClient())
            {
                if (emailConfig.Provider == EmailProvider.NetEase)
                {
                    _logger.LogInformation("NetEase email config: using implicit SSL (port 465)");
                    smtpClient.Connect(emailConfig.SmtpServer, emailConfig.SmtpPort, SecureSocketOptions.SslOnConnect);
                }
                else
                {
                    _logger.LogInformation("QQ email config: using STARTTLS (port 587)");
                    smtpClient.Connect(emailConfig.SmtpServer, emailConfig.SmtpPort, SecureSocketOptions.StartTls);
                }

                smtpClient.Authenticate(emailConfig.EmailAddress, authCode);
                smtpClient.Send(message);
                smtpClient.Disconnect(true);

                _logger.LogInformation("Email sent successfully!");
                _logger.LogInformation("Email details: From={From}, To={To}, Subject={Subject}",
                    MaskEmail(emailConfig.EmailAddress), MaskEmail(emailConfig.EmailAddress), "Screen Monitor");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Email send failed: {Message}", e.Message);
        }

        try
        {
            File.Delete(attachmentPath);
            _logger.LogInformation("Temp file deleted successfully: {AttachmentPath}", attachmentPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Temp file deletion failed: {Message}", ex.Message);
        }

        _logger.LogInformation("Email send process completed");
    }

    private ImageCodecInfo GetEncoderInfo(string mimeType)
    {
        ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
        foreach (ImageCodecInfo codec in codecs)
        {
            if (codec.MimeType == mimeType)
            {
                return codec;
            }
        }
        throw new ArgumentException("Image encoder for specified MIME type not found");
    }
}
