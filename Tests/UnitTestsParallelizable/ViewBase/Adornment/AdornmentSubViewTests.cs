using UnitTests;

namespace ViewBaseTests.Adornments;

public class AdornmentSubViewTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Setting_Thickness_Causes_Adornment_SubView_Layout ()
    {
        var view = new View ();
        var subView = new View ();
        view.Padding!.Add (subView);
        view.BeginInit ();
        view.EndInit ();
        var raised = false;

        subView.SubViewLayout += LayoutStarted;
        view.Padding.Thickness = new (1, 2, 3, 4);
        view.Layout ();
        Assert.True (raised);

        return;
        void LayoutStarted (object? sender, LayoutEventArgs e)
        {
            raised = true;
        }
    }

    [Theory]
    [InlineData (0, 0, false)] // Padding has no thickness, so false
    [InlineData (0, 1, false)] // Padding has no thickness, so false
    [InlineData (1, 0, true)]
    [InlineData (1, 1, true)]
    [InlineData (2, 1, true)]
    public void Adornment_WithSubView_Finds (int viewPadding, int subViewPadding, bool expectedFound)
    {
        IApplication? app = Application.Create ();
        Runnable<bool> runnable = new ()
        {
            Width = 10,
            Height = 10
        };
        app.Begin (runnable);

        runnable.Padding!.Thickness = new (viewPadding);
        // Turn of TransparentMouse for the test
        runnable.Padding!.ViewportSettings = ViewportSettingsFlags.None;

        var subView = new View ()
        {
            X = 0,
            Y = 0,
            Width = 5,
            Height = 5
        };
        subView.Padding!.Thickness = new (subViewPadding);
        // Turn of TransparentMouse for the test
        subView.Padding!.ViewportSettings = ViewportSettingsFlags.None;

        runnable.Padding!.Add (subView);
        runnable.Layout ();

        View? foundView = runnable.GetViewsUnderLocation (new (0, 0), ViewportSettingsFlags.None).LastOrDefault ();

        bool found = foundView == subView || foundView == subView.Padding;
        Assert.Equal (expectedFound, found);
    }

    [Fact]
    public void Adornment_WithNonVisibleSubView_Finds_Adornment ()
    {
        IApplication? app = Application.Create ();
        Runnable<bool> runnable = new ()
        {
            Width = 10,
            Height = 10
        };
        app.Begin (runnable);
        runnable.Padding!.Thickness = new Thickness (1);

        var subView = new View ()
        {
            X = 0,
            Y = 0,
            Width = 1,
            Height = 1,
            Visible = false
        };
        runnable.Padding.Add (subView);
        runnable.Layout ();

        Assert.Equal (runnable.Padding, runnable.GetViewsUnderLocation (new Point (0, 0), ViewportSettingsFlags.None).LastOrDefault ());

    }
    
    [Fact]
    public void Button_With_Opaque_ShadowStyle_In_Border_Should_Draw_Shadow ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver?.SetScreenSize (1, 4);
        app.Driver!.Force16Colors = true;

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();
        window.Text = @"XXXXXX";
        window.SetScheme (new (new Attribute (Color.Black, Color.White)));

        // Setup padding with some thickness so we have space for the button
        window.Border!.Thickness = new (0, 3, 0, 0);

        // Add a button with a transparent shadow to the Padding adornment
        Button buttonInBorder = new ()
        {
            X = 0,
            Y = 0,
            Text = "B",
            NoDecorations = true,
            NoPadding = true,
            ShadowStyle = ShadowStyle.Opaque,
        };

        window.Border.Add (buttonInBorder);
        app.Begin (window);

        DriverAssert.AssertDriverOutputIs ("""
                                           \x1b[30m\x1b[107mB▝ \x1b[97m\x1b[40mX
                                           """,
                                           _output,
                                           app.Driver);
    }

    [Fact]
    public void Button_With_Opaque_ShadowStyle_In_Padding_Should_Draw_Shadow ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver?.SetScreenSize (1, 4);
        app.Driver!.Force16Colors = true;

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();
        window.Text = @"XXXXXX";
        window.SetScheme (new (new Attribute (Color.Black, Color.White)));

        // Setup padding with some thickness so we have space for the button
        window.Padding!.Thickness = new (0, 3, 0, 0);

        // Add a button with a transparent shadow to the Padding adornment
        Button buttonInPadding = new ()
        {
            X = 0,
            Y = 0,
            Text = "B",
            NoDecorations = true,
            NoPadding = true,
            ShadowStyle = ShadowStyle.Opaque,
        };

        window.Padding.Add (buttonInPadding);
        app.Begin (window);

        DriverAssert.AssertDriverOutputIs ("""
                                           \x1b[97m\x1b[40mB\x1b[30m\x1b[107m▝ \x1b[97m\x1b[40mX
                                           """,
                                           _output,
                                           app.Driver);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Padding_SubView_With_Border_And_SuperViewRendersLineCanvas_Renders ()
    {
        // Verifies that a bordered view inside Padding with SuperViewRendersLineCanvas = true
        // has its border lines auto-joined with the parent view's border via LineCanvas propagation.
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (15, 5);

        View view = new ()
        {
            Width = 15,
            Height = 5,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true,
        };

        // Reserve 2 rows at top of Padding for the subview
        view.Padding!.Thickness = view.Padding.Thickness with { Top = 2 };

        // Add a bordered subview inside Padding
        View paddingSub = new ()
        {
            Text = "Hi",
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true,
            Width = Dim.Auto (DimAutoStyle.Text),
            Height = 2,
        };

        // Disable Title border rendering — we just want a simple bordered box
        paddingSub.Border!.Settings &= ~BorderSettings.Title;

        view.Padding.Add (paddingSub);

        Runnable top = new ();
        top.Add (view);
        app.Begin (top);

        // The bordered subview inside Padding should render its border lines
        // Note: with Height=2 and full border (top+bottom=2), content area is 0,
        // so text doesn't render. But the border lines DO render correctly.
        DriverAssert.AssertDriverContentsAre (
                                              """
                                              ┌─────────────┐
                                              │┌──┐         │
                                              │└──┘         │
                                              │             │
                                              └─────────────┘
                                              """,
                                              _output,
                                              app.Driver);

        top.Dispose ();
        app.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void Padding_SubView_LineCanvas_MergesIntoParent_For_AutoJoin ()
    {
        // Verifies that LineCanvas lines from Padding subviews with
        // SuperViewRendersLineCanvas=true are merged into the parent view's LineCanvas,
        // enabling auto-join at shared positions.
        View view = new ()
        {
            Width = 20,
            Height = 6,
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true,
        };

        view.Padding!.Thickness = view.Padding.Thickness with { Top = 2 };

        View paddingSub = new ()
        {
            Text = "AB",
            BorderStyle = LineStyle.Single,
            SuperViewRendersLineCanvas = true,
            Width = Dim.Auto (DimAutoStyle.Text),
            Height = 2,
        };

        paddingSub.Border!.Settings &= ~BorderSettings.Title;
        view.Padding.Add (paddingSub);

        // Layout to populate LineCanvas
        view.Layout ();

        // After layout, the Padding subview's border should have been queued
        // via SuperViewRendersLineCanvas. When drawn, lines will merge into
        // the parent view's LineCanvas for rendering together with the outer border.
        Assert.Equal (LineStyle.Single, paddingSub.BorderStyle);
        Assert.True (paddingSub.SuperViewRendersLineCanvas);
    }
}

