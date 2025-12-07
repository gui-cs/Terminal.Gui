#nullable enable

using System.Text;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("WideGlyphs", "Demonstrates wide glyphs with overlapped views & clipping")]
[ScenarioCategory ("Unicode")]
[ScenarioCategory ("Drawing")]

public sealed class WideGlyphs : Scenario
{
    private Rune [,]? _codepoints;

    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Window appWindow = new ()
        {
            Title = GetQuitKeyAndName (),
            BorderStyle = LineStyle.None
        };

        // Build the array of codepoints once when subviews are laid out
        appWindow.SubViewsLaidOut += (s, e) =>
        {
            View? view = s as View;
            if (view is null)
            {
                return;
            }

            // Only rebuild if size changed or array is null
            if (_codepoints is null || 
                _codepoints.GetLength (0) != view.Viewport.Height || 
                _codepoints.GetLength (1) != view.Viewport.Width)
            {
                _codepoints = new Rune [view.Viewport.Height, view.Viewport.Width];

                for (int r = 0; r < view.Viewport.Height; r++)
                {
                    for (int c = 0; c < view.Viewport.Width; c += 2)
                    {
                        _codepoints [r, c] = GetRandomWideCodepoint ();
                    }
                }
            }
        };

        // Fill the window with the pre-built codepoints array
        appWindow.DrawingContent += (s, e) =>
        {
            View? view = s as View;
            if (view is null || _codepoints is null)
            {
                return;
            }

            // Traverse the Viewport, using the pre-built array
            for (int r = 0; r < view.Viewport.Height && r < _codepoints.GetLength (0); r++)
            {
                for (int c = 0; c < view.Viewport.Width && c < _codepoints.GetLength (1); c += 2)
                {
                    Rune codepoint = _codepoints [r, c];
                    if (codepoint != default (Rune))
                    {
                        view.AddRune (c, r, codepoint);
                    }
                }
            }
        };

        Line verticalLineAtEven = new Line ()
        {
            X = 10,
            Orientation = Orientation.Vertical,
            Length = Dim.Fill ()
        };
        appWindow.Add (verticalLineAtEven);

        Line verticalLineAtOdd = new Line ()
        {
            X = 25,
            Orientation = Orientation.Vertical,
            Length = Dim.Fill ()
        };
        appWindow.Add (verticalLineAtOdd);

        View arrangeableViewAtEven = new ()
        {
            CanFocus = true,
            Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable,
            X = 30,
            Y = 5,
            Width = 15,
            Height = 5,
            BorderStyle = LineStyle.Dashed,
        };
        appWindow.Add (arrangeableViewAtEven);

        View arrangeableViewAtOdd = new ()
        {
            CanFocus = true,
            Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable,
            X = 31,
            Y = 11,
            Width = 15,
            Height = 5,
            BorderStyle = LineStyle.Dashed,
        };
        appWindow.Add (arrangeableViewAtOdd);
        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }

    private Rune GetRandomWideCodepoint ()
    {
        Random random = new ();
        int codepoint = random.Next (0x4E00, 0x9FFF);
        return new Rune (codepoint);
    }
}
