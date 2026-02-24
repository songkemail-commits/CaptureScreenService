// Copyright (c) 2026 songkemail-commits
// Licensed under the MIT License (MIT)

namespace CaptureScreenService;

/// <summary>
/// 存储模式枚举
/// </summary>
public enum StorageMode
{
    /// <summary>
    /// 本地存储模式
    /// </summary>
    Local,
    /// <summary>
    /// 邮箱发送模式
    /// </summary>
    Email
}

/// <summary>
/// 邮箱提供商枚举
/// </summary>
public enum EmailProvider
{
    /// <summary>
    /// QQ邮箱
    /// </summary>
    QQ,
    /// <summary>
    /// 网易邮箱
    /// </summary>
    NetEase
}

/// <summary>
/// 邮箱配置类
/// </summary>
public class EmailConfig
{
    /// <summary>
    /// 邮箱提供商
    /// </summary>
    public EmailProvider Provider { get; set; } = EmailProvider.QQ;

    /// <summary>
    /// SMTP服务器地址
    /// </summary>
    public string SmtpServer { get; set; } = "smtp.qq.com";

    /// <summary>
    /// SMTP服务器端口
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// 邮箱地址
    /// </summary>
    public string EmailAddress { get; set; } = "";

    /// <summary>
    /// 加密后的授权码
    /// </summary>
    public string EncryptedAuthCode { get; set; } = "";
}

/// <summary>
/// 本地存储配置类
/// </summary>
public class LocalConfig
{
    /// <summary>
    /// 截图保存路径
    /// </summary>
    public string SavePath { get; set; } = @"C:\temp\TempPics";
}

/// <summary>
/// 安全配置类
/// </summary>
public class SecurityConfig
{
    /// <summary>
    /// 加密熵值
    /// </summary>
    public string Entropy { get; set; } = "";
}

/// <summary>
/// 应用配置类
/// </summary>
public class AppConfig
{
    /// <summary>
    /// 存储模式
    /// </summary>
    public StorageMode StorageMode { get; set; } = StorageMode.Email;

    /// <summary>
    /// 本地存储配置
    /// </summary>
    public LocalConfig Local { get; set; } = new();

    /// <summary>
    /// 邮箱配置
    /// </summary>
    public EmailConfig Email { get; set; } = new();

    /// <summary>
    /// 安全配置
    /// </summary>
    public SecurityConfig Security { get; set; } = new();

    /// <summary>
    /// 截图间隔（分钟）
    /// </summary>
    public int CaptureIntervalMinutes { get; set; } = 5;
}
