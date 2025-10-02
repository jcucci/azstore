using System.CommandLine;

namespace AzStore.CLI;

public static class CommandLineOptions
{
    public static Option<string?> SessionsDirectory =>
        new(name: "--sessions-dir")
        {
            HelpName = "path",
            Description = "Override the directory where session data is stored"
        };

    public static Option<string?> StorageAccount =>
        new(name: "--storage-account", aliases: ["-a"])
        {
            HelpName = "name",
            Description = "Override the default Azure Storage account name"
        };

    public static Option<string?> LogLevel =>
        new(name: "--log-level", aliases: ["-l"])
        {
            HelpName = "level",
            Description = "Set the logging level (Debug, Information, Warning, Error, Fatal)"
        };

    public static Option<string?> FileConflict =>
        new(name: "--file-conflict")
        {
            HelpName = "behavior",
            Description = "Set file conflict resolution behavior (overwrite, skip, rename)"
        };

    public static Option<bool?> ConsoleLogging =>
        new(name: "--console-logging")
        {
            Description = "Enable console logging output"
        };

    public static Option<bool?> FileLogging =>
        new(name: "--file-logging")
        {
            Description = "Enable file logging output"
        };

    public static Option<string?> LogFile =>
        new(name: "--log-file")
        {
            HelpName = "path",
            Description = "Override the log file path"
        };

    public static Option<bool> Verbose =>
        new(name: "--verbose", aliases: ["-v"])
        {
            Description = "Enable verbose (debug) logging"
        };

    public static Option<bool> Quiet =>
        new(name: "--quiet", aliases: ["-q"])
        {
            Description = "Enable quiet mode (warnings and errors only)"
        };

    public static Option<string?> Session =>
        new(name: "--session", aliases: ["-s"])
        {
            HelpName = "name",
            Description = "Specify the session name to open or create on startup"
        };

    public static Dictionary<string, string?> ExtractConfigurationValues(ParseResult parseResult)
    {
        var args = new Dictionary<string, string?>();

        // Extract option values
        var sessionsDir = parseResult.GetValue(SessionsDirectory);
        if (!string.IsNullOrEmpty(sessionsDir))
            args["AzStore:SessionsDirectory"] = sessionsDir;

        var storageAccount = parseResult.GetValue(StorageAccount);
        if (!string.IsNullOrEmpty(storageAccount))
            args["AzStore:DefaultStorageAccount"] = storageAccount;

        var logLevel = parseResult.GetValue(LogLevel);
        if (!string.IsNullOrEmpty(logLevel))
            args["AzStore:Logging:LogLevel"] = logLevel;

        var fileConflict = parseResult.GetValue(FileConflict);
        if (!string.IsNullOrEmpty(fileConflict))
            args["AzStore:OnFileConflict"] = fileConflict;

        var consoleLogging = parseResult.GetValue(ConsoleLogging);
        if (consoleLogging.HasValue)
            args["AzStore:Logging:EnableConsoleLogging"] = consoleLogging.Value.ToString();

        var fileLogging = parseResult.GetValue(FileLogging);
        if (fileLogging.HasValue)
            args["AzStore:Logging:EnableFileLogging"] = fileLogging.Value.ToString();

        var logFile = parseResult.GetValue(LogFile);
        if (!string.IsNullOrEmpty(logFile))
            args["AzStore:Logging:LogFilePath"] = logFile;

        // Handle verbose and quiet options (mutually exclusive)
        var verbose = parseResult.GetValue(Verbose);
        var quiet = parseResult.GetValue(Quiet);

        if (verbose && !quiet)
            args["AzStore:Logging:LogLevel"] = "Debug";
        else if (quiet && !verbose)
            args["AzStore:Logging:LogLevel"] = "Warning";
        else if (verbose && quiet)
        {
            // Handle conflict - verbose takes precedence
            args["AzStore:Logging:LogLevel"] = "Debug";
        }

        return args;
    }
}