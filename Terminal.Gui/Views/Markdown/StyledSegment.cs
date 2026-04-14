namespace Terminal.Gui.Views;

/// <summary>Identifies the semantic role of a styled text segment within a <see cref="MarkdownView"/>.</summary>
/// <remarks>
///     The role determines how the segment is rendered (font style, color, background).
///     See <see cref="MarkdownView"/> for the mapping of roles to visual attributes.
/// </remarks>
public enum MarkdownStyleRole
{
    /// <summary>Plain body text with no special formatting.</summary>
    Normal,

    /// <summary>Heading text (<c># … ######</c>). Rendered bold.</summary>
    Heading,

    /// <summary>Emphasized text (<c>*italic*</c>). Rendered italic.</summary>
    Emphasis,

    /// <summary>Strongly emphasized text (<c>**bold**</c>). Rendered bold.</summary>
    Strong,

    /// <summary>Inline code span (<c>`code`</c>). Rendered bold with a dimmed background.</summary>
    InlineCode,

    /// <summary>Fenced code block line. Rendered bold with a full-width dimmed background.</summary>
    CodeBlock,

    /// <summary>Block-quote text (<c>&gt; …</c>). Rendered faint.</summary>
    Quote,

    /// <summary>The bullet or number prefix of a list item (e.g. <c>•</c>). Rendered bold.</summary>
    ListMarker,

    /// <summary>Hyperlink text (<c>[text](url)</c>). Absolute URLs are underlined; anchor links are plain.</summary>
    Link,

    /// <summary>Table row text. Rendered bold.</summary>
    Table,

    /// <summary>Thematic break (<c>---</c>, <c>***</c>, <c>___</c>). Rendered faint.</summary>
    ThematicBreak,

    /// <summary>Alt-text for an image (<c>![alt](src)</c>). Rendered italic.</summary>
    ImageAlt,

    /// <summary>Completed task-list item (<c>[x]</c>). Rendered with strikethrough.</summary>
    TaskDone,

    /// <summary>Incomplete task-list item (<c>[ ]</c>). Rendered bold.</summary>
    TaskTodo
}

/// <summary>A segment of styled text produced by <see cref="MarkdownView"/> during layout.</summary>
/// <remarks>
///     Each segment carries the display text, a <see cref="MarkdownStyleRole"/> that controls
///     visual rendering, and optional URL / image-source metadata for hyperlinks and images.
/// </remarks>
public sealed class StyledSegment
{
    /// <summary>Initializes a new <see cref="StyledSegment"/>.</summary>
    /// <param name="text">The display text of the segment.</param>
    /// <param name="styleRole">The semantic role that controls rendering style.</param>
    /// <param name="url">Optional hyperlink URL. <see langword="null"/> for non-link segments.</param>
    /// <param name="imageSource">Optional image source path. <see langword="null"/> for non-image segments.</param>
    public StyledSegment (string text, MarkdownStyleRole styleRole, string? url = null, string? imageSource = null)
    {
        Text = text;
        StyleRole = styleRole;
        Url = url;
        ImageSource = imageSource;
    }

    /// <summary>Gets the display text of this segment.</summary>
    public string Text { get; }

    /// <summary>Gets the semantic role that determines how this segment is rendered.</summary>
    public MarkdownStyleRole StyleRole { get; }

    /// <summary>Gets the hyperlink URL, or <see langword="null"/> if this is not a link segment.</summary>
    public string? Url { get; }

    /// <summary>Gets the image source path, or <see langword="null"/> if this is not an image segment.</summary>
    public string? ImageSource { get; }
}
