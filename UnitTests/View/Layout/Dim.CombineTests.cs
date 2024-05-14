using Xunit.Abstractions;
using static Terminal.Gui.Dim;

namespace Terminal.Gui.PosDimTests;

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
    public void DimCombine_ObtuseScenario_Does_Not_Throw_If_Two_SubViews_Refs_The_Same_SuperView ()
    {
        var t = new View { Width = 80, Height = 25, Text = "top" };

        var w = new Window
        {
            Width = Dim.Width (t) - 2, // 78
            Height = Dim.Height (t) - 2 // 23
        };
        var f = new FrameView ();

        var v1 = new View
        {
            Width = Dim.Width (w) - 2, // 76
            Height = Dim.Height (w) - 2 // 21
        };

        var v2 = new View
        {
            Width = Dim.Width (v1) - 2, // 74
            Height = Dim.Height (v1) - 2 // 19
        };

        f.Add (v1, v2);
        w.Add (f);
        t.Add (w);
        t.BeginInit ();
        t.EndInit ();

        f.Width = Dim.Width (t) - Dim.Width (w) + 4; // 80 - 74 = 6
        f.Height = Dim.Height (t) - Dim.Height (w) + 4; // 25 - 19 = 6

        // BUGBUG: v2 - f references t and w here; t is f's super-superview and w is f's superview. This is supported!
        Exception exception = Record.Exception (t.LayoutSubviews);
        Assert.Null (exception);
        Assert.Equal (80, t.Frame.Width);
        Assert.Equal (25, t.Frame.Height);
        Assert.Equal (78, w.Frame.Width);
        Assert.Equal (23, w.Frame.Height);
        Assert.Equal (6, f.Frame.Width);
        Assert.Equal (6, f.Frame.Height);
        Assert.Equal (76, v1.Frame.Width);
        Assert.Equal (21, v1.Frame.Height);
        Assert.Equal (74, v2.Frame.Width);
        Assert.Equal (19, v2.Frame.Height);
        t.Dispose ();
    }

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.

    /// <summary>This is an intentionally obtuse test. See https://github.com/gui-cs/Terminal.Gui/issues/2461</summary>
    [Fact]
    [TestRespondersDisposed]
    public void DimCombine_ObtuseScenario_Throw_If_SuperView_Refs_SubView ()
    {
        var t = new View { Width = 80, Height = 25 };

        var w = new Window
        {
            Width = Dim.Width (t) - 2, // 78
            Height = Dim.Height (t) - 2 // 23
        };
        var f = new FrameView ();

        var v1 = new View
        {
            Width = Dim.Width (w) - 2, // 76
            Height = Dim.Height (w) - 2 // 21
        };

        var v2 = new View
        {
            Width = Dim.Width (v1) - 2, // 74
            Height = Dim.Height (v1) - 2 // 19
        };

        f.Add (v1, v2);
        w.Add (f);
        t.Add (w);
        t.BeginInit ();
        t.EndInit ();

        f.Width = Dim.Width (t) - Dim.Width (v2); // 80 - 74 = 6
        f.Height = Dim.Height (t) - Dim.Height (v2); // 25 - 19 = 6

        Assert.Throws<InvalidOperationException> (t.LayoutSubviews);
        Assert.Equal (80, t.Frame.Width);
        Assert.Equal (25, t.Frame.Height);
        Assert.Equal (78, w.Frame.Width);
        Assert.Equal (23, w.Frame.Height);
        Assert.Equal (6, f.Frame.Width);
        Assert.Equal (6, f.Frame.Height);
        Assert.Equal (76, v1.Frame.Width);
        Assert.Equal (21, v1.Frame.Height);
        Assert.Equal (74, v2.Frame.Width);
        Assert.Equal (19, v2.Frame.Height);
        t.Dispose ();
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
