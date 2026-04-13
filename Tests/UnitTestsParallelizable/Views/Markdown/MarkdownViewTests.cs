using JetBrains.Annotations;
using UnitTests;

namespace ViewsTests.Markdown;

[TestSubject (typeof (MarkdownView))]
public class MarkdownViewTests
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

        Runnable window = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.None
        };

        MarkdownView markdownView = new ("Visit [Terminal.Gui](https://github.com/gui-cs/Terminal.Gui)")
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        window.Add (markdownView);

        app.Begin (window);
        app.LayoutAndDraw ();
        app.Driver!.Refresh ();

        string? output = app.Driver.GetOutput ().GetLastOutput ();

        Assert.Contains (EscSeqUtils.OSC_StartHyperlink ("https://github.com/gui-cs/Terminal.Gui"), output);
        Assert.Contains (EscSeqUtils.OSC_EndHyperlink (), output);

        window.Dispose ();
    }

    [Fact]
    public void Mouse_Click_On_Link_Raises_LinkClicked ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        Runnable window = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.None
        };

        MarkdownView markdownView = new ("[Click](https://example.com)")
        {
            Width = 20,
            Height = 3
        };

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
}
