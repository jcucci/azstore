using AzStore.Core.IO;
using AzStore.Core.Services.Abstractions;
using AzStore.Terminal.Utilities;
using Microsoft.Extensions.Logging;

namespace AzStore.Terminal.Commands;

public class ListCommand : ICommand
{
    private readonly ILogger<ListCommand> _logger;
    private readonly ISessionManager _sessionManager;

    public string Name => "list";
    public string[] Aliases => ["ls"];
    public string Description => "List downloaded files for current session";

    public ListCommand(ILogger<ListCommand> logger, ISessionManager sessionManager)
    {
        _logger = logger;
        _sessionManager = sessionManager;
    }

    public Task<CommandResult> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("User requested file list");
        cancellationToken.ThrowIfCancellationRequested();

        var session = _sessionManager.GetActiveSession();
        if (session == null)
            return Task.FromResult(CommandResult.Error("No active session. Use ':session list' and ':session switch <name>' to select a session."));

        var sessionRoot = GetSessionRoot(session);
        if (!Directory.Exists(sessionRoot))
            return Task.FromResult(CommandResult.Ok("No files downloaded yet."));

        var options = ListCommandOptions.FromArgs(args);

        try
        {
            var files = EnumerateFiles(sessionRoot, cancellationToken).ToList();
            if (files.Count == 0)
                return Task.FromResult(CommandResult.Ok("No files downloaded yet."));

            var filtered = FilterByContainer(files, sessionRoot, options.Container);
            filtered = FilterByPattern(filtered, sessionRoot, options.Pattern);

            var list = filtered.ToList();
            if (list.Count == 0)
            {
                var msg = string.IsNullOrWhiteSpace(options.Container) && string.IsNullOrWhiteSpace(options.Pattern)
                    ? "No files downloaded yet."
                    : "No matching files.";

                return Task.FromResult(CommandResult.Ok(msg));
            }

            var sorted = SortFiles(list, sessionRoot, options.SortKey, options.Descending);
            var output = string.Join('\n', FormatLines(sorted, sessionRoot));
            return Task.FromResult(CommandResult.Ok(output));
        }
        catch (OperationCanceledException)
        {
            return Task.FromResult(CommandResult.Error(":ls canceled"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list downloaded files");
            return Task.FromResult(CommandResult.Error($":ls failed: {ex.Message}"));
        }
    }

    private static string GetSessionRoot(Core.Models.Session.Session session) =>
        Path.Combine(session.Directory, PathHelper.CreateSafeDirectoryName(session.Name));

    private static IEnumerable<FileInfo> EnumerateFiles(string root, CancellationToken ct)
    {
        foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            yield return new FileInfo(path);
        }
    }

    private static IEnumerable<FileInfo> FilterByContainer(IEnumerable<FileInfo> files, string root, string? container)
    {
        if (string.IsNullOrWhiteSpace(container)) return files;
        var safeContainer = PathHelper.CreateSafeDirectoryName(container);
        return files.Where(fi =>
        {
            var rel = Path.GetRelativePath(root, fi.FullName);
            var firstSep = rel.IndexOf(Path.DirectorySeparatorChar);
            var firstSegment = firstSep >= 0 ? rel[..firstSep] : rel;
            return string.Equals(firstSegment, safeContainer, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static IEnumerable<FileInfo> FilterByPattern(IEnumerable<FileInfo> files, string root, string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern)) return files;
        var regex = Glob.ToRegex(pattern);
        return files.Where(fi => regex.IsMatch(Path.GetRelativePath(root, fi.FullName)));
    }

    private static List<FileInfo> SortFiles(IEnumerable<FileInfo> files, string root, string sortKey, bool descending)
    {
        var ordered = sortKey.ToLowerInvariant() switch
        {
            "size" => files.OrderBy(f => f.Length),
            "date" or "time" => files.OrderBy(f => f.LastWriteTimeUtc),
            _ => files.OrderBy(f => Path.GetRelativePath(root, f.FullName), StringComparer.OrdinalIgnoreCase)
        };

        var list = ordered.ToList();
        if (descending) list.Reverse();
        return list;
    }

    private static IEnumerable<string> FormatLines(IEnumerable<FileInfo> files, string root) =>
        files.Select(fi =>
        {
            var rel = Path.GetRelativePath(root, fi.FullName);
            var size = TerminalUtils.FormatSize(fi.Length);
            var date = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm");
            return $"{rel}    {size,8}    {date}";
        });
}
