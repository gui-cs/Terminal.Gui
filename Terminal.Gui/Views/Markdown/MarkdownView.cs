using System.Text.RegularExpressions;
using Markdig;

namespace Terminal.Gui.Views;

/// <summary>
///     A read-only view that renders Markdown-formatted text with styled headings, lists, links, code blocks, and
///     more.
/// </summary>
/// <remarks>
///     <para>
///         Set the <see cref="Markdown"/> property to supply content. The view parses the Markdown,
///         performs word-wrap layout, and draws styled output. Fenced code blocks receive a
///         full-width dimmed background; inline code, emphasis, strong, and other elements are
///         rendered with appropriate text styles and colors.
///     </para>
///     <para>
///         Hyperlinks raise the <see cref="LinkClicked"/> event. Anchor links (URLs beginning with
///         <c>#</c>) are handled automatically by scrolling to the matching heading.
///     </para>
///     <para>
///         By default, the view uses the <see cref="Schemes.Dialog"/> scheme so that it has a
///         non-transparent background. A vertical scroll bar is enabled by default.
///     </para>
/// </remarks>
public partial class MarkdownView : View, IDesignable
{
    private const int MIN_WRAP_WIDTH = 4;

    private readonly List<IntermediateBlock> _blocks = [];
    private readonly List<RenderedLine> _renderedLines = [];
    private readonly List<MarkdownLinkRange> _linkRanges = [];
    private readonly HashSet<string> _queuedSixelIds = [];
    private readonly Dictionary<string, int> _headingAnchors = new (StringComparer.OrdinalIgnoreCase);

    private string _markdown = string.Empty;
    private bool _parsed;
    private int _layoutWidth = -1;
    private int _maxLineWidth;

    /// <summary>Initializes a new instance of the <see cref="MarkdownView"/> class with no content.</summary>
    public MarkdownView ()
    {
        CanFocus = true;
        SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Dialog);
        ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;

        SetupBindingsAndCommands ();
    }

    /// <summary>Initializes a new instance of the <see cref="MarkdownView"/> class with the specified Markdown text.</summary>
    /// <param name="markdown">The Markdown-formatted string to render.</param>
    public MarkdownView (string markdown) : this () => Markdown = markdown;

    /// <summary>Gets or sets the Markdown-formatted text displayed by this view.</summary>
    /// <value>The raw Markdown string. Setting this property triggers reparsing, re-layout, and a redraw.</value>
    public string Markdown { get => _markdown; set => SetMarkdown (value); }

    /// <summary>Gets or sets the Markdig <see cref="Markdig.MarkdownPipeline"/> used for parsing.</summary>
    /// <value>
    ///     A custom pipeline, or <see langword="null"/> to use the default pipeline
    ///     (built with <c>UseAdvancedExtensions</c>).
    /// </value>
    public MarkdownPipeline? MarkdownPipeline
    {
        get;
        set
        {
            if (ReferenceEquals (field, value))
            {
                return;
            }

            field = value;
            InvalidateParsedAndLayout ();
        }
    }

    /// <summary>Gets or sets an optional syntax highlighter for fenced code blocks.</summary>
    /// <value>An <see cref="ISyntaxHighlighter"/> implementation, or <see langword="null"/> for plain-text code blocks.</value>
    public ISyntaxHighlighter? SyntaxHighlighter { get; set; }

    /// <summary>Gets or sets an optional callback that loads image data as UTF-8 encoded sixel payloads.</summary>
    /// <value>A function that accepts an image source path and returns sixel bytes, or <see langword="null"/>.</value>
    public Func<string, byte []?>? ImageLoader { get; set; }

    /// <summary>Gets or sets whether sixel image rendering is enabled.</summary>
    /// <value><see langword="true"/> to attempt sixel rendering for images; otherwise <see langword="false"/>.</value>
    public bool EnableSixelImages { get; set; }

    /// <summary>Gets the total number of rendered lines after parsing and word-wrap layout.</summary>
    public int LineCount => _renderedLines.Count;

    /// <summary>
    ///     Raised when a hyperlink is clicked. Set <see cref="MarkdownLinkEventArgs.Handled"/> to prevent default
    ///     navigation.
    /// </summary>
    public event EventHandler<MarkdownLinkEventArgs>? LinkClicked;

    /// <summary>Raised after the <see cref="Markdown"/> property changes and the content has been reparsed.</summary>
    public event EventHandler<EventArgs>? MarkdownChanged;

    /// <summary>Called when a hyperlink is clicked, before the <see cref="LinkClicked"/> event is raised.</summary>
    /// <param name="args">The event data containing the link URL.</param>
    /// <returns><see langword="true"/> if the link click was handled and no further processing should occur.</returns>
    protected virtual bool OnLinkClicked (MarkdownLinkEventArgs args) => false;

    /// <summary>Called after the <see cref="Markdown"/> property changes, before <see cref="MarkdownChanged"/> is raised.</summary>
    protected virtual void OnMarkdownChanged () { }

    /// <inheritdoc/>
    protected override void OnViewportChanged (DrawEventArgs e)
    {
        base.OnViewportChanged (e);

        if (_layoutWidth == Viewport.Width)
        {
            return;
        }
        EnsureLayout ();
        SetNeedsDraw ();
    }

    /// <summary>Scrolls the viewport so that the heading matching the given anchor slug is visible at the top.</summary>
    /// <param name="anchor">
    ///     The anchor identifier (with or without a leading <c>#</c>). Anchors are generated from heading text
    ///     using GitHub-style slug rules: lowercase, spaces become hyphens, non-alphanumeric characters are removed,
    ///     and duplicate headings receive <c>-1</c>, <c>-2</c>, etc. suffixes.
    /// </param>
    /// <returns>
    ///     <see langword="true"/> if a matching heading was found and the viewport was scrolled; otherwise
    ///     <see langword="false"/>.
    /// </returns>
    public bool ScrollToAnchor (string anchor)
    {
        if (string.IsNullOrEmpty (anchor))
        {
            return false;
        }

        EnsureLayout ();

        string slug = anchor.StartsWith ('#') ? anchor [1..] : anchor;

        if (!_headingAnchors.TryGetValue (slug, out int lineIndex))
        {
            return false;
        }

        Viewport = Viewport with { Y = Math.Min (lineIndex, Math.Max (_renderedLines.Count - Viewport.Height, 0)) };
        SetNeedsDraw ();

        return true;
    }

    /// <inheritdoc/>
    bool IDesignable.EnableForDesign ()
    {
        Markdown =
            "# MarkdownView\n\nVisit [Terminal.Gui](https://github.com/gui-cs/Terminal.Gui).\n\n- [x] Task\n- [ ] Todo\n\n```csharp\nConsole.WriteLine (\"Hello\");\n```";

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
        _headingAnchors.Clear ();
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

    /// <summary>Generates a GitHub-style anchor slug from heading text.</summary>
    internal static string GenerateAnchorSlug (string headingText)
    {
        string lower = headingText.Trim ().ToLowerInvariant ();

        // Remove non-alphanumeric, non-hyphen, non-space, non-underscore chars
        string slug = Regex.Replace (lower, @"[^\w\s-]", "", RegexOptions.None);

        // Replace whitespace runs with a single hyphen
        slug = Regex.Replace (slug, @"\s+", "-");

        return slug.Trim ('-');
    }
}
