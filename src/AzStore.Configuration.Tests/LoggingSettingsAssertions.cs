using AzStore.Configuration;
using Serilog.Events;
using Xunit;

namespace AzStore.Configuration.Tests;

public static class LoggingSettingsAssertions
{
    public static void ShouldHaveDefaultValues(LoggingSettings settings)
    {
        Assert.Equal(LogEventLevel.Information, settings.LogLevel);
        Assert.True(settings.EnableConsoleLogging);
        Assert.True(settings.EnableFileLogging);
        Assert.Null(settings.LogFilePath);
        Assert.Equal(10 * 1024 * 1024, settings.MaxFileSizeBytes);
        Assert.Equal(7, settings.RetainedFileCountLimit);
    }

    public static void ShouldHaveCustomValues(LoggingSettings settings)
    {
        Assert.Equal(LogEventLevel.Debug, settings.LogLevel);
        Assert.False(settings.EnableConsoleLogging);
        Assert.False(settings.EnableFileLogging);
        Assert.Equal("/custom/path/app.log", settings.LogFilePath);
        Assert.Equal(5000000, settings.MaxFileSizeBytes);
        Assert.Equal(10, settings.RetainedFileCountLimit);
    }
}