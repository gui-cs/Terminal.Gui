using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console

namespace Terminal.Gui.ViewTests;

public class PosTests
{
    private readonly ITestOutputHelper _output;
    public PosTests (ITestOutputHelper output) { _output = output; }

    [Fact]
    public void AnchorEnd_Equal ()
    {
        var n1 = 0;
        var n2 = 0;

        Pos pos1 = Pos.AnchorEnd (n1);
        Pos pos2 = Pos.AnchorEnd (n2);
        Assert.Equal (pos1, pos2);

        // Test inequality
        n2 = 5;
        pos2 = Pos.AnchorEnd (n2);
        Assert.NotEqual (pos1, pos2);
    }

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    [AutoInitShutdown]
    public void AnchorEnd_Equal_Inside_Window ()
    {
        var viewWidth = 10;
        var viewHeight = 1;

        var tv = new TextView
        {
            X = Pos.AnchorEnd (viewWidth), Y = Pos.AnchorEnd (viewHeight), Width = viewWidth, Height = viewHeight
        };

        var win = new Window ();

        win.Add (tv);

        Toplevel top = new ();
        top.Add (win);
        RunState rs = Application.Begin (top);

        Assert.Equal (new Rectangle (0, 0, 80, 25), top.Frame);
        Assert.Equal (new Rectangle (0, 0, 80, 25), win.Frame);
        Assert.Equal (new Rectangle (68, 22, 10, 1), tv.Frame);
        Application.End (rs);
    }

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    [AutoInitShutdown]
    public void AnchorEnd_Equal_Inside_Window_With_MenuBar_And_StatusBar_On_Toplevel ()
    {
        var viewWidth = 10;
        var viewHeight = 1;

        var tv = new TextView
        {
            X = Pos.AnchorEnd (viewWidth), Y = Pos.AnchorEnd (viewHeight), Width = viewWidth, Height = viewHeight
        };

        var win = new Window ();

        win.Add (tv);

        var menu = new MenuBar ();
        var status = new StatusBar ();
        Toplevel top = new ();
        top.Add (win, menu, status);
        RunState rs = Application.Begin (top);

        Assert.Equal (new Rectangle (0, 0, 80, 25), top.Frame);
        Assert.Equal (new Rectangle (0, 0, 80, 1), menu.Frame);
        Assert.Equal (new Rectangle (0, 24, 80, 1), status.Frame);
        Assert.Equal (new Rectangle (0, 1, 80, 23), win.Frame);
        Assert.Equal (new Rectangle (68, 20, 10, 1), tv.Frame);

        Application.End (rs);
    }

    [Fact]
    public void AnchorEnd_Negative_Throws ()
    {
        Pos pos;
        int n = -1;
        Assert.Throws<ArgumentException> (() => pos = Pos.AnchorEnd (n));
    }

    [Fact]
    public void AnchorEnd_SetsValue ()
    {
        var n = 0;
        Pos pos = Pos.AnchorEnd ();
        Assert.Equal ($"AnchorEnd({n})", pos.ToString ());

        n = 5;
        pos = Pos.AnchorEnd (n);
        Assert.Equal ($"AnchorEnd({n})", pos.ToString ());
    }

