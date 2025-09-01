using AzStore.Core.Models.Downloads;
using AzStore.Core.Models.Storage;
using AzStore.Terminal.Utilities;
using System.Text;

namespace AzStore.Terminal.UI;

/// <summary>
/// Renders download progress in the terminal using inline progress bars and status text.
/// </summary>
public class TerminalProgressRenderer
{
    private const int DefaultProgressBarWidth = 40;
    private const char FilledChar = '=';
    private const char UnfilledChar = '-';
    private const char ProgressChar = '>';
    
    /// <summary>
    /// Renders a blob download progress display suitable for terminal output.
    /// </summary>
    /// <param name="progress">The blob download progress information.</param>
    /// <param name="progressBarWidth">Width of the progress bar in characters.</param>
    /// <returns>A formatted string representing the download progress.</returns>
    public static string RenderBlobDownloadProgress(BlobDownloadProgress progress, int progressBarWidth = DefaultProgressBarWidth)
    {
        var progressBar = CreateProgressBar(progress.ProgressPercentage, progressBarWidth);
        var speed = TerminalUtils.FormatBytes(progress.BytesPerSecond);
        var downloaded = TerminalUtils.FormatBytes(progress.DownloadedBytes);
        var total = TerminalUtils.FormatBytes(progress.TotalBytes);
        var eta = progress.EstimatedTimeRemainingSeconds.HasValue
            ? TimeSpan.FromSeconds(progress.EstimatedTimeRemainingSeconds.Value).ToString(@"mm\:ss")
            : "--:--";

        var retryText = progress.RetryAttempt > 0 ? $" (retry {progress.RetryAttempt})" : "";
        var stageText = progress.Stage != DownloadStage.Downloading ? $" [{progress.Stage}]" : "";

        return $"{progressBar} {progress.ProgressPercentage:F1}% {downloaded}/{total} {speed}/s ETA: {eta}{retryText}{stageText}";
    }

    /// <summary>
    /// Renders an overall download progress for multiple files.
    /// </summary>
    /// <param name="progress">The overall download progress information.</param>
    /// <param name="progressBarWidth">Width of the progress bar in characters.</param>
    /// <returns>A formatted string representing the overall progress.</returns>
    public static string RenderOverallDownloadProgress(DownloadProgress progress, int progressBarWidth = DefaultProgressBarWidth)
    {
        var percentage = progress.TotalBlobs > 0
            ? (double)progress.CompletedBlobs / progress.TotalBlobs * 100
            : 0;

        var progressBar = CreateProgressBar(percentage, progressBarWidth);
        var totalBytes = TerminalUtils.FormatBytes(progress.TotalBytesDownloaded);
        var currentFile = string.IsNullOrEmpty(progress.CurrentBlobName) 
            ? "Preparing..." 
            : Path.GetFileName(progress.CurrentBlobName);

        return $"{progressBar} {percentage:F1}% ({progress.CompletedBlobs}/{progress.TotalBlobs} files) {totalBytes} - {currentFile}";
    }

    /// <summary>
    /// Creates a simple progress display for operations without detailed metrics.
    /// </summary>
    /// <param name="message">The operation message to display.</param>
    /// <param name="percentage">Optional percentage complete (0-100).</param>
    /// <param name="progressBarWidth">Width of the progress bar in characters.</param>
    /// <returns>A formatted string representing the simple progress.</returns>
    public static string RenderSimpleProgress(string message, double? percentage = null, int progressBarWidth = DefaultProgressBarWidth)
    {
        if (percentage.HasValue)
        {
            var progressBar = CreateProgressBar(percentage.Value, progressBarWidth);
            return $"{progressBar} {percentage:F1}% {message}";
        }
        else
        {
            // Spinning animation for indeterminate progress
            var spinner = GetSpinnerChar(DateTime.Now.Millisecond);
            return $"[{spinner}] {message}";
        }
    }

    /// <summary>
    /// Renders a confirmation prompt in the terminal format.
    /// </summary>
    /// <param name="message">The confirmation message.</param>
    /// <param name="defaultChoice">The default choice (Y or N).</param>
    /// <returns>A formatted confirmation prompt string.</returns>
    public static string RenderConfirmationPrompt(string message, char defaultChoice = 'Y')
    {
        var prompt = defaultChoice == 'Y' ? "[Y/n]" : "[y/N]";
        return $"{message} {prompt}: ";
    }

    /// <summary>
    /// Renders item details in a formatted display.
    /// </summary>
    /// <param name="item">The storage item to display details for.</param>
    /// <returns>A multi-line string containing item details.</returns>
    public static string RenderItemDetails(StorageItem item)
    {
        var sb = new StringBuilder();
        
        // Item type icon and name
        var icon = item switch
        {
            Container => "📁",
            Blob b => b.BlobType switch
            {
                BlobType.BlockBlob => "📄",
                BlobType.PageBlob => "📋",
                BlobType.AppendBlob => "📝",
                _ => "📄"
            },
            _ => "❓"
        };

        sb.AppendLine($"{icon} {item.Name}");
        
        // Size information
        if (item is Blob blob && blob.Size.HasValue)
        {
            sb.AppendLine($"Size: {TerminalUtils.FormatBytes(blob.Size.Value)}");
            sb.AppendLine($"Type: {blob.BlobType} Blob");
        }
        else if (item is Container)
        {
            sb.AppendLine("Type: Container");
        }

        // Last modified
        if (item.LastModified.HasValue)
        {
            sb.AppendLine($"Modified: {item.LastModified.Value:yyyy-MM-dd HH:mm:ss} UTC");
        }

        // Path
        if (!string.IsNullOrEmpty(item.Path))
        {
            sb.AppendLine($"Path: {item.Path}");
        }

        // Additional blob properties
        if (item is Blob blobItem)
        {
            if (!string.IsNullOrEmpty(blobItem.ContentType))
            {
                sb.AppendLine($"Content Type: {blobItem.ContentType}");
            }
            if (!string.IsNullOrEmpty(blobItem.ETag))
            {
                sb.AppendLine($"ETag: {blobItem.ETag}");
            }
            if (!string.IsNullOrEmpty(blobItem.ContentHash))
            {
                sb.AppendLine($"MD5: {blobItem.ContentHash}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Press any key to continue...");

        return sb.ToString();
    }

    /// <summary>
    /// Creates a progress bar string.
    /// </summary>
    /// <param name="percentage">Progress percentage (0-100).</param>
    /// <param name="width">Width of the progress bar in characters.</param>
    /// <returns>A formatted progress bar string.</returns>
    private static string CreateProgressBar(double percentage, int width)
    {
        var filled = (int)(percentage / 100 * width);
        var empty = width - filled;

        // Add progress character at the end of filled portion
        if (filled > 0 && filled < width)
        {
            filled--;
            empty = width - filled - 1; // Recalculate empty to account for ProgressChar
            return $"[{new string(FilledChar, filled)}{ProgressChar}{new string(UnfilledChar, empty)}]";
        }
        
        return $"[{new string(FilledChar, filled)}{new string(UnfilledChar, empty)}]";
    }


    /// <summary>
    /// Gets a spinning character for indeterminate progress.
    /// </summary>
    /// <param name="timeMs">Current time in milliseconds for animation.</param>
    /// <returns>A character representing the current spinner state.</returns>
    private static char GetSpinnerChar(int timeMs)
    {
        var spinnerChars = new[] { '|', '/', '-', '\\' };
        var index = (timeMs / 250) % spinnerChars.Length;
        return spinnerChars[index];
    }
}