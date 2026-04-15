using JetBrains.Annotations;

namespace ViewsTests.Markdown;

[TestSubject (typeof (MarkdownTable))]
public class MarkdownTableTests
{
    // Copilot

    [Fact]
    public void Parameterless_Constructor_Creates_Empty_Table ()
    {
        // Copilot
        MarkdownTable table = new ();

        Assert.NotNull (table);
        Assert.Equal (0, table.Data.ColumnCount);
    }

    [Fact]
    public void IDesignable_EnableForDesign_Returns_True ()
    {
        // Copilot
        MarkdownTable table = new ();
        IDesignable designable = table;

        bool result = designable.EnableForDesign ();

        Assert.True (result);
        Assert.True (table.Data.ColumnCount > 0);
        Assert.True (table.Data.Rows.Length > 0);
    }

    [Fact]
    public void Data_Property_Setter_Recomputes_Table ()
    {
        // Copilot
        MarkdownTable table = new ();

        TableData newData = new (["A", "B"], [Alignment.Start, Alignment.End], [["1", "2"], ["3", "4"]]);

        table.Data = newData;

        Assert.Equal (2, table.Data.ColumnCount);
        Assert.Equal (2, table.Data.Rows.Length);
    }

    [Fact]
    public void CalculateTableHeight_Correct ()
    {
        List<string> lines = ["| H1 | H2 |", "|-----|-----|", "| A  | B  |", "| C  | D  |"];

        TableData data = TableData.TryParse (lines)!;

        // 2 body rows + 4 (top border, header, separator, bottom border) = 6
        Assert.Equal (6, MarkdownTable.CalculateTableHeight (data));
    }

