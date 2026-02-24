// Copyright (c) 2026 songkemail-commits
// Licensed under the MIT License (MIT)

using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Encoder = System.Drawing.Imaging.Encoder;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace CaptureScreenService;

/// <summary>
/// 屏幕捕获服务类
/// </summary>
public sealed class ScreenCapService
{
    private readonly ILogger<ScreenCapService> _logger;
    private readonly AppConfig _config;
    private readonly EncryptionService _encryptionService;

    // Windows API 函数声明
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

    // 常量定义
    private const int SRCCOPY = 0x00CC0020;
    private const int DESKTOPHORZRES = 118;
    private const int DESKTOPVERTRES = 117;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="config">应用配置</param>
    /// <param name="encryptionService">加密服务</param>
    public ScreenCapService(ILogger<ScreenCapService> logger, AppConfig config, EncryptionService encryptionService)
    {
        _logger = logger;
        _config = config;
        _encryptionService = encryptionService;
    }

    /// <summary>
    /// 邮箱地址掩码处理
    /// </summary>
    /// <param name="email">邮箱地址</param>
    /// <returns>掩码处理后的邮箱地址</returns>
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

    /// <summary>
    /// 捕获主屏幕
    /// </summary>
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

        // 确定保存目录
        string saveDirectory = _config.StorageMode == StorageMode.Local
            ? _config.Local?.SavePath ?? @"C:\temp\TempPics"
            : @"C:\temp\TempPics";

        try
        {
            // 创建保存目录
            Directory.CreateDirectory(saveDirectory);
            _logger.LogInformation("Save directory: {SaveDirectory}", saveDirectory);

            // 生成文件名和路径
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string fileName = $"screenshot_{timestamp}.png";
            string filePath = Path.Combine(saveDirectory, fileName);

            // 获取屏幕设备上下文
            IntPtr hdcScreen = GetDC(IntPtr.Zero);
            if (hdcScreen == IntPtr.Zero)
            {
                _logger.LogError("GetDC failed: {Error}", Marshal.GetLastWin32Error());
                return;
            }

            // 创建兼容的设备上下文
            IntPtr hdcCompatible = CreateCompatibleDC(hdcScreen);
            if (hdcCompatible == IntPtr.Zero)
            {
                _logger.LogError("CreateCompatibleDC failed: {Error}", Marshal.GetLastWin32Error());
                ReleaseDC(IntPtr.Zero, hdcScreen);
                return;
            }

            // 获取屏幕分辨率
            int screenWidth = GetDeviceCaps(hdcScreen, DESKTOPHORZRES);
            int screenHeight = GetDeviceCaps(hdcScreen, DESKTOPVERTRES);

            // 创建兼容的位图
            IntPtr hBitmap = CreateCompatibleBitmap(hdcScreen, screenWidth, screenHeight);
            if (hBitmap == IntPtr.Zero)
            {
                _logger.LogError("CreateCompatibleBitmap failed: {Error}", Marshal.GetLastWin32Error());
                ReleaseDC(IntPtr.Zero, hdcScreen);
                DeleteObject(hdcCompatible);
                return;
            }

            // 选择位图到设备上下文
            IntPtr hOld = SelectObject(hdcCompatible, hBitmap);
            if (hOld == IntPtr.Zero)
            {
                _logger.LogError("SelectObject failed: {Error}", Marshal.GetLastWin32Error());
                ReleaseDC(IntPtr.Zero, hdcScreen);
                DeleteObject(hdcCompatible);
                DeleteObject(hBitmap);
                return;
            }

            // 复制屏幕内容到位图
            if (BitBlt(hdcCompatible, 0, 0, screenWidth, screenHeight, hdcScreen, 0, 0, SRCCOPY) == 0)
            {
                int error = Marshal.GetLastWin32Error();
                _logger.LogError("BitBlt failed: {Error}, Message: {Message}", error, new System.ComponentModel.Win32Exception(error).Message);
                ReleaseDC(IntPtr.Zero, hdcScreen);
                DeleteObject(hdcCompatible);
                DeleteObject(hBitmap);
                return;
            }

            // 处理捕获的位图
            using (Bitmap bmp = Bitmap.FromHbitmap(hBitmap))
            {
                // 清理资源
                SelectObject(hdcCompatible, hOld);
                DeleteObject(hBitmap);
                DeleteObject(hdcCompatible);
                ReleaseDC(IntPtr.Zero, hdcScreen);

                // 保存为 PNG 文件
                bmp.Save(filePath, ImageFormat.Png);
                _logger.LogInformation("Screenshot saved to: {FilePath}", filePath);

                // 根据存储模式处理
                if (_config.StorageMode == StorageMode.Email)
                {
                    // 转换为 JPG 并发送邮件
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

    /// <summary>
    /// 将位图转换为 JPG 并限制大小
    /// </summary>
    /// <param name="bitmap">位图对象</param>
    /// <param name="outputPath">输出路径</param>
    /// <param name="maxSizeKB">最大大小（KB）</param>
    private void ConvertToJpgWithSizeLimit(Bitmap bitmap, string outputPath, int maxSizeKB)
    {
        try
        {
            var encoderParams = new EncoderParameters(1);
            ImageCodecInfo encoderInfo = GetEncoderInfo("image/jpeg");
            long targetSize = maxSizeKB * 1024;

            // 计算原始大小
            long srcSize;
            using (var tempMs = new MemoryStream())
            {
                encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);
                bitmap.Save(tempMs, encoderInfo, encoderParams);
                srcSize = tempMs.Length;
            }

            // 计算压缩质量
            long quality = (long)Math.Round(100.0 * targetSize / srcSize);
            quality = Math.Clamp(quality, 5, 90);

            // 保存为 JPG 文件
            encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);
            bitmap.Save(outputPath, encoderInfo, encoderParams);
            _logger.LogInformation("JPG converted: {OutputPath}", outputPath);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "JPG conversion failed");
        }
    }

