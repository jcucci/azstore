using AzStore.Configuration;
using AzStore.Core.Models.Downloads;
using AzStore.Core.Services.Abstractions;
using AzStore.Terminal.UI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzStore.Terminal.Utilities;

public sealed class ConsoleFileConflictResolver : IFileConflictResolver
{
    private readonly ILogger<ConsoleFileConflictResolver> _logger;
    private readonly ISessionManager _sessionManager;
    private readonly IOptions<AzStoreSettings> _settings;

    private readonly Dictionary<string, ConflictResolution> _userSessionPreference = [];

    public ConsoleFileConflictResolver(ILogger<ConsoleFileConflictResolver> logger, ISessionManager sessionManager, IOptions<AzStoreSettings> settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task<FileConflictDecision> ResolveAsync(string desiredPath, ConflictResolution mode, FileConflictInfo info, CancellationToken cancellationToken = default)
    {
        if (!info.LocalExists)
            return FileConflictDecision.UsePath(desiredPath, mode);

        if (mode == ConflictResolution.Overwrite)
            return FileConflictDecision.UsePath(desiredPath, ConflictResolution.Overwrite);

        if (mode == ConflictResolution.Skip)
            return FileConflictDecision.SkipOnce();

        if (mode == ConflictResolution.Rename)
            return FileConflictDecision.UsePath(GenerateUniqueFileName(desiredPath), ConflictResolution.Rename);

        var sessionName = _sessionManager.GetActiveSession()?.Name;
        var sessionDecision = GetSessionPreferenceDecision(sessionName, desiredPath);
        if (sessionDecision != null)
            return sessionDecision;

        string? localChecksum = null;
        if (_settings.Value.CompareChecksumOnConflict && info.LocalExists)
        {
            try
            {
                using var s = File.OpenRead(desiredPath);
                using var md5 = System.Security.Cryptography.MD5.Create();
                var hash = await md5.ComputeHashAsync(s, cancellationToken);
                localChecksum = Convert.ToHexString(hash);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to compute local checksum for {Path}", desiredPath);
            }
        }

        var fileName = Path.GetFileName(desiredPath);
        var result = TerminalConfirmation.ShowDetailedConflictResolutionPrompt(
            fileName: fileName,
            localSize: info.LocalSize,
            localModifiedUtc: info.LocalLastModifiedUtc,
            localChecksum: localChecksum,
            remoteSize: info.RemoteSize,
            remoteModifiedUtc: info.RemoteLastModifiedUtc,
            remoteChecksum: info.RemoteChecksumMd5,
            showSize: _settings.Value.CompareSizeOnConflict,
            showDate: _settings.Value.CompareLastModifiedOnConflict,
            showChecksum: _settings.Value.CompareChecksumOnConflict);

        if (!string.IsNullOrEmpty(sessionName) && result.RememberForSession)
        {
            _userSessionPreference[sessionName] = result.Decision;
        }

        return result.Decision switch
        {
            ConflictResolution.Overwrite => FileConflictDecision.UsePath(desiredPath, ConflictResolution.Overwrite, result.ApplyToAll, result.RememberForSession),
            ConflictResolution.Skip => FileConflictDecision.SkipOnce(),
            ConflictResolution.Rename => FileConflictDecision.UsePath(GenerateUniqueFileName(desiredPath), ConflictResolution.Rename, result.ApplyToAll, result.RememberForSession),
            _ => FileConflictDecision.SkipOnce()
        };
    }

    private FileConflictDecision? GetSessionPreferenceDecision(string? sessionName, string desiredPath)
    {
        if (!string.IsNullOrEmpty(sessionName) && _userSessionPreference.TryGetValue(sessionName, out var remembered))
        {
            return remembered switch
            {
                ConflictResolution.Overwrite => FileConflictDecision.UsePath(desiredPath, ConflictResolution.Overwrite),
                ConflictResolution.Skip => FileConflictDecision.SkipOnce(),
                ConflictResolution.Rename => FileConflictDecision.UsePath(GenerateUniqueFileName(desiredPath), ConflictResolution.Rename),
                _ => FileConflictDecision.SkipOnce()
            };
        }

        return null;
    }

    private static string GenerateUniqueFileName(string originalPath)
    {
        var directory = Path.GetDirectoryName(originalPath) ?? string.Empty;
        var baseName = Path.GetFileNameWithoutExtension(originalPath);
        var extension = Path.GetExtension(originalPath);

        var normalizedBase = System.Text.RegularExpressions.Regex.Replace(baseName, @" \((\d+)\)$", "");

        var counter = 1;
        string newPath;
        do
        {
            var newFileName = $"{normalizedBase} ({counter}){extension}";
            newPath = Path.Combine(directory, newFileName);
            counter++;
        } while (File.Exists(newPath));

        return newPath;
    }
}

