using Xunit.Abstractions;
using static Terminal.Gui.Dim;

namespace Terminal.Gui.LayoutTests;

public class DimViewTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void DimView_Equal ()
    {
        var view1 = new View ();
        var view2 = new View ();

        Dim dim1 = Width (view1);
        Dim dim2 = Width (view1);
        Assert.Equal (dim1, dim2);

        dim2 = Width (view2);
        Assert.NotEqual (dim1, dim2);

        dim2 = Height (view1);
        Assert.NotEqual (dim1, dim2);
    }


    [Fact]
    public void DimView_Calculate_ReturnsCorrectValue ()
    {
        var view = new View { Width = 10 };
        view.Layout ();
        var dim = new DimView (view, Dimension.Width);
        var result = dim.Calculate (0, 100, null, Dimension.None);
        Assert.Equal (10, result);
    }

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    [TestRespondersDisposed]
    public void Dim_Referencing_SuperView_Does_Not_Throw ()
    {
        var super = new View { Width = 10, Height = 10, Text = "super" };

        var view = new View
        {
            Width = Dim.Width (super), // this is allowed
            Height = Dim.Height (super), // this is allowed
            Text = "view"
        };

        super.Add (view);
        super.BeginInit ();
        super.EndInit ();

        Exception exception = Record.Exception (super.LayoutSubviews);
        Assert.Null (exception);
        super.Dispose ();
    }
}
