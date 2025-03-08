using UnitTests;

namespace Terminal.Gui.LayoutTests;

public class PosAnchorEndTests ()
{
    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    [AutoInitShutdown]
    public void PosAnchorEnd_Equal_Inside_Window ()
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

        Assert.Equal (new (0, 0, 80, 25), top.Frame);
        Assert.Equal (new (0, 0, 80, 25), win.Frame);
        Assert.Equal (new (68, 22, 10, 1), tv.Frame);
        Application.End (rs);
        top.Dispose ();
    }

    //// TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    //// TODO: A new test that calls SetRelativeLayout directly is needed.
    //[Fact]
    //[AutoInitShutdown]
    //public void  PosAnchorEnd_Equal_Inside_Window_With_MenuBar_And_StatusBar_On_Toplevel ()
    //{
    //    var viewWidth = 10;
    //    var viewHeight = 1;

    //    var tv = new TextView
    //    {
    //        X = Pos.AnchorEnd (viewWidth), Y = Pos.AnchorEnd (viewHeight), Width = viewWidth, Height = viewHeight
    //    };

    //    var win = new Window ();

    //    win.Add (tv);

    //    var menu = new MenuBar ();
    //    var status = new StatusBar ();
    //    Toplevel top = new ();
    //    top.Add (win, menu, status);
    //    RunState rs = Application.Begin (top);

    //    Assert.Equal (new (0, 0, 80, 25), top.Frame);
    //    Assert.Equal (new (0, 0, 80, 1), menu.Frame);
    //    Assert.Equal (new (0, 24, 80, 1), status.Frame);
    //    Assert.Equal (new (0, 1, 80, 23), win.Frame);
    //    Assert.Equal (new (68, 20, 10, 1), tv.Frame);

    //    Application.End (rs);
    //    top.Dispose ();
    //}
}
