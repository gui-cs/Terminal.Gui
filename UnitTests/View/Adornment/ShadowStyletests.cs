using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class ShadowStyleTests (ITestOutputHelper _output)
{
    [Fact]
    public void Default_None ()
    {
        var view = new View ();
        Assert.Equal (ShadowStyle.None, view.ShadowStyle);
        Assert.Equal (ShadowStyle.None, view.Margin.ShadowStyle);
        view.Dispose ();
    }

    [Theory]
    [InlineData (ShadowStyle.None)]
    [InlineData (ShadowStyle.Opaque)]
    [InlineData (ShadowStyle.Transparent)]
    public void Set_View_Sets_Margin (ShadowStyle style)
    {
        var view = new View ();

        view.ShadowStyle = style;
        Assert.Equal (style, view.ShadowStyle);
        Assert.Equal (style, view.Margin.ShadowStyle);
        view.Dispose ();
    }

    [Theory]
    [InlineData (ShadowStyle.None, 3)]
    [InlineData (ShadowStyle.Opaque, 4)]
    [InlineData (ShadowStyle.Transparent, 4)]
    public void Style_Changes_Magin_Thickness (ShadowStyle style, int expected)
    {
        var view = new View ();
        view.Margin.Thickness = new (3);
        view.ShadowStyle = style;
        Assert.Equal (new (3, 3, expected, expected), view.Margin.Thickness);

        view.ShadowStyle = ShadowStyle.None;
        Assert.Equal (new (3), view.Margin.Thickness);
        view.Dispose ();
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
                    01#$
                    AB▌$
                    !▀▘$
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
        var superView = new View
        {
            Width = 4,
            Height = 4,
            Text = "!@#$".Repeat (4)
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
        superView.BeginInit ();
        superView.EndInit ();
        superView.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        view.Dispose ();
    }
}
