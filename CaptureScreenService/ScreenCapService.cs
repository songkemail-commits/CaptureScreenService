using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security.Authentication;
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

    public void CaptureMainScreen()
    {
        _logger.LogInformation("=== 配置诊断开始 ===");
        _logger.LogInformation("StorageMode: {StorageMode}", _config.StorageMode);
        _logger.LogInformation("CaptureIntervalMinutes: {CaptureIntervalMinutes}", _config.CaptureIntervalMinutes);
        _logger.LogInformation("Local.SavePath: {SavePath}", _config.Local?.SavePath ?? "NULL");
        _logger.LogInformation("Email对象是否为null: {IsNull}", _config.Email == null);
        if (_config.Email != null)
        {
            _logger.LogInformation("Email.Provider: {Provider}", _config.Email.Provider);
            _logger.LogInformation("Email.SmtpServer: {SmtpServer}", _config.Email.SmtpServer ?? "NULL");
            _logger.LogInformation("Email.SmtpPort: {SmtpPort}", _config.Email.SmtpPort);
            _logger.LogInformation("Email.EmailAddress: {EmailAddress}", _config.Email.EmailAddress ?? "NULL");
            _logger.LogInformation("Email.EncryptedAuthCode长度: {Length}", _config.Email.EncryptedAuthCode?.Length ?? 0);
        }
        _logger.LogInformation("=== 配置诊断结束 ===");
        
        string saveDirectory = _config.StorageMode == StorageMode.Local
            ? _config.Local.SavePath
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
        _logger.LogInformation("开始处理邮件发送 - 目标邮箱: {EmailAddress}", emailConfig.EmailAddress);
        _logger.LogInformation("SMTP服务器: {SmtpServer}, 端口: {SmtpPort}", emailConfig.SmtpServer, emailConfig.SmtpPort);
        _logger.LogInformation("邮箱提供商: {Provider}", emailConfig.Provider);
        _logger.LogInformation("加密授权码长度: {Length}", emailConfig.EncryptedAuthCode?.Length ?? 0);
        
        string authCode = _encryptionService.Decrypt(emailConfig.EncryptedAuthCode);
        _logger.LogInformation("授权码解密完成，长度: {Length}", authCode?.Length ?? 0);
        
        if (string.IsNullOrEmpty(emailConfig.EmailAddress))
        {
            _logger.LogError("邮件配置不完整: 邮箱地址为空");
            File.Delete(attachmentPath);
            _logger.LogInformation("临时文件已删除: {AttachmentPath}", attachmentPath);
            return;
        }
        
        if (string.IsNullOrEmpty(authCode))
        {
            _logger.LogError("邮件配置不完整: 授权码为空或解密失败");
            File.Delete(attachmentPath);
            _logger.LogInformation("临时文件已删除: {AttachmentPath}", attachmentPath);
            return;
        }

        _logger.LogInformation("邮件配置验证通过，准备发送邮件");
        
        try
        {
            _logger.LogInformation("正在发送邮件到: {EmailAddress}", emailConfig.EmailAddress);
            _logger.LogInformation("邮件主题: 电脑监控");
            _logger.LogInformation("邮件附件: {AttachmentPath}", attachmentPath);
            
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("", emailConfig.EmailAddress));
            message.To.Add(new MailboxAddress("", emailConfig.EmailAddress));
            message.Subject = "电脑监控";
            
            var bodyBuilder = new BodyBuilder
            {
                TextBody = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss 的监控截图")
            };
            bodyBuilder.Attachments.Add(attachmentPath);
            message.Body = bodyBuilder.ToMessageBody();

            _logger.LogInformation("邮件消息创建完成，正在发送...");
            
            using (var smtpClient = new SmtpClient())
            {
                if (emailConfig.Provider == EmailProvider.NetEase)
                {
                    _logger.LogInformation("网易邮箱配置: 使用隐式SSL (端口465)");
                    smtpClient.Connect(emailConfig.SmtpServer, emailConfig.SmtpPort, SecureSocketOptions.SslOnConnect);
                }
                else
                {
                    _logger.LogInformation("QQ邮箱配置: 使用STARTTLS (端口587)");
                    smtpClient.Connect(emailConfig.SmtpServer, emailConfig.SmtpPort, SecureSocketOptions.StartTls);
                }

                smtpClient.Authenticate(emailConfig.EmailAddress, authCode);
                smtpClient.Send(message);
                smtpClient.Disconnect(true);
                
                _logger.LogInformation("邮件发送成功！");
                _logger.LogInformation("邮件发送详情: 发件人={From}, 收件人={To}, 主题={Subject}", 
                    emailConfig.EmailAddress, emailConfig.EmailAddress, "电脑监控");
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "邮件发送失败: {Message}", e.Message);
            _logger.LogError("错误详情: {StackTrace}", e.StackTrace);
        }
        
        try
        {
            File.Delete(attachmentPath);
            _logger.LogInformation("临时文件删除成功: {AttachmentPath}", attachmentPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "临时文件删除失败: {Message}", ex.Message);
        }
        
        _logger.LogInformation("邮件发送流程处理完成");
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
        throw new ArgumentException("未找到指定MIME类型的图像编码器");
    }
}
