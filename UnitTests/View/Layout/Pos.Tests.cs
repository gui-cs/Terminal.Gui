using Xunit.Abstractions;
using static Terminal.Gui.Dim;
using static Terminal.Gui.Pos;

namespace Terminal.Gui.LayoutTests;

public class PosTests (ITestOutputHelper output)
{
    // Was named AutoSize_Pos_Validation_Do_Not_Throws_If_NewValue_Is_PosAbsolute_And_OldValue_Is_Another_Type_After_Sets_To_LayoutStyle_Absolute ()
    // but doesn't actually have anything to do with AutoSize.
    [Fact]
    public void
        Pos_Validation_Do_Not_Throws_If_NewValue_Is_PosAbsolute_And_OldValue_Is_Another_Type_After_Sets_To_LayoutStyle_Absolute ()
    {
        Application.Init (new FakeDriver ());

        Toplevel t = new ();

        var w = new Window { X = Pos.Left (t) + 2, Y = Pos.Absolute (2) };

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
    public void PosCombine_Calculate_ReturnsExpectedValue ()
    {
        var posCombine = new PosCombine (true, new PosAbsolute (5), new PosAbsolute (3));
        var result = posCombine.Calculate (10, new DimAbsolute (2), null, Dimension.None);
        Assert.Equal (8, result);
    }

    [Fact]
    public void PosFactor_Calculate_ReturnsExpectedValue ()
    {
        var posFactor = new PosPercent (0.5f);
        var result = posFactor.Calculate (10, new DimAbsolute (2), null, Dimension.None);
        Assert.Equal (5, result);
    }

    [Fact]
    public void PosFunc_Calculate_ReturnsExpectedValue ()
    {
        var posFunc = new PosFunc (() => 5);
        var result = posFunc.Calculate (10, new DimAbsolute (2), null, Dimension.None);
        Assert.Equal (5, result);
    }

    [Fact]
    public void PosView_Calculate_ReturnsExpectedValue ()
    {
        var posView = new PosView (new View { Frame = new Rectangle (5, 5, 10, 10) }, 0);
        var result = posView.Calculate (10, new DimAbsolute (2), null, Dimension.None);
        Assert.Equal (5, result);
    }


    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    [TestRespondersDisposed]
    public void PosCombine_WHY_Throws ()
    {
        Application.Init (new FakeDriver ());

        Toplevel t = new Toplevel();

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

    [Fact]
    public void PosCombine_DoesNotReturn ()
    {
        var v = new View { Id = "V" };

        Pos pos = Pos.Left (v);

        Assert.Equal (
                      $"View(side=left,target=View(V){v.Frame})",
                      pos.ToString ()
                     );

        pos = Pos.X (v);

        Assert.Equal (
                      $"View(side=left,target=View(V){v.Frame})",
                      pos.ToString ()
                     );

        pos = Pos.Top (v);

        Assert.Equal (
                      $"View(side=top,target=View(V){v.Frame})",
                      pos.ToString ()
                     );

        pos = Pos.Y (v);

        Assert.Equal (
                      $"View(side=top,target=View(V){v.Frame})",
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
    public void PosFunction_Equal ()
    {
        Func<int> f1 = () => 0;
        Func<int> f2 = () => 0;

        Pos pos1 = Pos.Func (f1);
        Pos pos2 = Pos.Func (f2);
        Assert.Equal (pos1, pos2);

        f2 = () => 1;
        pos2 = Pos.Func (f2);
        Assert.NotEqual (pos1, pos2);
    }

    [Fact]
    public void PosFunction_SetsValue ()
    {
        var text = "Test";
        Pos pos = Pos.Func (() => text.Length);
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
        var posFactor = new PosPercent (0.10F);
        Assert.Equal (10, posFactor.Anchor (100));

        var posAnchorEnd = new PosAnchorEnd (1);
        Assert.Equal (99, posAnchorEnd.Anchor (100));

        var posCenter = new PosCenter ();
        Assert.Equal (50, posCenter.Anchor (100));

        var posAbsolute = new PosAbsolute (10);
        Assert.Equal (10, posAbsolute.Anchor (0));

        var posCombine = new PosCombine (true, posFactor, posAbsolute);
        Assert.Equal (posCombine.Left, posFactor);
        Assert.Equal (posCombine.Right, posAbsolute);
        Assert.Equal (20, posCombine.Anchor (100));

        posCombine = new (true, posAbsolute, posFactor);
        Assert.Equal (posCombine.Left, posAbsolute);
        Assert.Equal (posCombine.Right, posFactor);
        Assert.Equal (20, posCombine.Anchor (100));

        var view = new View { Frame = new (20, 10, 20, 1) };
        var posViewX = new PosView (view, Side.Left);
        Assert.Equal (20, posViewX.Anchor (0));
        var posViewY = new PosView (view, Side.Top);
        Assert.Equal (10, posViewY.Anchor (0));
        var posRight = new PosView (view, Side.Right);
        Assert.Equal (40, posRight.Anchor (0));
        var posViewBottom = new PosView (view, Side.Bottom);
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
        // Test cases:
        (Toplevel top, Window win, Button button) app = Setup ();
        app.button.Y = Pos.Left (app.win);
        RunState rs = Application.Begin (app.top);

        // If Application.RunState is used then we must use Application.RunLoop with the rs parameter
        Application.RunLoop (rs);
        Cleanup (rs);

        app = Setup ();
        app.button.Y = Pos.X (app.win);
        rs = Application.Begin (app.top);

        // If Application.RunState is used then we must use Application.RunLoop with the rs parameter
        Application.RunLoop (rs);
        Cleanup (rs);

        app = Setup ();
        app.button.Y = Pos.Top (app.win);
        rs = Application.Begin (app.top);

        // If Application.RunState is used then we must use Application.RunLoop with the rs parameter
        Application.RunLoop (rs);
        Cleanup (rs);

        app = Setup ();
        app.button.Y = Pos.Y (app.win);
        rs = Application.Begin (app.top);

        // If Application.RunState is used then we must use Application.RunLoop with the rs parameter
        Application.RunLoop (rs);
        Cleanup (rs);

        app = Setup ();
        app.button.Y = Pos.Bottom (app.win);
        rs = Application.Begin (app.top);

        // If Application.RunState is used then we must use Application.RunLoop with the rs parameter
        Application.RunLoop (rs);
        Cleanup (rs);

        app = Setup ();
        app.button.Y = Pos.Right (app.win);
        rs = Application.Begin (app.top);

        // If Application.RunState is used then we must use Application.RunLoop with the rs parameter
        Application.RunLoop (rs);
        Cleanup (rs);

        return;

        void Cleanup (RunState rs)
        {
            // Cleanup
            Application.End (rs);

            Application.Top.Dispose ();

            // Shutdown must be called to safely clean up Application if Init has been called
            Application.Shutdown ();
        }

        // Setup Fake driver
        (Toplevel top, Window win, Button button) Setup ()
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
    }

    [Fact]
    public void New_Works ()
    {
        var pos = new Pos ();
        Assert.Equal ("Terminal.Gui.Pos", pos.ToString ());
    }

    [Fact]
    public void PosPercent_Equal ()
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
    public void Validation_Does_Not_Throw_If_NewValue_Is_PosAbsolute_And_OldValue_Is_Null ()
    {
        Application.Init (new FakeDriver ());

        Toplevel t = new Toplevel ();

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
}
