using CaptureScreenService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using System.IO;

// 检查命令行参数
if (args.Length > 0 && args[0] == "--test")
{
    // 手动测试模式：执行一次截图并退出
    Console.WriteLine("CaptureScreenService 测试模式启动...");
    
    // 创建必要的服务实例
    var config = new AppConfig();
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        .Build();
    
    configuration.GetSection("AppConfig").Bind(config);
    
    var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddEventLog(new EventLogSettings
        {
            SourceName = "ScreenCapSvc",
            LogName = "Application"
        });
        builder.SetMinimumLevel(LogLevel.Information);
    });
    
    var logger = loggerFactory.CreateLogger<ScreenCapService>();
    var encryptionService = new EncryptionService();
    var screenCapService = new ScreenCapService(logger, config, encryptionService);
    
    Console.WriteLine("正在执行截图测试...");
    logger.LogInformation("测试模式：开始执行截图");
    
    try
    {
        screenCapService.CaptureMainScreen();
        Console.WriteLine("截图测试完成！");
        logger.LogInformation("测试模式：截图执行完成");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"测试失败：{ex.Message}");
        logger.LogError(ex, "测试模式：截图执行失败");
    }
    
    Console.WriteLine("按任意键退出...");
    Console.ReadKey();
    return;
}

// 正常服务模式
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<EncryptionService>();
builder.Services.Configure<AppConfig>(builder.Configuration.GetSection("AppConfig"));
builder.Services.AddSingleton<AppConfig>(sp =>
{
    var config = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AppConfig>>().Value;
    return config;
});
builder.Services.AddSingleton<ScreenCapService>();
builder.Services.AddHostedService<Worker>();

builder.Logging.AddEventLog(new EventLogSettings
{
    SourceName = "ScreenCapSvc",
    LogName = "Application"
});

var host = builder.Build();
host.Run();
