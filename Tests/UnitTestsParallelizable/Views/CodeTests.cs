// Copilot

namespace ViewsTests;

public class CodeTests
{
    [Fact]
    public void Text_Set_Renders_Highlighted_Role ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (10, 2);

        Attribute keywordAttr = new (Color.Blue, Color.Black, TextStyle.Bold);
        Scheme scheme = new ()
        {
            Normal = new Attribute (Color.White, Color.Black),
            CodeKeyword = keywordAttr
        };

        Code code = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Language = "cs",
            SyntaxHighlighter = new RoleHighlighter (VisualRole.CodeKeyword),
            Text = "keyword"
        };
        code.SetScheme (scheme);

        using Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        window.Add (code);

        app.Begin (window);
        app.LayoutAndDraw ();

        Cell [,]? contents = app.Driver.Contents;
        Assert.NotNull (contents);
        Assert.Equal (keywordAttr, contents! [0, 0].Attribute);
    }

    private sealed class RoleHighlighter (VisualRole role) : ISyntaxHighlighter
    {
        public IReadOnlyList<StyledSegment> Highlight (string code, string? language) => [new (code, MarkdownStyleRole.CodeBlock, role: role)];

        public void ResetState () { }

        public string ThemeName => string.Empty;

        public Color? DefaultBackground => null;

        public Attribute? GetAttributeForScope (MarkdownStyleRole role) => null;
    }
}