    /// <summary>
    /// 发送带附件的邮件
    /// </summary>
    /// <param name="attachmentPath">附件路径</param>
    private void SendEmailWithAttachment(string attachmentPath)
    {
        var emailConfig = _config.Email;
        _logger.LogInformation("Starting email send process - Target: {EmailAddress}", MaskEmail(emailConfig.EmailAddress));
        _logger.LogInformation("SMTP server: {SmtpServer}, Port: {SmtpPort}", emailConfig.SmtpServer, emailConfig.SmtpPort);
        _logger.LogInformation("Email provider: {Provider}", emailConfig.Provider);
        _logger.LogInformation("Encrypted auth code length: {Length}", emailConfig.EncryptedAuthCode?.Length ?? 0);

        // 解密授权码
        string authCode = _encryptionService.Decrypt(emailConfig.EncryptedAuthCode ?? "");
        _logger.LogInformation("Auth code decryption completed, length: {Length}", authCode?.Length ?? 0);

        // 验证邮箱配置
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

            // 创建邮件消息
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("", emailConfig.EmailAddress));
            message.To.Add(new MailboxAddress("", emailConfig.EmailAddress));
            message.Subject = "Screen Monitor";

            // 构建邮件内容
            var bodyBuilder = new BodyBuilder
            {
                TextBody = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss monitor screenshot")
            };
            bodyBuilder.Attachments.Add(attachmentPath);
            message.Body = bodyBuilder.ToMessageBody();

            _logger.LogInformation("Email message created, sending...");

            // 发送邮件
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
        finally
        {
            // 清理临时文件
            try
            {
                File.Delete(attachmentPath);
                _logger.LogInformation("Temp file deleted successfully: {AttachmentPath}", attachmentPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Temp file deletion failed: {Message}", ex.Message);
            }
        }

        _logger.LogInformation("Email send process completed");
    }

    /// <summary>
    /// 获取指定 MIME 类型的图像编码器
    /// </summary>
    /// <param name="mimeType">MIME 类型</param>
    /// <returns>图像编码器信息</returns>
    /// <exception cref="ArgumentException">当找不到指定 MIME 类型的编码器时抛出</exception>
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
