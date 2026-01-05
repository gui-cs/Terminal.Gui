using UnitTests;
using Xunit.Abstractions;

namespace ApplicationTests;

/// <summary>
/// Tests for cursor positioning functionality using instance-based application model.
/// These tests verify ApplicationNavigation.UpdateCursor() and View.PositionCursor() behavior.
/// </summary>
public class CursorTests : TestDriverBase
{
    public CursorTests (ITestOutputHelper output) { }

    private class TestView : View
    {
        public Point? TestLocation { get; set; }

        /// <inheritdoc/>
        public override Point? PositionCursor ()
        {
            if (TestLocation.HasValue && HasFocus)
            {
                // Check if cursor is within viewport bounds
                if (TestLocation.Value.X >= 0 &&
                    TestLocation.Value.X < Viewport.Width &&
                    TestLocation.Value.Y >= 0 &&
                    TestLocation.Value.Y < Viewport.Height)
                {
                    return TestLocation;
                }

                // Cursor outside viewport - hide it
                return null;
            }

            return TestLocation;
        }
    }

    [Fact]
    public void UpdateCursor_No_Focus_Hides_Cursor ()
    {
        using var app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        
        app.Navigation.SetFocused (null);

        // UpdateCursor should hide cursor when there's no focus
        app.Navigation.UpdateCursor (app.Driver!.GetOutput ());

        app.Driver.GetCursorVisibility (out CursorVisibility visibility);
        Assert.Equal (CursorVisibility.Invisible, visibility);
    }

    [Fact]
    public void UpdateCursor_View_Without_CanFocus_Hides_Cursor ()
    {
        using var app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        
        var view = new TestView
        {
            CanFocus = false,
            Width = 10,
            Height = 5
        };
        view.TestLocation = new Point (0, 0);

        var runnable = new Runnable ();
        runnable.Add (view);
        app.Begin (runnable);

        // UpdateCursor should hide cursor when focused view can't have focus
        app.Navigation.UpdateCursor (app.Driver!.GetOutput ());

        app.Driver.GetCursorVisibility (out CursorVisibility visibility);
        Assert.Equal (CursorVisibility.Invisible, visibility);
    }

    [Fact]
    public void UpdateCursor_View_Returns_Null_Position_Hides_Cursor ()
    {
        using var app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        
        var view = new TestView
        {
            CanFocus = true,
            Width = 10,
            Height = 5
        };
        // TestLocation is null by default

        var runnable = new Runnable ();
        runnable.Add (view);
        app.Begin (runnable);
        
        view.SetFocus ();
        Assert.True (view.HasFocus);

        // UpdateCursor should hide cursor when PositionCursor returns null
        app.Navigation.UpdateCursor (app.Driver!.GetOutput ());

        app.Driver.GetCursorVisibility (out CursorVisibility visibility);
        Assert.Equal (CursorVisibility.Invisible, visibility);
    }

    [Fact]
    public void UpdateCursor_Valid_Position_Shows_Cursor ()
    {
        using var app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        
        var view = new TestView
        {
            CanFocus = true,
            Width = 10,
            Height = 5,
            CursorVisibility = CursorVisibility.Default
        };
        view.TestLocation = new Point (3, 2);

        var runnable = new Runnable ();
        runnable.Add (view);
        app.Begin (runnable);
        
        view.SetFocus ();
        Assert.True (view.HasFocus);

        // UpdateCursor should show cursor at correct position
        app.Navigation.UpdateCursor (app.Driver!.GetOutput ());

        app.Driver.GetCursorVisibility (out CursorVisibility visibility);
        Assert.Equal (CursorVisibility.Default, visibility);
    }

    [Fact]
    public void PositionCursor_Defaults_To_Null ()
    {
        var view = new View
        {
            CanFocus = true,
            Width = 10,
            Height = 5
        };

        var runnable = new View ();
        runnable.Add (view);
        runnable.BeginInit ();
        runnable.EndInit ();
        
        view.SetFocus ();
        Assert.True (view.HasFocus);

        // Default implementation returns null (cursor hidden)
        Point? cursor = view.PositionCursor ();
        Assert.Null (cursor);
    }

    // Tests for Issue #3444 - Cursor should be hidden when positioned outside viewport
    [Fact]
    public void PositionCursor_OutsideViewport_Returns_Null ()
    {
        // Test with a generic View that positions cursor outside viewport
        var view = new TestView
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 5,
            CanFocus = true
        };

        var runnable = new View ();
        runnable.Add (view);
        runnable.BeginInit ();
        runnable.EndInit ();
        
        view.SetFocus ();

        // Position cursor outside viewport width
        view.TestLocation = new Point (15, 0);
        Point? cursorPos = view.PositionCursor ();

        // Cursor should be hidden (return null) when outside viewport
        Assert.Null (cursorPos);

        // Position cursor outside viewport height
        view.TestLocation = new Point (0, 10);
        cursorPos = view.PositionCursor ();

        Assert.Null (cursorPos);

        // Position cursor at negative position
        view.TestLocation = new Point (-1, 0);
        cursorPos = view.PositionCursor ();

