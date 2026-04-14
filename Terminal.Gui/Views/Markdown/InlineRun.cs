namespace Terminal.Gui.Views;

internal sealed class InlineRun (string text, MarkdownStyleRole styleRole, string? url = null, string? imageSource = null)
{
    public string Text { get; } = text;
    public MarkdownStyleRole StyleRole { get; } = styleRole;
    public string? Url { get; } = url;
    public string? ImageSource { get; } = imageSource;
}
