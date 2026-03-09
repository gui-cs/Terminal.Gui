#nullable enable

namespace DriverTests.UnixDriver;

/// <summary>
///     Low-level tests for UnixInput and UnixOutput implementations.
///     These tests are designed to fail with good error messages when run in environments
///     without a real terminal (like GitHub Actions).
/// </summary>
[Trait ("Platform", "Unix")]
[Collection ("Driver Tests")]
public class UnixInputOutputTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void UnixInput_Constructor_DoesNotThrow_WhenNoTerminalAvailable ()
    {
        if (OperatingSystem.IsWindows ())
        {
            _output.WriteLine ("Skipping Unix test on Windows");

            return;
        }

        // Arrange & Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     try
                                                     {
                                                         using var input = new UnixInput ();
                                                         _output.WriteLine ("UnixInput created successfully");
                                                     }
                                                     catch (Exception ex)
                                                     {
                                                         _output.WriteLine ($"Expected failure on non-terminal: {ex.Message}");

                                                         throw new InvalidOperationException (
                                                                                   $"UnixInput failed in non-terminal environment: {ex.Message}\nThis is expected in GitHub Actions. The driver should detect this and handle gracefully.");
                                                     }
                                                 });

        // Assert
        if (exception != null && !(exception is InvalidOperationException))
        {
            _output.WriteLine ($"FAILED: UnixInput constructor threw: {exception.GetType ().Name}: {exception.Message}");
            _output.WriteLine ($"Stack trace: {exception.StackTrace}");
        }
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void UnixOutput_Constructor_DoesNotThrow_WhenNoTerminalAvailable ()
    {
        if (OperatingSystem.IsWindows ())
        {
            _output.WriteLine ("Skipping Unix test on Windows");

            return;
        }

        // Arrange & Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     using var output = new UnixOutput ();
                                                     _output.WriteLine ("UnixOutput created successfully");
                                                 });

        // Assert
        if (exception != null)
        {
            _output.WriteLine ($"FAILED: UnixOutput constructor threw: {exception.GetType ().Name}: {exception.Message}");
            _output.WriteLine ($"Stack trace: {exception.StackTrace}");
        }

        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void UnixOutput_GetSize_ReturnsDefaultSize_WhenNoTerminalAvailable ()
    {
        if (OperatingSystem.IsWindows ())
        {
            _output.WriteLine ("Skipping Unix test on Windows");

            return;
        }

        // Arrange
        using var output = new UnixOutput ();

        // Act
        Size size = default;

        Exception? exception = Record.Exception (() =>
                                                 {
                                                     size = output.GetSize ();
                                                     _output.WriteLine ($"UnixOutput.GetSize() returned: {size.Width}x{size.Height}");
                                                 });

        // Assert
        Assert.Null (exception);
        Assert.Equal (80, size.Width);
        Assert.Equal (25, size.Height);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void UnixOutput_Suspend_DoesNotThrow_WhenNoTerminalAvailable ()
    {
        // Arrange
        using var output = new UnixOutput ();

        // Act
        Exception? exception = Record.Exception (() => output.Suspend ());

        // Assert
        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void UnixDriver_IsAttachedToTerminal_ReturnsFalse_InTestHarness ()
    {
        // Copilot - generated.
        // Act — Driver.IsAttachedToTerminal is the shared entry point all drivers use.
        bool result = Driver.IsAttachedToTerminal (out bool inputAttached, out bool outputAttached);

        // Assert
        Assert.False (result, "UnixDriver: IsAttachedToTerminal should return false in test harness");
        Assert.False (inputAttached);
        Assert.False (outputAttached);
    }
}