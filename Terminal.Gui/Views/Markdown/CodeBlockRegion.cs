namespace Terminal.Gui.Views;

/// <summary>Describes a contiguous region of code block lines within a <see cref="MarkdownView"/>.</summary>
/// <remarks>
///     Used internally to track where code blocks are located after layout so that copy buttons
///     can be drawn and click targets can be detected.
/// </remarks>
internal readonly struct CodeBlockRegion
{
    /// <summary>Initializes a new <see cref="CodeBlockRegion"/>.</summary>
    /// <param name="startLine">The inclusive rendered-line index where the code block begins.</param>
    /// <param name="endLineExclusive">The exclusive rendered-line index where the code block ends.</param>
    public CodeBlockRegion (int startLine, int endLineExclusive)
    {
        StartLine = startLine;
        EndLineExclusive = endLineExclusive;
    }

    /// <summary>Gets the inclusive rendered-line index where the code block begins.</summary>
    public int StartLine { get; }

    /// <summary>Gets the exclusive rendered-line index where the code block ends.</summary>
    public int EndLineExclusive { get; }

    /// <summary>Extracts the plain text of this code block from the given rendered lines.</summary>
    public string ExtractText (IReadOnlyList<RenderedLine> renderedLines)
    {
        List<string> lineTexts = [];

        for (int i = StartLine; i < EndLineExclusive && i < renderedLines.Count; i++)
        {
            string lineText = string.Concat (renderedLines [i].Segments.Select (s => s.Text));
            lineTexts.Add (lineText);
        }

        return string.Join (Environment.NewLine, lineTexts);
    }
}
