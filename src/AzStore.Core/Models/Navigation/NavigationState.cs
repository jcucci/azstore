using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AzStore.Core.Models.Navigation;

/// <summary>
/// Represents the current navigation context within the Azure Blob Storage REPL.
/// </summary>
/// <param name="SessionName">The name of the current active session.</param>
/// <param name="StorageAccountName">The Azure Storage account currently being browsed.</param>
/// <param name="ContainerName">The current container being browsed, if any.</param>
/// <param name="BlobPrefix">The current blob prefix (virtual directory path) being browsed, if any.</param>
/// <param name="BreadcrumbPath">The hierarchical path for display purposes.</param>
/// <param name="SelectedIndex">The currently selected item index in the current view.</param>
public record NavigationState(
    [property: Required, MinLength(1)] string SessionName,
    [property: Required, MinLength(1)] string StorageAccountName,
    [property: JsonPropertyName("containerName")] string? ContainerName,
    [property: JsonPropertyName("blobPrefix")] string? BlobPrefix,
    [property: JsonPropertyName("breadcrumbPath")] string BreadcrumbPath,
    [property: JsonPropertyName("selectedIndex")] int SelectedIndex)
{
    /// <summary>
    /// Creates a new navigation state at the storage account root level.
    /// </summary>
    /// <param name="sessionName">The name of the active session.</param>
    /// <param name="storageAccountName">The Azure Storage account name.</param>
    /// <returns>A new NavigationState at the account root level.</returns>
    public static NavigationState CreateAtRoot(string sessionName, string storageAccountName)
    {
        return new NavigationState(
            sessionName,
            storageAccountName,
            ContainerName: null,
            BlobPrefix: null,
            BreadcrumbPath: storageAccountName,
            SelectedIndex: 0);
    }

    /// <summary>
    /// Creates a new navigation state within a specific container.
    /// </summary>
    /// <param name="sessionName">The name of the active session.</param>
    /// <param name="storageAccountName">The Azure Storage account name.</param>
    /// <param name="containerName">The container name.</param>
    /// <param name="blobPrefix">Optional blob prefix (virtual directory).</param>
    /// <returns>A new NavigationState within the specified container.</returns>
    public static NavigationState CreateInContainer(string sessionName, string storageAccountName, string containerName, string? blobPrefix = null)
    {
        var breadcrumb = string.IsNullOrEmpty(blobPrefix)
            ? $"{storageAccountName}/{containerName}"
            : $"{storageAccountName}/{containerName}/{blobPrefix}";

        return new NavigationState(
            sessionName,
            storageAccountName,
            ContainerName: containerName,
            BlobPrefix: blobPrefix,
            BreadcrumbPath: breadcrumb,
            SelectedIndex: 0);
    }

    /// <summary>
    /// Creates a copy of this navigation state with an updated selected index.
    /// </summary>
    /// <param name="newIndex">The new selected index.</param>
    /// <returns>A new NavigationState with the updated selected index.</returns>
    public NavigationState WithSelectedIndex(int newIndex)
    {
        return this with { SelectedIndex = Math.Max(0, newIndex) };
    }

    /// <summary>
    /// Creates a copy of this navigation state navigated to a deeper level.
    /// </summary>
    /// <param name="containerName">The container to navigate into (if currently at root).</param>
    /// <param name="blobPrefix">The blob prefix to navigate into (if currently in a container).</param>
    /// <returns>A new NavigationState at the deeper level.</returns>
    public NavigationState NavigateInto(string? containerName = null, string? blobPrefix = null)
    {
        if (ContainerName == null && !string.IsNullOrEmpty(containerName))
        {
            return CreateInContainer(SessionName, StorageAccountName, containerName);
        }

        if (ContainerName != null && !string.IsNullOrEmpty(blobPrefix))
        {
            var newPrefix = string.IsNullOrEmpty(BlobPrefix)
                ? blobPrefix
                : $"{BlobPrefix}/{blobPrefix}";

            return CreateInContainer(SessionName, StorageAccountName, ContainerName, newPrefix);
        }

        return this;
    }

    /// <summary>
    /// Creates a copy of this navigation state navigated to the parent level.
    /// </summary>
    /// <returns>A new NavigationState at the parent level, or the current state if already at root.</returns>
    public NavigationState NavigateUp()
    {
        if (!string.IsNullOrEmpty(BlobPrefix))
        {
            var lastSlash = BlobPrefix.LastIndexOf('/');
            var newPrefix = lastSlash > 0 ? BlobPrefix[..lastSlash] : null;
            return ContainerName is not null
                ? CreateInContainer(SessionName, StorageAccountName, ContainerName, newPrefix)
                : this;
        }

        if (ContainerName != null)
        {
            return CreateAtRoot(SessionName, StorageAccountName);
        }

        return this;
    }

    /// <summary>
    /// Gets the current navigation level.
    /// </summary>
    /// <returns>The current navigation level.</returns>
    public NavigationLevel GetLevel()
    {
        if (ContainerName == null)
            return NavigationLevel.StorageAccount;

        if (string.IsNullOrEmpty(BlobPrefix))
            return NavigationLevel.Container;

        return NavigationLevel.BlobPrefix;
    }

    /// <summary>
    /// Gets a value indicating whether navigation up is possible from the current level.
    /// </summary>
    /// <returns>true if navigation up is possible; otherwise, false.</returns>
    public bool CanNavigateUp() => GetLevel() != NavigationLevel.StorageAccount;

    /// <summary>
    /// Returns a string representation of the current navigation state.
    /// </summary>
    /// <returns>A formatted string containing the breadcrumb path and level information.</returns>
    public override string ToString()
    {
        var level = GetLevel() switch
        {
            NavigationLevel.StorageAccount => "Account",
            NavigationLevel.Container => "Container",
            NavigationLevel.BlobPrefix => "Folder",
            _ => "Unknown"
        };

        return $"{level}: {BreadcrumbPath} (item {SelectedIndex})";
    }

    /// <summary>
    /// Creates a copy of this navigation state navigated to a specific blob path within the current container.
    /// </summary>
    /// <param name="blobPath">The full blob path to navigate to.</param>
    /// <returns>A new NavigationState for the directory containing the specified blob.</returns>
    public NavigationState NavigateToBlobPath(string blobPath)
    {
        if (ContainerName == null)
            return this;

        var lastSlash = blobPath.LastIndexOf('/');
        if (lastSlash <= 0)
        {
            return CreateInContainer(SessionName, StorageAccountName, ContainerName);
        }

        var directoryPrefix = blobPath[..lastSlash];
        return CreateInContainer(SessionName, StorageAccountName, ContainerName, directoryPrefix);
    }

    /// <summary>
    /// Gets the parent navigation state for back navigation.
    /// </summary>
    /// <returns>The parent navigation state, or null if already at the root level.</returns>
    public NavigationState? GetParentState()
    {
        return CanNavigateUp() ? NavigateUp() : null;
    }

    /// <summary>
    /// Gets all path segments from the storage account to the current location.
    /// </summary>
    /// <returns>An array of path segments for breadcrumb navigation.</returns>
    public string[] GetPathSegments()
    {
        var segments = new List<string> { StorageAccountName };

        if (ContainerName != null)
        {
            segments.Add(ContainerName);

            if (!string.IsNullOrEmpty(BlobPrefix))
            {
                var prefixSegments = BlobPrefix.TrimEnd('/').Split('/');
                segments.AddRange(prefixSegments);
            }
        }

        return [.. segments];
    }

    /// <summary>
    /// Creates a navigation state for a specific path segment index.
    /// Used for breadcrumb navigation to jump to any parent level.
    /// </summary>
    /// <param name="segmentIndex">The index of the segment to navigate to (0 = storage account, 1 = container, 2+ = blob prefixes).</param>
    /// <returns>A new NavigationState at the specified segment level.</returns>
    public NavigationState NavigateToSegment(int segmentIndex)
    {
        var segments = GetPathSegments();
        if (segmentIndex < 0 || segmentIndex >= segments.Length)
            return this;

        if (segmentIndex == 0)
        {
            return CreateAtRoot(SessionName, StorageAccountName);
        }

        if (segmentIndex == 1 && segments.Length > 1)
        {
            return CreateInContainer(SessionName, StorageAccountName, segments[1]);
        }

        if (segmentIndex > 1 && segments.Length > 2)
        {
            var containerName = segments[1];
            var prefixSegments = segments.Skip(2).Take(segmentIndex - 1).ToArray();
            var prefix = string.Join("/", prefixSegments);
            return CreateInContainer(SessionName, StorageAccountName, containerName, prefix);
        }

        return this;
    }

    /// <summary>
    /// Gets the current path as a unified string suitable for display or persistence.
    /// </summary>
    /// <returns>A forward-slash separated path string.</returns>
    public string GetCurrentPath()
    {
        var segments = GetPathSegments();
        return string.Join("/", segments);
    }

    /// <summary>
    /// Determines if the current state represents a deeper level than the specified state.
    /// </summary>
    /// <param name="other">The other navigation state to compare against.</param>
    /// <returns>true if this state is deeper than the other state.</returns>
    public bool IsDeeperThan(NavigationState other)
    {
        var thisSegments = GetPathSegments();
        var otherSegments = other.GetPathSegments();
        return thisSegments.Length > otherSegments.Length;
    }

    /// <summary>
    /// Determines if the current state represents the same location as another state.
    /// </summary>
    /// <param name="other">The other navigation state to compare against.</param>
    /// <returns>true if both states represent the same location.</returns>
    public bool IsSameLocationAs(NavigationState other)
    {
        return StorageAccountName == other.StorageAccountName &&
               ContainerName == other.ContainerName &&
               BlobPrefix == other.BlobPrefix;
    }

    /// <summary>
    /// Creates a copy of this navigation state with search context added.
    /// </summary>
    /// <param name="searchQuery">The search query being performed.</param>
    /// <returns>A new NavigationState representing the search context.</returns>
    public NavigationState WithSearchContext(string searchQuery)
    {
        var searchBreadcrumb = string.IsNullOrEmpty(BlobPrefix)
            ? $"{StorageAccountName}/{ContainerName} (search: {searchQuery})"
            : $"{StorageAccountName}/{ContainerName}/{BlobPrefix} (search: {searchQuery})";

        return this with { BreadcrumbPath = searchBreadcrumb };
    }
}
