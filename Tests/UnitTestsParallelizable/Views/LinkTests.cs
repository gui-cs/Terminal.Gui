#nullable enable
using UnitTests;
using Xunit.Abstractions;

namespace ViewsTests;

/// <summary>
///     Unit tests for <see cref="Link"/> that don't require static Application dependencies.
///     These tests can run in parallel without interference.
/// </summary>
public class LinkTests (ITestOutputHelper output) : TestDriverBase
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Constructor_Defaults ()
    {
        var link = new Link ();

        Assert.Equal (Link.DEFAULT_URL, link.Url);
        Assert.Equal (Link.DEFAULT_URL, link.Text);
        Assert.Equal (Dim.Auto (DimAutoStyle.Text), link.Height);
        Assert.Equal (Dim.Auto (DimAutoStyle.Text), link.Width);
        Assert.True (link.CanFocus);
    }

    [Fact]
    public void Text_Set_Updates_Title ()
    {
        var link = new Link { Text = "Click here" };

        Assert.Equal ("Click here", link.Text);
        Assert.Equal ("Click here", link.Title);
    }

    [Fact]
    public void Text_Returns_Url_When_Title_Is_Empty ()
    {
        var link = new Link { Url = "https://github.com" };

        Assert.Equal ("https://github.com", link.Text);
    }

    [Fact]
    public void Url_Set_Validates_Uri ()
    {
        var link = new Link ();

        Assert.Throws<UriFormatException> (() => link.Url = "not a valid url");
        Assert.Throws<UriFormatException> (() => link.Url = "");
    }

    [Fact]
    public void Url_Set_Fires_UrlChanged_Event ()
    {
        var link = new Link ();
        var eventFired = false;

        link.UrlChanged += (s, e) => eventFired = true;
        link.Url = "https://github.com";

        Assert.True (eventFired);
        Assert.Equal ("https://github.com", link.Url);
    }

    [Fact]
    public void Copy_Copies_Url_To_Clipboard ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Clipboard = new FakeClipboard ();

        var link = new Link { App = app, Url = "https://github.com" };
        
        var copied = link.Copy ();

        Assert.True (copied);
        Assert.Equal ("https://github.com", app.Clipboard?.GetClipboardData ());
    }

    [Fact]
    public void Link_Renders_With_OSC8_Hyperlink ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Clipboard = new FakeClipboard ();

        var link = new Link
        {
            App = app,
            Url = "https://github.com/gui-cs/Terminal.Gui",
            Text = "Terminal.Gui"
        };

        link.BeginInit ();
        link.EndInit ();
        link.SetRelativeLayout (new (30, 3));
        link.Draw ();

        // Get the ANSI output
        string ansi = app.Driver!.ToAnsi ();

        // Verify OSC 8 sequences are present
        string expectedStart = EscSeqUtils.OSC_StartHyperlink ("https://github.com/gui-cs/Terminal.Gui");
        string expectedEnd = EscSeqUtils.OSC_EndHyperlink ();

        Assert.Contains (expectedStart, ansi);
        Assert.Contains (expectedEnd, ansi);
        Assert.Contains ("Terminal.Gui", ansi);

        link.Dispose ();
    }

    [Fact]
    public void Link_Renders_Without_OSC8_When_Url_Is_Default ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Clipboard = new FakeClipboard ();

        var link = new Link
        {
            App = app,
            Text = "Not a link"
        };

        link.BeginInit ();
        link.EndInit ();
        link.SetRelativeLayout (new (30, 3));
        link.Draw ();

        // Get the ANSI output
        string ansi = app.Driver!.ToAnsi ();

        // Verify OSC 8 sequences are NOT present for default URL
        Assert.DoesNotContain (EscSeqUtils.OSC_StartHyperlink (Link.DEFAULT_URL), ansi);
        Assert.Contains ("Not a link", ansi);

        link.Dispose ();
    }

    [Fact]
    public void Link_With_HotKey_Passes_To_Next_View ()
    {
        var superView = new View { CanFocus = true };
        var link = new Link { Text = "_Link", CanFocus = false };
        var nextView = new View { CanFocus = true };
        
        superView.Add (link, nextView);
        superView.BeginInit ();
        superView.EndInit ();

        Assert.False (link.HasFocus);
        Assert.False (nextView.HasFocus);

        // Invoke hotkey
        link.InvokeCommand (Command.HotKey);

        // Next view should get focus since Link can't focus
        Assert.True (nextView.HasFocus);

        superView.Dispose ();
    }

    [Fact]
    public void Link_Multiple_Links_Each_Get_Their_Own_Url ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Clipboard = new FakeClipboard ();

        var link1 = new Link
        {
            App = app,
            X = 0,
            Y = 0,
            Url = "https://github.com",
            Text = "GitHub"
        };

        var link2 = new Link
        {
            App = app,
            X = 0,
            Y = 1,
            Url = "https://microsoft.com",
            Text = "Microsoft"
        };

        link1.BeginInit ();
        link1.EndInit ();
        link1.SetRelativeLayout (new (30, 10));
        link1.Draw ();

        link2.BeginInit ();
        link2.EndInit ();
        link2.SetRelativeLayout (new (30, 10));
        link2.Draw ();

        // Get the ANSI output
        string ansi = app.Driver!.ToAnsi ();

        // Verify both URLs are present
        Assert.Contains (EscSeqUtils.OSC_StartHyperlink ("https://github.com"), ansi);
        Assert.Contains (EscSeqUtils.OSC_StartHyperlink ("https://microsoft.com"), ansi);
        Assert.Contains ("GitHub", ansi);
        Assert.Contains ("Microsoft", ansi);

        link1.Dispose ();
        link2.Dispose ();
    }

    [Fact]
    public void Link_Url_Changes_Update_Hyperlink ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Clipboard = new FakeClipboard ();

        var link = new Link
        {
            App = app,
            Url = "https://example.com",
            Text = "Example"
        };

        link.BeginInit ();
        link.EndInit ();
        link.SetRelativeLayout (new (30, 3));
        link.Draw ();

        string ansi1 = app.Driver!.ToAnsi ();
        Assert.Contains (EscSeqUtils.OSC_StartHyperlink ("https://example.com"), ansi1);

        // Clear and change URL
        app.Driver.ClearContents ();
        link.Url = "https://newurl.com";
        link.SetNeedsDraw ();
        link.Draw ();

        string ansi2 = app.Driver.ToAnsi ();
        Assert.Contains (EscSeqUtils.OSC_StartHyperlink ("https://newurl.com"), ansi2);

        link.Dispose ();
    }

    [Fact]
    public void Link_With_Focus_Draws_With_Focus_Colors ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Clipboard = new FakeClipboard ();

        var link = new Link
        {
            App = app,
            Url = "https://github.com",
            Text = "GitHub",
            CanFocus = true
        };

        link.BeginInit ();
        link.EndInit ();
        link.SetRelativeLayout (new (30, 3));

        // Without focus
        link.Draw ();
        Assert.False (link.HasFocus);

        // Set focus
        app.Driver!.ClearContents ();
        link.SetFocus ();
        link.Draw ();
        Assert.True (link.HasFocus);

        // The link should still have OSC 8 sequences
        string ansi = app.Driver.ToAnsi ();
        Assert.Contains (EscSeqUtils.OSC_StartHyperlink ("https://github.com"), ansi);

        link.Dispose ();
    }

    [Fact]
    public void IDesignable_EnableForDesign_Sets_Default_Text ()
    {
        var link = new Link ();
        var designable = link as IDesignable;

        var result = designable.EnableForDesign ();

        Assert.True (result);
        Assert.Equal ("_Link", link.Text);

        link.Dispose ();
    }

    [Fact]
    public void Link_LeftButtonReleased_InvokesHotKey_OnNextView ()
    {
        var superView = new View { CanFocus = true, Height = 1, Width = 15 };
        var link = new Link { X = 0, HotKey = Key.L.WithAlt, CanFocus = false };
        var nextView = new View { CanFocus = true, X = 10, Width = 4, Height = 1 };
        
        superView.Add (link, nextView);
        superView.BeginInit ();
        superView.EndInit ();

        Assert.False (link.HasFocus);
        Assert.False (nextView.HasFocus);

        // Click on the link
        link.NewMouseEvent (new () { Position = new (0, 0), Flags = MouseFlags.LeftButtonReleased });
        
        Assert.False (link.HasFocus);
        Assert.True (nextView.HasFocus);

        superView.Dispose ();
    }

    [Fact]
    public void Link_Cell_Url_Is_Set_In_Buffer ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Clipboard = new FakeClipboard ();

        var link = new Link
        {
            App = app,
            X = 0,
            Y = 0,
            Url = "https://github.com",
            Text = "GitHub"
        };

        link.BeginInit ();
        link.EndInit ();
        link.SetRelativeLayout (new (30, 3));
        link.Draw ();

        // Verify that cells in the buffer have the URL set
        Cell [,] contents = app.Driver!.Contents!;
        
        // The first cell of "GitHub" should have the URL
        Assert.Equal ("https://github.com", contents [0, 0].Url);
        Assert.Equal ("G", contents [0, 0].Grapheme);

        // All cells in "GitHub" should have the URL
        Assert.Equal ("https://github.com", contents [0, 1].Url);
        Assert.Equal ("https://github.com", contents [0, 2].Url);

        link.Dispose ();
    }
}
