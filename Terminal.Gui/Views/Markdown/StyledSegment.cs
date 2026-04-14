namespace Terminal.Gui.Views;

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
