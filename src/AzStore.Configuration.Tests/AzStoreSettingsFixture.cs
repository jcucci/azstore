using AzStore.Configuration;

namespace AzStore.Configuration.Tests;

public static class AzStoreSettingsFixture
{
    public static AzStoreSettings CreateDefault() => new();

    public static AzStoreSettings CreateWithValidPath() => new()
    {
        SessionsDirectory = "/tmp/azstore-test",
        DefaultStorageAccount = "teststorage",
        OnFileConflict = FileConflictBehavior.Skip
    };

    public static AzStoreSettings CreateWithInvalidPath() => new()
    {
        SessionsDirectory = ""
    };
}