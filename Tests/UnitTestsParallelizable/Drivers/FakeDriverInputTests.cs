using Xunit;
using Xunit.Abstractions;

namespace UnitTests_Parallelizable.Drivers;

/// <summary>
/// Tests for FakeDriver mouse and keyboard input functionality.
/// These tests prove that FakeDriver can be used for testing input handling in Terminal.Gui applications.
/// </summary>
public class FakeDriverInputTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region Keyboard Input Tests

    [Fact]
    public void FakeDriver_Can_Push_Mock_KeyPress ()
    {
        // Arrange
        var driver = new FakeDriver ();
        driver.Init ();

        // Act - Push a mock key press onto the FakeConsole
        FakeConsole.PushMockKeyPress (KeyCode.A);

        // Assert - Stack should have the key
        Assert.True (FakeConsole.MockKeyPresses.Count > 0);

        driver.End ();
    }

    [Fact]
    public void FakeDriver_MockKeyPresses_Stack_Works ()
    {
        // Arrange
        var driver = new FakeDriver ();
        driver.Init ();

        // Clear any previous state from other tests
        FakeConsole.MockKeyPresses.Clear ();

        // Act - Push multiple keys
        FakeConsole.PushMockKeyPress (KeyCode.A);
        FakeConsole.PushMockKeyPress (KeyCode.B);
        FakeConsole.PushMockKeyPress (KeyCode.C);

        // Assert
        Assert.Equal (3, FakeConsole.MockKeyPresses.Count);

        // Pop and verify order (stack is LIFO)
        var key1 = FakeConsole.MockKeyPresses.Pop ();
        var key2 = FakeConsole.MockKeyPresses.Pop ();
        var key3 = FakeConsole.MockKeyPresses.Pop ();

        Assert.Equal ('C', key1.KeyChar);
        Assert.Equal ('B', key2.KeyChar);
        Assert.Equal ('A', key3.KeyChar);

        driver.End ();
    }

    [Fact]
    public void FakeDriver_Can_Clear_MockKeyPresses ()
    {
        // Arrange
        var driver = new FakeDriver ();
        driver.Init ();

        FakeConsole.PushMockKeyPress (KeyCode.A);
        FakeConsole.PushMockKeyPress (KeyCode.B);

        // Act
        FakeConsole.MockKeyPresses.Clear ();

        // Assert
        Assert.Empty (FakeConsole.MockKeyPresses);

        driver.End ();
    }

    [Fact]
    public void FakeDriver_Supports_Special_Keys ()
    {
        // Arrange
        var driver = new FakeDriver ();
        driver.Init ();

        // Act - Push special keys
        FakeConsole.PushMockKeyPress (KeyCode.Enter);
        FakeConsole.PushMockKeyPress (KeyCode.Esc);
        FakeConsole.PushMockKeyPress (KeyCode.Tab);
        FakeConsole.PushMockKeyPress (KeyCode.CursorUp);

        // Assert
        Assert.Equal (4, FakeConsole.MockKeyPresses.Count);

        driver.End ();
    }

    [Fact]
    public void FakeDriver_Supports_Modified_Keys ()
    {
        // Arrange
        var driver = new FakeDriver ();
        driver.Init ();

        // Act - Push modified keys
        FakeConsole.PushMockKeyPress (KeyCode.A | KeyCode.CtrlMask);
        FakeConsole.PushMockKeyPress (KeyCode.S | KeyCode.ShiftMask);
        FakeConsole.PushMockKeyPress (KeyCode.F1 | KeyCode.AltMask);

        // Assert
        Assert.Equal (3, FakeConsole.MockKeyPresses.Count);

        var key1 = FakeConsole.MockKeyPresses.Pop ();
        Assert.True (key1.Modifiers.HasFlag (ConsoleModifiers.Alt));

        driver.End ();
    }

    #endregion

    #region FakeConsole Tests

    [Fact]
    public void FakeConsole_Has_Default_Dimensions ()
    {
        // Arrange
        var driver = new FakeDriver ();
        driver.Init ();

        // Assert
        Assert.Equal (80, FakeConsole.WindowWidth);
        Assert.Equal (25, FakeConsole.WindowHeight);
        Assert.Equal (80, FakeConsole.BufferWidth);
        Assert.Equal (25, FakeConsole.BufferHeight);

        driver.End ();
    }

    [Fact]
    public void FakeConsole_Can_Set_Buffer_Size ()
    {
        // Arrange
        var driver = new FakeDriver ();
        driver.Init ();

        // Act
        FakeConsole.SetBufferSize (100, 40);

        // Assert
        Assert.Equal (100, FakeConsole.BufferWidth);
        Assert.Equal (40, FakeConsole.BufferHeight);

        driver.End ();
    }

    [Fact]
    public void FakeConsole_Can_Set_Cursor_Position ()
    {
        // Arrange
        var driver = new FakeDriver ();
        driver.Init ();

        // Act
        FakeConsole.SetCursorPosition (15, 10);

        // Assert
        Assert.Equal (15, FakeConsole.CursorLeft);
        Assert.Equal (10, FakeConsole.CursorTop);

        driver.End ();
    }

    [Fact]
    public void FakeConsole_Tracks_Colors ()
    {
        // Arrange
        var driver = new FakeDriver ();
        driver.Init ();

        // Act
        FakeConsole.ForegroundColor = ConsoleColor.Red;
        FakeConsole.BackgroundColor = ConsoleColor.Blue;

        // Assert
        Assert.Equal (ConsoleColor.Red, FakeConsole.ForegroundColor);
        Assert.Equal (ConsoleColor.Blue, FakeConsole.BackgroundColor);

        driver.End ();
    }

    [Fact]
    public void FakeConsole_Can_Reset_Colors ()
    {
        // Arrange
        var driver = new FakeDriver ();
        driver.Init ();

        FakeConsole.ForegroundColor = ConsoleColor.Red;
        FakeConsole.BackgroundColor = ConsoleColor.Blue;

        // Act
        FakeConsole.ResetColor ();

        // Assert
        Assert.Equal (ConsoleColor.Gray, FakeConsole.ForegroundColor);
        Assert.Equal (ConsoleColor.Black, FakeConsole.BackgroundColor);

        driver.End ();
    }

    #endregion
}
