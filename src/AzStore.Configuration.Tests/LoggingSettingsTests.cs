using Xunit;

namespace AzStore.Configuration.Tests;

public class LoggingSettingsTests
{
    [Fact]
    public void LoggingSettings_HasCorrectDefaults()
    {
        var settings = LoggingSettingsFixture.CreateDefault();
        
        LoggingSettingsAssertions.ShouldHaveDefaultValues(settings);
    }

    [Fact]
    public void LoggingSettings_CanSetCustomValues()
    {
        var settings = LoggingSettingsFixture.CreateWithCustomValues();
        
        LoggingSettingsAssertions.ShouldHaveCustomValues(settings);
    }
}