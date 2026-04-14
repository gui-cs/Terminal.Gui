namespace Terminal.Gui.Views;

/// <summary>
///     Resolves <see cref="MarkdownStyleRole"/> values to <see cref="Attribute"/> instances
///     for rendering styled Markdown content. Used by both <see cref="MarkdownView"/> and
///     <see cref="MarkdownTable"/> to ensure consistent visual treatment of inline styles.
/// </summary>
internal static class MarkdownAttributeHelper
{
    /// <summary>
    ///     Returns the <see cref="Attribute"/> that should be used to render a
    ///     <see cref="StyledSegment"/> based on its <see cref="MarkdownStyleRole"/>.
    /// </summary>
    /// <param name="view">
    ///     The <see cref="View"/> whose scheme provides the base <see cref="Attribute"/>
    ///     via <see cref="View.GetAttributeForRole"/>.
    /// </param>
    /// <param name="segment">The styled segment to resolve an attribute for.</param>
    /// <returns>A fully resolved <see cref="Attribute"/> ready for drawing.</returns>
    public static Attribute GetAttributeForSegment (View view, StyledSegment segment)
    {
        Attribute normal = view.GetAttributeForRole (VisualRole.Normal);

        return segment.StyleRole switch
        {
            MarkdownStyleRole.Heading => normal with { Style = normal.Style | TextStyle.Bold },
            MarkdownStyleRole.Emphasis => normal with { Style = normal.Style | TextStyle.Italic },
            MarkdownStyleRole.Strong => normal with { Style = normal.Style | TextStyle.Bold },
            MarkdownStyleRole.InlineCode or MarkdownStyleRole.CodeBlock => MakeCodeAttribute (normal),
            MarkdownStyleRole.Link => MakeLinkAttribute (normal, segment),
            MarkdownStyleRole.Quote => normal with { Style = normal.Style | TextStyle.Faint },
            MarkdownStyleRole.Table => normal with { Style = normal.Style | TextStyle.Bold },
            MarkdownStyleRole.ThematicBreak => normal with { Style = normal.Style | TextStyle.Faint },
            MarkdownStyleRole.ImageAlt => normal with { Style = normal.Style | TextStyle.Italic },
            MarkdownStyleRole.TaskDone => normal with { Style = normal.Style | TextStyle.Strikethrough },
            MarkdownStyleRole.TaskTodo => normal with { Style = normal.Style | TextStyle.Bold },
            MarkdownStyleRole.ListMarker => normal with { Style = normal.Style | TextStyle.Bold },
            _ => normal
        };
    }

    /// <summary>
    ///     Converts a list of <see cref="InlineRun"/> (from parsing) into
    ///     <see cref="StyledSegment"/> instances suitable for rendering.
    /// </summary>
    public static List<StyledSegment> ToStyledSegments (IReadOnlyList<InlineRun> runs)
    {
        List<StyledSegment> segments = new (runs.Count);

        foreach (InlineRun run in runs)
        {
            segments.Add (new StyledSegment (run.Text, run.StyleRole, run.Url, run.ImageSource));
        }

        return segments;
    }

    private static Attribute MakeCodeAttribute (Attribute normal)
    {
        Color codeBg = normal.Background.GetDimmerColor ();

        return new Attribute (normal.Foreground, codeBg) { Style = normal.Style | TextStyle.Bold };
    }

    private static Attribute MakeLinkAttribute (Attribute normal, StyledSegment segment)
    {
        bool isClickable = !string.IsNullOrWhiteSpace (segment.Url)
                           && (Uri.IsWellFormedUriString (segment.Url, UriKind.Absolute) || segment.Url!.StartsWith ('#'));

        return isClickable ? normal with { Style = normal.Style | TextStyle.Underline } : normal;
    }
}
