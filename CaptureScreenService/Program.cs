// Copyright (c) 2026 songkemail-commits
// Licensed under the MIT License (MIT)

using CaptureScreenService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using System.IO;
using System.Text.Json;

if (args.Length > 0 && args[0] == "--test")
{
    Console.WriteLine("CaptureScreenService test mode starting...");

    var config = new AppConfig();
    var configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
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
    var encryptionLogger = loggerFactory.CreateLogger<EncryptionService>();
    var encryptionService = new EncryptionService(config.Security?.Entropy, encryptionLogger);
    var screenCapService = new ScreenCapService(logger, config, encryptionService);

    Console.WriteLine("Executing screenshot test...");
    logger.LogInformation("Test mode: Starting screenshot");

    try
    {
        screenCapService.CaptureMainScreen();
        Console.WriteLine("Screenshot test completed!");
        logger.LogInformation("Test mode: Screenshot completed");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Test failed: {ex.Message}");
        logger.LogError(ex, "Test mode: Screenshot failed");
    }

    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
    return;
}

if (args.Length > 0 && args[0] == "--encrypt" && args.Length > 1)
{
    var configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
    var config = new AppConfig();

    if (File.Exists(configPath))
    {
        var jsonContent = File.ReadAllText(configPath);
        using var doc = JsonDocument.Parse(jsonContent);
        if (doc.RootElement.TryGetProperty("AppConfig", out var appConfigElement))
        {
            if (appConfigElement.TryGetProperty("Security", out var securityElement))
            {
                config.Security = new SecurityConfig
                {
                    Entropy = securityElement.GetProperty("Entropy").GetString() ?? ""
                };
            }
        }
    }

    var encryptionService = new EncryptionService(config.Security?.Entropy);
    var encrypted = encryptionService.Encrypt(args[1]);

    Console.WriteLine($"Encrypted auth code: {encrypted}");

    var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "encrypt_output.txt");
    File.WriteAllText(outputPath, encrypted);
    Console.WriteLine($"Saved to: {outputPath}");
    return;
}

var builder = Host.CreateApplicationBuilder(args);

var configPath2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
var config2 = new AppConfig();

if (File.Exists(configPath2))
{
    var jsonContent = File.ReadAllText(configPath2);
    using var doc = JsonDocument.Parse(jsonContent);
    if (doc.RootElement.TryGetProperty("AppConfig", out var appConfigElement))
    {
        if (appConfigElement.TryGetProperty("Security", out var securityElement) &&
            securityElement.TryGetProperty("Entropy", out var entropyElement))
        {
            config2.Security = new SecurityConfig
            {
                Entropy = entropyElement.GetString() ?? ""
            };
        }
    }
}

if (string.IsNullOrEmpty(config2.Security?.Entropy))
{
    var newEntropy = Convert.ToBase64String(EncryptionService.GenerateEntropy());
    config2.Security = new SecurityConfig { Entropy = newEntropy };

    UpdateConfigFile(configPath2, newEntropy);
}

builder.Services.AddSingleton<EncryptionService>(sp =>
{
    var cfg = sp.GetRequiredService<AppConfig>();
    var logger = sp.GetRequiredService<ILogger<EncryptionService>>();
    return new EncryptionService(cfg.Security?.Entropy, logger);
});

builder.Services.Configure<AppConfig>(builder.Configuration.GetSection("AppConfig"));
builder.Services.AddSingleton<AppConfig>(sp =>
{
    var config = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AppConfig>>().Value;
    if (string.IsNullOrEmpty(config.Security?.Entropy))
    {
        config.Security = config2.Security;
    }
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

static void UpdateConfigFile(string configPath, string entropy)
{
    try
    {
        var jsonContent = File.ReadAllText(configPath);
        using var doc = JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();

            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Name == "AppConfig")
                {
                    writer.WritePropertyName("AppConfig");
                    writer.WriteStartObject();

                    foreach (var appProp in prop.Value.EnumerateObject())
                    {
                        if (appProp.Name == "Security")
                        {
                            writer.WritePropertyName("Security");
                            writer.WriteStartObject();
                            writer.WriteString("Entropy", entropy);
                            writer.WriteEndObject();
                        }
                        else
                        {
                            writer.WritePropertyName(appProp.Name);
                            appProp.Value.WriteTo(writer);
                        }
                    }

                    if (!prop.Value.TryGetProperty("Security", out _))
                    {
                        writer.WritePropertyName("Security");
                        writer.WriteStartObject();
                        writer.WriteString("Entropy", entropy);
                        writer.WriteEndObject();
                    }

                    writer.WriteEndObject();
                }
                else
                {
                    writer.WritePropertyName(prop.Name);
                    prop.Value.WriteTo(writer);
                }
            }

            writer.WriteEndObject();
        }

        File.WriteAllText(configPath, System.Text.Encoding.UTF8.GetString(stream.ToArray()));
    }
    catch
    {
    }
}
