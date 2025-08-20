using Xunit;

namespace AzStore.Terminal.Tests;

[Trait("Category", "Unit")]
public class ReplEngineTests
{
    [Fact]
    public void ReplEngine_CanBeInstantiated()
    {
        var engine = ReplEngineFixture.CreateWithDefaults();
        
        ReplEngineAssertions.InstanceCreated(engine);
    }

    [Fact]
    public void ReplEngine_AcceptsCustomTheme()
    {
        var engine = ReplEngineFixture.CreateWithCustomTheme("Yellow");
        
        ReplEngineAssertions.InstanceCreated(engine);
    }
}