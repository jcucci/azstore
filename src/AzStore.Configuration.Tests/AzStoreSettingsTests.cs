using Xunit;

namespace AzStore.Configuration.Tests;

public class AzStoreSettingsTests
{
    [Fact]
    public void AzStoreSettings_DefaultsSet()
    {
        var settings = AzStoreSettingsFixture.CreateDefault();
        
        AzStoreSettingsAssertions.DefaultsSet(settings);
    }

    [Fact]
    public void AzStoreSettings_SectionNameCorrect()
    {
        AzStoreSettingsAssertions.ExpectedSectionName();
    }

    [Fact]
    public void AzStoreSettings_ValidPathReturnsTrue()
    {
        var settings = AzStoreSettingsFixture.CreateWithValidPath();
        
        Assert.True(settings.IsValid());
    }

    [Fact]
    public void AzStoreSettings_InvalidPathReturnsFalse()
    {
        var settings = AzStoreSettingsFixture.CreateWithInvalidPath();
        
        Assert.False(settings.IsValid());
    }
}