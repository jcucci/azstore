using AzStore.Terminal.UI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzStore.CLI;

public sealed class TuiHostedService : BackgroundService
{
    private readonly ILogger<TuiHostedService> _logger;
    private readonly ITerminalUI _ui;
    private readonly IHostApplicationLifetime _lifetime;

    public TuiHostedService(ILogger<TuiHostedService> logger, ITerminalUI ui, IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _ui = ui;
        _lifetime = lifetime;
        _lifetime.ApplicationStopping.Register(TryShutdown);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting AzStore TUI");
            await _ui.RunAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("TUI canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TUI encountered an error");
        }
        finally
        {
            try { _ui.Shutdown(); } catch { }
            _lifetime.StopApplication();
        }
    }

    private void TryShutdown()
    {
        try { _ui.Shutdown(); } catch { }
    }
}