    // This test used to be Dialog_In_Window_With_TextField_And_Button_AnchorEnd in DialogTests.
    [Fact]
    [SetupFakeDriver]
    public void AnchorEnd_View_And_Button ()
    {
        ((FakeDriver)Application.Driver).SetBufferSize (20, 5);

        var b = $"{CM.Glyphs.LeftBracket} Ok {CM.Glyphs.RightBracket}";

        var frame = new FrameView { Width = 18, Height = 3 };
        Assert.Equal (16, frame.Bounds.Width);

        Button btn = null;

        int Btn_Width () { return btn?.Bounds.Width ?? 0; }

        btn = new Button { Text = "Ok", X = Pos.AnchorEnd () - Pos.Function (Btn_Width) };

        var view = new View
        {
            Text = "0123456789abcdefghij",

            // Dim.Fill (1) fills remaining space minus 1 (16 - 1 = 15)
            // Dim.Function (Btn_Width) is 6
            // Width should be 15 - 6 = 9
            Width = Dim.Fill (1) - Dim.Function (Btn_Width),
            Height = 1
        };

        frame.Add (btn, view);
        frame.BeginInit ();
        frame.EndInit ();
        frame.Draw ();

        Assert.Equal (6, btn.Bounds.Width);
        Assert.Equal (10, btn.Frame.X); // frame.Bounds.Width (16) - btn.Frame.Width (6) = 10
        Assert.Equal (0, btn.Frame.Y);
        Assert.Equal (6, btn.Frame.Width);
        Assert.Equal (1, btn.Frame.Height);

        Assert.Equal (9, view.Bounds.Width); // frame.Bounds.Width (16) - Dim.Fill (1) - Dim.Function (6) = 9
        Assert.Equal (0, view.Frame.X);
        Assert.Equal (0, view.Frame.Y);
        Assert.Equal (9, view.Frame.Width);
        Assert.Equal (1, view.Frame.Height);

        var expected = $@"
┌────────────────┐
│012345678 {
    b
}│
└────────────────┘
";
        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
    }

    [Fact]
    public void At_Equal ()
    {
        var n1 = 0;
        var n2 = 0;

        Pos pos1 = Pos.At (n1);
        Pos pos2 = Pos.At (n2);
        Assert.Equal (pos1, pos2);
    }

    [Fact]
    public void At_SetsValue ()
    {
        Pos pos = Pos.At (0);
        Assert.Equal ("Absolute(0)", pos.ToString ());

        pos = Pos.At (5);
        Assert.Equal ("Absolute(5)", pos.ToString ());

        pos = Pos.At (-1);
        Assert.Equal ("Absolute(-1)", pos.ToString ());
    }

    [Fact]
    public void Center_SetsValue ()
    {
        Pos pos = Pos.Center ();
        Assert.Equal ("Center", pos.ToString ());
    }

    [Fact]
    public void DoNotReturnPosCombine ()
    {
        var v = new View { Id = "V" };

        Pos pos = Pos.Left (v);

        Assert.Equal (
                      $"View(side=x,target=View(V){v.Frame})",
                      pos.ToString ()
                     );

        pos = Pos.X (v);

        Assert.Equal (
                      $"View(side=x,target=View(V){v.Frame})",
                      pos.ToString ()
                     );

        pos = Pos.Top (v);

        Assert.Equal (
                      $"View(side=y,target=View(V){v.Frame})",
                      pos.ToString ()
                     );

        pos = Pos.Y (v);

        Assert.Equal (
                      $"View(side=y,target=View(V){v.Frame})",
                      pos.ToString ()
                     );

        pos = Pos.Right (v);

        Assert.Equal (
                      $"View(side=right,target=View(V){v.Frame})",
                      pos.ToString ()
                     );

        pos = Pos.Bottom (v);

        Assert.Equal (
                      $"View(side=bottom,target=View(V){v.Frame})",
                      pos.ToString ()
                     );
    }

    [Fact]
    public void Function_Equal ()
    {
        Func<int> f1 = () => 0;
        Func<int> f2 = () => 0;

        Pos pos1 = Pos.Function (f1);
        Pos pos2 = Pos.Function (f2);
        Assert.Equal (pos1, pos2);

        f2 = () => 1;
        pos2 = Pos.Function (f2);
        Assert.NotEqual (pos1, pos2);
    }

    [Fact]
    public void Function_SetsValue ()
    {
        var text = "Test";
        Pos pos = Pos.Function (() => text.Length);
        Assert.Equal ("PosFunc(4)", pos.ToString ());

        text = "New Test";
        Assert.Equal ("PosFunc(8)", pos.ToString ());

        text = "";
        Assert.Equal ("PosFunc(0)", pos.ToString ());
    }

