namespace Terminal.Gui.Views;

internal sealed class RenderedLine (IReadOnlyList<StyledSegment> segments, bool wrapEligible, int width, bool isCodeBlock = false, bool isThematicBreak = false)
{
    public IReadOnlyList<StyledSegment> Segments { get; } = segments;
    public bool WrapEligible { get; } = wrapEligible;
    public int Width { get; } = width;
    public bool IsCodeBlock { get; } = isCodeBlock;
    public bool IsThematicBreak { get; } = isThematicBreak;
}
