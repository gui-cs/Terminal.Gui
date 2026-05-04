namespace Terminal.Gui.Views;

internal sealed class RenderedLine (IReadOnlyList<StyledSegment> segments, bool wrapEligible, int width, bool isCodeBlock = false, bool isThematicBreak = false, bool isTable = false, string? codeLanguage = null, TableData? tableData = null)
{
    public IReadOnlyList<StyledSegment> Segments { get; } = segments;
    public bool WrapEligible { get; } = wrapEligible;
    public int Width { get; } = width;
    public bool IsCodeBlock { get; } = isCodeBlock;
    public bool IsThematicBreak { get; } = isThematicBreak;
    public bool IsTable { get; } = isTable;

    /// <summary>
    ///     The fenced code-block language specifier (e.g. <c>"cs"</c>), or <see langword="null"/>
    ///     when this line is not part of a code block or no language was given.
    /// </summary>
    public string? CodeLanguage { get; } = codeLanguage;

    /// <summary>
    ///     The parsed table data when <see cref="IsTable"/> is <see langword="true"/>;
    ///     otherwise <see langword="null"/>.  Used to reconstruct pipe-table markdown
    ///     when this line falls within a partial selection.
    /// </summary>
    public TableData? TableData { get; } = tableData;
}