    [Fact]
    [TestRespondersDisposed]
    public void Internal_Tests ()
    {
        var posFactor = new Pos.PosFactor (0.10F);
        Assert.Equal (10, posFactor.Anchor (100));

        var posAnchorEnd = new Pos.PosAnchorEnd (1);
        Assert.Equal (99, posAnchorEnd.Anchor (100));

        var posCenter = new Pos.PosCenter ();
        Assert.Equal (50, posCenter.Anchor (100));

        var posAbsolute = new Pos.PosAbsolute (10);
        Assert.Equal (10, posAbsolute.Anchor (0));

        var posCombine = new Pos.PosCombine (true, posFactor, posAbsolute);
        Assert.Equal (posCombine._left, posFactor);
        Assert.Equal (posCombine._right, posAbsolute);
        Assert.Equal (20, posCombine.Anchor (100));

        posCombine = new Pos.PosCombine (true, posAbsolute, posFactor);
        Assert.Equal (posCombine._left, posAbsolute);
        Assert.Equal (posCombine._right, posFactor);
        Assert.Equal (20, posCombine.Anchor (100));

        var view = new View { Frame = new Rectangle (20, 10, 20, 1) };
        var posViewX = new Pos.PosView (view, 0);
        Assert.Equal (20, posViewX.Anchor (0));
        var posViewY = new Pos.PosView (view, 1);
        Assert.Equal (10, posViewY.Anchor (0));
        var posRight = new Pos.PosView (view, 2);
        Assert.Equal (40, posRight.Anchor (0));
        var posViewBottom = new Pos.PosView (view, 3);
        Assert.Equal (11, posViewBottom.Anchor (0));

        view.Dispose ();
    }

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    // See: https://github.com/gui-cs/Terminal.Gui/issues/504
    [Fact]
    [TestRespondersDisposed]
    public void LeftTopBottomRight_Win_ShouldNotThrow ()
    {
        // Setup Fake driver
        (Toplevel top, Window win, Button button) setup ()
        {
            Application.Init (new FakeDriver ());
            Application.Iteration += (s, a) => { Application.RequestStop (); };
            var win = new Window { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };
            var top = new Toplevel ();
            top.Add (win);

            var button = new Button { X = Pos.Center (), Text = "button" };
            win.Add (button);

            return (top, win, button);
        }

        RunState rs;

        void cleanup (RunState rs)
        {
            // Cleanup
            Application.End (rs);

            Application.Top.Dispose ();
            // Shutdown must be called to safely clean up Application if Init has been called
            Application.Shutdown ();
        }

        // Test cases:
        (Toplevel top, Window win, Button button) app = setup ();
        app.button.Y = Pos.Left (app.win);
        rs = Application.Begin (app.top);

        // If Application.RunState is used then we must use Application.RunLoop with the rs parameter
        Application.RunLoop (rs);
        cleanup (rs);

        app = setup ();
        app.button.Y = Pos.X (app.win);
        rs = Application.Begin (app.top);

        // If Application.RunState is used then we must use Application.RunLoop with the rs parameter
        Application.RunLoop (rs);
        cleanup (rs);

        app = setup ();
        app.button.Y = Pos.Top (app.win);
        rs = Application.Begin (app.top);

        // If Application.RunState is used then we must use Application.RunLoop with the rs parameter
        Application.RunLoop (rs);
        cleanup (rs);

        app = setup ();
        app.button.Y = Pos.Y (app.win);
        rs = Application.Begin (app.top);

        // If Application.RunState is used then we must use Application.RunLoop with the rs parameter
        Application.RunLoop (rs);
        cleanup (rs);

        app = setup ();
        app.button.Y = Pos.Bottom (app.win);
        rs = Application.Begin (app.top);

        // If Application.RunState is used then we must use Application.RunLoop with the rs parameter
        Application.RunLoop (rs);
        cleanup (rs);

        app = setup ();
        app.button.Y = Pos.Right (app.win);
        rs = Application.Begin (app.top);

        // If Application.RunState is used then we must use Application.RunLoop with the rs parameter
        Application.RunLoop (rs);
        cleanup (rs);
    }

