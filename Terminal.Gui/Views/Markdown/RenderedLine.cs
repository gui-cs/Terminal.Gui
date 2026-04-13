namespace Terminal.Gui.Views;

internal sealed class RenderedLine
{
    public RenderedLine (IReadOnlyList<StyledSegment> segments, bool wrapEligible, int width)
    {
        Segments = segments;
        WrapEligible = wrapEligible;
        Width = width;
    }

    public IReadOnlyList<StyledSegment> Segments { get; }
    public bool WrapEligible { get; }
    public int Width { get; }
}
