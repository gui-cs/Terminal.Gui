namespace UnitTests.Parallelizable;

/// <summary>
///     Base class for parallelizable tests. Ensures that tests can run in parallel without interference
///     by setting various Terminal.Gui static properties to their default values. E.g. View.EnableDebugIDisposableAsserts.
/// </summary>
[Collection ("Global Test Setup")]
public abstract class ParallelizableBase
{
    // Common setup or utilities for all tests can go here

    /// <summary>
    ///     Creates a new FakeDriver instance with the specified buffer size.
    ///     This is a convenience method for tests that need to use Draw() and DriverAssert
    ///     without relying on Application.Driver.
    /// </summary>
    /// <param name="width">Width of the driver buffer</param>
    /// <param name="height">Height of the driver buffer</param>
    /// <returns>A configured IFakeConsoleDriver instance</returns>
    protected static IConsoleDriver CreateFakeDriver (int width = 80, int height = 25)
    {
        var output = new FakeConsoleOutput ();

        ConsoleDriverFacade<ConsoleKeyInfo> facade = new (
                                                          new NetInputProcessor (null),
                                                          new OutputBufferImpl (),
                                                          output,
                                                          new AnsiRequestScheduler(new AnsiResponseParser()),
                                                          new ConsoleSizeMonitorImpl (output));

        facade.SetScreenSize (width, height);

        return facade;
    }
}
