using global::Spectre.Console.Rendering;
using Terminal.Gui.Drawing;
using Terminal.Gui.Text;
using Terminal.Gui.ViewBase;
using TgAttribute = Terminal.Gui.Drawing.Attribute;
using DrawingSize = System.Drawing.Size;

namespace Terminal.Gui.Interop.Spectre;

/// <summary>
///     A read-only <see cref="View"/> that renders a Spectre <see cref="IRenderable"/>.
/// </summary>
public class SpectreView : View
{
    private IRenderable? _renderable;
    private bool _autoSize = true;

    /// <summary>
    ///     Gets or sets the Spectre renderable to display.
    /// </summary>
    public IRenderable? Renderable
    {
        get => _renderable;
        set
        {
            if (ReferenceEquals (_renderable, value))
            {
                return;
            }

            _renderable = value;
            UpdateContentSizeFromRenderable ();
            SetNeedsDraw ();
        }
    }

    /// <summary>
    ///     Gets or sets whether this view updates content size from the rendered Spectre content.
    /// </summary>
    public bool AutoSize
    {
        get => _autoSize;
        set
        {
            if (_autoSize == value)
            {
                return;
            }

            _autoSize = value;
            UpdateContentSizeFromRenderable ();
            SetNeedsDraw ();
        }
    }

    /// <inheritdoc/>
    protected override void OnViewportChanged (DrawEventArgs e)
    {
        base.OnViewportChanged (e);
        UpdateContentSizeFromRenderable ();
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        if (Renderable is null)
        {
            return true;
        }

        int maxWidth = Math.Max (Viewport.Width, 1);
        (IReadOnlyList<Segment> segments, _, _) = RenderSegments (Renderable, maxWidth);

        int row = 0;
        int col = 0;

        foreach (Segment segment in segments)
        {
            if (segment.IsLineBreak)
            {
                row++;
                col = 0;

                continue;
            }

            if (string.IsNullOrEmpty (segment.Text))
            {
                continue;
            }

            DrawSegment (segment, row, col);
            col += segment.Text.GetColumns ();
        }

        return true;
    }

    private void DrawSegment (Segment segment, int row, int col)
    {
        if (row < Viewport.Y || row >= Viewport.Bottom)
        {
            return;
        }

        TgAttribute attribute = segment.Style.ToAttribute ();
        int x = col;

        foreach (string grapheme in GraphemeHelper.GetGraphemes (segment.Text))
        {
            int graphemeWidth = Math.Max (grapheme.GetColumns (), 1);
            bool visible = x + graphemeWidth > Viewport.X && x < Viewport.Right;

            if (visible)
            {
                SetAttribute (attribute);
                AddStr (x - Viewport.X, row - Viewport.Y, grapheme);
            }

            x += graphemeWidth;
        }
    }

    private void UpdateContentSizeFromRenderable ()
    {
        if (!AutoSize)
        {
            SetContentSize (null);

            return;
        }

        if (Renderable is null)
        {
            SetContentSize (new DrawingSize (0, 0));

            return;
        }

        int maxWidth = Math.Max (Viewport.Width, 1);
        (_, int contentWidth, int contentHeight) = RenderSegments (Renderable, maxWidth);
        SetContentSize (new DrawingSize (contentWidth, contentHeight));
    }

    private static (IReadOnlyList<Segment> Segments, int ContentWidth, int ContentHeight) RenderSegments (IRenderable renderable, int maxWidth)
    {
        RenderOptions renderOptions = RenderOptions.Create (global::Spectre.Console.AnsiConsole.Console, null);
        Measurement measurement = renderable.Measure (renderOptions, maxWidth);
        List<Segment> segments = [.. renderable.Render (renderOptions, maxWidth)];

        if (segments.Count == 0)
        {
            return (segments, 0, 0);
        }

        int maxLineWidth = 0;
        int lineWidth = 0;
        int lineCount = 1;

        foreach (Segment segment in segments)
        {
            if (segment.IsLineBreak)
            {
                maxLineWidth = Math.Max (maxLineWidth, lineWidth);
                lineWidth = 0;
                lineCount++;

                continue;
            }

            if (string.IsNullOrEmpty (segment.Text))
            {
                continue;
            }

            lineWidth += segment.Text.GetColumns ();
        }

        maxLineWidth = Math.Max (maxLineWidth, lineWidth);
        int contentWidth = Math.Max (measurement.Max, maxLineWidth);

        return (segments, contentWidth, lineCount);
    }
}
