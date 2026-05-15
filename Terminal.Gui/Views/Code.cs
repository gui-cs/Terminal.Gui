namespace Terminal.Gui.Views;

/// <summary>A read-only view that renders syntax-highlighted source code.</summary>
public class Code : View, IDesignable
{
    private string _text = string.Empty;
    private IReadOnlyList<IReadOnlyList<StyledSegment>> _lines = [];
    private string? _language;
    private ISyntaxHighlighter? _syntaxHighlighter = new TextMateSyntaxHighlighter ();

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
        get => _text;
        set
        {
            if (_text == value)
            {
                return;
            }

            _text = value ?? string.Empty;
            UpdateStyledLines ();
        }
    }

    /// <summary>Gets or sets the language hint used for syntax highlighting.</summary>
    public string? Language
    {
        get => _language;
        set
        {
            if (_language == value)
            {
                return;
            }

            _language = value;
            UpdateStyledLines ();
        }
    }

    /// <summary>Gets or sets the syntax highlighter used to tokenize <see cref="Text"/>.</summary>
    public ISyntaxHighlighter? SyntaxHighlighter
    {
        get => _syntaxHighlighter;
        set
        {
            if (ReferenceEquals (_syntaxHighlighter, value))
            {
                return;
            }

            _syntaxHighlighter = value;
            UpdateStyledLines ();
        }
    }

    private void UpdateStyledLines ()
    {
        string [] lines = _text.ReplaceLineEndings ("\n").Split ('\n');
        List<IReadOnlyList<StyledSegment>> styledLines = [];

        if (_syntaxHighlighter is { } highlighter && !string.IsNullOrEmpty (_language))
        {
            highlighter.ResetState ();

            foreach (string line in lines)
            {
                styledLines.Add (highlighter.Highlight (line, _language));
            }
        }
        else
        {
            foreach (string line in lines)
            {
                styledLines.Add ([new StyledSegment (line, MarkdownStyleRole.CodeBlock, role: VisualRole.Code)]);
            }
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
        int x = 0;

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
