namespace Terminal.Gui.Views;

/// <summary>Provides syntax highlighting for fenced code blocks in <see cref="MarkdownView"/>.</summary>
/// <remarks>
///     Assign an implementation to <see cref="MarkdownView.SyntaxHighlighter"/> to colorize
///     code blocks. Each line of the code block is passed individually to <see cref="Highlight"/>.
/// </remarks>
public interface ISyntaxHighlighter
{
    /// <summary>Highlights a single line of code and returns styled segments.</summary>
    /// <param name="code">The source code line to highlight.</param>
    /// <param name="language">The language identifier from the fence (e.g. <c>csharp</c>), or <see langword="null"/> if not specified.</param>
    /// <returns>A list of <see cref="StyledSegment"/> instances representing the highlighted tokens.</returns>
    IReadOnlyList<StyledSegment> Highlight (string code, string? language);
}
