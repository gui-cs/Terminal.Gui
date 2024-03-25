using Xunit.Abstractions;
using static Unix.Terminal.Curses;

// Alias Console to MockConsole so we don't accidentally use Console

namespace Terminal.Gui.ViewTests;

public class LayoutTests
{
    private readonly ITestOutputHelper _output;
    public LayoutTests (ITestOutputHelper output) { _output = output; }

    // Tested in AbsoluteLayoutTests.cs
    // public void Pos_Dim_Are_Null_If_Not_Initialized_On_Constructor_IsAdded_False ()

    [Theory]
    [AutoInitShutdown]
    [InlineData (1)]
    [InlineData (2)]
    [InlineData (3)]
    [InlineData (4)]
    [InlineData (5)]
    [InlineData (6)]
    [InlineData (7)]
    [InlineData (8)]
    [InlineData (9)]
    [InlineData (10)]
    public void Dim_CenteredSubView_85_Percent_Height (int height)
    {
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };

        var subview = new Window
        {
            X = Pos.Center (), Y = Pos.Center (), Width = Dim.Percent (85), Height = Dim.Percent (85)
        };

        win.Add (subview);

        RunState rs = Application.Begin (win);
        var firstIteration = false;

        ((FakeDriver)Application.Driver).SetBufferSize (20, height);
        Application.RunIteration (ref rs, ref firstIteration);
        var expected = string.Empty;

