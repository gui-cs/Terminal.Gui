using Xunit.Abstractions;
using static Terminal.Gui.ViewBase.Dim;

namespace Terminal.Gui.LayoutTests;

public class DimFuncWithViewTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void DimFuncWithView_Equal ()
    {
        Func<View, int> f1 = v => v.Frame.Width;
        Func<View, int> f2 = v => v.Frame.Width;
        View view1 = new ();
        View view2 = new ();

        Dim dim1 = FuncWithView (f1, view1);
        Dim dim2 = FuncWithView (f1, view1);
        Assert.Equal (dim1, dim2);

        dim2 = FuncWithView (f2, view2);
        Assert.NotEqual (dim1, dim2);

        view2.Width = 1;
        Assert.NotEqual (dim1, dim2);
        Assert.Equal (1, f2 (view2));
    }

    [Fact]
    public void DimFuncWithView_SetsValue ()
    {
        View view = new () { Text = "Test" };
        Dim dim = FuncWithView (v => v.Text.Length, view);
        Assert.Equal ("DimFuncWithView(4)", dim.ToString ());

        view.Text = "New Test";
        Assert.Equal ("DimFuncWithView(8)", dim.ToString ());

        view.Text = "";
        Assert.Equal ("DimFuncWithView(0)", dim.ToString ());
    }

    [Fact]
    public void DimFuncWithView_Calculate_ReturnsCorrectValue ()
    {
        View view = new () { Width = 10 };
        var dim = new DimFuncWithView (v => v.Frame.Width, view);
        int result = dim.Calculate (0, 100, view, Dimension.None);
        Assert.Equal (10, result);
    }

    [Fact]
    public void DimFuncWithView_Throws_ArgumentNullException_If_View_Is_Null ()
    {
        Assert.Throws<ArgumentNullException> (() => FuncWithView (v => 0, null));
    }
}
