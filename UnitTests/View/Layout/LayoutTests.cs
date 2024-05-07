using Xunit.Abstractions;

namespace Terminal.Gui.LayoutTests;

public class LayoutTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void LayoutSubviews_No_SuperView ()
    {
        var root = new View ();

        var first = new View
        {
            Id = "first",
            X = 1,
            Y = 2,
            Height = 3,
            Width = 4
        };
        root.Add (first);

        var second = new View { Id = "second" };
        root.Add (second);

        second.X = Pos.Right (first) + 1;

        root.LayoutSubviews ();

        Assert.Equal (6, second.Frame.X);
        root.Dispose ();
        first.Dispose ();
        second.Dispose ();
    }

    [Fact]
    public void LayoutSubviews_RootHas_SuperView ()
    {
        var top = new View ();
        var root = new View ();
        top.Add (root);

        var first = new View
        {
            Id = "first",
            X = 1,
            Y = 2,
            Height = 3,
            Width = 4
        };
        root.Add (first);

        var second = new View { Id = "second" };
        root.Add (second);

        second.X = Pos.Right (first) + 1;

        root.LayoutSubviews ();

        Assert.Equal (6, second.Frame.X);
        root.Dispose ();
        top.Dispose ();
        first.Dispose ();
        second.Dispose ();
    }

    [Fact]
    public void LayoutSubviews_ViewThatRefsSubView_Throws ()
    {
        var root = new View ();
        var super = new View ();
        root.Add (super);
        var sub = new View ();
        super.Add (sub);
        super.Width = Dim.Width (sub);
        Assert.Throws<InvalidOperationException> (() => root.LayoutSubviews ());
        root.Dispose ();
        super.Dispose ();
    }

    [Fact]
    public void TopologicalSort_Missing_Add ()
    {
        var root = new View ();
        var sub1 = new View ();
        root.Add (sub1);
        var sub2 = new View ();
        sub1.Width = Dim.Width (sub2);

        Assert.Throws<InvalidOperationException> (() => root.LayoutSubviews ());

        sub2.Width = Dim.Width (sub1);

        Assert.Throws<InvalidOperationException> (() => root.LayoutSubviews ());
        root.Dispose ();
        sub1.Dispose ();
        sub2.Dispose ();
    }

    [Fact]
    public void TopologicalSort_Recursive_Ref ()
    {
        var root = new View ();
        var sub1 = new View ();
        root.Add (sub1);
        var sub2 = new View ();
        root.Add (sub2);
        sub2.Width = Dim.Width (sub2);

        Exception exception = Record.Exception (root.LayoutSubviews);
        Assert.Null (exception);
        root.Dispose ();
        sub1.Dispose ();
        sub2.Dispose ();
    }

    //[Fact]
    //[AutoInitShutdown]
    //public void TrySetHeight_ForceValidatePosDim ()
    //{
    //    var top = new View { X = 0, Y = 0, Height = 20 };

    //    var v = new View { Height = Dim.Fill (), ValidatePosDim = true };
    //    top.Add (v);

    //    Assert.False (v.TrySetHeight (10, out int rHeight));
    //    Assert.Equal (10, rHeight);

    //    v.Height = Dim.Fill (1);
    //    Assert.False (v.TrySetHeight (10, out rHeight));
    //    Assert.Equal (9, rHeight);

    //    v.Height = 0;
    //    Assert.True (v.TrySetHeight (10, out rHeight));
    //    Assert.Equal (10, rHeight);
    //    Assert.False (v.IsInitialized);

    //    var toplevel = new Toplevel ();
    //    toplevel.Add (top);
    //    Application.Begin (toplevel);

    //    Assert.True (v.IsInitialized);

    //    v.Height = 15;
    //    Assert.True (v.TrySetHeight (5, out rHeight));
    //    Assert.Equal (5, rHeight);
    //}

    //[Fact]
    //[AutoInitShutdown]
    //public void TrySetWidth_ForceValidatePosDim ()
    //{
    //    var top = new View { X = 0, Y = 0, Width = 80 };

    //    var v = new View { Width = Dim.Fill (), ValidatePosDim = true };
    //    top.Add (v);

    //    Assert.False (v.TrySetWidth (70, out int rWidth));
    //    Assert.Equal (70, rWidth);

    //    v.Width = Dim.Fill (1);
    //    Assert.False (v.TrySetWidth (70, out rWidth));
    //    Assert.Equal (69, rWidth);

    //    v.Width = 0;
    //    Assert.True (v.TrySetWidth (70, out rWidth));
    //    Assert.Equal (70, rWidth);
    //    Assert.False (v.IsInitialized);

    //    var toplevel = new Toplevel ();
    //    toplevel.Add (top);
    //    Application.Begin (toplevel);

    //    Assert.True (v.IsInitialized);
    //    v.Width = 75;
    //    Assert.True (v.TrySetWidth (60, out rWidth));
    //    Assert.Equal (60, rWidth);
    //}
}
