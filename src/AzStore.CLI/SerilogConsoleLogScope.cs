using AzStore.Terminal.Utilities;
using Serilog.Core;
using Serilog.Events;

namespace AzStore.CLI;

/// Controls console logging verbosity via Serilog's LoggingLevelSwitch
/// so interactive UI (pickers) are not disrupted by log lines.
public sealed class SerilogConsoleLogScope : IConsoleLogScope
{
    private readonly LoggingLevelSwitch _consoleLevelSwitch;

    public SerilogConsoleLogScope(LoggingLevelSwitch consoleLevelSwitch)
    {
        _consoleLevelSwitch = consoleLevelSwitch;
    }

    public IDisposable Suppress()
    {
        var previous = _consoleLevelSwitch.MinimumLevel;
        // Elevate to Fatal so only catastrophic errors would leak to console
        _consoleLevelSwitch.MinimumLevel = LogEventLevel.Fatal;
        return new RestoreDisposable(_consoleLevelSwitch, previous);
    }

    private sealed class RestoreDisposable : IDisposable
    {
        private readonly LoggingLevelSwitch _switch;
        private readonly LogEventLevel _previous;
        private bool _disposed;

        public RestoreDisposable(LoggingLevelSwitch @switch, LogEventLevel previous)
        {
            _switch = @switch;
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _switch.MinimumLevel = _previous;
            _disposed = true;
        }
    }
}

