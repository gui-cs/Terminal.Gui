namespace Terminal.Gui.Views;

/// <summary>A read-only view that renders syntax-highlighted source code.</summary>
public class Code : View, IDesignable
{
    /// <summary>Initializes a new instance of the <see cref="Code"/> class.</summary>
    public Code ()
    {
        CanFocus = true;
        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);
        ViewportSettings |= ViewportSettingsFlags.HasHorizontalScrollBar | ViewportSettingsFlags.HasVerticalScrollBar;
    }

    /// <summary>Gets or sets the source text to render.</summary>
    public override string Text
    {
        get => base.Text;
        set
        {
            if (base.Text == value)
            {
                return;
            }

            base.Text = value;
            UpdateStyledLines ();
        }
    }

    /// <summary>Gets or sets the language hint used for syntax highlighting.</summary>
    public string? Language
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            UpdateStyledLines ();
        }
    }

    /// <summary>Gets or sets the syntax highlighter used to tokenize <see cref="Text"/>.</summary>
    public ISyntaxHighlighter? SyntaxHighlighter
    {
        get;
        set
        {
            if (ReferenceEquals (field, value))
            {
                return;
            }

            field = value;
            UpdateStyledLines ();
        }
    } = new TextMateSyntaxHighlighter ();

    private IReadOnlyList<IReadOnlyList<StyledSegment>> _lines = [];

    private void UpdateStyledLines ()
    {
        string [] lines = base.Text.ReplaceLineEndings ("\n").Split ('\n');
        List<IReadOnlyList<StyledSegment>> styledLines = [];

        if (SyntaxHighlighter is { } highlighter && !string.IsNullOrEmpty (Language))
        {
            highlighter.ResetState ();

            styledLines.AddRange (lines.Select (line => highlighter.Highlight (line, Language)));
        }
        else
        {
            styledLines.AddRange (lines.Select (line => (IReadOnlyList<StyledSegment>)
                                                [
                                                    new StyledSegment (line, MarkdownStyleRole.CodeBlock, role: VisualRole.Code)
                                                ]));
        }

        _lines = styledLines;
        int width = _lines.Count == 0 ? 0 : _lines.Max (line => line.Sum (segment => segment.Text.GetColumns ()));
        SetContentSize (new Size (width, _lines.Count));
        SetNeedsLayout ();
        SetNeedsDraw ();
    }

    /// <inheritdoc/>
    protected override bool OnClearingViewport ()
    {
        if (base.OnClearingViewport ())
        {
            return true;
        }

        SetAttribute (GetAttributeForRole (VisualRole.Code));
        ClearViewport ();

        return true;
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        int endLine = Math.Min (Viewport.Y + Viewport.Height, _lines.Count);

        for (int lineIndex = Viewport.Y; lineIndex < endLine; lineIndex++)
        {
            DrawLine (_lines [lineIndex], lineIndex - Viewport.Y);
        }

        return true;
    }

    private void DrawLine (IReadOnlyList<StyledSegment> segments, int y)
    {
        var x = 0;

        foreach (StyledSegment segment in segments)
        {
            Attribute attr = MarkdownAttributeHelper.GetAttributeForSegment (this, segment);
            SetAttribute (attr);

            foreach (string grapheme in GraphemeHelper.GetGraphemes (segment.Text))
            {
                int graphemeWidth = Math.Max (grapheme.GetColumns (), 1);
                bool visible = x + graphemeWidth > Viewport.X && x < Viewport.X + Viewport.Width;

                if (!visible)
                {
                    x += graphemeWidth;

                    continue;
                }

                AddStr (x - Viewport.X, y, grapheme);
                x += graphemeWidth;
            }
        }
    }

    /// <inheritdoc/>
    bool IDesignable.EnableForDesign ()
    {
        Language = "cs";

        Text = """
               using IApplication app = Application.Create ().Init ();
               app.Run<MyWindow> ();
               """;

        return true;
    }
}