        Assert.Null (cursorPos);

        view.TestLocation = new Point (0, -1);
        cursorPos = view.PositionCursor ();

        Assert.Null (cursorPos);

        // Position cursor inside viewport - should be visible
        view.TestLocation = new Point (5, 2);
        cursorPos = view.PositionCursor ();

        Assert.NotNull (cursorPos);
        Assert.Equal (new Point (5, 2), cursorPos.Value);
    }

    [Fact]
    public void UpdateCursor_CursorOutsideAncestorViewport_Is_Hidden ()
    {
        using var app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        // Parent view with limited viewport
        var parent = new View
        {
            X = 0,
            Y = 0,
            Width = 10,  // Limited width
            Height = 5
        };

        // Child view that can position cursor beyond parent bounds
        var child = new TestView
        {
            X = 0,
            Y = 0,
            Width = 20,  // Wider than parent
            Height = 5,
            CanFocus = true,
            CursorVisibility = CursorVisibility.Default,
            TestLocation = new Point (12, 0) // Cursor at X=12, beyond parent's width of 10
        };

        parent.Add (child);

        var runnable = new Runnable ();
        runnable.Add (parent);
        app.Begin (runnable);
        
        child.SetFocus ();
        Assert.True (child.HasFocus);

        // Verify child would return cursor position if asked directly
        Point? cursorPos = child.PositionCursor ();
        Assert.NotNull (cursorPos);
        Assert.Equal (new Point (12, 0), cursorPos.Value);

        // UpdateCursor should hide cursor because it's outside parent viewport
        app.Navigation.UpdateCursor (app.Driver!.GetOutput ());

        app.Driver.GetCursorVisibility (out CursorVisibility visibility);
        Assert.Equal (CursorVisibility.Invisible, visibility);
    }

    [Fact]
    public void UpdateCursor_CursorWithinAllAncestors_Is_Visible ()
    {
        using var app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        // Parent view
        var parent = new View
        {
            X = 0,
            Y = 0,
            Width = 20,
            Height = 10
        };

        // Child view with cursor within parent bounds
        var child = new TestView
        {
            X = 0,
            Y = 0,
            Width = 15,
            Height = 5,
            CanFocus = true,
            CursorVisibility = CursorVisibility.Default,
            TestLocation = new Point (5, 2) // Cursor within both child and parent
        };

        parent.Add (child);

        var runnable = new Runnable ();
        runnable.Add (parent);
        app.Begin (runnable);
        
        child.SetFocus ();
        Assert.True (child.HasFocus);

        // UpdateCursor should show cursor because it's within all ancestor viewports
        app.Navigation.UpdateCursor (app.Driver!.GetOutput ());

        app.Driver.GetCursorVisibility (out CursorVisibility visibility);
        Assert.Equal (CursorVisibility.Default, visibility);
    }

    [Fact]
    public void TextField_CursorPosition_Stays_Within_Viewport ()
    {
        // TextField with a small viewport
        var textField = new TextField
        {
            X = 0,
            Y = 0,
            Width = 5,
            Text = "Hello World"
        };

        var runnable = new View ();
        runnable.Add (textField);
        runnable.BeginInit ();
        runnable.EndInit ();
        
        textField.SetFocus ();

        // Set cursor to position 0 (beginning)
        textField.CursorPosition = 0;
        Point? cursorPos = textField.PositionCursor ();

        // Cursor at start should be visible
        Assert.NotNull (cursorPos);
        Assert.True (cursorPos.Value.X >= 0 && cursorPos.Value.X < textField.Viewport.Width);

        // Now move cursor forward - at some point it should scroll
        // and cursor should stay within viewport
        for (var i = 0; i <= textField.Text.Length; i++)
        {
            textField.CursorPosition = i;
            cursorPos = textField.PositionCursor ();

            if (cursorPos.HasValue)
            {
                // Cursor position should always be within viewport bounds
                Assert.True (
                             cursorPos.Value.X >= 0,
                             $"Cursor X={cursorPos.Value.X} should be >= 0 at position {i}");

                Assert.True (
                             cursorPos.Value.X < textField.Viewport.Width,
                             $"Cursor X={cursorPos.Value.X} should be < {textField.Viewport.Width} at position {i}");
            }
        }
    }

    [Fact]
    public void SetCursorNeedsUpdate_Signals_Cursor_Update ()
    {
        using var app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        var view = new TestView
        {
            CanFocus = true,
            Width = 10,
            Height = 5,
            CursorVisibility = CursorVisibility.Default,
            TestLocation = new Point (3, 2)
        };

        var runnable = new Runnable ();
        runnable.Add (view);
        app.Begin (runnable);
        
        view.SetFocus ();

        // Call SetCursorNeedsUpdate to signal cursor position changed
        view.SetCursorNeedsUpdate ();

        // This should trigger cursor update on next iteration
        app.Navigation.UpdateCursor (app.Driver!.GetOutput ());

        app.Driver.GetCursorVisibility (out CursorVisibility visibility);
        Assert.Equal (CursorVisibility.Default, visibility);
    }
}
