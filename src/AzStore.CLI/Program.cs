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
        try
        {
            var configDir = GetConfigurationDirectory();
            Directory.CreateDirectory(configDir);
            var configPath = Path.Combine(configDir, "azstore.toml");

            configuration
                .AddJsonFile("appsettings.json", optional: true)
                .AddTomlFile(configPath, optional: true)
                .AddEnvironmentVariables("AZSTORE_");
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"Warning: Cannot access configuration directory: {ex.Message}");
            Console.WriteLine("Using default configuration without file-based settings.");
            
            configuration
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables("AZSTORE_");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Configuration directory setup failed: {ex.Message}");
            Console.WriteLine("Using default configuration without file-based settings.");
            
            configuration
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables("AZSTORE_");
        }
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
            try
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
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Warning: Cannot create log directory due to insufficient permissions: {ex.Message}");
                Console.WriteLine("File logging disabled. Console logging will continue.");
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine($"Warning: Cannot create log directory - path not found: {ex.Message}");
                Console.WriteLine("File logging disabled. Console logging will continue.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to configure file logging: {ex.Message}");
                Console.WriteLine("File logging disabled. Console logging will continue.");
            }
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