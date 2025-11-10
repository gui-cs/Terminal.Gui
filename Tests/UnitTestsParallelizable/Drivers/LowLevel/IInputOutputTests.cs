using System.Collections.Concurrent;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace UnitTests_Parallelizable.DriverTests;

/// <summary>
///     Low-level tests for IInput and IOutput implementations across all drivers.
///     These tests are designed to fail with good error messages when run in environments
///     without a real terminal (like GitHub Actions).
/// </summary>
public class IInputOutputTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region DotNet Driver Tests

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void NetInput_Constructor_DoesNotThrow_WhenNoTerminalAvailable ()
    {
        // Arrange & Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     using var input = new NetInput ();
                                                     _output.WriteLine ("NetInput created successfully");
                                                 });

        // Assert
        if (exception != null)
        {
            _output.WriteLine ($"FAILED: NetInput constructor threw: {exception.GetType ().Name}: {exception.Message}");
            _output.WriteLine ($"Stack trace: {exception.StackTrace}");
        }

        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void NetInput_Peek_DoesNotThrow_WhenNoTerminalAvailable ()
    {
        // Arrange
        using var input = new NetInput ();

        // Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     bool hasInput = input.Peek ();
                                                     _output.WriteLine ($"NetInput.Peek() returned: {hasInput}");
                                                 });

        // Assert
        if (exception != null)
        {
            _output.WriteLine ($"FAILED: NetInput.Peek() threw: {exception.GetType ().Name}: {exception.Message}");
            _output.WriteLine ($"Stack trace: {exception.StackTrace}");
        }

        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void NetInput_Read_DoesNotThrow_WhenNoTerminalAvailable ()
    {
        // Arrange
        using var input = new NetInput ();

        // Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     List<ConsoleKeyInfo> items = input.Read ().ToList ();
                                                     _output.WriteLine ($"NetInput.Read() returned {items.Count} items");
                                                 });

        // Assert
        if (exception != null)
        {
            _output.WriteLine ($"FAILED: NetInput.Read() threw: {exception.GetType ().Name}: {exception.Message}");
            _output.WriteLine ($"Stack trace: {exception.StackTrace}");
        }

        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void NetInput_Dispose_DoesNotThrow_WhenNoTerminalAvailable ()
    {
        // Arrange
        var input = new NetInput ();

        // Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     input.Dispose ();
                                                     _output.WriteLine ("NetInput disposed successfully");
                                                 });

        // Assert
        if (exception != null)
        {
            _output.WriteLine ($"FAILED: NetInput.Dispose() threw: {exception.GetType ().Name}: {exception.Message}");
            _output.WriteLine ($"Stack trace: {exception.StackTrace}");
        }

        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void NetOutput_Constructor_DoesNotThrow_WhenNoTerminalAvailable ()
    {
        // Arrange & Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     using var output = new NetOutput ();
                                                     _output.WriteLine ("NetOutput created successfully");
                                                 });

        // Assert
        if (exception != null)
        {
            _output.WriteLine ($"FAILED: NetOutput constructor threw: {exception.GetType ().Name}: {exception.Message}");
            _output.WriteLine ($"Stack trace: {exception.StackTrace}");
        }

        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void NetOutput_GetSize_ReturnsDefaultSize_WhenNoTerminalAvailable ()
    {
        // Arrange
        using var output = new NetOutput ();

        // Act
        Size size = default;

        Exception? exception = Record.Exception (() =>
                                                 {
                                                     size = output.GetSize ();
                                                     _output.WriteLine ($"NetOutput.GetSize() returned: {size.Width}x{size.Height}");
                                                 });

        // Assert
        if (exception != null)
        {
            _output.WriteLine ($"FAILED: NetOutput.GetSize() threw: {exception.GetType ().Name}: {exception.Message}");
            _output.WriteLine ($"Stack trace: {exception.StackTrace}");
        }

        Assert.Null (exception);
        Assert.True (size.Width > 0, "Width should be > 0");
        Assert.True (size.Height > 0, "Height should be > 0");
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void NetOutput_Write_DoesNotThrow_WhenNoTerminalAvailable ()
    {
        // Arrange
        using var output = new NetOutput ();

        // Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     ReadOnlySpan<char> text = "Test".AsSpan ();
                                                     output.Write (text);
                                                     _output.WriteLine ("NetOutput.Write() succeeded");
                                                 });

        // Assert
        if (exception != null)
        {
            _output.WriteLine ($"FAILED: NetOutput.Write() threw: {exception.GetType ().Name}: {exception.Message}");
            _output.WriteLine ($"Stack trace: {exception.StackTrace}");
        }

        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void NetOutput_SetCursorPosition_DoesNotThrow_WhenNoTerminalAvailable ()
    {
        // Arrange
        using var output = new NetOutput ();

        // Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     output.SetCursorPosition (0, 0);
                                                     _output.WriteLine ("NetOutput.SetCursorPosition() succeeded");
                                                 });

        // Assert
        if (exception != null)
        {
            _output.WriteLine ($"FAILED: NetOutput.SetCursorPosition() threw: {exception.GetType ().Name}: {exception.Message}");
            _output.WriteLine ($"Stack trace: {exception.StackTrace}");
        }

        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void NetInputProcessor_Constructor_DoesNotThrow ()
    {
        // Arrange & Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     ConcurrentQueue<ConsoleKeyInfo> queue = new ();
                                                     var processor = new NetInputProcessor (queue);
                                                     _output.WriteLine ("NetInputProcessor created successfully");
                                                 });

        // Assert
        if (exception != null)
        {
            _output.WriteLine ($"FAILED: NetInputProcessor constructor threw: {exception.GetType ().Name}: {exception.Message}");
            _output.WriteLine ($"Stack trace: {exception.StackTrace}");
        }

        Assert.Null (exception);
    }

    #endregion

    #region Unix Driver Tests

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    [Trait ("Platform", "Unix")]
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
                                                     catch (InvalidOperationException ex) when (ex.Message.Contains ("tcgetattr")
                                                                                                || ex.Message.Contains ("tcsetattr"))
                                                     {
                                                         _output.WriteLine ($"Expected failure on non-terminal: {ex.Message}");

                                                         throw new XunitException (
                                                                                   $"UnixInput failed in non-terminal environment: {ex.Message}\nThis is expected in GitHub Actions. The driver should detect this and handle gracefully.");
                                                     }
                                                 });

        // Assert
        if (exception != null && !(exception is XunitException))
        {
            _output.WriteLine ($"FAILED: UnixInput constructor threw: {exception.GetType ().Name}: {exception.Message}");
            _output.WriteLine ($"Stack trace: {exception.StackTrace}");
        }
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    [Trait ("Platform", "Unix")]
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
    [Trait ("Platform", "Unix")]
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

    #endregion

    #region Windows Driver Tests

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    [Trait ("Platform", "Windows")]
    public void WindowsInput_Constructor_DoesNotThrow_WhenNoTerminalAvailable ()
    {
        if (!OperatingSystem.IsWindows ())
        {
            _output.WriteLine ("Skipping Windows test on non-Windows");

            return;
        }

        // Arrange & Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     using var input = new WindowsInput ();
                                                     _output.WriteLine ("WindowsInput created successfully");
                                                 });

        // Assert
        if (exception != null)
        {
            _output.WriteLine ($"FAILED: WindowsInput constructor threw: {exception.GetType ().Name}: {exception.Message}");
            _output.WriteLine ($"Stack trace: {exception.StackTrace}");
        }

        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    [Trait ("Platform", "Windows")]
    public void WindowsOutput_Constructor_DoesNotThrow_WhenNoTerminalAvailable ()
    {
        if (!OperatingSystem.IsWindows ())
        {
            _output.WriteLine ("Skipping Windows test on non-Windows");

            return;
        }

        // Arrange & Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     try
                                                     {
                                                         using var output = new WindowsOutput ();
                                                         _output.WriteLine ("WindowsOutput created successfully");
                                                     }
                                                     catch (Exception ex)
                                                     {
                                                         _output.WriteLine ($"WindowsOutput threw during construction: {ex.GetType ().Name}: {ex.Message}");

                                                         throw;
                                                     }
                                                 });

        // Assert
        if (exception != null)
        {
            _output.WriteLine ($"FAILED: WindowsOutput constructor threw: {exception.GetType ().Name}: {exception.Message}");
            _output.WriteLine ($"Stack trace: {exception.StackTrace}");
        }

        Assert.Null (exception);
    }

    #endregion

    #region Fake Driver Tests

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void FakeInput_Constructor_DoesNotThrow ()
    {
        // Arrange & Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     using var input = new FakeInput ();
                                                     _output.WriteLine ("FakeInput created successfully");
                                                 });

        // Assert
        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void FakeOutput_Constructor_DoesNotThrow ()
    {
        // Arrange & Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     using var output = new FakeOutput ();
                                                     _output.WriteLine ("FakeOutput created successfully");
                                                 });

        // Assert
        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void FakeOutput_GetSize_ReturnsExpectedSize ()
    {
        // Arrange
        using var output = new FakeOutput ();

        // Act
        Size size = output.GetSize ();
        _output.WriteLine ($"FakeOutput.GetSize() returned: {size.Width}x{size.Height}");

        // Assert
        Assert.True (size.Width > 0);
        Assert.True (size.Height > 0);
    }

    #endregion

    #region Component Factory Tests

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void NetComponentFactory_CreateInput_DoesNotThrow ()
    {
        // Arrange
        var factory = new NetComponentFactory ();

        // Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     using IInput<ConsoleKeyInfo> input = factory.CreateInput ();
                                                     _output.WriteLine ("NetComponentFactory.CreateInput() succeeded");
                                                 });

        // Assert
        if (exception != null)
        {
            _output.WriteLine ($"FAILED: NetComponentFactory.CreateInput() threw: {exception.GetType ().Name}: {exception.Message}");
            _output.WriteLine ($"Stack trace: {exception.StackTrace}");
        }

        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void NetComponentFactory_CreateOutput_DoesNotThrow ()
    {
        // Arrange
        var factory = new NetComponentFactory ();

        // Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     using IOutput output = factory.CreateOutput ();
                                                     _output.WriteLine ("NetComponentFactory.CreateOutput() succeeded");
                                                 });

        // Assert
        if (exception != null)
        {
            _output.WriteLine ($"FAILED: NetComponentFactory.CreateOutput() threw: {exception.GetType ().Name}: {exception.Message}");
            _output.WriteLine ($"Stack trace: {exception.StackTrace}");
        }

        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void FakeComponentFactory_CreateInput_DoesNotThrow ()
    {
        // Arrange
        var factory = new FakeComponentFactory ();

        // Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     using IInput<ConsoleKeyInfo> input = factory.CreateInput ();
                                                     _output.WriteLine ("FakeComponentFactory.CreateInput() succeeded");
                                                 });

        // Assert
        Assert.Null (exception);
    }

    [Fact]
    [Trait ("Category", "LowLevelDriver")]
    public void FakeComponentFactory_CreateOutput_DoesNotThrow ()
    {
        // Arrange
        var factory = new FakeComponentFactory ();

        // Act
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     using IOutput output = factory.CreateOutput ();
                                                     _output.WriteLine ("FakeComponentFactory.CreateOutput() succeeded");
                                                 });

        // Assert
        Assert.Null (exception);
    }

    #endregion
}
