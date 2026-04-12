// Copilot

namespace ApplicationTests;

/// <summary>
///     Tests that terminal resize in inline mode resets the inline region to row 0
///     and re-performs the initial sizing on the next layout pass.
/// </summary>
[Collection ("Application Tests")]
public class InlineResizeTests
{
    /// <summary>
    ///     Verifies that after a terminal resize in inline mode, <c>App.Screen.Y</c> resets to 0
    ///     and the inline region is re-sized from scratch.
    /// </summary>
    [Fact]
    public void TerminalResize_Inline_ResetsScreenToRowZero ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.AppModel = AppModel.Inline;
        app.ForceInlineCursorRow = 15;
        app.Init (DriverRegistry.Names.ANSI);

        // Simulate that the inline region was initially sized at row 15
        app.Screen = new Rectangle (0, 15, 120, 5);

        Assert.Equal (15, app.Screen.Y);
        Assert.Equal (120, app.Screen.Width);

        // Act: Simulate a terminal resize (e.g., narrower terminal)
        app.Driver!.SetScreenSize (80, 25);

        // Assert: Screen.Y should be reset to 0
        Assert.Equal (0, app.Screen.Y);
        Assert.Equal (80, app.Screen.Width);
    }

    /// <summary>
    ///     Verifies that after a terminal resize in inline mode, the driver's
    ///     <see cref="InlineState.InlineCursorRow"/> is reset to 0.
    /// </summary>
    [Fact]
    public void TerminalResize_Inline_ResetsInlineCursorRow ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.AppModel = AppModel.Inline;
        app.ForceInlineCursorRow = 20;
        app.Init (DriverRegistry.Names.ANSI);

        // Simulate that the driver has InlineCursorRow set (as CPR would do)
        app.Driver!.InlineState = new InlineState { InlineCursorRow = 20 };
        app.Screen = new Rectangle (0, 20, 100, 3);
        Assert.Equal (20, app.Driver!.InlineState.InlineCursorRow);

        // Act
        app.Driver!.SetScreenSize (80, 25);

        // Assert
        Assert.Equal (0, app.Driver!.InlineState.InlineCursorRow);
    }
}