        switch (height)
        {
            case 1:
                //Assert.Equal (new (0, 0, 17, 0), subview.Frame);
                expected = @"
────────────────────";

                break;
            case 2:
                //Assert.Equal (new (0, 0, 17, 1), subview.Frame);
                expected = @"
┌──────────────────┐
└──────────────────┘
";

                break;
            case 3:
                //Assert.Equal (new (0, 0, 17, 2), subview.Frame);
                expected = @"
┌──────────────────┐
│                  │
└──────────────────┘
";

                break;
            case 4:
                //Assert.Equal (new (0, 0, 17, 3), subview.Frame);
                expected = @"
┌──────────────────┐
│ ───────────────  │
│                  │
└──────────────────┘";

                break;
            case 5:
                //Assert.Equal (new (0, 0, 17, 3), subview.Frame);
                expected = @"
┌──────────────────┐
│ ┌─────────────┐  │
│ └─────────────┘  │
│                  │
└──────────────────┘";

                break;
            case 6:
                //Assert.Equal (new (0, 0, 17, 3), subview.Frame);
                expected = @"
┌──────────────────┐
│ ┌─────────────┐  │
│ │             │  │
│ └─────────────┘  │
│                  │
└──────────────────┘";

                break;
            case 7:
                //Assert.Equal (new (0, 0, 17, 3), subview.Frame);
                expected = @"
┌──────────────────┐
│ ┌─────────────┐  │
│ │             │  │
│ │             │  │
│ └─────────────┘  │
│                  │
└──────────────────┘";

                break;
            case 8:
                //Assert.Equal (new (0, 0, 17, 3), subview.Frame);
                expected = @"
┌──────────────────┐
│ ┌─────────────┐  │
│ │             │  │
│ │             │  │
│ │             │  │
│ └─────────────┘  │
│                  │
└──────────────────┘";

                break;
            case 9:
                //Assert.Equal (new (0, 0, 17, 3), subview.Frame);
                expected = @"
┌──────────────────┐
│                  │
│ ┌─────────────┐  │
│ │             │  │
│ │             │  │
│ │             │  │
│ └─────────────┘  │
│                  │
└──────────────────┘";

                break;
            case 10:
                //Assert.Equal (new (0, 0, 17, 3), subview.Frame);
                expected = @"
┌──────────────────┐
│                  │
│ ┌─────────────┐  │
│ │             │  │
│ │             │  │
│ │             │  │
│ │             │  │
│ └─────────────┘  │
│                  │
└──────────────────┘";

                break;
        }

        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Application.End (rs);
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (1)]
    [InlineData (2)]
    [InlineData (3)]
    [InlineData (4)]
    [InlineData (5)]
    [InlineData (6)]
    [InlineData (7)]
    [InlineData (8)]
    [InlineData (9)]
    [InlineData (10)]
    public void Dim_CenteredSubView_85_Percent_Width (int width)
    {
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };

        var subview = new Window
        {
            X = Pos.Center (), Y = Pos.Center (), Width = Dim.Percent (85), Height = Dim.Percent (85)
        };

        win.Add (subview);

        RunState rs = Application.Begin (win);
        var firstIteration = false;

        ((FakeDriver)Application.Driver).SetBufferSize (width, 7);
        Application.RunIteration (ref rs, ref firstIteration);
        var expected = string.Empty;

        switch (width)
        {
            case 1:
                Assert.Equal (new Rectangle (0, 0, 0, 4), subview.Frame);

                expected = @"
│
│
│
│
│
│
│";

                break;
            case 2:
                Assert.Equal (new Rectangle (0, 0, 0, 4), subview.Frame);

                expected = @"
┌┐
││
││
││
││
││
└┘";

                break;
            case 3:
                Assert.Equal (new Rectangle (0, 0, 0, 4), subview.Frame);

                expected = @"
┌─┐
│ │
│ │
│ │
│ │
│ │
└─┘";

                break;
            case 4:
                Assert.Equal (new Rectangle (0, 0, 1, 4), subview.Frame);

                expected = @"
┌──┐
││ │
││ │
││ │
││ │
│  │
└──┘";

                break;
            case 5:
                Assert.Equal (new Rectangle (0, 0, 2, 4), subview.Frame);

                expected = @"
┌───┐
│┌┐ │
│││ │
│││ │
│└┘ │
│   │
└───┘";

                break;
            case 6:
                Assert.Equal (new Rectangle (0, 0, 3, 4), subview.Frame);

                expected = @"
┌────┐
│┌─┐ │
││ │ │
││ │ │
│└─┘ │
│    │
└────┘";

                break;
            case 7:
                Assert.Equal (new Rectangle (0, 0, 4, 4), subview.Frame);

                expected = @"
┌─────┐
│┌──┐ │
││  │ │
││  │ │
│└──┘ │
│     │
└─────┘";

                break;
            case 8:
                Assert.Equal (new Rectangle (0, 0, 5, 4), subview.Frame);

                expected = @"
┌──────┐
│┌───┐ │
││   │ │
││   │ │
│└───┘ │
│      │
└──────┘";

                break;
            case 9:
                Assert.Equal (new Rectangle (1, 0, 5, 4), subview.Frame);

                expected = @"
┌───────┐
│ ┌───┐ │
│ │   │ │
│ │   │ │
│ └───┘ │
│       │
└───────┘";

                break;
            case 10:
                Assert.Equal (new Rectangle (1, 0, 6, 4), subview.Frame);

                expected = @"
┌────────┐
│ ┌────┐ │
│ │    │ │
│ │    │ │
│ └────┘ │
│        │
└────────┘";

                break;
        }

        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Application.End (rs);
    }

    [Fact]
    [AutoInitShutdown]
    public void DimFill_SizedCorrectly ()
    {
        var view = new View { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.Single };
        var top = new Toplevel ();
        top.Add (view);
        RunState rs = Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (32, 5);

        //view.SetNeedsLayout ();
        top.LayoutSubviews ();

        //view.SetRelativeLayout (new (0, 0, 32, 5));
        Assert.Equal (32, view.Frame.Width);
        Assert.Equal (5, view.Frame.Height);
    }

    [Fact]
    [TestRespondersDisposed]
    public void Draw_Throws_IndexOutOfRangeException_With_Negative_Bounds ()
    {
        Application.Init (new FakeDriver ());

        Toplevel top = new ();

        var view = new View { X = -2, Text = "view" };
        top.Add (view);

        Application.Iteration += (s, a) =>
                                 {
                                     Assert.Equal (-2, view.X);

                                     Application.RequestStop ();
                                 };

        try
        {
            Application.Run (top);
        }
        catch (IndexOutOfRangeException ex)
        {
            // After the fix this exception will not be caught.
            Assert.IsType<IndexOutOfRangeException> (ex);
        }

        top.Dispose ();
        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();
    }

    [Fact]
    [TestRespondersDisposed]
    public void Draw_Vertical_Throws_IndexOutOfRangeException_With_Negative_Bounds ()
    {
        Application.Init (new FakeDriver ());

        Toplevel top = new ();

        var view = new View { Y = -2, Height = 10, TextDirection = TextDirection.TopBottom_LeftRight, Text = "view" };
        top.Add (view);

        Application.Iteration += (s, a) =>
                                 {
                                     Assert.Equal (-2, view.Y);

                                     Application.RequestStop ();
                                 };

        try
        {
            Application.Run (top);
        }
        catch (IndexOutOfRangeException ex)
        {
            // After the fix this exception will not be caught.
            Assert.IsType<IndexOutOfRangeException> (ex);
        }

        top.Dispose ();
        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();
    }

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

    // Was named AutoSize_Pos_Validation_Do_Not_Throws_If_NewValue_Is_PosAbsolute_And_OldValue_Is_Another_Type_After_Sets_To_LayoutStyle_Absolute ()
    // but doesn't actually have anything to do with AutoSize.
    [Fact]
    public void
        Pos_Validation_Do_Not_Throws_If_NewValue_Is_PosAbsolute_And_OldValue_Is_Another_Type_After_Sets_To_LayoutStyle_Absolute ()
    {
        Application.Init (new FakeDriver ());

        Toplevel t = new ();

        var w = new Window { X = Pos.Left (t) + 2, Y = Pos.At (2) };

        var v = new View { X = Pos.Center (), Y = Pos.Percent (10) };

        w.Add (v);
        t.Add (w);

        t.Ready += (s, e) =>
                   {
                       v.Frame = new Rectangle (2, 2, 10, 10);
                       Assert.Equal (2, v.X = 2);
                       Assert.Equal (2, v.Y = 2);
                   };

        Application.Iteration += (s, a) => Application.RequestStop ();

        Application.Run (t);
        t.Dispose ();
        Application.Shutdown ();
    }

    [Fact]
    [SetupFakeDriver]
    public void PosCombine_DimCombine_View_With_SubViews ()
    {
        var clicked = false;
        Toplevel top = new Toplevel() { Width = 80, Height = 25 };
        var win1 = new Window { Id = "win1", Width = 20, Height = 10 };
        var view1 = new View { Text = "view1", AutoSize = true }; // BUGBUG: AutoSize or Width must be set
        var win2 = new Window { Id = "win2", Y = Pos.Bottom (view1) + 1, Width = 10, Height = 3 };
        var view2 = new View { Id = "view2", Width = Dim.Fill (), Height = 1, CanFocus = true };
        view2.MouseClick += (sender, e) => clicked = true;
        var view3 = new View { Id = "view3", Width = Dim.Fill (1), Height = 1, CanFocus = true };

        view2.Add (view3);
        win2.Add (view2);
        win1.Add (view1, win2);
        top.Add (win1);
        top.BeginInit();
        top.EndInit();

        Assert.Equal (new Rectangle (0, 0, 80, 25), top.Frame);
        Assert.Equal (new Rectangle (0, 0, 5, 1), view1.Frame);
        Assert.Equal (new Rectangle (0, 0, 20, 10), win1.Frame);
        Assert.Equal (new Rectangle (0, 2, 10, 3), win2.Frame);
        Assert.Equal (new Rectangle (0, 0, 8, 1), view2.Frame);
        Assert.Equal (new Rectangle (0, 0, 7, 1), view3.Frame);
        var foundView = View.FindDeepestView (top, 9, 4);
        Assert.Equal (foundView, view2);
    }

    [Fact]
    public void PosCombine_Refs_SuperView_Throws ()
    {
        Application.Init (new FakeDriver ());

        var top = new Toplevel ();
        var w = new Window { X = Pos.Left (top) + 2, Y = Pos.Top (top) + 2 };
        var f = new FrameView ();
        var v1 = new View { X = Pos.Left (w) + 2, Y = Pos.Top (w) + 2 };
        var v2 = new View { X = Pos.Left (v1) + 2, Y = Pos.Top (v1) + 2 };

        f.Add (v1, v2);
        w.Add (f);
        top.Add (w);
        Application.Begin (top);

        f.X = Pos.X (Application.Top) + Pos.X (v2) - Pos.X (v1);
        f.Y = Pos.Y (Application.Top) + Pos.Y (v2) - Pos.Y (v1);

        Application.Top.LayoutComplete += (s, e) =>
                                          {
                                              Assert.Equal (0, Application.Top.Frame.X);
                                              Assert.Equal (0, Application.Top.Frame.Y);
                                              Assert.Equal (2, w.Frame.X);
                                              Assert.Equal (2, w.Frame.Y);
                                              Assert.Equal (2, f.Frame.X);
                                              Assert.Equal (2, f.Frame.Y);
                                              Assert.Equal (4, v1.Frame.X);
                                              Assert.Equal (4, v1.Frame.Y);
                                              Assert.Equal (6, v2.Frame.X);
                                              Assert.Equal (6, v2.Frame.Y);
                                          };

        Application.Iteration += (s, a) => Application.RequestStop ();

        Assert.Throws<InvalidOperationException> (() => Application.Run ());
        top.Dispose ();
        Application.Shutdown ();
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
    [AutoInitShutdown]
    public void TrySetHeight_ForceValidatePosDim ()
    {
        var top = new View { X = 0, Y = 0, Height = 20 };

        var v = new View { Height = Dim.Fill (), ValidatePosDim = true };
        top.Add (v);

        Assert.False (v.TrySetHeight (10, out int rHeight));
        Assert.Equal (10, rHeight);

        v.Height = Dim.Fill (1);
        Assert.False (v.TrySetHeight (10, out rHeight));
        Assert.Equal (9, rHeight);

        v.Height = 0;
        Assert.True (v.TrySetHeight (10, out rHeight));
        Assert.Equal (10, rHeight);
        Assert.False (v.IsInitialized);

        var toplevel = new Toplevel ();
        toplevel.Add (top);
        Application.Begin (toplevel);

        Assert.True (v.IsInitialized);

        v.Height = 15;
        Assert.True (v.TrySetHeight (5, out rHeight));
        Assert.Equal (5, rHeight);
    }

    [Fact]
    [AutoInitShutdown]
    public void TrySetWidth_ForceValidatePosDim ()
    {
        var top = new View { X = 0, Y = 0, Width = 80 };

        var v = new View { Width = Dim.Fill (), ValidatePosDim = true };
        top.Add (v);

        Assert.False (v.TrySetWidth (70, out int rWidth));
        Assert.Equal (70, rWidth);

        v.Width = Dim.Fill (1);
        Assert.False (v.TrySetWidth (70, out rWidth));
        Assert.Equal (69, rWidth);

        v.Width = 0;
        Assert.True (v.TrySetWidth (70, out rWidth));
        Assert.Equal (70, rWidth);
        Assert.False (v.IsInitialized);

        var toplevel = new Toplevel ();
        toplevel.Add (top);
        Application.Begin (toplevel);

        Assert.True (v.IsInitialized);
        v.Width = 75;
        Assert.True (v.TrySetWidth (60, out rWidth));
        Assert.Equal (60, rWidth);
    }
}
