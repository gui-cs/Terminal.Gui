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
    private IReadOnlyList<IReadOnlyList<StyledSegment>> _lines;

    /// <summary>Initializes a new <see cref="MarkdownCodeBlock"/> with no content.</summary>
    public MarkdownCodeBlock () : this ([]) { }

    /// <summary>Initializes a new <see cref="MarkdownCodeBlock"/>.</summary>
    /// <param name="lines">
    ///     The rendered segments for each line of the code block. Each entry is a list of
    ///     <see cref="StyledSegment"/> values for one code-block line.
    /// </param>
    public MarkdownCodeBlock (IReadOnlyList<IReadOnlyList<StyledSegment>> lines)
    {
        _lines = lines ?? [];

        Height = _lines.Count;

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

    /// <summary>
    ///     Gets or sets the plain-text code lines. Setting this re-creates the internal styled
    ///     segments with <see cref="MarkdownStyleRole.CodeBlock"/> styling and updates <see cref="View.Height"/>.
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
            Height = _lines.Count;
            SetNeedsDraw ();
        }
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
        Width = Dim.Auto ();

        return true;
    }
}
