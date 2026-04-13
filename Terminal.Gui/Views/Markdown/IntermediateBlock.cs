namespace Terminal.Gui.Views;

internal sealed class IntermediateBlock
{
    public IntermediateBlock (IReadOnlyList<InlineRun> runs, bool wrap, string prefix = "", string continuationPrefix = "")
    {
        Runs = runs;
        Wrap = wrap;
        Prefix = prefix;
        ContinuationPrefix = continuationPrefix;
    }

    public IReadOnlyList<InlineRun> Runs { get; }
    public bool Wrap { get; }
    public string Prefix { get; }
    public string ContinuationPrefix { get; }
}