    [Fact]
    public void New_Works ()
    {
        var pos = new Pos ();
        Assert.Equal ("Terminal.Gui.Pos", pos.ToString ());
    }

    [Fact]
    public void Percent_Equal ()
    {
        float n1 = 0;
        float n2 = 0;
        Pos pos1 = Pos.Percent (n1);
        Pos pos2 = Pos.Percent (n2);
        Assert.Equal (pos1, pos2);

        n1 = n2 = 1;
        pos1 = Pos.Percent (n1);
        pos2 = Pos.Percent (n2);
        Assert.Equal (pos1, pos2);

        n1 = n2 = 0.5f;
        pos1 = Pos.Percent (n1);
        pos2 = Pos.Percent (n2);
        Assert.Equal (pos1, pos2);

        n1 = n2 = 100f;
        pos1 = Pos.Percent (n1);
        pos2 = Pos.Percent (n2);
        Assert.Equal (pos1, pos2);

        n1 = 0;
        n2 = 1;
        pos1 = Pos.Percent (n1);
        pos2 = Pos.Percent (n2);
        Assert.NotEqual (pos1, pos2);

        n1 = 0.5f;
        n2 = 1.5f;
        pos1 = Pos.Percent (n1);
        pos2 = Pos.Percent (n2);
        Assert.NotEqual (pos1, pos2);
    }

    [Fact]
    public void Percent_SetsValue ()
    {
        float f = 0;
        Pos pos = Pos.Percent (f);
        Assert.Equal ($"Factor({f / 100:0.###})", pos.ToString ());
        f = 0.5F;
        pos = Pos.Percent (f);
        Assert.Equal ($"Factor({f / 100:0.###})", pos.ToString ());
        f = 100;
        pos = Pos.Percent (f);
        Assert.Equal ($"Factor({f / 100:0.###})", pos.ToString ());
    }

