namespace CaptureScreenService;

public class Worker(ScreenCapService screenCapService, AppConfig config, ILogger<Worker> logger) : BackgroundService
{
    private readonly ScreenCapService _screenCapService = screenCapService;
    private readonly AppConfig _config = config;
    private readonly ILogger<Worker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CaptureScreenService started. Storage mode: {StorageMode}", _config.StorageMode);
        _logger.LogInformation("Capture interval: {Minutes} minutes", _config.CaptureIntervalMinutes);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _screenCapService.CaptureMainScreen();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Capture failed: {Message}", ex.Message);
                }

                await Task.Delay(TimeSpan.FromMinutes(_config.CaptureIntervalMinutes), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("CaptureScreenService is stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error: {Message}", ex.Message);
            Environment.Exit(1);
        }
    }
}
