using AzStore.Configuration;
using Serilog.Events;

namespace AzStore.Configuration.Tests;

public static class LoggingSettingsFixture
{
    public static LoggingSettings CreateDefault() => new();

    public static LoggingSettings CreateWithCustomValues() => new()
    {
        LogLevel = LogEventLevel.Debug,
        EnableConsoleLogging = false,
        EnableFileLogging = false,
        LogFilePath = "/custom/path/app.log",
        MaxFileSizeBytes = 5000000,
        RetainedFileCountLimit = 10
    };
}