// Copilot
#nullable enable

namespace UnitTests.NonParallelizable.ApplicationTests;

/// <summary>
///     Tests for the <see cref="SynchronizationContext"/> that is set during the Terminal.Gui application lifecycle.
///     Must run non-concurrently because <see cref="IApplication.Init"/> and <see cref="IDisposable.Dispose"/> mutate
///     the process-wide <see cref="SynchronizationContext.Current"/>.
/// </summary>
public class SynchronizationContextTests
{
    [Fact]
    public void Init_SetsSynchronizationContext_Dispose_ClearsIt ()
    {
        IApplication app = Application.Create ();

        try
        {
            app.Init (DriverRegistry.Names.ANSI);

            Assert.NotNull (SynchronizationContext.Current);
        }
        finally
        {
            app.Dispose ();
        }

        Assert.Null (SynchronizationContext.Current);
    }

    [Fact]
    public void Init_SynchronizationContext_CreateCopy_ReturnsDifferentInstance ()
    {
        IApplication app = Application.Create ();

        try
        {
            app.Init (DriverRegistry.Names.ANSI);

            SynchronizationContext context = SynchronizationContext.Current!;
            SynchronizationContext copy = context.CreateCopy ();

            Assert.NotNull (copy);
            Assert.NotSame (context, copy);
        }
        finally
        {
            app.Dispose ();
        }
    }
}