    [Fact]
    public void Percent_ThrowsOnIvalid ()
    {
        Pos pos = Pos.Percent (0);
        Assert.Throws<ArgumentException> (() => pos = Pos.Percent (-1));
        Assert.Throws<ArgumentException> (() => pos = Pos.Percent (101));
        Assert.Throws<ArgumentException> (() => pos = Pos.Percent (100.0001F));
        Assert.Throws<ArgumentException> (() => pos = Pos.Percent (1000001));
    }

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    [TestRespondersDisposed]
    public void Pos_Add_Operator ()
    {
        Application.Init (new FakeDriver ());

        Toplevel top = new ();

        var view = new View { X = 0, Y = 0, Width = 20, Height = 20 };
        var field = new TextField { X = 0, Y = 0, Width = 20 };
        var count = 0;

        field.KeyDown += (s, k) =>
                         {
                             if (k.KeyCode == KeyCode.Enter)
                             {
                                 field.Text = $"View {count}";
                                 var view2 = new View { X = 0, Y = field.Y, Width = 20, Text = field.Text };
                                 view.Add (view2);
                                 Assert.Equal ($"View {count}", view2.Text);
                                 Assert.Equal ($"Absolute({count})", view2.Y.ToString ());

                                 Assert.Equal ($"Absolute({count})", field.Y.ToString ());
                                 field.Y += 1;
                                 count++;
                                 Assert.Equal ($"Absolute({count})", field.Y.ToString ());
                             }
                         };

        Application.Iteration += (s, a) =>
                                 {
                                     while (count < 20)
                                     {
                                         field.NewKeyDownEvent (Key.Enter);
                                     }

                                     Application.RequestStop ();
                                 };

        var win = new Window ();
        win.Add (view);
        win.Add (field);

        top.Add (win);

        Application.Run (top);

        Assert.Equal (20, count);

        top.Dispose ();
        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();
    }

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    [TestRespondersDisposed]
    public void Pos_Subtract_Operator ()
    {
        Application.Init (new FakeDriver ());

        Toplevel top = new ();

        var view = new View { X = 0, Y = 0, Width = 20, Height = 20 };
        var field = new TextField { X = 0, Y = 0, Width = 20 };
        var count = 20;
        List<View> listViews = new ();

        for (var i = 0; i < count; i++)
        {
            field.Text = $"View {i}";
            var view2 = new View { X = 0, Y = field.Y, Width = 20, Text = field.Text };
            view.Add (view2);
            Assert.Equal ($"View {i}", view2.Text);
            Assert.Equal ($"Absolute({i})", field.Y.ToString ());
            listViews.Add (view2);

            Assert.Equal ($"Absolute({i})", field.Y.ToString ());
            field.Y += 1;
            Assert.Equal ($"Absolute({i + 1})", field.Y.ToString ());
        }

        field.KeyDown += (s, k) =>
                         {
                             if (k.KeyCode == KeyCode.Enter)
                             {
                                 Assert.Equal ($"View {count - 1}", listViews [count - 1].Text);
                                 view.Remove (listViews [count - 1]);
                                 listViews [count - 1].Dispose ();

                                 Assert.Equal ($"Absolute({count})", field.Y.ToString ());
                                 field.Y -= 1;
                                 count--;
                                 Assert.Equal ($"Absolute({count})", field.Y.ToString ());
                             }
                         };

        Application.Iteration += (s, a) =>
                                 {
                                     while (count > 0)
                                     {
                                         field.NewKeyDownEvent (Key.Enter);
                                     }

                                     Application.RequestStop ();
                                 };

        var win = new Window ();
        win.Add (view);
        win.Add (field);

        top.Add (win);

        Application.Run (top);

        Assert.Equal (0, count);

        top.Dispose ();
        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();
    }

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    public void Pos_Validation_Do_Not_Throws_If_NewValue_Is_PosAbsolute_And_OldValue_Is_Null ()
    {
        Application.Init (new FakeDriver ());

        Toplevel t = new ();

        var w = new Window { X = 1, Y = 2, Width = 3, Height = 5 };
        t.Add (w);

        t.Ready += (s, e) =>
                   {
                       Assert.Equal (2, w.X = 2);
                       Assert.Equal (2, w.Y = 2);
                   };

        Application.Iteration += (s, a) => Application.RequestStop ();

        Application.Run (t);
        t.Dispose ();
        Application.Shutdown ();
    }

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    public void PosCombine_Referencing_Same_View ()
    {
        var super = new View { Width = 10, Height = 10, Text = "super" };
        var view1 = new View { Width = 2, Height = 2, Text = "view1" };
        var view2 = new View { Width = 2, Height = 2, Text = "view2" };
        view2.X = Pos.AnchorEnd () - (Pos.Right (view2) - Pos.Left (view2));

        super.Add (view1, view2);
        super.BeginInit ();
        super.EndInit ();

        Exception exception = Record.Exception (super.LayoutSubviews);
        Assert.Null (exception);
        Assert.Equal (new Rectangle (0, 0, 10, 10), super.Frame);
        Assert.Equal (new Rectangle (0, 0, 2, 2), view1.Frame);
        Assert.Equal (new Rectangle (8, 0, 2, 2), view2.Frame);

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

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Theory]
    [AutoInitShutdown]
    [InlineData (true)]
    [InlineData (false)]
    public void PosPercentPlusOne (bool testHorizontal)
    {
        var container = new View { Width = 100, Height = 100 };

        var view = new View
        {
            X = testHorizontal ? Pos.Percent (50) + Pos.Percent (10) + 1 : 1,
            Y = testHorizontal ? 1 : Pos.Percent (50) + Pos.Percent (10) + 1,
            Width = 10,
            Height = 10
        };

        container.Add (view);
        var top = new Toplevel ();
        top.Add (container);
        top.LayoutSubviews ();

        Assert.Equal (100, container.Frame.Width);
        Assert.Equal (100, container.Frame.Height);

        if (testHorizontal)
        {
            Assert.Equal (61, view.Frame.X);
            Assert.Equal (1, view.Frame.Y);
        }
        else
        {
            Assert.Equal (1, view.Frame.X);
            Assert.Equal (61, view.Frame.Y);
        }
    }

