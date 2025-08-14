using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AzStore.Core.Models;

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
            return CreateInContainer(SessionName, StorageAccountName, ContainerName!, newPrefix);
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
    public bool CanNavigateUp()
    {
        return GetLevel() != NavigationLevel.StorageAccount;
    }

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
}

/// <summary>
/// Defines the navigation levels within the Azure Blob Storage hierarchy.
/// </summary>
public enum NavigationLevel
{
    /// <summary>
    /// At the storage account level, viewing containers.
    /// </summary>
    StorageAccount,

    /// <summary>
    /// At the container level, viewing blobs and virtual directories.
    /// </summary>
    Container,

    /// <summary>
    /// At a blob prefix level (virtual directory), viewing nested blobs and directories.
    /// </summary>
    BlobPrefix
}