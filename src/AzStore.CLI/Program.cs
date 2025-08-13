using AzStore.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Tomlyn.Extensions.Configuration;

namespace AzStore.CLI;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        
        ConfigureConfiguration(builder.Configuration);
        ConfigureSerilog(builder.Configuration);
        
        builder.Services.AddSerilog();
        builder.Services.AddAzStoreServices();

        var host = builder.Build();

        try
        {
            Log.Information("Starting AzStore CLI");
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static void ConfigureConfiguration(IConfigurationBuilder configuration)
    {
        var configDir = GetConfigurationDirectory();
        var configPath = Path.Combine(configDir, "azstore.toml");

        configuration
            .AddJsonFile("appsettings.json", optional: true)
            .AddTomlFile(configPath, optional: true)
            .AddEnvironmentVariables("AZSTORE_");
    }

    private static void ConfigureSerilog(IConfiguration configuration)
    {
        var loggingSettings = new LoggingSettings();
        configuration.GetSection($"{AzStoreSettings.SectionName}:Logging").Bind(loggingSettings);

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Is(loggingSettings.LogLevel);

        if (loggingSettings.EnableConsoleLogging)
        {
            loggerConfig.WriteTo.Console();
        }

        if (loggingSettings.EnableFileLogging)
        {
            var logFilePath = loggingSettings.LogFilePath ?? GetDefaultLogFilePath();
            var logDirectory = Path.GetDirectoryName(logFilePath);
            
            if (!string.IsNullOrEmpty(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            loggerConfig.WriteTo.File(
                logFilePath,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: loggingSettings.MaxFileSizeBytes,
                retainedFileCountLimit: loggingSettings.RetainedFileCountLimit);
        }

        Log.Logger = loggerConfig.CreateLogger();
    }

    private static string GetConfigurationDirectory()
    {
        var configDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            configDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            configDir = Path.Combine(configDir, ".config");
        }
        
        return Path.Combine(configDir, "azstore");
    }

    private static string GetDefaultLogFilePath()
    {
        if (OperatingSystem.IsWindows())
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "azstore", "logs", "azstore.log");
        }
        else if (OperatingSystem.IsMacOS())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Library", "Logs", "azstore", "azstore.log");
        }
        else
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".local", "share", "azstore", "logs", "azstore.log");
        }
    }
}