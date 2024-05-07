using Microsoft.VisualStudio.TestPlatform.Utilities;
using Xunit.Abstractions;
using static Terminal.Gui.Dim;
using static Terminal.Gui.Pos;

namespace Terminal.Gui.PosDimTests;

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

        Exception exception = Record.Exception (super.LayoutSubviews);
        Assert.Null (exception);
        Assert.Equal (new (0, 0, 10, 10), super.Frame);
        Assert.Equal (new (0, 0, 2, 2), view1.Frame);
        Assert.Equal (new (8, 0, 2, 2), view2.Frame);

        super.Dispose ();
    }

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    [TestRespondersDisposed]
    public void PosCombine_Will_Throws ()
    {
        Application.Init (new FakeDriver ());

        Toplevel t = new ();

        var w = new Window { X = Pos.Left (t) + 2, Y = Pos.Top (t) + 2 };
        var f = new FrameView ();
        var v1 = new View { X = Pos.Left (w) + 2, Y = Pos.Top (w) + 2 };
        var v2 = new View { X = Pos.Left (v1) + 2, Y = Pos.Top (v1) + 2 };

        f.Add (v1); // v2 not added
        w.Add (f);
        t.Add (w);

        f.X = Pos.X (v2) - Pos.X (v1);
        f.Y = Pos.Y (v2) - Pos.Y (v1);

        Assert.Throws<InvalidOperationException> (() => Application.Run (t));
        t.Dispose ();
        Application.Shutdown ();

        v2.Dispose ();
    }
}
