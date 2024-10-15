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
    public void Add_Does_Not_Call_LayoutSubviews ()
    {
        var superView = new View { Id = "superView" };
        var view = new View { Id = "view" };
        bool layoutStartedRaised = false;
        bool layoutCompleteRaised = false;
        superView.LayoutStarted += (sender, e) => layoutStartedRaised = true;
        superView.LayoutComplete += (sender, e) => layoutCompleteRaised = true;

        superView.Add (view);

        Assert.False (layoutStartedRaised);
        Assert.False (layoutCompleteRaised);

        superView.Remove(view);

        superView.BeginInit();
        superView.EndInit ();

        superView.Add (view);

        Assert.False (layoutStartedRaised);
        Assert.False (layoutCompleteRaised);

    }

    [Fact]
    public void BeginEndInit_Do_Not_Call_LayoutSubviews ()
    {
        var superView = new View { Id = "superView" };
        bool layoutStartedRaised = false;
        bool layoutCompleteRaised = false;
        superView.LayoutStarted += (sender, e) => layoutStartedRaised = true;
        superView.LayoutComplete += (sender, e) => layoutCompleteRaised = true;
        superView.BeginInit ();
        superView.EndInit ();
        Assert.False (layoutStartedRaised);
        Assert.False (layoutCompleteRaised);
    }

    [Fact]
    public void LayoutSubViews_Raises_LayoutStarted_LayoutComplete ()
    {
        var superView = new View { Id = "superView" };
        int layoutStartedRaised = 0;
        int layoutCompleteRaised = 0;
        superView.LayoutStarted += (sender, e) => layoutStartedRaised++;
        superView.LayoutComplete += (sender, e) => layoutCompleteRaised++;

        superView.LayoutSubviews ();
        Assert.Equal (1, layoutStartedRaised);
        Assert.Equal (1, layoutCompleteRaised);

        superView.BeginInit ();
        superView.EndInit ();
        superView.LayoutSubviews ();
        Assert.Equal (2, layoutStartedRaised);
        Assert.Equal (2, layoutCompleteRaised);
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

    [Fact]
    public void LayoutSubviews_Uses_ContentSize ()
    {
        var superView = new View ()
        {
            Width = 5,
            Height = 5,
        };
        superView.SetContentSize (new (10, 10));
        var view = new View ()
        {
            X = Pos.Center ()
        };
        superView.Add (view);

        superView.LayoutSubviews ();

        Assert.Equal (5, view.Frame.X);
        superView.Dispose ();
    }

    // Test OnLayoutStarted/OnLayoutComplete - ensure that they are called at right times
    [Fact]
    public void LayoutSubviews_LayoutStarted_Complete ()
    {
        var superView = new View ();
        var view = new View ();
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();

        var layoutStarted = false;
        var layoutComplete = false;

        var borderLayoutStarted = false;
        var borderLayoutComplete = false;

        view.LayoutStarted += (sender, e) => layoutStarted = true;
        view.LayoutComplete += (sender, e) => layoutComplete = true;

        view.Border.LayoutStarted += (sender, e) =>
                                     {
                                         Assert.True (layoutStarted);
                                         borderLayoutStarted = true;
                                     };
        view.Border.LayoutComplete += (sender, e) =>
                                      {
                                          Assert.True (layoutStarted);
                                          Assert.False (layoutComplete);
                                          borderLayoutComplete = true;
                                      };

        superView.LayoutSubviews ();

        Assert.True (borderLayoutStarted);
        Assert.True (borderLayoutComplete);

        Assert.True (layoutStarted);
        Assert.True (layoutComplete);
        superView.Dispose ();
    }
}
