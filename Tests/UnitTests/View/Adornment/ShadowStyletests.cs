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
            new (fg.GetDarkerColor (), bg.GetDarkerColor ())
        };

        var superView = new Toplevel
        {
            Height = 3,
            Width = 3,
            Text = "012ABC!@#",
            ColorScheme = new (new Attribute (fg, bg))
        };
        superView.TextFormatter.WordWrap = true;

        View view = new ()
        {
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            Text = "*",
            ShadowStyle = style,
            ColorScheme = new (Attribute.Default)
        };
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
}
