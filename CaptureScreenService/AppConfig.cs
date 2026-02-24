namespace CaptureScreenService;

public enum StorageMode
{
    Local,
    Email
}

public enum EmailProvider
{
    QQ,
    NetEase
}

public class EmailConfig
{
    public EmailProvider Provider { get; set; } = EmailProvider.QQ;
    public string SmtpServer { get; set; } = "smtp.qq.com";
    public int SmtpPort { get; set; } = 587;
    public string EmailAddress { get; set; } = "";
    public string EncryptedAuthCode { get; set; } = "";
}

public class LocalConfig
{
    public string SavePath { get; set; } = @"C:\temp\TempPics";
}

public class AppConfig
{
    public StorageMode StorageMode { get; set; } = StorageMode.Email;
    public LocalConfig Local { get; set; } = new();
    public EmailConfig Email { get; set; } = new();
    public int CaptureIntervalMinutes { get; set; } = 5;
}
