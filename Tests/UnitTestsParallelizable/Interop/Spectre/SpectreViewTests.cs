// Copilot

using System.Text;
using JetBrains.Annotations;
using global::Spectre.Console;
using global::Spectre.Console.Rendering;
using Terminal.Gui.Interop.Spectre;
using Terminal.Gui.Drawing;
using Terminal.Gui.Views;
using SpectreColor = Spectre.Console.Color;
using TgAttribute = Terminal.Gui.Drawing.Attribute;

namespace InteropTests.Spectre;

[TestSubject (typeof (SpectreView))]
public class SpectreViewTests
{
    [Fact]
    public void SpectreMarkupBridge_ToAttribute_Converts_Color_And_Decoration ()
    {
        Style style = new (SpectreColor.Red, SpectreColor.Blue, Decoration.Bold | Decoration.Underline | Decoration.Strikethrough);

        TgAttribute attribute = style.ToAttribute ();

        Assert.Equal (255, attribute.Foreground.R);
        Assert.Equal (0, attribute.Foreground.G);
        Assert.Equal (0, attribute.Foreground.B);
        Assert.Equal (0, attribute.Background.R);
        Assert.Equal (0, attribute.Background.G);
        Assert.Equal (255, attribute.Background.B);
        Assert.True ((attribute.Style & TextStyle.Bold) != 0);
        Assert.True ((attribute.Style & TextStyle.Underline) != 0);
        Assert.True ((attribute.Style & TextStyle.Strikethrough) != 0);
    }

    [Fact]
    public void SpectreView_Renders_Table_Panel_Rule_Tree_BarChart_And_FigletText ()
    {
        Table table = new ();
        table.AddColumn ("Name");
        table.AddColumn ("Score");
        table.AddRow ("Alice", "100");

        string tableOutput = RenderToText (table, 80, 8);
        Assert.Contains ("Alice", tableOutput);

        Panel panel = new ("Panel Body");
        string panelOutput = RenderToText (panel, 80, 6);
        Assert.Contains ("Panel Body", panelOutput);

        Rule rule = new ("Rule Title");
        string ruleOutput = RenderToText (rule, 80, 3);
        Assert.Contains ("Rule Title", ruleOutput);

        Tree tree = new ("Root");
        tree.AddNode ("Branch");
        string treeOutput = RenderToText (tree, 80, 6);
        Assert.Contains ("Root", treeOutput);
        Assert.Contains ("Branch", treeOutput);

        BarChart barChart = new ();
        barChart.AddItem ("A", 10, SpectreColor.Green);
        string chartOutput = RenderToText (barChart, 80, 6);
        Assert.Contains ("A", chartOutput);

        FigletText figlet = new ("Hi");
        string figletOutput = RenderToText (figlet, 80, 8);
        Assert.Contains ("_", figletOutput);
    }

    [Fact]
    public void SpectreView_Renderable_Change_Triggers_Rerender ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (60, 6);

        SpectreView view = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Renderable = new Rule ("Before")
        };

        using Runnable root = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        root.Add (view);
        app.Begin (root);
        app.LayoutAndDraw ();

        string before = GetDriverText (app.Driver.Contents!);
        Assert.Contains ("Before", before);

        view.Renderable = new Rule ("After");
        app.LayoutAndDraw ();

        string after = GetDriverText (app.Driver.Contents!);
        Assert.Contains ("After", after);
    }

    [Fact]
    public void SpectreView_AutoSize_Reports_ContentSize_For_DimAuto ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 25);

        Table table = new ();
        table.AddColumn ("Name");
        table.AddColumn ("Value");
        table.AddRow ("Alice", "100");
        table.AddRow ("Bob", "95");

        SpectreView view = new ()
        {
            Width = 30,
            Height = Dim.Auto (DimAutoStyle.Content),
            Renderable = table
        };

        using Runnable root = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        root.Add (view);

        app.Begin (root);
        app.LayoutAndDraw ();

        Assert.True (view.GetContentSize ().Height > 0);
        Assert.True (view.Frame.Height > 0);
    }

    [Fact]
    public void SpectreView_Integrates_With_Window_And_Interactive_Views ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (80, 10);

        Button button = new () { Text = "Go", X = 0, Y = 0 };
        SpectreView spectre = new ()
        {
            Renderable = new Rule ("Decor"),
            X = 0,
            Y = 1,
            Width = Dim.Fill (),
            Height = 3
        };

        Window window = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        window.Add (button, spectre);

        app.Begin (window);
        app.LayoutAndDraw ();

        string output = GetDriverText (app.Driver.Contents!);
        Assert.Contains ("Go", output);
        Assert.Contains ("Decor", output);
    }

    private static string RenderToText (IRenderable renderable, int width, int height)
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (width, height);

        SpectreView view = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Renderable = renderable
        };

        using Runnable root = new () { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.None };
        root.Add (view);

        app.Begin (root);
        app.LayoutAndDraw ();

        return GetDriverText (app.Driver.Contents!);
    }

    private static string GetDriverText (Cell [,] contents)
    {
        int rowCount = contents.GetLength (0);
        int colCount = contents.GetLength (1);
        List<string> lines = [];

        for (int row = 0; row < rowCount; row++)
        {
            StringBuilder lineBuilder = new ();

            for (int col = 0; col < colCount; col++)
            {
                lineBuilder.Append (contents [row, col].Grapheme);
            }

            lines.Add (lineBuilder.ToString ());
        }

        return string.Join ("\n", lines);
    }
}
