namespace UnitTests;

/// <summary>
///     Enables tests to create an instance of the ANSI driver configured for testing purposes.
/// </summary>
[Collection ("Global Test Setup")]
public abstract class FakeDriverBase
{
    /// <summary>
    ///     Creates a new ANSI driver instance with the specified buffer size.
    ///     This is a convenience method for tests that need to use Draw() and DriverAssert
    ///     without relying on Application.Driver.
    /// </summary>
    /// <param name="width">Width of the driver buffer</param>
    /// <param name="height">Height of the driver buffer</param>
    /// <returns>A configured IDriver instance</returns>
    protected static IDriver CreateFakeDriver (int width = 80, int height = 25)
    {
        var output = new AnsiOutput ();
        var factory = new AnsiComponentFactory (null, output, null);
        var parser = new AnsiResponseParser ();
        var scheduler = new AnsiRequestScheduler (parser);
        var sizeMonitor = factory.CreateSizeMonitor (output, new OutputBufferImpl ());

        DriverImpl driver = new (
                                 factory,
                                 new AnsiInputProcessor (null),
                                 new OutputBufferImpl (),
                                 output,
                                 scheduler,
                                 sizeMonitor);

        // Initialize the size monitor with the driver (generic pattern for all drivers)
        sizeMonitor.Initialize (driver);

        driver.SetScreenSize (width, height);

        return driver;
    }
}
