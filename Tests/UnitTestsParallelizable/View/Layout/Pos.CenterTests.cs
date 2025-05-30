using Xunit.Abstractions;
using static Terminal.Gui.ViewBase.Pos;

namespace Terminal.Gui.LayoutTests;

public class PosCenterTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void PosCenter_Constructor ()
    {
        var posCenter = new PosCenter ();
        Assert.NotNull (posCenter);
    }

    [Fact]
    public void PosCenter_ToString ()
    {
        var posCenter = new PosCenter ();
        var expectedString = "Center";

        Assert.Equal (expectedString, posCenter.ToString ());
    }

    [Fact]
    public void PosCenter_GetAnchor ()
    {
        var posCenter = new PosCenter ();
        var width = 50;
        int expectedAnchor = width / 2;

        Assert.Equal (expectedAnchor, posCenter.GetAnchor (width));
    }

    [Fact]
    public void PosCenter_CreatesCorrectInstance ()
    {
        Pos pos = Center ();
        Assert.IsType<PosCenter> (pos);
    }

    [Theory]
    [InlineData (10, 2, 4)]
    [InlineData (10, 10, 0)]
    [InlineData (10, 11, 0)]
    [InlineData (10, 12, -1)]
    [InlineData (19, 20, 0)]
    public void PosCenter_Calculate_ReturnsExpectedValue (int superviewDimension, int width, int expectedX)
    {
        var posCenter = new PosCenter ();
        int result = posCenter.Calculate (superviewDimension, new DimAbsolute (width), null!, Dimension.Width);
        Assert.Equal (expectedX, result);
    }

    [Fact]
    public void PosCenter_Bigger_Than_SuperView ()
    {
        var superView = new View { Width = 10, Height = 10 };
        var view = new View { X = Center (), Y = Center (), Width = 20, Height = 20 };
        superView.Add (view);
        superView.LayoutSubViews ();

        Assert.Equal (-5, view.Frame.Left);
        Assert.Equal (-5, view.Frame.Top);
    }
}
