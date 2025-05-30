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

        Assert.Throws<LayoutException> (() => Application.Run (t));
        t.Dispose ();
        Application.Shutdown ();

        v2.Dispose ();
    }


    [Fact]
    [SetupFakeDriver]
    public void PosCombine_DimCombine_View_With_SubViews ()
    {
        Application.Top = new Toplevel () { Width = 80, Height = 25 };
        var win1 = new Window { Id = "win1", Width = 20, Height = 10 };
        var view1 = new View
        {
            Text = "view1",
            Width = Auto (DimAutoStyle.Text),
            Height = Auto (DimAutoStyle.Text)

        };
        var win2 = new Window { Id = "win2", Y = Pos.Bottom (view1) + 1, Width = 10, Height = 3 };
        var view2 = new View { Id = "view2", Width = Dim.Fill (), Height = 1, CanFocus = true };

        //var clicked = false;
        //view2.MouseClick += (sender, e) => clicked = true;
        var view3 = new View { Id = "view3", Width = Dim.Fill (1), Height = 1, CanFocus = true };

        view2.Add (view3);
        win2.Add (view2);
        win1.Add (view1, win2);
        Application.Top.Add (win1);
        Application.Top.Layout ();

        Assert.Equal (new Rectangle (0, 0, 80, 25), Application.Top.Frame);
        Assert.Equal (new Rectangle (0, 0, 5, 1), view1.Frame);
        Assert.Equal (new Rectangle (0, 0, 20, 10), win1.Frame);
        Assert.Equal (new Rectangle (0, 2, 10, 3), win2.Frame);
        Assert.Equal (new Rectangle (0, 0, 8, 1), view2.Frame);
        Assert.Equal (new Rectangle (0, 0, 7, 1), view3.Frame);
        var foundView = View.GetViewsUnderLocation (new Point(9, 4), ViewportSettingsFlags.None).LastOrDefault ();
        Assert.Equal (foundView, view2);
        Application.Top.Dispose ();
        Application.ResetState (ignoreDisposed: true);

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

        Application.Top.SubViewsLaidOut += (s, e) =>
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

        Assert.Throws<LayoutException> (() => Application.Run ());
        top.Dispose ();
        Application.ResetState (ignoreDisposed: true);
    }

}
