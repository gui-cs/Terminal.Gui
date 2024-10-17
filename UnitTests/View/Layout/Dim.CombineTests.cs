using Xunit.Abstractions;
using static Terminal.Gui.Dim;

namespace Terminal.Gui.LayoutTests;

public class DimCombineTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;


    [Fact]
    public void DimCombine_Calculate_ReturnsCorrectValue ()
    {
        var dim1 = new DimAbsolute (10);
        var dim2 = new DimAbsolute (20);
        var dim = dim1 + dim2;
        var result = dim.Calculate (0, 100, null, Dimension.None);
        Assert.Equal (30, result);
    }


    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    [TestRespondersDisposed]
    public void DimCombine_View_Not_Added_Throws ()
    {
        var t = new View { Width = 80, Height = 50 };

        var super = new View { Width = Dim.Width (t) - 2, Height = Dim.Height (t) - 2 };
        t.Add (super);

        var sub = new View ();
        super.Add (sub);

        var v1 = new View { Width = Dim.Width (super) - 2, Height = Dim.Height (super) - 2 };
        var v2 = new View { Width = Dim.Width (v1) - 2, Height = Dim.Height (v1) - 2 };
        sub.Add (v1);

        // v2 not added to sub; should cause exception on Layout since it's referenced by sub.
        sub.Width = Dim.Fill () - Dim.Width (v2);
        sub.Height = Dim.Fill () - Dim.Height (v2);

        t.BeginInit ();
        t.EndInit ();

        Assert.Throws<InvalidOperationException> (() => t.LayoutSubviews ());
        t.Dispose ();
        v2.Dispose ();
    }

}
