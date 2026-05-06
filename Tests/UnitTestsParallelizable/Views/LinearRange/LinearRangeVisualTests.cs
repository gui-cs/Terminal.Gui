using UnitTests;

namespace ViewsTests;

/// <summary>
///     Visual + input-driven tests for <see cref="LinearRange{T}"/> (range-only).
/// </summary>
public class LinearRangeVisualTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    // Copilot
    [Fact]
    public void Renders_Closed_Range ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver?.SetScreenSize (12, 3);

        IRunnable runnable = new Runnable ();
        LinearRange<int> r = new ([1, 2, 3, 4]);
        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.Closed, 2, 3, 1, 2);
        (runnable as View)?.Add (r);
        app.Begin (runnable);

        app.LayoutAndDraw ();

        // Indexes 1..2 selected: '●' (option), '─' (space), '█' (set), '░' (range).
        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       """
                                                       ●─█░█─●
                                                       1 2 3 4
                                                       """,
                                                       _output,
                                                       app.Driver);
    }

    // Copilot
    [Fact]
    public void Mouse_Drag_Adjusts_End_Of_Range ()
    {
        // This is the regression test for the user-reported bug:
        //   "Mouse dragging of the end of a range is not adjusting the values."
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver?.SetScreenSize (15, 3);

        IRunnable runnable = new Runnable ();
        LinearRange<int> r = new ([1, 2, 3, 4, 5]) { AllowEmpty = true };
        (runnable as View)?.Add (r);
        app.Begin (runnable);
        app.LayoutAndDraw ();

        Assert.True (r.TryGetPositionByOption (1, out (int x, int y) p1));
        Assert.True (r.TryGetPositionByOption (4, out (int x, int y) p4));
        Point sStart = r.ViewportToScreen (new Point (p1.x, p1.y));
        Point sEnd = r.ViewportToScreen (new Point (p4.x, p4.y));

        // Press at index 1 (the start of the range).
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport, ScreenPosition = sStart });

        // Drag to index 4 — the end of the range.
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport, ScreenPosition = sEnd });

        // Release.
        app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = sEnd });

        // Range should span [2..5] = data values at indexes 1..4.
        Assert.Equal (LinearRangeSpanKind.Closed, r.Value.Kind);
        Assert.Equal (2, r.Value.Start);
        Assert.Equal (5, r.Value.End);
    }

    // Copilot
    [Fact]
    public void Keyboard_Extends_Range ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver?.SetScreenSize (15, 3);

        IRunnable runnable = new Runnable ();
        LinearRange<int> r = new ([1, 2, 3, 4, 5]) { AllowEmpty = true };
        (runnable as View)?.Add (r);
        app.Begin (runnable);
        r.SetFocus ();

        // Activate at index 0 (start of range).
        app.InjectKey (new Key (KeyCode.Space));
        Assert.Equal (1, r.Value.Start);

        // Ctrl+Right extends the range.
        app.InjectKey (new Key (KeyCode.CursorRight | KeyCode.CtrlMask));
        app.InjectKey (new Key (KeyCode.CursorRight | KeyCode.CtrlMask));

        Assert.Equal (LinearRangeSpanKind.Closed, r.Value.Kind);
        Assert.Equal (1, r.Value.Start);
        Assert.Equal (3, r.Value.End);
    }
}
