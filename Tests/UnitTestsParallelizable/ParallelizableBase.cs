
using TerminalGuiFluentTesting;

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
    /// Creates a new FakeDriver instance with the specified buffer size.
    /// This is a convenience method for tests that need to use Draw() and DriverAssert
    /// without relying on Application.Driver.
    /// </summary>
    /// <param name="width">Width of the driver buffer</param>
    /// <param name="height">Height of the driver buffer</param>
    /// <returns>A configured IFakeConsoleDriver instance</returns>
    protected static IFakeConsoleDriver CreateFakeDriver (int width = 25, int height = 25)
    {
        var factory = new FakeDriverFactory ();
        IFakeConsoleDriver driver = factory.Create ();
        driver.SetBufferSize (width, height);
        return driver;
    }
}
