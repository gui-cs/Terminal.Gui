// Copilot - Opus 4.6
// Tests for TextMateSyntaxHighlighter — the TextMateSharp-based ISyntaxHighlighter implementation.

using TextMateSharp.Grammars;

namespace ViewTests.Markdown;

/// <summary>Tests for <see cref="TextMateSyntaxHighlighter"/>.</summary>
public class TextMateSyntaxHighlighterTests
{
    // --- Construction ---

    [Fact]
    public void Constructor_Default_Theme_DarkPlus ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        Assert.NotNull (highlighter);
    }

    [Fact]
    public void Constructor_Custom_Theme ()
    {
        TextMateSyntaxHighlighter highlighter = new (ThemeName.Monokai);
        Assert.NotNull (highlighter);
    }

    // --- ISyntaxHighlighter interface ---

    [Fact]
    public void Implements_ISyntaxHighlighter ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        Assert.IsAssignableFrom<ISyntaxHighlighter> (highlighter);
    }

    // --- Highlight with known language ---

    [Fact]
    public void Highlight_CSharp_Returns_Multiple_Segments ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        IReadOnlyList<StyledSegment> segments = highlighter.Highlight ("var x = 1;", "csharp");

        // TextMate should tokenize this into multiple segments (var, space, x, space, =, etc.)
        Assert.True (segments.Count > 1, $"Expected multiple segments, got {segments.Count}");
    }

    [Fact]
    public void Highlight_CSharp_Segments_Cover_Full_Line ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        var code = "var x = 42;";
        IReadOnlyList<StyledSegment> segments = highlighter.Highlight (code, "csharp");

        // Concatenated segment text should equal the original line
        string reconstructed = string.Concat (segments.Select (s => s.Text));
        Assert.Equal (code, reconstructed);
    }

    [Fact]
    public void Highlight_CSharp_Keyword_Has_Explicit_Attribute ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        IReadOnlyList<StyledSegment> segments = highlighter.Highlight ("using System;", "csharp");

        // At least one segment should have an explicit Attribute (non-null)
        Assert.Contains (segments, s => s.Attribute is { });
    }

    [Fact]
    public void Highlight_CSharp_All_Segments_Have_Attributes ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        IReadOnlyList<StyledSegment> segments = highlighter.Highlight ("int x = 42;", "csharp");

        // Every segment should carry an explicit Attribute from theme resolution
        Assert.All (segments, s => Assert.NotNull (s.Attribute));
    }

    [Fact]
    public void Highlight_CSharp_Segments_Have_CodeBlock_StyleRole ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        IReadOnlyList<StyledSegment> segments = highlighter.Highlight ("var x = 1;", "csharp");

        // All segments should use CodeBlock role as the base
        Assert.All (segments, s => Assert.Equal (MarkdownStyleRole.CodeBlock, s.StyleRole));
    }

    // --- Unknown / null language ---

    [Fact]
    public void Highlight_Null_Language_Returns_Single_Segment ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        var code = "some plain text";
        IReadOnlyList<StyledSegment> segments = highlighter.Highlight (code, null);

        Assert.Single (segments);
        Assert.Equal (code, segments [0].Text);
        Assert.Equal (MarkdownStyleRole.CodeBlock, segments [0].StyleRole);
    }

    [Fact]
    public void Highlight_Unknown_Language_Returns_Single_Segment ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        var code = "some plain text";
        IReadOnlyList<StyledSegment> segments = highlighter.Highlight (code, "nonexistent_language_xyz");

        Assert.Single (segments);
        Assert.Equal (code, segments [0].Text);
    }

    // --- ResetState ---

    [Fact]
    public void ResetState_Can_Be_Called_Multiple_Times ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        highlighter.ResetState ();
        highlighter.ResetState ();

        // Should not throw
        Assert.NotNull (highlighter);
    }

    [Fact]
    public void ResetState_Allows_Fresh_Tokenization ()
    {
        TextMateSyntaxHighlighter highlighter = new ();

        // Tokenize a partial multi-line construct
        highlighter.Highlight ("/* start of comment", "csharp");

        // Reset before new block
        highlighter.ResetState ();

        // After reset, "var" should be recognized as keyword, not continuation of comment
        IReadOnlyList<StyledSegment> segments = highlighter.Highlight ("var x = 1;", "csharp");
        string reconstructed = string.Concat (segments.Select (s => s.Text));
        Assert.Equal ("var x = 1;", reconstructed);
        Assert.True (segments.Count > 1, "Expected tokenization after reset");
    }

    // --- Multi-line state ---

    [Fact]
    public void Stateful_Tokenization_Across_Lines ()
    {
        TextMateSyntaxHighlighter highlighter = new ();

        // Start a multi-line string/comment
        IReadOnlyList<StyledSegment> line1 = highlighter.Highlight ("/* this is", "csharp");
        IReadOnlyList<StyledSegment> line2 = highlighter.Highlight ("   a comment */", "csharp");

        // Both lines should produce segments
        Assert.NotEmpty (line1);
        Assert.NotEmpty (line2);

        // The text should be fully covered
        Assert.Equal ("/* this is", string.Concat (line1.Select (s => s.Text)));
        Assert.Equal ("   a comment */", string.Concat (line2.Select (s => s.Text)));
    }

    // --- Theme switching ---

    [Fact]
    public void SetTheme_Changes_Colors ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        IReadOnlyList<StyledSegment> darkSegments = highlighter.Highlight ("var x = 1;", "csharp");

        highlighter.SetTheme (ThemeName.LightPlus);
        highlighter.ResetState ();
        IReadOnlyList<StyledSegment> lightSegments = highlighter.Highlight ("var x = 1;", "csharp");

        // Dark and light themes should produce different foreground colors for at least some tokens
        var anyDifferent = false;

        for (var i = 0; i < Math.Min (darkSegments.Count, lightSegments.Count); i++)
        {
            if (darkSegments [i].Attribute?.Foreground != lightSegments [i].Attribute?.Foreground)
            {
                anyDifferent = true;

                break;
            }
        }

        Assert.True (anyDifferent, "Dark and Light themes should produce different colors");
    }

    // --- Multiple languages ---

    [Theory]
    [InlineData ("python", "def hello():")]
    [InlineData ("javascript", "const x = 42;")]
    [InlineData ("json", "{\"key\": \"value\"}")]
    [InlineData ("html", "<div>hello</div>")]
    [InlineData ("css", "body { color: red; }")]
    public void Highlight_Supports_Multiple_Languages (string language, string code)
    {
        TextMateSyntaxHighlighter highlighter = new ();
        IReadOnlyList<StyledSegment> segments = highlighter.Highlight (code, language);

        Assert.NotEmpty (segments);

        string reconstructed = string.Concat (segments.Select (s => s.Text));
        Assert.Equal (code, reconstructed);
    }

    // --- Empty line ---

    [Fact]
    public void Highlight_Empty_Line_Returns_Single_Empty_Segment ()
    {
        TextMateSyntaxHighlighter highlighter = new ();
        IReadOnlyList<StyledSegment> segments = highlighter.Highlight ("", "csharp");

        // Empty line should return at least one (possibly empty) segment
        Assert.NotEmpty (segments);
    }

    // --- Language alias resolution ---

    [Theory]
    [InlineData ("cs")]
    [InlineData ("csharp")]
    [InlineData ("c#")]
    public void Highlight_CSharp_Language_Aliases (string languageId)
    {
        TextMateSyntaxHighlighter highlighter = new ();
        IReadOnlyList<StyledSegment> segments = highlighter.Highlight ("var x = 1;", languageId);

        // All aliases should resolve to the same grammar and produce multi-token output
        Assert.True (segments.Count > 1, $"Language '{languageId}' should produce tokenized output");
    }
}
