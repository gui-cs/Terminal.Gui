namespace Terminal.Gui.Views;

internal sealed class InlineRun
{
    public InlineRun (string text, MarkdownStyleRole styleRole, string? url = null, string? imageSource = null)
    {
        Text = text;
        StyleRole = styleRole;
        Url = url;
        ImageSource = imageSource;
    }

    public string Text { get; }
    public MarkdownStyleRole StyleRole { get; }
    public string? Url { get; }
    public string? ImageSource { get; }
}
