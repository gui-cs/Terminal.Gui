namespace Terminal.Gui.Views;

/// <summary>
///     A read-only view that renders a single Markdown fenced code block with a dimmed background
///     and an optional copy button.
/// </summary>
/// <remarks>
///     <para>
///         When used inside a <see cref="MarkdownView"/>, instances are created automatically during
///         layout and positioned as SubViews at the correct content coordinate so that they scroll
///         naturally with the parent's viewport.
///     </para>
///     <para>
///         The dimmed background fills the full width of the view (via <c>Width = Dim.Fill()</c>),
///         so it automatically resizes when the content area width changes.
///     </para>
///     <para>
///         This view can also be used standalone. Use the parameterless constructor and set
///         <see cref="CodeLines"/> to provide plain-text code content.
///     </para>
/// </remarks>
public class MarkdownCodeBlock : View, IDesignable
{
    private IReadOnlyList<IReadOnlyList<StyledSegment>> _lines = [];

    /// <summary>Initializes a new <see cref="MarkdownCodeBlock"/>.</summary>
    public MarkdownCodeBlock ()
    {
        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);
    }

    /// <summary>
    ///     Gets or sets the styled line segments. Used internally by <see cref="MarkdownView"/> to set
    ///     pre-parsed styled content directly.
    /// </summary>
    internal IReadOnlyList<IReadOnlyList<StyledSegment>> StyledLines
    {
        get => _lines;
        set
        {
            _lines = value;
            UpdateContentSize ();
        }
    }

    /// <summary>
    ///     Gets or sets the plain-text code lines. Setting this re-creates the internal styled
    ///     segments with <see cref="MarkdownStyleRole.CodeBlock"/> styling.
    /// </summary>
    public IReadOnlyList<string> CodeLines
    {
        get
        {
            List<string> result = [];
            result.AddRange (_lines.Select (segments => string.Concat (segments.Select (s => s.Text))));

            return result;
        }
        set
        {
            List<IReadOnlyList<StyledSegment>> segments = [];
            segments.AddRange (value.Select (line => (IReadOnlyList<StyledSegment>)[new StyledSegment (line, MarkdownStyleRole.CodeBlock)]));

            _lines = segments;
            UpdateContentSize ();
        }
    }

    private void UpdateContentSize ()
    {
        var maxWidth = 0;

        foreach (IReadOnlyList<StyledSegment> line in _lines)
        {
            int lineWidth = line.Sum (s => s.Text.GetColumns ());
            maxWidth = Math.Max (maxWidth, lineWidth);
        }

        // Set explicit dimensions based on content.
        // We avoid SetContentSize because it sets ContentSizeTracksViewport = false,
        // which restricts Viewport width when Width = Dim.Fill() (embedded in MarkdownView).
        if (Width is DimAuto)
        {
            Width = maxWidth;
        }

        if (Height is DimAuto)
        {
            Height = _lines.Count;
        }

        SetNeedsLayout ();
        SetNeedsDraw ();
    }

    /// <inheritdoc/>
    protected override bool OnMouseEvent (Mouse mouse)
    {
        if (!mouse.Flags.HasFlag (MouseFlags.LeftButtonClicked))
        {
            return false;
        }

        if (mouse.Position is not { } pos)
        {
            return false;
        }

        int copyGlyphX = Viewport.Width - 1;

        if (pos.X != copyGlyphX || pos.Y != 0)
        {
            return false;
        }

        string codeText = ExtractText ();
        App?.Clipboard?.TrySetClipboardData (codeText);

        return true;
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        // TODO: We should ideally be using the "code" role here, but it doesn't exist yet.
        // TODO: For now, we'll just use ReadOnly with a dimmed background.
        Attribute normal = GetAttributeForRole (VisualRole.Editable);
        Color codeBg = normal.Background;
        Attribute codeAttr = new (normal.Foreground, codeBg);

        // TODO: Move this to OnClearingViewport where it belongs.
        // Fill entire area with dimmed background
        SetAttribute (codeAttr);
        FillRect (Viewport with { X = 0, Y = 0 }, (Rune)' ');

        for (var y = 0; y < _lines.Count && y < Viewport.Height; y++)
        {
            IReadOnlyList<StyledSegment> segments = _lines [y];
            var x = 0;

            foreach (StyledSegment segment in segments)
            {
                Attribute attr = MarkdownAttributeHelper.GetAttributeForSegment (this, segment);
                SetAttribute (attr);

                foreach (string grapheme in GraphemeHelper.GetGraphemes (segment.Text))
                {
                    int gw = Math.Max (grapheme.GetColumns (), 1);

                    if (x >= Viewport.Width)
                    {
                        break;
                    }

                    AddStr (x, y, grapheme);
                    x += gw;
                }
            }
        }

        // Draw the copy glyph in the top-right corner
        if (Viewport.Width <= 0 || Viewport.Height <= 0)
        {
            return true;
        }
        SetAttribute (codeAttr);
        AddStr (Viewport.Width - 1, 0, Glyphs.Copy.ToString ());

        return true;
    }

    /// <summary>Extracts the plain text of this code block.</summary>
    public string ExtractText ()
    {
        List<string> lineTexts = [];
        lineTexts.AddRange (_lines.Select (segments => string.Concat (segments.Select (s => s.Text))));

        return string.Join (Environment.NewLine, lineTexts);
    }

    /// <inheritdoc/>
    bool IDesignable.EnableForDesign ()
    {
        CodeLines =
        [
            "using IApplication app = Application.Create ();", "app.Init ();", "using ExampleWindow exampleWindow = new ();", "app.Run (exampleWindow)"
        ];

        return true;
    }
}
