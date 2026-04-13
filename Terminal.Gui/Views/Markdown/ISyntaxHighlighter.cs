namespace Terminal.Gui.Views;

public interface ISyntaxHighlighter
{
    IReadOnlyList<StyledSegment> Highlight (string code, string? language);
}
