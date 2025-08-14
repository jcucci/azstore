using AzStore.Terminal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzStore.CLI;

public class ReplHostedService : BackgroundService
{
    private readonly IReplEngine _replEngine;
    private readonly ILogger<ReplHostedService> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    public ReplHostedService(
        IReplEngine replEngine, 
        ILogger<ReplHostedService> logger,
        IHostApplicationLifetime lifetime)
    {
        _replEngine = replEngine;
        _logger = logger;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting AzStore REPL");
            await _replEngine.StartAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("REPL stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "REPL encountered an error");
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }
}