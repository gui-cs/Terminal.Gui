using System.Globalization;
using System.Text;
using Xunit.Abstractions;
using static Terminal.Gui.Dim;

namespace Terminal.Gui.PosDimTests;

public class DimPercentTests
{
    private readonly ITestOutputHelper _output;

    [Fact]
    public void DimFactor_Calculate_ReturnsCorrectValue ()
    {
        var dim = new DimPercent (0.5f);
        var result = dim.Calculate (0, 100, null, Dimension.None);
        Assert.Equal (50, result);
    }


    [Fact]
    public void DimPercent_Equals ()
    {
        float n1 = 0;
        float n2 = 0;
        Dim dim1 = Dim.Percent (n1);
        Dim dim2 = Dim.Percent (n2);
        Assert.Equal (dim1, dim2);

        n1 = n2 = 1;
        dim1 = Dim.Percent (n1);
        dim2 = Dim.Percent (n2);
        Assert.Equal (dim1, dim2);

        n1 = n2 = 0.5f;
        dim1 = Dim.Percent (n1);
        dim2 = Dim.Percent (n2);
        Assert.Equal (dim1, dim2);

        n1 = n2 = 100f;
        dim1 = Dim.Percent (n1);
        dim2 = Dim.Percent (n2);
        Assert.Equal (dim1, dim2);

        n1 = n2 = 0.3f;
        dim1 = Dim.Percent (n1, true);
        dim2 = Dim.Percent (n2, true);
        Assert.Equal (dim1, dim2);

        n1 = n2 = 0.3f;
        dim1 = Dim.Percent (n1);
        dim2 = Dim.Percent (n2, true);
        Assert.NotEqual (dim1, dim2);

        n1 = 0;
        n2 = 1;
        dim1 = Dim.Percent (n1);
        dim2 = Dim.Percent (n2);
        Assert.NotEqual (dim1, dim2);

        n1 = 0.5f;
        n2 = 1.5f;
        dim1 = Dim.Percent (n1);
        dim2 = Dim.Percent (n2);
        Assert.NotEqual (dim1, dim2);
    }

    [Fact]
    public void DimPercent_Invalid_Throws ()
    {
        Dim dim = Dim.Percent (0);
        Assert.Throws<ArgumentException> (() => dim = Dim.Percent (-1));
        Assert.Throws<ArgumentException> (() => dim = Dim.Percent (101));
        Assert.Throws<ArgumentException> (() => dim = Dim.Percent (100.0001F));
        Assert.Throws<ArgumentException> (() => dim = Dim.Percent (1000001));
    }

    [Theory]
    [InlineData (0, false, true, 12)]
    [InlineData (0, false, false, 12)]
    [InlineData (1, false, true, 12)]
    [InlineData (1, false, false, 12)]
    [InlineData (2, false, true, 12)]
    [InlineData (2, false, false, 12)]

    [InlineData (0, true, true, 12)]
    [InlineData (0, true, false, 12)]
    [InlineData (1, true, true, 12)]
    [InlineData (1, true, false, 12)]
    [InlineData (2, true, true, 11)]
    [InlineData (2, true, false, 11)]
    public void DimPercent_Position (int position, bool usePosition, bool width, int expected)
    {
        var super = new View { Width = 25, Height = 25 };

        var view = new View
        {
            X = width ? position : 0,
            Y = width ? 0 : position,
            Width = width ? Dim.Percent (50, usePosition) : 1,
            Height = width ? 1 : Dim.Percent (50, usePosition)
        };

        super.Add (view);
        super.BeginInit ();
        super.EndInit ();
        super.LayoutSubviews ();

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

    [Theory]
    [InlineData (0, true)]
    [InlineData (0, false)]
    [InlineData (50, true)]
    [InlineData (50, false)]
    public void DimPercent_PlusOne (int startingDistance, bool testHorizontal)
    {
        var super = new View { Width = 100, Height = 100 };

        var view = new View
        {
            X = testHorizontal ? startingDistance : 0,
            Y = testHorizontal ? 0 : startingDistance,
            Width = testHorizontal ? Dim.Percent (50) + 1 : 1,
            Height = testHorizontal ? 1 : Dim.Percent (50) + 1
        };

        super.Add (view);
        super.BeginInit ();
        super.EndInit ();
        super.LayoutSubviews ();

        Assert.Equal (100, super.Frame.Width);
        Assert.Equal (100, super.Frame.Height);

        if (testHorizontal)
        {
            Assert.Equal (51, view.Frame.Width);
            Assert.Equal (1, view.Frame.Height);
        }
        else
        {
            Assert.Equal (1, view.Frame.Width);
            Assert.Equal (51, view.Frame.Height);
        }
    }

    [Fact]
    public void DimPercent_SetsValue ()
    {
        float f = 0;
        Dim dim = Dim.Percent (f);
        Assert.Equal ($"Percent({f / 100:0.###},{false})", dim.ToString ());
        f = 0.5F;
        dim = Dim.Percent (f);
        Assert.Equal ($"Percent({f / 100:0.###},{false})", dim.ToString ());
        f = 100;
        dim = Dim.Percent (f);
        Assert.Equal ($"Percent({f / 100:0.###},{false})", dim.ToString ());
    }

}