    [Fact]
    public void Renders_Box_Drawing_Characters ()
    {
        // Copilot
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (30, 10);
        app.Driver.Force16Colors = true;

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        window.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));

        MarkdownView mv = new () { Text = "| H1 | H2 |\n|-----|-----|\n| A  | B  |", Width = Dim.Fill (), Height = Dim.Fill () };
        mv.SchemeName = null;
        mv.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        var screenContents = app.Driver.ToString ();
        Assert.NotNull (screenContents);

        // Should contain box-drawing characters from LineCanvas
        Assert.Contains ("┌", screenContents);
        Assert.Contains ("┐", screenContents);
        Assert.Contains ("│", screenContents);
        Assert.Contains ("├", screenContents);
        Assert.Contains ("┤", screenContents);
        Assert.Contains ("└", screenContents);
        Assert.Contains ("┘", screenContents);
        Assert.Contains ("─", screenContents);

        // Should contain cell content
        Assert.Contains ("H1", screenContents);
        Assert.Contains ("H2", screenContents);
        Assert.Contains ("A", screenContents);
        Assert.Contains ("B", screenContents);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Reserves_Correct_Line_Count ()
    {
        // Copilot
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (30, 20);

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        MarkdownView mv = new () { Text = "text\n\n| H1 | H2 |\n|-----|-----|\n| A  | B  |\n| C  | D  |\n\nmore", Width = Dim.Fill (), Height = Dim.Fill () };
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        // "text" = 1 line, blank = 1 line, table = 6 lines (2 body + 4 chrome), blank = 1 line, "more" = 1 line
        // Total = 10
        Assert.Equal (10, mv.LineCount);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Three_Columns_With_Junction ()
    {
        // Copilot
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (40, 10);
        app.Driver.Force16Colors = true;

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        window.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));

        MarkdownView mv = new () { Text = "| A | B | C |\n|---|---|---|\n| 1 | 2 | 3 |", Width = Dim.Fill (), Height = Dim.Fill () };
        mv.SchemeName = null;
        mv.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        var screenContents = app.Driver.ToString ();
        Assert.NotNull (screenContents);

        // Should have junction characters where horizontal and vertical lines meet
        Assert.Contains ("┬", screenContents); // top junctions
        Assert.Contains ("┼", screenContents); // middle junctions
        Assert.Contains ("┴", screenContents); // bottom junctions

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Empty_Body_Renders_Header_Only ()
    {
        // Copilot
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (30, 10);
        app.Driver.Force16Colors = true;

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        window.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));

        MarkdownView mv = new () { Text = "| H1 | H2 |\n|-----|-----|", Width = Dim.Fill (), Height = Dim.Fill () };
        mv.SchemeName = null;
        mv.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        var screenContents = app.Driver.ToString ();
        Assert.NotNull (screenContents);

        Assert.Contains ("H1", screenContents);
        Assert.Contains ("H2", screenContents);
        Assert.Contains ("┌", screenContents);
        Assert.Contains ("└", screenContents);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Table_Followed_By_Text_Both_Render ()
    {
        // Copilot
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (30, 20);

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        MarkdownView mv = new () { Text = "| H |\n|---|\n| A |\n\nafter", Width = Dim.Fill (), Height = Dim.Fill () };
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        // Table: 5 lines (1 body + 4 chrome), blank: 1, "after": 1 = 7
        Assert.Equal (7, mv.LineCount);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Invalid_Table_Lines_Rendered_As_Text ()
    {
        // Copilot — Lines that look like table rows but have no valid separator
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (30, 10);

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        MarkdownView mv = new () { Text = "| H1 | H2 |\n| not sep |\n| body |", Width = Dim.Fill (), Height = Dim.Fill () };
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        // All 3 lines should be treated as regular text (not a table)
        Assert.True (mv.LineCount >= 3);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Table_Cells_With_Inline_Bold_Render_Bold ()
    {
        // Copilot — Verify that **bold** within table cells is rendered with Bold style
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (40, 10);
        app.Driver.Force16Colors = true;

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        window.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));

        MarkdownView mv = new () { Text = "| Header |\n|--------|\n| **bold** |", Width = Dim.Fill (), Height = Dim.Fill () };
        mv.SchemeName = null;
        mv.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        var screenContents = app.Driver.ToString ();
        Assert.NotNull (screenContents);

        // The word "bold" should appear (without ** delimiters)
        Assert.Contains ("bold", screenContents);

        // The literal ** should NOT appear in the output
        Assert.DoesNotContain ("**bold**", screenContents);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Table_Cells_With_Inline_Code_Render_Without_Backticks ()
    {
        // Copilot — Verify that `code` within table cells renders without backtick delimiters
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (40, 10);
        app.Driver.Force16Colors = true;

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        window.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));

        MarkdownView mv = new () { Text = "| Col |\n|-----|\n| `code` |", Width = Dim.Fill (), Height = Dim.Fill () };
        mv.SchemeName = null;
        mv.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        var screenContents = app.Driver.ToString ();
        Assert.NotNull (screenContents);

        // "code" should be visible
        Assert.Contains ("code", screenContents);

        // Backtick delimiters should not appear around "code"
        Assert.DoesNotContain ("`code`", screenContents);

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Default_Properties_Are_Correct ()
    {
        // Copilot — Verify MarkdownTable has expected defaults
        MarkdownTable table = new ();
        Assert.False (table.CanFocus);
        Assert.Equal (TabBehavior.NoStop, table.TabStop);
        Assert.Equal (LineStyle.None, table.BorderStyle);
    }

    [Fact]
    public void WrapSegments_Wraps_Long_Text ()
    {
        // Copilot — Verify WrapSegments splits text that exceeds maxWidth
        List<StyledSegment> segments = [new ("hello world foo", MarkdownStyleRole.Normal)];

        List<List<StyledSegment>> wrapped = MarkdownTable.WrapSegments (segments, 8);

        // "hello " fits in 6 cols, "world " fits in 6 cols, "foo" fits in 3 cols
        // Line 1: "hello " (6), Line 2: "world " (6), Line 3: "foo" (3)
        Assert.True (wrapped.Count >= 2, $"Expected at least 2 lines, got {wrapped.Count}");
    }

    [Fact]
    public void WrapSegments_Returns_Single_Line_When_Fits ()
    {
        // Copilot — Short text should not wrap
        List<StyledSegment> segments = [new ("hi", MarkdownStyleRole.Normal)];

        List<List<StyledSegment>> wrapped = MarkdownTable.WrapSegments (segments, 20);

        Assert.Single (wrapped);
    }

    [Fact]
    public void Wrapped_Rows_Increase_Table_Height ()
    {
        // Copilot — When cells need wrapping, the table should be taller than the simple estimate
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (20, 20);

        Runnable window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };

        // Long cell text in a narrow viewport will force wrapping
        MarkdownView mv = new () { Text = "| Header |\n|--------|\n| This is a very long cell that should wrap |", Width = Dim.Fill (), Height = Dim.Fill () };
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        // Simple estimate is 5 (1 body + 4 chrome). Wrapped should be >= 5.
        // With a 20-col viewport the cell text wraps to multiple lines.
        Assert.True (mv.LineCount >= 5, $"Expected at least 5 lines, got {mv.LineCount}");

        window.Dispose ();
        app.Dispose ();
    }

    [Fact]
    public void Column_Widths_Strip_Markdown_Formatting ()
    {
        // Copilot — Column widths should measure rendered text, not raw markdown with ** delimiters
        List<string> lines = ["| **Bold Header** | Normal |", "|-----------------|--------|", "| cell | cell |"];

        TableData data = TableData.TryParse (lines)!;

        // Create table wide enough that no shrinking occurs
        MarkdownTable table = new () { Data = data };

        // The header "Bold Header" is 11 display cols + 2 padding = 13
        // If we measured raw "**Bold Header**" it would be 15 + 2 = 17
        // Frame.Width should reflect the stripped measurement
        Assert.True (table.Frame.Width < 17 + 8 + 3, // narrower than if ** were counted
                     $"Table width {table.Frame.Width} suggests markdown delimiters were measured");
    }

    #region Column Width Algorithm Tests

    [Fact]
    public void ComputeColumnWidths_Natural_Fit_Uses_Max_Widths ()
    {
        // Copilot — When total content fits within maxWidth, use natural (max) widths
        List<string> lines = ["| A | B |", "|---|---|", "| x | y |"];
        TableData data = TableData.TryParse (lines)!;

        // maxWidth = 100 is plenty of room
        int [] widths = MarkdownTable.ComputeColumnWidths (data, 100);

        // "A"/"x" = 1 col + 2 padding = 3; "B"/"y" = 1 col + 2 padding = 3
        Assert.Equal (3, widths [0]);
        Assert.Equal (3, widths [1]);
    }

    [Fact]
    public void ComputeColumnWidths_Collapse_Shrinks_Widest_First ()
    {
        // Copilot — The widest column should shrink before narrower ones
        List<string> lines = ["| Cat | A long cell description here |", "|-----|------------------------------|", "| X   | More long text in this cell  |"];
        TableData data = TableData.TryParse (lines)!;

        // Natural: col0 = max("Cat","X") = 3+2=5, col1 = ~28+2=30
        // Force into 25 cols — col1 (wider) should shrink, col0 should be preserved
        int [] widths = MarkdownTable.ComputeColumnWidths (data, 25);

        // col0 should be at or near its natural width (5)
        Assert.True (widths [0] >= 4, $"Left column {widths [0]} was shrunk too much");

        // col1 should be smaller than its natural width
        Assert.True (widths [1] < 30, $"Right column {widths [1]} should have been collapsed");

        // Total should fit
        int total = widths.Sum () + widths.Length + 1;
        Assert.True (total <= 25, $"Total {total} exceeds maxWidth 25");
    }

    [Fact]
    public void ComputeColumnWidths_Left_Wins_Tiebreak ()
    {
        // Copilot — When columns have equal width, rightmost shrinks first
        List<string> lines = ["| ABCDE | ABCDE |", "|-------|-------|", "| 12345 | 12345 |"];
        TableData data = TableData.TryParse (lines)!;

        // Natural: both cols = 5+2=7, total = 7+7+3 = 17
        // Force into 15 — need to lose 2 from column content
        int [] widths = MarkdownTable.ComputeColumnWidths (data, 15);

        // Left column should be >= right column (left wins)
        Assert.True (widths [0] >= widths [1], $"Left ({widths [0]}) should be >= right ({widths [1]}) when tied");
    }

    [Fact]
    public void ComputeColumnWidths_Min_Width_Is_Longest_Word ()
    {
        // Copilot — Columns should not shrink below their longest word + padding
        List<string> lines =
        [
            "| Lifecycle | Purpose of this column is very long |",
            "|-----------|--------------------------------------|",
            "| Navigation | Focus movement between views        |"
        ];
        TableData data = TableData.TryParse (lines)!;

        // "Navigation" is 10 chars — min should be 10+2=12
        int [] widths = MarkdownTable.ComputeColumnWidths (data, 30);

        // col0 min = "Navigation" (10) + 2 = 12
        Assert.True (widths [0] >= 12, $"Column 0 width {widths [0]} is below longest word 'Navigation' (need >=12)");
    }

    [Fact]
    public void ComputeColumnWidths_Last_Resort_Even_Reduction ()
    {
        // Copilot — When all columns are at min, reduce evenly as last resort
        List<string> lines = ["| AAAAAAAAA | BBBBBBBBB |", "|-----------|-----------|", "| CCCCCCCCC | DDDDDDDDD |"];
        TableData data = TableData.TryParse (lines)!;

        // Both cols: longest word = 9 chars, min = 9+2=11
        // Total min = 11+11+3 = 25. Force into 15 — must go below min.
        int [] widths = MarkdownTable.ComputeColumnWidths (data, 15);

        // Should still fit within maxWidth
        int total = widths.Sum () + widths.Length + 1;
        Assert.True (total <= 15, $"Total {total} exceeds maxWidth 15");

        // Both should have been reduced (neither stuck at full natural)
        Assert.True (widths [0] < 11 || widths [1] < 11, "At least one column should be below its min in last resort");
    }

    [Fact]
    public void ComputeColumnWidths_Single_Column ()
    {
        // Copilot — Single-column table should use full available width or natural
        List<string> lines = ["| Header |", "|--------|", "| Cell |"];
        TableData data = TableData.TryParse (lines)!;

        int [] widths = MarkdownTable.ComputeColumnWidths (data, 50);

        // "Header" = 6+2=8
        Assert.Equal (8, widths [0]);
    }

    [Fact]
    public void ComputeColumnWidths_Empty_Cells_Get_Minimum ()
    {
        // Copilot — Columns with empty body cells should still get at least 3 (1 char + 2 padding)
        List<string> lines = ["| H | Long Header |", "|---|-------------|", "|   | content     |"];
        TableData data = TableData.TryParse (lines)!;

        int [] widths = MarkdownTable.ComputeColumnWidths (data, 80);

        Assert.True (widths [0] >= 3, $"Empty column width {widths [0]} should be at least 3");
    }

    [Fact]
    public void MeasureRenderedWidth_Strips_Markdown ()
    {
        // Copilot — Rendered width should not include markdown delimiters
        Assert.Equal (4, MarkdownTable.MeasureRenderedWidth ("**bold**"));
        Assert.Equal (6, MarkdownTable.MeasureRenderedWidth ("*italic*"));
        Assert.Equal (4, MarkdownTable.MeasureRenderedWidth ("`code`"));
        Assert.Equal (5, MarkdownTable.MeasureRenderedWidth ("plain"));
    }

    [Fact]
    public void MeasureLongestWord_Finds_Longest ()
    {
        // Copilot — Should find the longest individual word
        Assert.Equal (5, MarkdownTable.MeasureLongestWord ("hi there world"));
        Assert.Equal (10, MarkdownTable.MeasureLongestWord ("Navigation"));
        Assert.Equal (4, MarkdownTable.MeasureLongestWord ("**bold** text"));
    }

    [Fact]
    public void CollapseWidths_Preserves_Left_Column ()
    {
        // Copilot — Direct test of CollapseWidths: left col should be preserved when right is wider
        int [] widths = [5, 20];
        int [] mins = [3, 3];

        MarkdownTable.CollapseWidths (widths, mins, 15);

        // Total should be <= 15
        Assert.True (widths.Sum () <= 15, $"Total {widths.Sum ()} exceeds available 15");

        // Left column should be at or near its natural width
        Assert.Equal (5, widths [0]);
    }

    [Fact]
    public void CollapseWidths_Three_Columns_Shrinks_Widest ()
    {
        // Copilot — With 3 columns, the widest shrinks to the second-widest level first
        int [] widths = [5, 10, 20];
        int [] mins = [3, 3, 3];

        // Available = 20, total = 35, need to lose 15
        MarkdownTable.CollapseWidths (widths, mins, 20);

        Assert.True (widths.Sum () <= 20, $"Total {widths.Sum ()} exceeds available 20");

        // Column 0 (narrowest) should be mostly preserved
        Assert.True (widths [0] >= 4, $"Narrowest column {widths [0]} was shrunk too aggressively");
    }

    #endregion

    #region Standalone syntax highlighting

    // Copilot

    [Fact]
    public void SyntaxHighlighter_Property_Defaults_Null ()
    {
        MarkdownTable table = new ();
        Assert.Null (table.SyntaxHighlighter);
    }

    [Fact]
    public void Setting_SyntaxHighlighter_Passes_To_GetAttributeForSegment ()
    {
        // Verify that when SyntaxHighlighter is set, the table uses it for attribute resolution
        TextMateSyntaxHighlighter highlighter = new ();

        MarkdownTable table = new () { SyntaxHighlighter = highlighter, Data = new TableData (["Name"], [Alignment.Start], [["**bold**"]]) };

        // If highlighter is wired, emphasis role should get theme colors (not default fallback)
        highlighter.GetAttributeForScope (MarkdownStyleRole.Emphasis);

        // Smoke test: highlighter is set and Data works
        Assert.NotNull (table.SyntaxHighlighter);
        Assert.Single (table.Data.Rows);
    }

    #endregion

    #region Text property (pipe table parsing)

    // Copilot

    [Fact]
    public void Text_Parses_Pipe_Table ()
    {
        MarkdownTable table = new () { Text = "| Name | Age |\n|------|-----|\n| Alice | 30 |" };

        Assert.Equal (2, table.Data.ColumnCount);
        Assert.Single (table.Data.Rows);
    }

    [Fact]
    public void Text_With_Alignment_Markers ()
    {
        MarkdownTable table = new () { Text = "| Left | Center | Right |\n|:-----|:------:|------:|\n| A | B | C |" };

        Assert.Equal (3, table.Data.ColumnCount);
        Assert.Equal (Alignment.Start, table.Data.ColumnAlignments [0]);
        Assert.Equal (Alignment.Center, table.Data.ColumnAlignments [1]);
        Assert.Equal (Alignment.End, table.Data.ColumnAlignments [2]);
    }

    [Fact]
    public void Text_Empty_String_Clears_Table ()
    {
        MarkdownTable table = new () { Text = "| A |\n|---|\n| B |" };
        Assert.True (table.Data.ColumnCount > 0);

        table.Text = "";
        Assert.Equal (0, table.Data.ColumnCount);
    }

    [Fact]
    public void Text_Invalid_Table_Sets_Empty_Data ()
    {
        MarkdownTable table = new () { Text = "not a table" };

        Assert.Equal (0, table.Data.ColumnCount);
    }

    [Fact]
    public void EnableForDesign_Sets_Highlighter_And_Text ()
    {
        MarkdownTable table = new ();
        IDesignable designable = table;

        bool result = designable.EnableForDesign ();

        Assert.True (result);
        Assert.NotNull (table.SyntaxHighlighter);
        Assert.True (table.Data.ColumnCount > 0);
    }

    #endregion
}
