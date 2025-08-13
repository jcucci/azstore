using AzStore.Configuration;
using Xunit;

namespace AzStore.Configuration.Tests;

public static class AzStoreSettingsAssertions
{
    public static void DefaultsSet(AzStoreSettings settings)
    {
        Assert.NotNull(settings.SessionsDirectory);
        Assert.NotEmpty(settings.SessionsDirectory);
        Assert.Equal(FileConflictBehavior.Overwrite, settings.OnFileConflict);
        Assert.NotNull(settings.KeyBindings);
        Assert.NotNull(settings.Theme);
        Assert.NotNull(settings.Aliases);
        Assert.NotNull(settings.Logging);
    }

    public static void ExpectedSectionName()
    {
        Assert.Equal("AzStore", AzStoreSettings.SectionName);
    }
}