    // TODO: Test Left, Top, Right bottom Equal

    /// <summary>Tests Pos.Left, Pos.X, Pos.Top, Pos.Y, Pos.Right, and Pos.Bottom set operations</summary>
    [Fact]
    [TestRespondersDisposed]
    public void PosSide_SetsValue ()
    {
        string side; // used in format string
        var testRect = Rectangle.Empty;
        var testInt = 0;
        Pos pos;

        // Pos.Left
        side = "x";
        testInt = 0;
        testRect = Rectangle.Empty;
        pos = Pos.Left (new View ());
        Assert.Equal ($"View(side={side},target=View(){testRect})", pos.ToString ());

        pos = Pos.Left (new View { Frame = testRect });
        Assert.Equal ($"View(side={side},target=View(){testRect})", pos.ToString ());

        testRect = new Rectangle (1, 2, 3, 4);
        pos = Pos.Left (new View { Frame = testRect });
        Assert.Equal ($"View(side={side},target=View(){testRect})", pos.ToString ());

        // Pos.Left(win) + 0
        pos = Pos.Left (new View { Frame = testRect }) + testInt;

        Assert.Equal (
                      $"Combine(View(side={side},target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );

        testInt = 1;

        // Pos.Left(win) +1
        pos = Pos.Left (new View { Frame = testRect }) + testInt;

        Assert.Equal (
                      $"Combine(View(side={side},target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );

        testInt = -1;

        // Pos.Left(win) -1
        pos = Pos.Left (new View { Frame = testRect }) - testInt;

        Assert.Equal (
                      $"Combine(View(side={side},target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );

        // Pos.X
        side = "x";
        testInt = 0;
        testRect = Rectangle.Empty;
        pos = Pos.X (new View ());
        Assert.Equal ($"View(side={side},target=View(){testRect})", pos.ToString ());

        pos = Pos.X (new View { Frame = testRect });
        Assert.Equal ($"View(side={side},target=View(){testRect})", pos.ToString ());

        testRect = new Rectangle (1, 2, 3, 4);
        pos = Pos.X (new View { Frame = testRect });
        Assert.Equal ($"View(side={side},target=View(){testRect})", pos.ToString ());

        // Pos.X(win) + 0
        pos = Pos.X (new View { Frame = testRect }) + testInt;

        Assert.Equal (
                      $"Combine(View(side={side},target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );

        testInt = 1;

        // Pos.X(win) +1
        pos = Pos.X (new View { Frame = testRect }) + testInt;

        Assert.Equal (
                      $"Combine(View(side={side},target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );

        testInt = -1;

        // Pos.X(win) -1
        pos = Pos.X (new View { Frame = testRect }) - testInt;

        Assert.Equal (
                      $"Combine(View(side={side},target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );

        // Pos.Top
        side = "y";
        testInt = 0;
        testRect = Rectangle.Empty;
        pos = Pos.Top (new View ());
        Assert.Equal ($"View(side={side},target=View(){testRect})", pos.ToString ());

        pos = Pos.Top (new View { Frame = testRect });
        Assert.Equal ($"View(side={side},target=View(){testRect})", pos.ToString ());

        testRect = new Rectangle (1, 2, 3, 4);
        pos = Pos.Top (new View { Frame = testRect });
        Assert.Equal ($"View(side={side},target=View(){testRect})", pos.ToString ());

        // Pos.Top(win) + 0
        pos = Pos.Top (new View { Frame = testRect }) + testInt;

        Assert.Equal (
                      $"Combine(View(side={side},target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );

        testInt = 1;

        // Pos.Top(win) +1
        pos = Pos.Top (new View { Frame = testRect }) + testInt;

        Assert.Equal (
                      $"Combine(View(side={side},target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );

        testInt = -1;

        // Pos.Top(win) -1
        pos = Pos.Top (new View { Frame = testRect }) - testInt;

        Assert.Equal (
                      $"Combine(View(side={side},target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );

        // Pos.Y
        side = "y";
        testInt = 0;
        testRect = Rectangle.Empty;
        pos = Pos.Y (new View ());
        Assert.Equal ($"View(side={side},target=View(){testRect})", pos.ToString ());

        pos = Pos.Y (new View { Frame = testRect });
        Assert.Equal ($"View(side={side},target=View(){testRect})", pos.ToString ());

        testRect = new Rectangle (1, 2, 3, 4);
        pos = Pos.Y (new View { Frame = testRect });
        Assert.Equal ($"View(side={side},target=View(){testRect})", pos.ToString ());

        // Pos.Y(win) + 0
        pos = Pos.Y (new View { Frame = testRect }) + testInt;

        Assert.Equal (
                      $"Combine(View(side={side},target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );

        testInt = 1;

        // Pos.Y(win) +1
        pos = Pos.Y (new View { Frame = testRect }) + testInt;

        Assert.Equal (
                      $"Combine(View(side={side},target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );

        testInt = -1;

        // Pos.Y(win) -1
        pos = Pos.Y (new View { Frame = testRect }) - testInt;

        Assert.Equal (
                      $"Combine(View(side={side},target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );

        // Pos.Bottom
        side = "bottom";
        testRect = Rectangle.Empty;
        testInt = 0;
        pos = Pos.Bottom (new View ());
        Assert.Equal ($"View(side={side},target=View(){testRect})", pos.ToString ());

        pos = Pos.Bottom (new View { Frame = testRect });
        Assert.Equal ($"View(side={side},target=View(){testRect})", pos.ToString ());

        testRect = new Rectangle (1, 2, 3, 4);
        pos = Pos.Bottom (new View { Frame = testRect });
        Assert.Equal ($"View(side={side},target=View(){testRect})", pos.ToString ());

        // Pos.Bottom(win) + 0
        pos = Pos.Bottom (new View { Frame = testRect }) + testInt;

        Assert.Equal (
                      $"Combine(View(side={side},target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );

        testInt = 1;

        // Pos.Bottom(win) +1
        pos = Pos.Bottom (new View { Frame = testRect }) + testInt;

        Assert.Equal (
                      $"Combine(View(side={side},target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );

        testInt = -1;

        // Pos.Bottom(win) -1
        pos = Pos.Bottom (new View { Frame = testRect }) - testInt;

        Assert.Equal (
                      $"Combine(View(side={side},target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );

#if DEBUG_IDISPOSABLE

        // HACK: Force clean up of Responders to avoid having to Dispose all the Views created above.
        Responder.Instances.Clear ();
#endif
    }

    [Fact]
    public void SetSide_Null_Throws ()
    {
        Pos pos = Pos.Left (null);
        Assert.Throws<NullReferenceException> (() => pos.ToString ());

        pos = Pos.X (null);
        Assert.Throws<NullReferenceException> (() => pos.ToString ());

        pos = Pos.Top (null);
        Assert.Throws<NullReferenceException> (() => pos.ToString ());

        pos = Pos.Y (null);
        Assert.Throws<NullReferenceException> (() => pos.ToString ());

        pos = Pos.Bottom (null);
        Assert.Throws<NullReferenceException> (() => pos.ToString ());

        pos = Pos.Right (null);
        Assert.Throws<NullReferenceException> (() => pos.ToString ());
    }
}
