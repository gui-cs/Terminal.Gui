namespace Terminal.Gui.Views;

internal sealed class IntermediateBlock (IReadOnlyList<InlineRun> runs, bool wrap, string prefix = "", string continuationPrefix = "", bool isCodeBlock = false, string? anchor = null)
{
    public IReadOnlyList<InlineRun> Runs { get; } = runs;
    public bool Wrap { get; } = wrap;
    public string Prefix { get; } = prefix;
    public string ContinuationPrefix { get; } = continuationPrefix;
    public bool IsCodeBlock { get; } = isCodeBlock;

    /// <summary>The GitHub-style anchor slug for heading blocks, or <see langword="null"/> for non-heading blocks.</summary>
    public string? Anchor { get; } = anchor;
}
