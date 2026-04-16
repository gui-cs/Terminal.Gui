namespace Terminal.Gui.Drawing;

/// <summary>
///     Resolves <see cref="MarkdownStyleRole"/> values to <see cref="Attribute"/> instances
///     for rendering styled Markdown content. Used by both <see cref="Markdown"/> and
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
    /// <param name="highlighter">
    ///     Optional syntax highlighter. When non-null, the highlighter is queried first for
    ///     a theme-derived attribute via <see cref="ISyntaxHighlighter.GetAttributeForScope"/>.
    /// </param>
    /// <param name="themeBackground">
    ///     When non-null, overrides the view's normal background with the given color.
    ///     Typically set to <see cref="ISyntaxHighlighter.DefaultBackground"/> when
    ///     <see cref="Markdown.UseThemeBackground"/> is <see langword="true"/>.
    /// </param>
    /// <returns>A fully resolved <see cref="Attribute"/> ready for drawing.</returns>
    public static Attribute GetAttributeForSegment (View view, StyledSegment segment, ISyntaxHighlighter? highlighter = null, Color? themeBackground = null)
    {
        if (segment.Attribute is { } explicitAttr)
        {
            return explicitAttr;
        }

        // Use the provided theme background, or fall back to the view's normal background.
        Attribute viewNormal = view.GetAttributeForRole (VisualRole.Normal);
        Color bg = themeBackground ?? viewNormal.Background;

        if (highlighter?.GetAttributeForScope (segment.StyleRole) is { } scopeAttr)
        {
            return scopeAttr with { Background = bg };
        }

        Attribute baseAttr = viewNormal with { Background = bg };

        return segment.StyleRole switch
               {
                   MarkdownStyleRole.Heading => baseAttr with { Style = baseAttr.Style | TextStyle.Bold },
                   MarkdownStyleRole.HeadingMarker => baseAttr with { Style = baseAttr.Style | TextStyle.Bold },
                   MarkdownStyleRole.Emphasis => baseAttr with { Style = baseAttr.Style | TextStyle.Italic },
                   MarkdownStyleRole.Strong => baseAttr with { Style = baseAttr.Style | TextStyle.Bold },
                   MarkdownStyleRole.InlineCode or MarkdownStyleRole.CodeBlock =>
                       themeBackground is { } codeBg
                           ? view.GetAttributeForRole (VisualRole.Code) with { Background = codeBg }
                           : view.GetAttributeForRole (VisualRole.Code),
                   MarkdownStyleRole.Link => MakeLinkAttribute (baseAttr, segment),
                   MarkdownStyleRole.Quote => baseAttr with { Style = baseAttr.Style | TextStyle.Faint },
                   MarkdownStyleRole.Table => baseAttr with { Style = baseAttr.Style | TextStyle.Bold },
                   MarkdownStyleRole.ThematicBreak => baseAttr with { Style = baseAttr.Style | TextStyle.Faint },
                   MarkdownStyleRole.ImageAlt => baseAttr with { Style = baseAttr.Style | TextStyle.Italic },
                   MarkdownStyleRole.TaskDone => baseAttr with { Style = baseAttr.Style | TextStyle.Strikethrough },
                   MarkdownStyleRole.TaskTodo => baseAttr with { Style = baseAttr.Style | TextStyle.Bold },
                   MarkdownStyleRole.ListMarker => baseAttr with { Style = baseAttr.Style | TextStyle.Bold },
                   _ => baseAttr
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
            segments.Add (new StyledSegment (run.Text, run.StyleRole, run.Url, run.ImageSource, run.Attribute));
        }

        return segments;
    }

    private static Attribute MakeLinkAttribute (Attribute normal, StyledSegment segment)
    {
        bool isClickable = !string.IsNullOrWhiteSpace (segment.Url)
                           && (Uri.IsWellFormedUriString (segment.Url, UriKind.Absolute) || segment.Url!.StartsWith ('#'));

        return isClickable ? normal with { Style = normal.Style | TextStyle.Underline } : normal;
    }
}
