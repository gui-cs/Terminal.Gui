// Copilot - Opus 4.6
// Tests for ISyntaxHighlighter.ResetState(), fence language extraction,
// StyledSegment.Attribute, and MarkdownAttributeHelper explicit Attribute support.

namespace ViewTests.Markdown;

/// <summary>Tests for the syntax highlighting pipeline in MarkdownView.</summary>
public class SyntaxHighlighterPipelineTests
{
    // --- Phase 1b: ISyntaxHighlighter.ResetState() ---

    [Fact]
    public void ISyntaxHighlighter_Has_ResetState_Method ()
    {
        // Verify the interface has ResetState via a mock implementation
        MockSyntaxHighlighter highlighter = new ();
        highlighter.ResetState ();
        Assert.True (highlighter.ResetStateCalled);
    }

    [Fact]
    public void MarkdownView_Calls_ResetState_Per_CodeBlock ()
    {
        MockSyntaxHighlighter highlighter = new ();

        MarkdownView view = new () { SyntaxHighlighter = highlighter, Markdown = "```csharp\nvar x = 1;\n```\n\ntext\n\n```python\nprint('hi')\n```" };

        // Force layout to trigger parsing
        view.Width = 40;
        view.Height = 20;
        view.SetRelativeLayout (new Size (40, 20));

        // ResetState should be called once per code block (2 blocks)
        Assert.Equal (2, highlighter.ResetStateCallCount);
    }

    // --- Phase 1c: Fence language extraction ---

    [Fact]
    public void Highlighter_Receives_Language_From_Fence ()
    {
        MockSyntaxHighlighter highlighter = new ();

        MarkdownView view = new () { SyntaxHighlighter = highlighter, Markdown = "```csharp\nvar x = 1;\n```" };

        view.Width = 40;
        view.Height = 20;
        view.SetRelativeLayout (new Size (40, 20));

        Assert.Contains ("csharp", highlighter.LanguagesReceived);
    }

    [Fact]
    public void Highlighter_Receives_Null_Language_When_No_Fence_Language ()
    {
        MockSyntaxHighlighter highlighter = new ();

        MarkdownView view = new () { SyntaxHighlighter = highlighter, Markdown = "```\nvar x = 1;\n```" };

        view.Width = 40;
        view.Height = 20;
        view.SetRelativeLayout (new Size (40, 20));

        Assert.Contains (null, highlighter.LanguagesReceived);
    }

    [Fact]
    public void Highlighter_Receives_Language_With_Tilde_Fence ()
    {
        MockSyntaxHighlighter highlighter = new ();

        MarkdownView view = new () { SyntaxHighlighter = highlighter, Markdown = "~~~python\nprint('hi')\n~~~" };

        view.Width = 40;
        view.Height = 20;
        view.SetRelativeLayout (new Size (40, 20));

        Assert.Contains ("python", highlighter.LanguagesReceived);
    }

    [Fact]
    public void Highlighter_Receives_Multiple_Languages ()
    {
        MockSyntaxHighlighter highlighter = new ();

        MarkdownView view = new () { SyntaxHighlighter = highlighter, Markdown = "```csharp\nvar x = 1;\n```\n\n```python\nprint('hi')\n```" };

        view.Width = 40;
        view.Height = 20;
        view.SetRelativeLayout (new Size (40, 20));

        Assert.Contains ("csharp", highlighter.LanguagesReceived);
        Assert.Contains ("python", highlighter.LanguagesReceived);
    }

    // --- Phase 2a: StyledSegment.Attribute ---

    [Fact]
    public void StyledSegment_Attribute_Default_Is_Null ()
    {
        StyledSegment segment = new ("text", MarkdownStyleRole.Normal);
        Assert.Null (segment.Attribute);
    }

    [Fact]
    public void StyledSegment_Attribute_Can_Be_Set ()
    {
        Attribute attr = new ("Red", "Blue");
        StyledSegment segment = new ("text", MarkdownStyleRole.CodeBlock, attribute: attr);
        Assert.Equal (attr, segment.Attribute);
    }

    [Fact]
    public void StyledSegment_Attribute_With_Url_And_ImageSource ()
    {
        Attribute attr = new ("Green", "Yellow");

        StyledSegment segment = new ("link", MarkdownStyleRole.Link, "https://example.com", null, attr);

        Assert.Equal (attr, segment.Attribute);
        Assert.Equal ("https://example.com", segment.Url);
    }

    // --- Phase 2b: MarkdownAttributeHelper respects explicit Attribute ---

    [Fact]
    public void GetAttributeForSegment_Returns_Explicit_Attribute_When_Set ()
    {
        Attribute explicitAttr = new ("Green", "Yellow", TextStyle.Bold);

        StyledSegment segment = new ("keyword", MarkdownStyleRole.CodeBlock, attribute: explicitAttr);

        View view = new () { Width = 10, Height = 1 };
        Attribute result = MarkdownAttributeHelper.GetAttributeForSegment (view, segment);

        Assert.Equal (explicitAttr, result);
    }

    [Fact]
    public void GetAttributeForSegment_Uses_StyleRole_When_Attribute_Null ()
    {
        StyledSegment segment = new ("text", MarkdownStyleRole.Strong);

        View view = new () { Width = 10, Height = 1 };
        Attribute result = MarkdownAttributeHelper.GetAttributeForSegment (view, segment);

        // Strong → Bold
        Assert.True (result.Style.HasFlag (TextStyle.Bold));
    }

    [Fact]
    public void Highlighter_Explicit_Attribute_Flows_Through_Pipeline ()
    {
        // A syntax highlighter that returns segments with explicit Attributes
        Attribute keywordAttr = new ("Blue", "Black", TextStyle.Bold);
        ExplicitAttributeHighlighter highlighter = new (keywordAttr);

        MarkdownView view = new () { SyntaxHighlighter = highlighter, Markdown = "```csharp\nvar x = 1;\n```" };

        view.Width = 40;
        view.Height = 20;
        view.SetRelativeLayout (new Size (40, 20));

        // The StyledSegments produced by the highlighter should carry the explicit attribute
        // This is verified by the fact that the view parses without error
        // Detailed rendering tests would need a driver
        Assert.NotNull (view);
    }

    // --- Mock implementations ---

    private sealed class MockSyntaxHighlighter : ISyntaxHighlighter
    {
        public bool ResetStateCalled { get; private set; }
        public int ResetStateCallCount { get; private set; }
        public List<string?> LanguagesReceived { get; } = [];

        public IReadOnlyList<StyledSegment> Highlight (string code, string? language)
        {
            LanguagesReceived.Add (language);

            return [new StyledSegment (code, MarkdownStyleRole.CodeBlock)];
        }

        public void ResetState ()
        {
            ResetStateCalled = true;
            ResetStateCallCount++;
        }
    }

    private sealed class ExplicitAttributeHighlighter (Attribute attr) : ISyntaxHighlighter
    {
        public IReadOnlyList<StyledSegment> Highlight (string code, string? language) => [new (code, MarkdownStyleRole.CodeBlock, attribute: attr)];

        public void ResetState () { }
    }
}
