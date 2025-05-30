using Microsoft.VisualStudio.TestPlatform.Utilities;
using UnitTests;
using Xunit.Abstractions;
using static Terminal.Gui.ViewBase.Dim;
using static Terminal.Gui.ViewBase.Pos;

namespace Terminal.Gui.LayoutTests;

public class PosCombineTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    public void PosCombine_Referencing_Same_View ()
    {
        var super = new View { Width = 10, Height = 10, Text = "super" };
        var view1 = new View { Width = 2, Height = 2, Text = "view1" };
        var view2 = new View { Width = 2, Height = 2, Text = "view2" };
        view2.X = Pos.AnchorEnd (0) - (Pos.Right (view2) - Pos.Left (view2));

        super.Add (view1, view2);
        super.BeginInit ();
        super.EndInit ();

        Exception exception = Record.Exception (super.LayoutSubViews);
        Assert.Null (exception);
        Assert.Equal (new (0, 0, 10, 10), super.Frame);
        Assert.Equal (new (0, 0, 2, 2), view1.Frame);
        Assert.Equal (new (8, 0, 2, 2), view2.Frame);

        super.Dispose ();
    }

}
