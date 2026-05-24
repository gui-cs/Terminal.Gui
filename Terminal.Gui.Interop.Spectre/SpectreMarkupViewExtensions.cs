using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using Terminal.Gui.Drawing;
using Terminal.Gui.Text;
using Terminal.Gui.ViewBase;
using TgAttribute = Terminal.Gui.Drawing.Attribute;

namespace Terminal.Gui.Interop.Spectre;

/// <summary>Extensions for applying Spectre markup to <see cref="View"/> text.</summary>
public static class SpectreMarkupViewExtensions
{
    private static readonly ConditionalWeakTable<View, MarkupState> _markupStates = new ();

    /// <summary>Sets the view text from Spectre markup and renders markup styles during draw.</summary>
    /// <param name="view">The target view.</param>
    /// <param name="markup">Spectre markup text.</param>
    public static void SetMarkup (this View view, string markup)
    {
        ArgumentNullException.ThrowIfNull (view);

        IReadOnlyList<StyledSegment> segments = SpectreMarkupBridge.ParseMarkup (markup);
        string plainText = string.Concat (segments.Select (segment => segment.Text));

        MarkupState state = _markupStates.GetValue (view, static _ => new MarkupState ());
        state.Segments = segments;
        state.PlainText = plainText;

        if (!state.DrawingHooked)
        {
            view.DrawingText += DrawMarkupText;
            state.DrawingHooked = true;
        }

        view.Text = plainText;
    }

    private static void DrawMarkupText (object? sender, DrawEventArgs drawEventArgs)
    {
        if (sender is not View view || !_markupStates.TryGetValue (view, out MarkupState? state))
        {
            return;
        }

        if (view.Text != state.PlainText)
        {
            view.DrawingText -= DrawMarkupText;
            _markupStates.Remove (view);

            return;
        }

        drawEventArgs.Cancel = true;
        DrawSegments (view, state.Segments, drawEventArgs.DrawContext);
    }

    private static void DrawSegments (View view, IReadOnlyList<StyledSegment> segments, DrawContext? drawContext)
    {
        Size contentSize = view.GetContentSize ();

        if (contentSize.Width <= 0 || contentSize.Height <= 0)
        {
            return;
        }

        drawContext?.AddDrawnRectangle (new Rectangle (view.ContentToScreen (Point.Empty), contentSize));

        var row = 0;
        var col = 0;

        foreach (StyledSegment segment in segments)
        {
            TgAttribute attribute = segment.Attribute ?? view.GetAttributeForRole (VisualRole.Normal);
            view.SetAttribute (attribute);

            foreach (string grapheme in GraphemeHelper.GetGraphemes (segment.Text))
            {
                if (grapheme.Contains ('\n'))
                {
                    int newLines = grapheme.Count (ch => ch == '\n');
                    row += newLines;
                    col = 0;

                    if (row >= contentSize.Height)
                    {
                        return;
                    }

                    continue;
                }

                int graphemeWidth = Math.Max (grapheme.GetColumns (), 1);

                if (col + graphemeWidth > contentSize.Width)
                {
                    row++;
                    col = 0;
                }

                if (row >= contentSize.Height)
                {
                    return;
                }

                view.AddStr (col, row, grapheme);
                col += graphemeWidth;
            }
        }
    }

    private sealed class MarkupState
    {
        public bool DrawingHooked { get; set; }
        public string PlainText { get; set; } = string.Empty;
        public IReadOnlyList<StyledSegment> Segments { get; set; } = [];
    }
}
