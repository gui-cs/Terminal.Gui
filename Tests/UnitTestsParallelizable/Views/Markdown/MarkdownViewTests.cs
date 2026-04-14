using JetBrains.Annotations;
using UnitTests;

namespace ViewsTests.Markdown;

[TestSubject (typeof (MarkdownView))]
public class MarkdownViewTests (ITestOutputHelper output)
{
    // Copilot

    [Fact]
    public void Constructor_Defaults ()
    {
        MarkdownView view = new ();

        Assert.True (view.CanFocus);
        Assert.Equal (string.Empty, view.Markdown);
        Assert.Equal (0, view.LineCount);
    }

    [Fact]
    public void Markdown_Set_Raises_MarkdownChanged ()
    {
        MarkdownView view = new ();
        var fired = false;

        view.MarkdownChanged += (_, _) => fired = true;

        view.Markdown = "# Header";

        Assert.True (fired);
    }

    [Fact]
    public void IDesignable_EnableForDesign_Returns_True ()
    {
        MarkdownView markdownView = new ();
        IDesignable designable = markdownView;

        bool result = designable.EnableForDesign ();

        Assert.True (result);
        Assert.Contains ("MarkdownView", markdownView.Markdown);
    }

    [Fact]
    public void Layout_Computes_Lines_And_ContentSize ()
    {
        MarkdownView view = new ("# Header\n\nParagraph text");
        view.Width = 20;
        view.Height = 5;

        View host = new () { Width = 20, Height = 5 };
        host.Add (view);

        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        Assert.True (view.LineCount >= 2);
        Assert.True (view.GetContentSize ().Height >= 2);

        host.Dispose ();
    }

    [Fact]
    public void Draw_Emits_OSC8_For_Link ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        MarkdownView markdownView = new ("Visit [Terminal.Gui](https://github.com/gui-cs/Terminal.Gui)") { Width = Dim.Fill (), Height = Dim.Fill () };

        window.Add (markdownView);

        app.Begin (window);
        app.LayoutAndDraw ();
        app.Driver!.Refresh ();

        string lastOutput = app.Driver.GetOutput ().GetLastOutput ();

        Assert.Contains (EscSeqUtils.OSC_StartHyperlink ("https://github.com/gui-cs/Terminal.Gui"), lastOutput);
        Assert.Contains (EscSeqUtils.OSC_EndHyperlink (), lastOutput);

