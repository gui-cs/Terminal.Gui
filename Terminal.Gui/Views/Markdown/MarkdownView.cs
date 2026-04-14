using Markdig;

namespace Terminal.Gui.Views;

/// <summary>
/// 
/// </summary>
public partial class MarkdownView : View, IDesignable
{
    private const int MIN_WRAP_WIDTH = 4;

    private readonly List<IntermediateBlock> _blocks = [];
    private readonly List<RenderedLine> _renderedLines = [];
    private readonly List<MarkdownLinkRange> _linkRanges = [];
    private readonly HashSet<string> _queuedSixelIds = [];

    private string _markdown = string.Empty;
    private MarkdownPipeline? _markdownPipeline;
    private bool _parsed;
    private int _layoutWidth = -1;
    private int _maxLineWidth;

    /// <summary>
    /// 
    /// </summary>
    public MarkdownView ()
    {
        CanFocus = true;
        ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;

        SetupBindingsAndCommands ();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="markdown"></param>
    public MarkdownView (string markdown) : this () => Markdown = markdown;

    /// <summary>
    /// 
    /// </summary>
    public string Markdown
    {
        get => _markdown;
        set => SetMarkdown (value ?? string.Empty);
    }

    /// <summary>
    /// 
    /// </summary>
    public MarkdownPipeline? MarkdownPipeline
    {
        get => _markdownPipeline;
        set
        {
            if (ReferenceEquals (_markdownPipeline, value))
            {
                return;
            }

            _markdownPipeline = value;
            InvalidateParsedAndLayout ();
        }
    }

    public ISyntaxHighlighter? SyntaxHighlighter { get; set; }

    /// <summary>
    /// Optional image loader. Return UTF-8 encoded sixel payload bytes to render images inline.
    /// </summary>
    public Func<string, byte[]?>? ImageLoader { get; set; }

    public bool EnableSixelImages { get; set; }

    public int LineCount => _renderedLines.Count;

    public event EventHandler<MarkdownLinkEventArgs>? LinkClicked;
    public event EventHandler<EventArgs>? MarkdownChanged;

    protected virtual bool OnLinkClicked (MarkdownLinkEventArgs args) => false;

    protected virtual void OnMarkdownChanged () { }

    protected override void OnViewportChanged (DrawEventArgs e)
    {
        base.OnViewportChanged (e);

        if (_layoutWidth != Viewport.Width)
        {
            EnsureLayout ();
            SetNeedsDraw ();
        }
    }

    bool IDesignable.EnableForDesign ()
    {
        Markdown = "# MarkdownView\n\nVisit [Terminal.Gui](https://github.com/gui-cs/Terminal.Gui).\n\n- [x] Task\n- [ ] Todo\n\n```csharp\nConsole.WriteLine (\"Hello\");\n```";

        return true;
    }

    private void SetMarkdown (string value)
    {
        if (_markdown == value)
        {
            return;
        }

        _markdown = value;
        InvalidateParsedAndLayout ();

        OnMarkdownChanged ();
        MarkdownChanged?.Invoke (this, EventArgs.Empty);
    }

    private void InvalidateParsedAndLayout ()
    {
        _parsed = false;
        _layoutWidth = -1;
        _blocks.Clear ();
        _renderedLines.Clear ();
        _linkRanges.Clear ();
        _maxLineWidth = 0;

        SetNeedsLayout ();
        SetNeedsDraw ();
    }

    private void EnsureLayout ()
    {
        EnsureParsed ();

        if (_layoutWidth == Viewport.Width)
        {
            return;
        }

        BuildRenderedLines ();
        _layoutWidth = Viewport.Width;

        SetContentSize (new Size (Math.Max (_maxLineWidth, Viewport.Width), Math.Max (_renderedLines.Count, Viewport.Height)));

        ClampViewport ();
    }

    private void ClampViewport ()
    {
        Size contentSize = GetContentSize ();

        int maxY = Math.Max (contentSize.Height - Viewport.Height, 0);
        int maxX = Math.Max (contentSize.Width - Viewport.Width, 0);

        int newY = Math.Min (Math.Max (Viewport.Y, 0), maxY);
        int newX = Math.Min (Math.Max (Viewport.X, 0), maxX);

        if (newY == Viewport.Y && newX == Viewport.X)
        {
            return;
        }

        Viewport = Viewport with { Y = newY, X = newX };
    }

    private bool RaiseLinkClicked (string url)
    {
        MarkdownLinkEventArgs args = new (url);

        if (OnLinkClicked (args) || args.Handled)
        {
            return true;
        }

        LinkClicked?.Invoke (this, args);

        return args.Handled;
    }
}
