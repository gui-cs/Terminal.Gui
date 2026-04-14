namespace Terminal.Gui.Views;

/// <summary>
///     A read-only view that renders a single Markdown fenced code block with a dimmed background
///     and an optional copy button.
/// </summary>
/// <remarks>
///     <para>
///         This view is created and managed internally by <see cref="MarkdownView"/> during layout.
///         It is positioned as a SubView at the correct content coordinate so that it scrolls
///         naturally with the parent's viewport.
///     </para>
///     <para>
///         The dimmed background fills the full width of the view (via <c>Width = Dim.Fill()</c>),
///         so it automatically resizes when <see cref="View.SetContentSize"/> changes the parent's
///         content area width.
///     </para>
/// </remarks>
internal sealed class MarkdownCodeBlock : View
{
    private readonly IReadOnlyList<IReadOnlyList<StyledSegment>> _lines;

    /// <summary>Initializes a new <see cref="MarkdownCodeBlock"/>.</summary>
    /// <param name="lines">
    ///     The rendered segments for each line of the code block. Each entry is a list of
    ///     <see cref="StyledSegment"/> values for one code-block line.
    /// </param>
    public MarkdownCodeBlock (IReadOnlyList<IReadOnlyList<StyledSegment>> lines)
    {
        _lines = lines;

        CanFocus = false;
        TabStop = TabBehavior.NoStop;

        // No adornments — we draw everything ourselves
        BorderStyle = LineStyle.None;
        Border.Thickness = new Thickness (0);
        Padding.Thickness = new Thickness (0);
        Margin.Thickness = new Thickness (0);

        Height = lines.Count;

        // Copy button
        Button copyBtn = new ()
        {
            X = Pos.AnchorEnd (),
            Y = 0,
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
            ShadowStyle = ShadowStyles.None,
            NoPadding = true,
            NoDecorations = true,
            Text = Glyphs.Copy.ToString ()
        };

        copyBtn.Accepted += (_, _) =>
                            {
                                string codeText = ExtractText ();
                                copyBtn.App?.Clipboard?.TrySetClipboardData (codeText);
                            };

        Add (copyBtn);
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        Attribute normal = GetAttributeForRole (VisualRole.Normal);
        Color codeBg = normal.Background.GetDimmerColor ();
        Attribute codeAttr = new (normal.Foreground, codeBg);

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

        return true;
    }

    /// <summary>Extracts the plain text of this code block.</summary>
    internal string ExtractText ()
    {
        List<string> lineTexts = [];

        foreach (IReadOnlyList<StyledSegment> segments in _lines)
        {
            lineTexts.Add (string.Concat (segments.Select (s => s.Text)));
        }

        return string.Join (Environment.NewLine, lineTexts);
    }
}
