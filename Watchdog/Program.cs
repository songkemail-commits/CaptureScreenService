using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<WatchdogService>();

builder.Logging.AddEventLog(new EventLogSettings
{
    SourceName = "ScreenCapWatchdog",
    LogName = "Application"
});

var host = builder.Build();
host.Run();

public class WatchdogService : BackgroundService
{
    private readonly ILogger<WatchdogService> _logger;
    private readonly string _mainExePath;
    private readonly int _checkIntervalSeconds = 30;

    public WatchdogService(ILogger<WatchdogService> logger)
    {
        _logger = logger;
        _mainExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CaptureScreenService.exe");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Watchdog started. Monitoring: {MainExe}", _mainExePath);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!IsMainProcessRunning())
                {
                    _logger.LogWarning("Main process not running. Starting...");
                    StartMainProcess();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking main process: {Message}", ex.Message);
            }

            await Task.Delay(TimeSpan.FromSeconds(_checkIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("Watchdog stopping...");
    }

    private bool IsMainProcessRunning()
    {
        var processes = Process.GetProcessesByName("CaptureScreenService");
        return processes.Length > 0;
    }

    private void StartMainProcess()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _mainExePath,
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
            };

            Process.Start(startInfo);
            _logger.LogInformation("Main process started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start main process: {Message}", ex.Message);
        }
    }
}
