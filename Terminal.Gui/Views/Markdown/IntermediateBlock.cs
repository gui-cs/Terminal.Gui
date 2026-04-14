namespace Terminal.Gui.Views;

internal sealed class IntermediateBlock (IReadOnlyList<InlineRun> runs, bool wrap, string prefix = "", string continuationPrefix = "", bool isCodeBlock = false)
{
    public IReadOnlyList<InlineRun> Runs { get; } = runs;
    public bool Wrap { get; } = wrap;
    public string Prefix { get; } = prefix;
    public string ContinuationPrefix { get; } = continuationPrefix;
    public bool IsCodeBlock { get; } = isCodeBlock;
}
