using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AzStore.Core.Services.Abstractions;
using AzStore.Terminal.UI;
using AzStore.Terminal.Utilities;

namespace AzStore.Terminal.Commands;

public class ExitCommand : ICommand
{
    private readonly ILogger<ExitCommand> _logger;
    private readonly ISessionManager _sessionManager;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IDownloadActivity _downloadActivity;

    public string Name => "exit";
    public string[] Aliases => ["q", "q!", "exit!"];
    public string Description => "Exit the application (:q, :q! for force)";

    public ExitCommand(ILogger<ExitCommand> logger, ISessionManager sessionManager, IHostApplicationLifetime lifetime, IDownloadActivity downloadActivity)
    {
        _logger = logger;
        _sessionManager = sessionManager;
        _lifetime = lifetime;
        _downloadActivity = downloadActivity;
    }

    public async Task<CommandResult> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("User initiated exit via command");

        var force = args.Any(a => string.Equals(a, "--force", StringComparison.OrdinalIgnoreCase));

        if (!force && _downloadActivity.HasActiveDownloads)
        {
            var count = _downloadActivity.ActiveCount;
            var result = TerminalConfirmation.ShowConfirmation(
                message: count == 1
                    ? "A download is in progress. Exiting will cancel it. Exit anyway?"
                    : $"{count} downloads are in progress. Exiting will cancel them. Exit anyway?",
                defaultChoice: 'N');

            if (result != ConfirmationResult.Yes)
            {
                _logger.LogInformation("Exit cancelled by user");
                return CommandResult.Ok("Exit cancelled.");
            }
        }

        try
        {
            await _sessionManager.SaveSessionsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save session metadata during exit");
        }

        try
        {
            _lifetime.StopApplication();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "StopApplication threw (can be ignored if host already stopping)");
        }

        return CommandResult.Exit("Goodbye!");
    }
}
