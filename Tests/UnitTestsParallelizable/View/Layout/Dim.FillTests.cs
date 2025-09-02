using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.LayoutTests;

public class DimFillTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;


    [Fact]
    public void DimFill_Equal ()
    {
        var margin1 = 0;
        var margin2 = 0;
        Dim dim1 = Dim.Fill (margin1);
        Dim dim2 = Dim.Fill (margin2);
        Assert.Equal (dim1, dim2);
    }

    // Tests that Dim.Fill honors the margin parameter correctly
    [Theory]
    [InlineData (0, true, 25)]
    [InlineData (0, false, 25)]
    [InlineData (1, true, 24)]
    [InlineData (1, false, 24)]
    [InlineData (2, true, 23)]
    [InlineData (2, false, 23)]
    [InlineData (-2, true, 27)]
    [InlineData (-2, false, 27)]
    public void DimFill_Margin (int margin, bool width, int expected)
    {
        var super = new View { Width = 25, Height = 25 };

        var view = new View
        {
            X = 0,
            Y = 0,
            Width = width ? Dim.Fill (margin) : 1,
            Height = width ? 1 : Dim.Fill (margin)
        };

        super.Add (view);
        super.BeginInit ();
        super.EndInit ();
        super.Layout ();

        Assert.Equal (25, super.Frame.Width);
        Assert.Equal (25, super.Frame.Height);

        if (width)
        {
            Assert.Equal (expected, view.Frame.Width);
            Assert.Equal (1, view.Frame.Height);
        }
        else
        {
            Assert.Equal (1, view.Frame.Width);
            Assert.Equal (expected, view.Frame.Height);
        }
    }

    // Tests that Dim.Fill fills the dimension REMAINING from the View's X position to the end of the super view's width
    [Theory]
    [InlineData (0, true, 25)]
    [InlineData (0, false, 25)]
    [InlineData (1, true, 24)]
    [InlineData (1, false, 24)]
    [InlineData (2, true, 23)]
    [InlineData (2, false, 23)]
    [InlineData (-2, true, 27)]
    [InlineData (-2, false, 27)]
    public void DimFill_Offset (int offset, bool width, int expected)
    {
        var super = new View { Width = 25, Height = 25 };

        var view = new View
        {
            X = width ? offset : 0,
            Y = width ? 0 : offset,
            Width = width ? Dim.Fill () : 1,
            Height = width ? 1 : Dim.Fill ()
        };

        super.Add (view);
        super.BeginInit ();
        super.EndInit ();
        super.Layout ();

        Assert.Equal (25, super.Frame.Width);
        Assert.Equal (25, super.Frame.Height);

        if (width)
        {
            Assert.Equal (expected, view.Frame.Width);
            Assert.Equal (1, view.Frame.Height);
        }
        else
        {
            Assert.Equal (1, view.Frame.Width);
            Assert.Equal (expected, view.Frame.Height);
        }
    }

    // TODO: Other Dim.Height tests (e.g. Equal?)

    [Fact]
    public void DimFill_SetsValue ()
    {
        var testMargin = 0;
        Dim dim = Dim.Fill ();
        Assert.Equal (testMargin, dim!.GetAnchor(0));

        testMargin = 0;
        dim = Dim.Fill (testMargin);
        Assert.Equal (testMargin, dim!.GetAnchor (0));

        testMargin = 5;
        dim = Dim.Fill (testMargin);
        Assert.Equal (-testMargin, dim!.GetAnchor (0));
    }

    [Fact]
    public void DimFill_Margin_Is_Dim_SetsValue ()
    {
        Dim testMargin = Dim.Func (_ => 0);
        Dim dim = Dim.Fill (testMargin);
        Assert.Equal (0, dim!.GetAnchor (0));


        testMargin = Dim.Func (_ => 5);
        dim = Dim.Fill (testMargin);
        Assert.Equal (-5, dim!.GetAnchor (0));
    }

    [Fact]
    public void DimFill_Calculate_ReturnsCorrectValue ()
    {
        var dim = Dim.Fill ();
        var result = dim.Calculate (0, 100, null, Dimension.None);
        Assert.Equal (100, result);
    }

    [Fact]
    public void ResizeView_With_Dim_Fill_After_IsInitialized ()
    {
        var super = new View { Frame = new (0, 0, 30, 80) };
        var view = new View { Width = Dim.Fill (), Height = Dim.Fill () };
        super.Add (view);

        view.Text = "New text\nNew line";
        super.Layout ();
        Rectangle expectedViewBounds = new (0, 0, 30, 80);

        Assert.Equal (expectedViewBounds, view.Viewport);
        Assert.False (view.IsInitialized);

        super.BeginInit ();
        super.EndInit ();

        Assert.True (view.IsInitialized);
        Assert.Equal (expectedViewBounds, view.Viewport);
    }
}
