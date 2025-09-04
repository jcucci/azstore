using AzStore.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;
using Serilog.Formatting.Compact;
using System.CommandLine;
using System.CommandLine.Parsing;
using Tomlyn.Extensions.Configuration;

namespace AzStore.CLI;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = CreateRootCommand();
        var parseResult = rootCommand.Parse(args);
        return await parseResult.InvokeAsync();
    }

    private static RootCommand CreateRootCommand()
    {
        var rootCommand = new RootCommand("azstore - Azure Blob Storage terminal interface")
        {
            CommandLineOptions.SessionsDirectory,
            CommandLineOptions.StorageAccount,
            CommandLineOptions.LogLevel,
            CommandLineOptions.FileConflict,
            CommandLineOptions.ConsoleLogging,
            CommandLineOptions.FileLogging,
            CommandLineOptions.LogFile,
            CommandLineOptions.Verbose,
            CommandLineOptions.Quiet
        };

        rootCommand.SetAction(async (parseResult, cancellationToken) => 
        {
            await RunApplication(parseResult);
            return 0;
        });
        return rootCommand;
    }

    private static async Task RunApplication(ParseResult parseResult)
    {
        var parsedArgs = CommandLineOptions.ExtractConfigurationValues(parseResult);
        var builder = Host.CreateApplicationBuilder();
        
        ConfigureConfiguration(builder.Configuration, parsedArgs);
        var consoleLevelSwitch = new LoggingLevelSwitch();
        ConfigureSerilog(builder.Configuration, consoleLevelSwitch);
        
        builder.Services.AddSerilog();
        // Make the console level switch and console log scope available to services
        builder.Services.AddSingleton(consoleLevelSwitch);
        builder.Services.AddSingleton<AzStore.Terminal.Utilities.IConsoleLogScope>(sp => new SerilogConsoleLogScope(consoleLevelSwitch));
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

    private static void ConfigureConfiguration(IConfigurationBuilder configuration, Dictionary<string, string?> commandLineArgs)
    {
        try
        {
            var configDir = GetConfigurationDirectory();
            Directory.CreateDirectory(configDir);
            var configPath = Path.Combine(configDir, "azstore.toml");

            configuration
                .AddJsonFile("appsettings.json", optional: true)
                .AddTomlFile(configPath, optional: true)
                .AddEnvironmentVariables("AZSTORE_")
                .AddInMemoryCollection(commandLineArgs);
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"Warning: Cannot access configuration directory: {ex.Message}");
            Console.WriteLine("Using default configuration without file-based settings.");
            
            configuration
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables("AZSTORE_")
                .AddInMemoryCollection(commandLineArgs);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Configuration directory setup failed: {ex.Message}");
            Console.WriteLine("Using default configuration without file-based settings.");
            
            configuration
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables("AZSTORE_")
                .AddInMemoryCollection(commandLineArgs);
        }
    }

    private static void ConfigureSerilog(IConfiguration configuration, LoggingLevelSwitch consoleLevelSwitch)
    {
        var loggingSettings = new LoggingSettings();
        configuration.GetSection($"{AzStoreSettings.SectionName}:Logging").Bind(loggingSettings);

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Is(loggingSettings.LogLevel);

        if (loggingSettings.EnableConsoleLogging)
        {
            // Use a level switch for console so we can temporarily suppress during interactive pickers
            consoleLevelSwitch.MinimumLevel = loggingSettings.LogLevel;
            loggerConfig.WriteTo.Console(
                levelSwitch: consoleLevelSwitch,
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
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
