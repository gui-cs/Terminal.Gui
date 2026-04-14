using JetBrains.Annotations;

namespace ViewsTests.Markdown;

[TestSubject (typeof (MarkdownCodeBlock))]
public class MarkdownCodeBlockTests
{
    // Copilot

    [Fact]
    public void Parameterless_Constructor_Creates_Empty_CodeBlock ()
    {
        // Copilot
        MarkdownCodeBlock codeBlock = new ();

        Assert.NotNull (codeBlock);
        Assert.Empty (codeBlock.CodeLines);
    }

    [Fact]
    public void IDesignable_EnableForDesign_Returns_True ()
    {
        // Copilot
        MarkdownCodeBlock codeBlock = new ();
        IDesignable designable = codeBlock;

        bool result = designable.EnableForDesign ();

        Assert.True (result);
        Assert.NotEmpty (codeBlock.CodeLines);
    }

    [Fact]
    public void CodeLines_Property_Sets_Content ()
    {
        // Copilot
        MarkdownCodeBlock codeBlock = new ();

        codeBlock.CodeLines = ["line1", "line2", "line3"];

        Assert.Equal (3, codeBlock.CodeLines.Count);
        Assert.Equal ("line1", codeBlock.CodeLines [0]);
        Assert.Equal ("line2", codeBlock.CodeLines [1]);
        Assert.Equal ("line3", codeBlock.CodeLines [2]);
    }

    [Fact]
    public void ExtractText_Returns_Joined_Lines ()
    {
        // Copilot
        MarkdownCodeBlock codeBlock = new ();
        codeBlock.CodeLines = ["Console.WriteLine (\"Hello\");", "var x = 42;"];

        string text = codeBlock.ExtractText ();

        Assert.Contains ("Console.WriteLine", text);
        Assert.Contains ("var x = 42;", text);
    }

    [Fact]
    public void Height_Updates_When_CodeLines_Set ()
    {
        // Copilot
        MarkdownCodeBlock codeBlock = new ()
        {
            Width = 40,
        };

        View host = new () { Width = 40, Height = 10 };
        host.Add (codeBlock);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        // Initially zero code lines, zero height
        Assert.Equal (0, codeBlock.Frame.Height);

        codeBlock.CodeLines = ["a", "b", "c"];
        host.Layout ();

        Assert.Equal (3, codeBlock.Frame.Height);
    }
}
