using Serilog.Events;

namespace AzStore.Configuration;

public class LoggingSettings
{
    public LogEventLevel LogLevel { get; set; } = LogEventLevel.Information;
    public bool EnableConsoleLogging { get; set; } = true;
    public bool EnableFileLogging { get; set; } = true;
    public string? LogFilePath { get; set; }
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB
    public int RetainedFileCountLimit { get; set; } = 7;
}