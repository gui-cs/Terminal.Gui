// Copilot
#nullable enable

namespace UnitTests.NonParallelizable.ApplicationTests;

/// <summary>
///     Tests for the <see cref="SynchronizationContext"/> that is set during the Terminal.Gui application lifecycle.
///     Must run non-concurrently because <see cref="Application.Init"/> and <see cref="Application.Shutdown"/> mutate
///     the process-wide <see cref="SynchronizationContext.Current"/>.
/// </summary>
public class SynchronizationContextTests
{
    [Fact]
    public void Init_SetsSynchronizationContext_Shutdown_ClearsIt ()
    {
        Application.Init (DriverRegistry.Names.ANSI);

        Assert.NotNull (SynchronizationContext.Current);

        Application.Shutdown ();

        Assert.Null (SynchronizationContext.Current);
    }

    [Fact]
    public void Init_SynchronizationContext_CreateCopy_ReturnsDifferentInstance ()
    {
        Application.Init (DriverRegistry.Names.ANSI);

        SynchronizationContext context = SynchronizationContext.Current!;
        SynchronizationContext copy = context.CreateCopy ();

        Assert.NotNull (copy);
        Assert.NotSame (context, copy);

        Application.Shutdown ();
    }
}