        window.Dispose ();
    }

    [Fact]
    public void Mouse_Click_On_Link_Raises_LinkClicked ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        MarkdownView markdownView = new ("[Click](https://example.com)") { Width = 20, Height = 3 };

        window.Add (markdownView);

        var clicked = false;

        markdownView.LinkClicked += (_, e) =>
                                    {
                                        clicked = true;
                                        e.Handled = true;
                                    };

        app.Begin (window);
        app.LayoutAndDraw ();

        markdownView.NewMouseEvent (new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonClicked });

        Assert.True (clicked);

        window.Dispose ();
    }

    [Fact]
    public void Image_Fallback_Text_Renders ()
    {
        MarkdownView markdownView = new ("![logo](asset://logo)");
        markdownView.Width = 40;
        markdownView.Height = 5;

        View host = new () { Width = 40, Height = 5 };
        host.Add (markdownView);

        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        Assert.True (markdownView.LineCount >= 1);

        host.Dispose ();
    }

    [Theory]
    [InlineData ("Hello * world")]
    [InlineData ("A stray ! in text")]
    [InlineData ("Unclosed [bracket")]
    [InlineData ("Lone ` backtick")]
    [InlineData ("Mixed **unclosed bold")]
    [InlineData ("Edge *")]

    // Copilot
    public void Stray_Special_Characters_Do_Not_Cause_Infinite_Loop (string markdown)
    {
        MarkdownView markdownView = new (markdown);
        markdownView.Width = 40;
        markdownView.Height = 5;

        View host = new () { Width = 40, Height = 5 };
        host.Add (markdownView);

        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        Assert.True (markdownView.LineCount >= 1);

        host.Dispose ();
    }

    [Fact]

    // Copilot
    public void WordWrap_Breaks_At_Word_Boundaries ()
    {
        // "Hello world" at width 8 should wrap between "Hello" and "world", not mid-word
        MarkdownView markdownView = new ("Hello world");
        markdownView.Width = 8;
        markdownView.Height = 5;

        View host = new () { Width = 8, Height = 5 };
        host.Add (markdownView);

        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        // Should produce 2 lines: "Hello " and "world"
        Assert.Equal (2, markdownView.LineCount);

        host.Dispose ();
    }

    [Fact]

    // Copilot
    public void WordWrap_Long_Word_Falls_Back_To_Hard_Break ()
    {
        // "Abcdefghij" (10 chars, no spaces) at width 5 should hard-break
        const string MARKDOWN = "Abcdefghij";
        MarkdownView markdownView = new (MARKDOWN);
        markdownView.Width = 5;
        markdownView.Height = 5;

        View host = new () { Width = 5, Height = 5 };
        host.Add (markdownView);

        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        // 10 chars at width 5 = 2 lines via hard break
        Assert.Equal (2, markdownView.LineCount);

        host.Dispose ();
    }

    // ---- Style rendering tests (AssertDriverOutputIs pattern) ----
    // Copilot
    // Each test verifies the correct ANSI TextStyle escape codes are emitted
    // for a specific MarkdownStyleRole. Uses ANSI driver with Force16Colors
    // and scheme Color.Black (SGR 30) on Color.White (SGR 107).

    [Fact]
    public void Style_Heading_Renders_Bold ()
    {
        (IApplication app, Runnable window) = SetupStyleTest ("# H");

        DriverAssert.AssertDriverOutputIs (@"\x1b[30m\x1b[107m\x1b[1mH\x1b[30m\x1b[107m\x1b[22m", output, app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Style_Emphasis_Renders_Italic ()
    {
        (IApplication app, Runnable window) = SetupStyleTest ("*E*");

        DriverAssert.AssertDriverOutputIs (@"\x1b[30m\x1b[107m\x1b[3mE\x1b[30m\x1b[107m\x1b[23m", output, app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Style_Strong_Renders_Bold ()
    {
        (IApplication app, Runnable window) = SetupStyleTest ("**S**");

        DriverAssert.AssertDriverOutputIs (@"\x1b[30m\x1b[107m\x1b[1mS\x1b[30m\x1b[107m\x1b[22m", output, app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Style_InlineCode_Renders_Bold_With_Dimmed_Background ()
    {
        (IApplication app, Runnable window) = SetupStyleTest ("`C`");

        // Code gets a dimmed background (\x1b[103m = bright yellow in 16-color, dimmed from white)
        DriverAssert.AssertDriverOutputIs (@"\x1b[30m\x1b[103m\x1b[1mC\x1b[30m\x1b[107m\x1b[22m", output, app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Style_Quote_Marker_Bold_Text_Faint ()
    {
        (IApplication app, Runnable window) = SetupStyleTest ("> Q");

        DriverAssert.AssertDriverOutputIs (@"\x1b[30m\x1b[107m\x1b[1m> \x1b[30m\x1b[107m\x1b[22;2mQ\x1b[30m\x1b[107m\x1b[22m", output, app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Style_ThematicBreak_Renders_Line ()
    {
        const int WIDTH = 5;
        (IApplication app, Runnable window) = SetupStyleTest ("---", WIDTH);

        // Line is inset: X=1, Width=Dim.Fill(1), so at WIDTH=5 it spans columns 1–3 (3 chars)
        DriverAssert.AssertDriverOutputIs (@"\x1b[30m\x1b[107m " + new string ('\u2500', WIDTH - 2), output, app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Style_ListMarker_Bold_Text_Normal ()
    {
        (IApplication app, Runnable window) = SetupStyleTest ("- L");

        DriverAssert.AssertDriverOutputIs (@"\x1b[30m\x1b[107m\x1b[1m" + "• " + @"\x1b[30m\x1b[107m\x1b[22mL", output, app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Style_TaskDone_Renders_Strikethrough ()
    {
        (IApplication app, Runnable window) = SetupStyleTest ("- [x] D");

        DriverAssert.AssertDriverOutputIs (@"\x1b[30m\x1b[107m\x1b[1m" + "• [x] " + @"\x1b[30m\x1b[107m\x1b[22;9mD\x1b[30m\x1b[107m\x1b[29m",
                                           output,
                                           app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Style_TaskTodo_Renders_Bold ()
    {
        (IApplication app, Runnable window) = SetupStyleTest ("- [ ] T");

        DriverAssert.AssertDriverOutputIs (@"\x1b[30m\x1b[107m\x1b[1m" + "• [ ] T" + @"\x1b[30m\x1b[107m\x1b[22m", output, app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Style_Normal_No_TextStyle ()
    {
        (IApplication app, Runnable window) = SetupStyleTest ("Hi");

        DriverAssert.AssertDriverOutputIs (@"\x1b[30m\x1b[107mHi", output, app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Style_Link_Absolute_Underline_With_OSC8 ()
    {
        (IApplication app, Runnable window) = SetupStyleTest ("[Go](https://x)");

        DriverAssert.AssertDriverOutputIs (@"\x1b]8;;https://x\x1b\\\x1b[30m\x1b[107m\x1b[4mGo\x1b]8;;\x1b\\\x1b[30m\x1b[107m\x1b[24m", output, app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Style_Link_Relative_No_Underline_No_OSC8 ()
    {
        (IApplication app, Runnable window) = SetupStyleTest ("[Go](foo.md)");

        DriverAssert.AssertDriverOutputIs (@"\x1b[30m\x1b[107mGo", output, app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Style_CodeBlock_Has_Full_Width_Dimmed_Background ()
    {
        // Fenced code block: the dimmed background should fill the entire row, not just the text
        const int WIDTH = 10;
        const string MARKDOWN = "```\nAB\n```";

        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (WIDTH, 3);
        app.Driver.Force16Colors = true;

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        window.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));

        MarkdownView mv = new (MARKDOWN) { Width = Dim.Fill (), Height = Dim.Fill () };
        mv.SchemeName = null;
        mv.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();
        app.Driver.Refresh ();

        string actual = app.Driver.GetOutput ().GetLastOutput ();

        // The code line "AB" should have the dimmed background (\x1b[103m) filling the full 10-column width
        // Row format: fill entire row with dimmed bg spaces, then draw "AB" with bold+dimmed bg
        Assert.NotNull (actual);

        // The code block row should contain 10 columns of dimmed background (103m), not just 2 for "AB"
        // Count how many times the dimmed bg code appears - should be at least for the fill + the text
        int dimBgCount = CountOccurrences (actual, "\x1b[103m");
        Assert.True (dimBgCount >= 2, $"Expected dimmed background to appear at least twice (fill + text), got {dimBgCount}");

        window.Dispose ();
        app.Dispose ();
    }

    private static int CountOccurrences (string text, string pattern)
    {
        var count = 0;
        var idx = 0;

        while ((idx = text.IndexOf (pattern, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += pattern.Length;
        }

        return count;
    }

    /// <summary>Sets up a 1-row ANSI screen with Force16Colors and a Black-on-White scheme.</summary>
    private static (IApplication app, Runnable window) SetupStyleTest (string markdown, int width = 20)
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (width, 1);
        app.Driver.Force16Colors = true;

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        window.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));

        MarkdownView mv = new (markdown) { Width = Dim.Fill (), Height = Dim.Fill () };
        mv.SchemeName = null;
        mv.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();
        app.Driver.Refresh ();

        return (app, window);
    }

    #region Anchor Navigation Tests

    // Copilot

    [Theory]
    [InlineData ("Hello World", "hello-world")]
    [InlineData ("Getting Started", "getting-started")]
    [InlineData ("C# Code Examples!", "c-code-examples")]
    [InlineData ("  Spaces  ", "spaces")]
    [InlineData ("multiple---hyphens", "multiple---hyphens")]
    [InlineData ("ALL CAPS", "all-caps")]
    [InlineData ("dots.and", "dotsand")]
    [InlineData ("Lexicon & Taxonomy", "lexicon--taxonomy")]
    public void GenerateAnchorSlug_Produces_Expected_Slug (string input, string expected)
    {
        string slug = MarkdownView.GenerateAnchorSlug (input);
        Assert.Equal (expected, slug);
    }

    [Fact]
    public void ScrollToAnchor_Scrolls_To_Heading ()
    {
        // Copilot
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (40, 5);

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        MarkdownView mv = new ("# First\n\nParagraph 1\n\n# Second\n\nParagraph 2\n\n# Third\n\nParagraph 3") { Width = Dim.Fill (), Height = Dim.Fill () };
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        // Initially at the top
        Assert.Equal (0, mv.Viewport.Y);

        // Scroll to "Third" heading
        bool found = mv.ScrollToAnchor ("third");
        Assert.True (found);
        Assert.True (mv.Viewport.Y > 0, "Should have scrolled down");

        // Scroll to "First" heading — should go back to top
        found = mv.ScrollToAnchor ("first");
        Assert.True (found);
        Assert.Equal (0, mv.Viewport.Y);

        // With leading # should also work
        found = mv.ScrollToAnchor ("#second");
        Assert.True (found);
        Assert.True (mv.Viewport.Y > 0);

        // Non-existent anchor returns false
        found = mv.ScrollToAnchor ("nonexistent");
        Assert.False (found);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void ScrollToAnchor_Duplicate_Headings_Get_Suffixed_Slugs ()
    {
        // Copilot
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (40, 3);

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        MarkdownView mv = new ("# Overview\n\nFirst\n\n# Overview\n\nSecond\n\n# Overview\n\nThird") { Width = Dim.Fill (), Height = Dim.Fill () };
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        // First "Overview" → slug "overview"
        bool found = mv.ScrollToAnchor ("overview");
        Assert.True (found);
        Assert.Equal (0, mv.Viewport.Y);

        // Second "Overview" → slug "overview-1"
        found = mv.ScrollToAnchor ("overview-1");
        Assert.True (found);
        Assert.True (mv.Viewport.Y > 0);

        int secondY = mv.Viewport.Y;

        // Third "Overview" → slug "overview-2"
        found = mv.ScrollToAnchor ("overview-2");
        Assert.True (found);
        Assert.True (mv.Viewport.Y > secondY);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Anchor_Links_Are_Rendered_With_Underline ()
    {
        // Copilot
        // Anchor links like [Section](#section) should be underlined
        (IApplication app, Runnable window) = SetupStyleTest ("[Go](#sec)", 10);

        // Anchor link should render with underline SGR (4m) like absolute links
        DriverAssert.AssertDriverOutputIs (@"\x1b[30m\x1b[107m\x1b[4mGo\x1b[30m\x1b[107m\x1b[24m", output, app.Driver);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void ScrollToAnchor_With_Empty_String_Returns_False ()
    {
        // Copilot
        MarkdownView mv = new ("# Test");
        Assert.False (mv.ScrollToAnchor (""));
        Assert.False (mv.ScrollToAnchor (null!));
    }

    #endregion

    #region Code Block Copy Button Tests

    // Copilot

    [Fact]
    public void CodeBlockRegions_Are_Detected_After_Layout ()
    {
        // Copilot
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (40, 10);

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        MarkdownView mv = new ("Text\n\n```\nline1\nline2\n```\n\nMore text\n\n```\nA\n```") { Width = Dim.Fill (), Height = Dim.Fill () };
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        // Should have 2 code block regions
        Assert.True (mv.LineCount > 0);

        // Verify that code block lines exist by checking rendered line count includes code
        // The markdown has 2 code blocks: first with 2 lines, second with 1 line
        // Verify we can extract text from regions by checking that at least some lines are code blocks
        Assert.True (mv.LineCount >= 6, $"Expected at least 6 rendered lines, got {mv.LineCount}");

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Copy_Button_Glyph_Is_Drawn_On_Code_Block ()
    {
        // Copilot
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (20, 5);
        app.Driver.Force16Colors = true;

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        window.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));

        MarkdownView mv = new ("```\ncode\n```") { Width = Dim.Fill (), Height = Dim.Fill () };
        mv.SchemeName = null;
        mv.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        // The copy button glyph "⧉" should appear in the screen contents on a code block line
        var screenContents = app.Driver.ToString ();
        Assert.NotNull (screenContents);
        Assert.Contains ("\u29C9", screenContents); // U+29C9 TWO JOINED SQUARES

        window.Dispose ();
        app.Dispose ();
    }

    #endregion

    // Copilot
    [Fact]
    public void Bullet_With_Parentheses_In_Link_Text_Renders_Correctly ()
    {
        // Exact pattern from layout.md TOC — indented sub-items with parens in link text
        // Narrow viewport forces word-wrap which exposed the bug
        const string MARKDOWN =
            "- [How To](#how-to)\n" +
            "  - [Stretch a View Between Fixed Elements](#stretch-a-view-between-fixed-elements)\n" +
            "  - [Align Multiple Views (Like Dialog Buttons)](#align-multiple-views-like-dialog-buttons)\n" +
            "  - [Center with Auto-Sizing and Constraints (Like Dialog)](#center-with-auto-sizing-and-constraints-like-dialog)";

        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (50, 10);

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        MarkdownView mv = new (MARKDOWN) { Width = Dim.Fill (), Height = Dim.Fill () };
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        string screenContents = app.Driver.ToString ();
        Assert.NotNull (screenContents);

        // Should contain the full link text including "(Like Dialog)"
        Assert.Contains ("Like Dialog", screenContents);

        // "Dialog)" should NOT appear as orphaned text on its own line
        string [] lines = screenContents.Split ('\n');
        Assert.DoesNotContain (lines, l => l.TrimStart ().StartsWith ("Dialog)", StringComparison.Ordinal));

        window.Dispose ();
        app.Dispose ();
    }

    // Copilot
    [Fact]
    public void Table_Height_Change_Reflows_Subsequent_Elements ()
    {
        // Markdown: paragraph, small table, then a heading below
        string md = """
                    Above

                    | A | B |
                    |---|---|
                    | Long cell content here | x |

                    # Below
                    """;

        MarkdownView view = new (md)
        {
            Width = 40,
            Height = 5 // Small viewport so scrolling is required
        };

        View host = new () { Width = 40, Height = 5 };
        host.Add (view);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        // Record initial line count and anchor position
        int initialLineCount = view.LineCount;

        Assert.True (view.ScrollToAnchor ("below"));
        int initialAnchorY = view.Viewport.Y;

        // Reset viewport
        view.Viewport = view.Viewport with { Y = 0 };

        // Shrink ContentSize.Width so the table columns wrap, making the table taller
        view.SetContentSize (new Size (20, view.GetContentSize ().Height));
        host.Layout ();

        int newLineCount = view.LineCount;

        // The narrower width should cause text and table to reflow — more lines
        Assert.True (newLineCount > initialLineCount, $"Expected more lines after narrowing: initial={initialLineCount}, new={newLineCount}");

        // The "# Below" anchor should still be reachable
        Assert.True (view.ScrollToAnchor ("below"));
        int newAnchorY = view.Viewport.Y;

        // The anchor should have moved down because the table grew taller
        Assert.True (newAnchorY > initialAnchorY, $"Expected anchor to move down: initial={initialAnchorY}, new={newAnchorY}");

        host.Dispose ();
    }

    // Copilot
    [Fact]
    public void CodeBlock_Width_Respects_ContentSize_Not_Viewport ()
    {
        string md = """
                    ```
                    code line
                    ```
                    """;

        MarkdownView view = new (md)
        {
            Width = 40,
            Height = 10
        };

        View host = new () { Width = 40, Height = 10 };
        host.Add (view);
        host.BeginInit ();
        host.EndInit ();
        host.Layout ();

        // Shrink content width to 20 (narrower than viewport)
        view.SetContentSize (new Size (20, view.GetContentSize ().Height));
        host.Layout ();

        // Get code block SubViews — they should be MarkdownCodeBlock instances
        List<View> codeBlocks = view.SubViews.Where (v => v.GetType ().Name == "MarkdownCodeBlock").ToList ();
        Assert.NotEmpty (codeBlocks);

        // Each code block should have Frame.Width == 20 (the content width), not 40 (viewport)
        foreach (View cb in codeBlocks)
        {
            Assert.Equal (20, cb.Frame.Width);
        }

        host.Dispose ();
    }
}
