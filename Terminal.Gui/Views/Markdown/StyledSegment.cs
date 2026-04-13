namespace Terminal.Gui.Views;

public enum MarkdownStyleRole
{
    Normal,
    Heading,
    Emphasis,
    Strong,
    InlineCode,
    CodeBlock,
    Quote,
    ListMarker,
    Link,
    Table,
    ThematicBreak,
    ImageAlt,
    TaskDone,
    TaskTodo
}

public sealed class StyledSegment
{
    public StyledSegment (string text, MarkdownStyleRole styleRole, string? url = null, string? imageSource = null)
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
