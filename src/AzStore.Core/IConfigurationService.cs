using AzStore.Configuration;

namespace AzStore.Core;

/// <summary>
/// Provides centralized access to application configuration settings.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets the current application settings.
    /// </summary>
    /// <returns>The current AzStoreSettings instance.</returns>
    AzStoreSettings GetSettings();

    /// <summary>
    /// Gets a specific configuration value by key.
    /// </summary>
    /// <typeparam name="T">The type of the configuration value.</typeparam>
    /// <param name="key">The configuration key to retrieve.</param>
    /// <param name="defaultValue">The default value to return if the key is not found.</param>
    /// <returns>The configuration value, or the default value if not found.</returns>
    T GetValue<T>(string key, T defaultValue = default!);

    /// <summary>
    /// Gets a specific configuration section.
    /// </summary>
    /// <typeparam name="T">The type to bind the configuration section to.</typeparam>
    /// <param name="sectionKey">The configuration section key.</param>
    /// <returns>The bound configuration section, or null if not found.</returns>
    T? GetSection<T>(string sectionKey) where T : class;

    /// <summary>
    /// Checks if a configuration key exists.
    /// </summary>
    /// <param name="key">The configuration key to check.</param>
    /// <returns>true if the key exists; otherwise, false.</returns>
    bool HasKey(string key);

    /// <summary>
    /// Gets all configuration keys that match the specified prefix.
    /// </summary>
    /// <param name="prefix">The prefix to match against configuration keys.</param>
    /// <returns>A collection of matching configuration keys.</returns>
    IEnumerable<string> GetKeysWithPrefix(string prefix);

    /// <summary>
    /// Gets the connection string for Azure Storage, if configured.
    /// </summary>
    /// <param name="name">The name of the connection string (default is "DefaultConnection").</param>
    /// <returns>The connection string, or null if not found.</returns>
    string? GetConnectionString(string name = "DefaultConnection");

    /// <summary>
    /// Gets logging configuration settings.
    /// </summary>
    /// <returns>The logging settings from configuration.</returns>
    LoggingSettings GetLoggingSettings();

    /// <summary>
    /// Gets theme configuration settings.
    /// </summary>
    /// <returns>The theme settings from configuration.</returns>
    ThemeSettings GetThemeSettings();

    /// <summary>
    /// Gets key bindings configuration settings.
    /// </summary>
    /// <returns>The key bindings settings from configuration.</returns>
    KeyBindings GetKeyBindings();

    /// <summary>
    /// Gets file conflict behavior configuration.
    /// </summary>
    /// <returns>The file conflict behavior setting.</returns>
    FileConflictBehavior GetFileConflictBehavior();

    /// <summary>
    /// Gets the configured local data directory path.
    /// </summary>
    /// <returns>The path to the local data directory.</returns>
    string GetDataDirectory();

    /// <summary>
    /// Gets the configured session storage directory path.
    /// </summary>
    /// <returns>The path to the session storage directory.</returns>
    string GetSessionDirectory();

    /// <summary>
    /// Gets the configured cache directory path.
    /// </summary>
    /// <returns>The path to the cache directory.</returns>
    string GetCacheDirectory();

    /// <summary>
    /// Reloads configuration from all sources.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task ReloadConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the current configuration settings.
    /// </summary>
    /// <returns>A collection of validation errors, or empty if valid.</returns>
    IEnumerable<string> ValidateConfiguration();

    /// <summary>
    /// Gets configuration values as a dictionary for debugging purposes.
    /// </summary>
    /// <param name="includeSecrets">Whether to include potentially sensitive configuration values.</param>
    /// <returns>A dictionary of configuration key-value pairs.</returns>
    IDictionary<string, string?> GetConfigurationDictionary(bool includeSecrets = false);
}