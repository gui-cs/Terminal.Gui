#nullable enable
using JetBrains.Annotations;
using UnitTests;
using Xunit.Abstractions;

namespace ViewsTests;

/// <summary>
///     Unit tests for <see cref="Link"/> that don't require static Application dependencies.
///     These tests can run in parallel without interference.
/// </summary>
[TestSubject (typeof (Link))]
public class LinkTests (ITestOutputHelper output) : TestDriverBase
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Constructor_Defaults ()
    {
        Link link = new();

        Assert.Equal (Link.DEFAULT_URL, link.Url);
        Assert.Equal (Link.DEFAULT_URL, link.Text);
        Assert.Equal (Dim.Auto (DimAutoStyle.Text), link.Height);
        Assert.Equal (Dim.Auto (DimAutoStyle.Text), link.Width);
        Assert.True (link.CanFocus);
    }

    [Fact]
    public void Text_Set_Updates_Title ()
    {
        Link link = new () { Text = "Click here" };

        Assert.Equal ("Click here", link.Text);
        Assert.Equal ("Click here", link.Title);
    }

    [Fact]
    public void Text_Returns_Url_When_Title_Is_Empty ()
    {
        Link link = new () { Url = "https://github.com" };

        Assert.Equal ("https://github.com", link.Text);
    }

    [Fact]
    public void Url_Set_Validates_Uri ()
    {
        Link link = new() { Url = "https://github.com" };

        link.Url = "not a valid url";
        Assert.Equal (Link.DEFAULT_URL, link.Url); // Url should not change on invalid

        link.Url = "";
        Assert.Equal (Link.DEFAULT_URL, link.Url); // Url should not change on invalid
    }
    
    [Fact]
    public void Url_Set_Fires_UrlChanged_Event ()
    {
        string oldUrl = "http://oldvalue.io";
        string newUrl = "http://newvalue.io";

        Link link = new() { Url = oldUrl };
        bool eventFired = false;
        bool eventArgsValid = false;

        link.UrlChanged += (s, e) =>
        {
            eventFired = true;
            eventArgsValid = e.OldValue == oldUrl && e.NewValue == newUrl;
        };
        link.Url = newUrl;

        Assert.True (eventFired);
        Assert.True (eventArgsValid);
        Assert.Equal (newUrl, link.Url);
    }

    [Fact]
    public void Url_Set_Fires_UrlChanging_Event ()
    {
        string oldUrl = "http://oldvalue.io";
        string newUrl = "http://newvalue.io";

        Link link = new () { Url = oldUrl };
        bool eventFired = false;
        bool eventArgsValid = false;
        bool valueChanged = false;

        link.UrlChanging += (s, e) =>
        {
            eventFired = true;
            eventArgsValid = e.CurrentValue == oldUrl && e.NewValue == newUrl;
            // Should be false since the change hasn't happened yet
            valueChanged = e.CurrentValue == newUrl || link.Url == newUrl;
        };
        link.Url = newUrl;

        Assert.True (eventFired);
        Assert.True (eventArgsValid);
        Assert.False (valueChanged);
        Assert.Equal (newUrl, link.Url);
    }

    [Fact]
    public void Copy_Copies_Url_To_Clipboard ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Clipboard = new FakeClipboard ();

        Link link = new () { App = app, Url = "https://github.com" };
        
        var copied = link.Copy ();

        Assert.True (copied);
        Assert.Equal ("https://github.com", app.Clipboard?.GetClipboardData ());
    }

    [Fact]
    public void Link_Renders_With_OSC8_Hyperlink ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable window = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.None
        };

        Link link = new ()
        {
            App = app,
            Url = "https://github.com/gui-cs/Terminal.Gui",
            Text = "Terminal.Gui"
        };
        window.Add (link);

        app.Begin (window);
        app.LayoutAndDraw ();
        app.Driver!.Refresh ();

        // Get the ANSI output
        string ansi = app.Driver.ToAnsi ();
        string? look = app.Driver.GetOutput ().GetLastOutput ();

        // Verify OSC 8 sequences are present
        string expectedStart = EscSeqUtils.OSC_StartHyperlink ("https://github.com/gui-cs/Terminal.Gui");
        string expectedEnd = EscSeqUtils.OSC_EndHyperlink ();

        Assert.Contains (expectedStart, look);
        Assert.Contains (expectedEnd, look);
        Assert.Contains ("Terminal.Gui", look);
        Assert.Contains ("Terminal.Gui", ansi);

        window.Dispose ();
    }

    [Fact]
    public void Link_Renders_Without_OSC8_When_Url_Is_Default ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable window = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.None
        };

        Link link = new ()
        {
            App = app,
            Text = "Not a link"
        };
        window.Add (link);

        app.Begin (window);
        app.LayoutAndDraw ();
        app.Driver!.Refresh ();

        // Get the ANSI output
        string ansi = app.Driver!.ToAnsi ();
        string? look = app.Driver.GetOutput ().GetLastOutput ();

        // Verify OSC 8 sequences are NOT present for default URL
        Assert.DoesNotContain (EscSeqUtils.OSC_StartHyperlink (Link.DEFAULT_URL), look);
        Assert.Contains ("Not a link", look);
        Assert.Contains ("Not a link", ansi);

        window.Dispose ();
    }

    [Fact]
    public void Link_With_HotKey_Passes_To_Next_View ()
    {
        View superView = new () { CanFocus = true };
        Link link = new () { Text = "_Link", CanFocus = false };
        View nextView = new () { CanFocus = true };
        
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
        app.Driver!.SetScreenSize (40, 2);

        using Runnable window = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.None
        };

        // ✅ Ajouter les DEUX liens dès le début
        Link link1 = new ()
        {
            App = app,
            X = 0,
            Y = 0,
            Url = "https://github.com",
            Text = "GitHub"
        };

        Link link2 = new ()
        {
            App = app,
            X = 0,
            Y = 1,
            Url = "https://microsoft.com",
            Text = "Microsoft"
        };

        window.Add (link1, link2);

        app.Begin (window);
        app.LayoutAndDraw ();
        app.Driver.Refresh ();

        string? look = app.Driver.GetOutput ().GetLastOutput ();
        string ansi = app.Driver.ToAnsi ();

        // Verify both URLs are present in the same output
        Assert.Contains (EscSeqUtils.OSC_StartHyperlink ("https://github.com"), look);
        Assert.Contains (EscSeqUtils.OSC_StartHyperlink ("https://microsoft.com"), look);
        Assert.Contains ("GitHub", look);
        Assert.Contains ("Microsoft", look);
        Assert.Contains ("GitHub", ansi);
        Assert.Contains ("Microsoft", ansi);

        window.Dispose ();
    }

    [Fact]
    public void Link_Url_Changes_Update_Hyperlink ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable window = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.None
        };

        Link link = new ()
        {
            App = app,
            Url = "https://example.com",
            Text = "Example"
        };
        window.Add (link);

        app.Begin (window);
        app.LayoutAndDraw ();
        app.Driver!.Refresh ();

        string? look1 = app.Driver.GetOutput ().GetLastOutput ();
        Assert.Contains (EscSeqUtils.OSC_StartHyperlink ("https://example.com"), look1);

        // Clear and change URL
        app.Driver.ClearContents ();
        link.Url = "https://newurl.com";
        app.LayoutAndDraw ();
        app.Driver!.Refresh ();

        string? look2 = app.Driver.GetOutput ().GetLastOutput ();
        Assert.Contains (EscSeqUtils.OSC_StartHyperlink ("https://newurl.com"), look2);

        window.Dispose ();
    }

    [Fact]
    public void Link_With_Focus_Draws_With_Focus_Colors ()
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable window = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.None
        };

        // ✅ Add a dummy view to take initial focus
        View dummyView = new () { CanFocus = true, Width = 1, Height = 1 };
        window.Add (dummyView);

        Link link = new ()
        {
            App = app,
            Url = "https://github.com",
            Text = "GitHub",
            CanFocus = true,
            Y = 1  // Place it below dummyView
        };
        window.Add (link);

        app.Begin (window);
        app.LayoutAndDraw ();
        app.Driver!.Refresh ();

        // ✅ Without focus - dummyView has focus instead
        Assert.False (link.HasFocus);
        Assert.True (dummyView.HasFocus);

        // Set focus on link
        app.Driver.ClearContents ();
        link.SetFocus ();
        link.Draw ();
        Assert.True (link.HasFocus);
        Assert.False (dummyView.HasFocus);

        // The link should still have OSC 8 sequences
        string? look = app.Driver.GetOutput ().GetLastOutput ();
        Assert.Contains (EscSeqUtils.OSC_StartHyperlink ("https://github.com"), look);

        window.Dispose ();
    }

    [Fact]
    public void IDesignable_EnableForDesign_Sets_Default_Text ()
    {
        Link link = new();
        IDesignable designable = link as IDesignable;

        var result = designable.EnableForDesign ();

        Assert.True (result);
        Assert.Equal ("_Link", link.Text);

        link.Dispose ();
    }

    [Fact]
    public void Link_LeftButtonReleased_InvokesHotKey_OnNextView ()
    {
        View superView = new () { CanFocus = true, Height = 1, Width = 15 };
        Link link = new () { X = 0, HotKey = Key.L.WithAlt, CanFocus = false };
        View nextView = new () { CanFocus = true, X = 10, Width = 4, Height = 1 };
        
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
    public void Link_Osc8_Emits_StartTextEnd_And_Outputs_Correctly ()
    {
        string text = "GitHub";
        string url = "https://github.com";

        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (40, 1);
        app.Driver.Force16Colors = true;

        using Runnable window = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.None
        };
        window.SetScheme (new (new Attribute (Color.Black, Color.White)));

        Link link = new ()
        {
            X = 0,
            Y = 0,
            Width = 60,
            Height = 1,
            Text = text,
            Url = url,
        };
        window.Add (link);

        app.Begin(window);
        app.LayoutAndDraw ();
        app.Driver.Refresh ();

        DriverAssert.AssertDriverOutputIs ("""
            \x1b]8;;https://github.com\x1b\\\x1b[97m\x1b[40mGitHub\x1b]8;;\x1b\\\x1b[30m\x1b[107m
            """, _output, app.Driver);
    }
}
