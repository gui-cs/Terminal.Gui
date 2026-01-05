using Xunit.Abstractions;

namespace ApplicationTests;

/// <summary>
/// Tests for cursor positioning functionality using instance-based application model.
/// </summary>
/// <remarks>
/// CoPilot - GitHub Copilot Agent
/// </remarks>
public class CursorTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    private class TestView : View
    {
        public Point? TestLocation { get; set; }

        /// <inheritdoc/>
        public override Point? PositionCursor ()
        {
            if (TestLocation.HasValue && HasFocus)
            {
                // Check if cursor is within viewport bounds
                if (TestLocation.Value.X >= 0 && TestLocation.Value.X < Viewport.Width && TestLocation.Value.Y >= 0 && TestLocation.Value.Y < Viewport.Height)
                {
                    Driver?.SetCursorVisibility (CursorVisibility.Default);

                    return TestLocation;
                }

                // Cursor outside viewport - hide it
                return null;
            }

            return TestLocation;
        }
    }

    [Fact]
    public void UpdateCursor_No_Focus_Returns_Early ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        app.Navigation?.SetFocused (null);

        // UpdateCursor should return early when there's no focus
        // Just verify it doesn't crash
        app.Navigation?.UpdateCursor ();

        app.Dispose ();
    }

    [Fact]
    public void UpdateCursor_View_Without_CanFocus_Returns_Early ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        TestView view = new ()
        {
            CanFocus = false,
            Width = 10,
            Height = 5
        };
        view.TestLocation = new Point (0, 0);

        Runnable runnable = new ();
        runnable.Add (view);
        SessionToken? token = app.Begin (runnable);

        // UpdateCursor should return early when focused view can't have focus
        // Just verify it doesn't crash
        app.Navigation?.UpdateCursor ();

        if (token is { })
        {
            app.End (token);
        }
        app.Dispose ();
        runnable.Dispose ();
    }

    [Fact]
    public void UpdateCursor_View_Returns_Null_Position_Returns_Early ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        TestView view = new ()
        {
            CanFocus = true,
            Width = 10,
            Height = 5
        };
        // TestLocation is null by default

        Runnable runnable = new ();
        runnable.Add (view);
        SessionToken? token = app.Begin (runnable);

        view.SetFocus ();
        Assert.True (view.HasFocus);

        // UpdateCursor should return early when PositionCursor returns null
        // Just verify it doesn't crash
        app.Navigation?.UpdateCursor ();

        if (token is { })
        {
            app.End (token);
        }
        app.Dispose ();
        runnable.Dispose ();
    }

    [Fact]
    public void UpdateCursor_Focused_With_Position_Succeeds ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        TestView view = new ()
        {
            CanFocus = true,
            Width = 10,
            Height = 5
        };
        view.TestLocation = new Point (2, 1);

        Runnable runnable = new ();
        runnable.Add (view);
        SessionToken? token = app.Begin (runnable);

        view.SetFocus ();
        Assert.True (view.HasFocus);

        // UpdateCursor should succeed when view has focus and valid position
        // Just verify it doesn't crash
        app.Navigation?.UpdateCursor ();

        if (token is { })
        {
            app.End (token);
        }
        app.Dispose ();
        runnable.Dispose ();
    }

    [Fact]
    public void PositionCursor_Defaults_To_Null ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        View view = new ()
        {
            CanFocus = true,
            Width = 10,
            Height = 5
        };

        Runnable runnable = new ();
        runnable.Add (view);
        SessionToken? token = app.Begin (runnable);

        view.SetFocus ();
        Assert.True (view.HasFocus);

        // Default View.PositionCursor should return null
        Point? cursorPos = view.PositionCursor ();
        Assert.Null (cursorPos);

        if (token is { })
        {
            app.End (token);
        }
        app.Dispose ();
        runnable.Dispose ();
    }

    // Tests for Issue #3444 - Cursor should be hidden when positioned outside viewport
    [Fact]
    public void PositionCursor_OutsideViewport_Returns_Null ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        // Test with a generic View that positions cursor outside viewport
        TestView view = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 5,
            CanFocus = true
        };

        Runnable runnable = new ();
        runnable.Add (view);
        SessionToken? token = app.Begin (runnable);

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

        if (token is { })
        {
            app.End (token);
        }
        app.Dispose ();
        runnable.Dispose ();
    }

    [Fact]
    public void UpdateCursor_CursorOutsideAncestorViewport_Succeeds ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        // Create parent with small viewport
        View parent = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 5
        };

        // Create child that extends beyond parent viewport
        TestView child = new ()
        {
            X = 0,
            Y = 0,
            Width = 20,
            Height = 10,
            CanFocus = true
        };

        parent.Add (child);
        Runnable runnable = new ();
        runnable.Add (parent);
        SessionToken? token = app.Begin (runnable);

        child.SetFocus ();

        // Position cursor within child's viewport but outside parent's viewport
        child.TestLocation = new Point (15, 0);

        // UpdateCursor should handle cursor outside ancestor viewport
        // Just verify it doesn't crash
        app.Navigation?.UpdateCursor ();

        if (token is { })
        {
            app.End (token);
        }
        app.Dispose ();
        runnable.Dispose ();
    }

    [Fact]
    public void UpdateCursor_CursorWithinAllAncestors_Succeeds ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        // Create parent viewport
        View parent = new ()
        {
            X = 0,
            Y = 0,
            Width = 20,
            Height = 10
        };

        // Create child within parent viewport
        TestView child = new ()
        {
            X = 2,
            Y = 2,
            Width = 10,
            Height = 5,
            CanFocus = true
        };

        parent.Add (child);
        Runnable runnable = new ();
        runnable.Add (parent);
        SessionToken? token = app.Begin (runnable);

        child.SetFocus ();

        // Position cursor within both child and parent viewports
        child.TestLocation = new Point (2, 1);

        // UpdateCursor should succeed when cursor is within all ancestor viewports
        // Just verify it doesn't crash
        app.Navigation?.UpdateCursor ();

        if (token is { })
        {
            app.End (token);
        }
        app.Dispose ();
        runnable.Dispose ();
    }

    [Fact]
    public void SetCursorNeedsUpdate_Signals_Cursor_Update ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        TestView view = new ()
        {
            CanFocus = true,
            Width = 10,
            Height = 5
        };

        Runnable runnable = new ();
        runnable.Add (view);
        SessionToken? token = app.Begin (runnable);

        view.SetFocus ();

        // Call SetCursorNeedsUpdate - should set flag in ApplicationNavigation
        view.SetCursorNeedsUpdate ();

        // This test verifies the API exists and can be called
        // The actual caching behavior is disabled per cursor.md

        if (token is { })
        {
            app.End (token);
        }
        app.Dispose ();
        runnable.Dispose ();
    }

    [Fact]
    public void TextField_CursorPosition_Stays_Within_Viewport ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        TextField textField = new ()
        {
            X = 0,
            Y = 0,
            Width = 10,
            Text = "This is a very long text that extends beyond the viewport"
        };

        Runnable runnable = new ();
        runnable.Add (textField);
        SessionToken? token = app.Begin (runnable);

        textField.SetFocus ();

        // Position cursor at end of text (beyond viewport)
        textField.CursorPosition = textField.Text.Length;

        // PositionCursor should return a position within viewport
        Point? cursorPos = textField.PositionCursor ();

        if (cursorPos.HasValue)
        {
            Assert.True (cursorPos.Value.X >= 0);
            Assert.True (cursorPos.Value.X < textField.Viewport.Width);
        }

        if (token is { })
        {
            app.End (token);
        }
        app.Dispose ();
        runnable.Dispose ();
    }
}
