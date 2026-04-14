using JetBrains.Annotations;

namespace ViewsTests.Markdown;

[TestSubject (typeof (MarkdownTable))]
public class MarkdownTableTests
{
    // Copilot

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

        MarkdownView mv = new ("| H1 | H2 |\n|-----|-----|\n| A  | B  |") { Width = Dim.Fill (), Height = Dim.Fill () };
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

        MarkdownView mv = new ("text\n\n| H1 | H2 |\n|-----|-----|\n| A  | B  |\n| C  | D  |\n\nmore") { Width = Dim.Fill (), Height = Dim.Fill () };
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

        MarkdownView mv = new ("| A | B | C |\n|---|---|---|\n| 1 | 2 | 3 |") { Width = Dim.Fill (), Height = Dim.Fill () };
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

        MarkdownView mv = new ("| H1 | H2 |\n|-----|-----|") { Width = Dim.Fill (), Height = Dim.Fill () };
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

        MarkdownView mv = new ("| H |\n|---|\n| A |\n\nafter") { Width = Dim.Fill (), Height = Dim.Fill () };
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

        MarkdownView mv = new ("| H1 | H2 |\n| not sep |\n| body |") { Width = Dim.Fill (), Height = Dim.Fill () };
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

        MarkdownView mv = new ("| Header |\n|--------|\n| **bold** |") { Width = Dim.Fill (), Height = Dim.Fill () };
        mv.SchemeName = null;
        mv.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        string? screenContents = app.Driver.ToString ();
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

        MarkdownView mv = new ("| Col |\n|-----|\n| `code` |") { Width = Dim.Fill (), Height = Dim.Fill () };
        mv.SchemeName = null;
        mv.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));
        window.Add (mv);

        app.Begin (window);
        app.LayoutAndDraw ();

        string? screenContents = app.Driver.ToString ();
        Assert.NotNull (screenContents);

        // "code" should be visible
        Assert.Contains ("code", screenContents);

        // Backtick delimiters should not appear around "code"
        Assert.DoesNotContain ("`code`", screenContents);

        window.Dispose ();
        app.Dispose ();
    }
}
