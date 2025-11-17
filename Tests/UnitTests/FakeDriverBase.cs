namespace UnitTests;

/// <summary>
///     Enables tests to create a FakeDriver for testing purposes.
/// </summary>
[Collection ("Global Test Setup")]
public abstract class FakeDriverBase : IDisposable
{
    /// <summary>
    ///     Creates a new FakeDriver instance with the specified buffer size.
    ///     This is a convenience method for tests that need to use Draw() and DriverAssert
    ///     without relying on Application.Driver.
    /// </summary>
    /// <param name="width">Width of the driver buffer</param>
    /// <param name="height">Height of the driver buffer</param>
    /// <returns>A configured IFakeDriver instance</returns>
    protected static IDriver CreateFakeDriver (int width = 80, int height = 25)
    {
        var output = new FakeOutput ();

        DriverImpl driver = new (
                                 new FakeInputProcessor (null),
                                 new OutputBufferImpl (),
                                 output,
                                 new AnsiRequestScheduler (new AnsiResponseParser ()),
                                 new SizeMonitorImpl (output));

        driver.SetScreenSize (width, height);

        return driver;
    }

    /// <inheritdoc />
    public void Dispose ()
    {
        Application.ResetState (true);
        GC.SuppressFinalize(this);
    }
}
