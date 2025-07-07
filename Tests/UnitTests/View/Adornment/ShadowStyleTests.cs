using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class ShadowStyleTests (ITestOutputHelper output)
{
    [Theory]
    [InlineData (
                    ShadowStyle.None,
                    """
                    011
                    111
                    111
                    """)]
    [InlineData (
                    ShadowStyle.Transparent,
                    """
                    031
                    131
                    111
                    """)]
    [InlineData (
                    ShadowStyle.Opaque,
                    """
                    021
                    221
                    111
                    """)]
    [SetupFakeDriver]
    public void ShadowView_Colors (ShadowStyle style, string expectedAttrs)
    {
        ((FakeDriver)Application.Driver!).SetBufferSize (5, 5);
        Color fg = Color.Red;
        Color bg = Color.Green;

        // 0 - View
        // 1 - SuperView
        // 2 - Opaque - fg is Black, bg is SuperView.Bg
        // 3 - Transparent - fg is darker fg, bg is darker bg
        Attribute [] attributes =
        {
            Attribute.Default,
            new (fg, bg),
            new (Color.Black, bg),
            new (fg.GetDimColor (), bg.GetDimColor ())
        };

        var superView = new Toplevel
        {
            Height = 3,
            Width = 3,
            Text = "012ABC!@#",
        };
        superView.SetScheme (new (new Attribute (fg, bg)));
        superView.TextFormatter.WordWrap = true;

        View view = new ()
        {
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            Text = "*",
            ShadowStyle = style,
        };
        view.SetScheme (new (Attribute.Default));

        superView.Add (view);
        Application.TopLevels.Push (superView);
        Application.LayoutAndDraw (true);
        DriverAssert.AssertDriverAttributesAre (expectedAttrs, output, Application.Driver, attributes);
        Application.ResetState (true);
    }

    // Visual tests
    [Theory]
    [InlineData (
                    ShadowStyle.None,
                    """
                    01#$
                    AB#$
                    !@#$
                    !@#$
                    """)]
    [InlineData (
                    ShadowStyle.Opaque,
                    """
                    01▖$
                    AB▌$
                    ▝▀▘$
                    !@#$
                    """)]
    [InlineData (
                    ShadowStyle.Transparent,
                    """
                    01#$
                    AB#$
                    !@#$
                    !@#$
                    """)]
    [SetupFakeDriver]
    public void Visual_Test (ShadowStyle style, string expected)
    {
        ((FakeDriver)Application.Driver!).SetBufferSize (5, 5);

        var superView = new Toplevel
        {
            Width = 4,
            Height = 4,
            Text = "!@#$".Repeat (4)!
        };
        superView.TextFormatter.WordWrap = true;

        var view = new View
        {
            Text = "01\nAB",
            Width = Dim.Auto (),
            Height = Dim.Auto ()
        };
        view.ShadowStyle = style;
        superView.Add (view);
        Application.TopLevels.Push (superView);
        Application.LayoutAndDraw (true);

        DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
        view.Dispose ();
        Application.ResetState (true);
    }


    [Theory]
    [InlineData (ShadowStyle.None, 0, 0, 0, 0)]
    [InlineData (ShadowStyle.Opaque, 1, 0, 0, 1)]
    [InlineData (ShadowStyle.Transparent, 1, 0, 0, 1)]
    public void ShadowStyle_Button1Pressed_Causes_Movement (ShadowStyle style, int expectedLeft, int expectedTop, int expectedRight, int expectedBottom)
    {
        Application.Init (new FakeDriver ());
        var superView = new View
        {
            Height = 10, Width = 10
        };

        View view = new ()
        {
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            Text = "0123",
            HighlightStates = MouseState.Pressed,
            ShadowStyle = style,
            CanFocus = true
        };

        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        Thickness origThickness = view.Margin!.Thickness;
        view.NewMouseEvent (new () { Flags = MouseFlags.Button1Pressed, Position = new (0, 0) });
        Assert.Equal (new (expectedLeft, expectedTop, expectedRight, expectedBottom), view.Margin.Thickness);

        view.NewMouseEvent (new () { Flags = MouseFlags.Button1Released, Position = new (0, 0) });
        Assert.Equal (origThickness, view.Margin.Thickness);

        // Button1Pressed, Button1Released cause Application.MouseGrabHandler.MouseGrabView to be set
        Application.ResetState (true);
    }
}
