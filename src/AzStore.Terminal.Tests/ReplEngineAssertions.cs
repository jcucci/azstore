using AzStore.Terminal;
using AzStore.Terminal.Repl;
using Xunit;

namespace AzStore.Terminal.Tests;

public static class ReplEngineAssertions
{
    public static void InstanceCreated(ReplEngine engine)
    {
        Assert.NotNull(engine);
    }
}