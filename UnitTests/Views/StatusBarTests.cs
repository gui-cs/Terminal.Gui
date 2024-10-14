using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;
public class StatusBarTests
{
    [Fact]
    public void AddItemAt_RemoveItem_Replacing ()
    {
        var sb = new StatusBar ([
                                    new (Key.O.WithCtrl, "Open", null),
                                    new (Key.S.WithCtrl, "Save", null),
                                    new (Key.Q.WithCtrl, "Quit", null)
                                ]
                               );

        sb.AddShortcutAt (2, new (Key.C.WithCtrl, "Close", null));

        Assert.Equal ("Open", sb.Subviews [0].Title);
        Assert.Equal ("Save", sb.Subviews [1].Title);
        Assert.Equal ("Close", sb.Subviews [2].Title);
        Assert.Equal ("Quit", sb.Subviews [^1].Title);

        Assert.Equal ("Save", sb.RemoveShortcut (1).Title);

        Assert.Equal ("Open", sb.Subviews [0].Title);
        Assert.Equal ("Close", sb.Subviews [1].Title);
        Assert.Equal ("Quit", sb.Subviews [^1].Title);

        sb.AddShortcutAt (1, new Shortcut (Key.A.WithCtrl, "Save As", null));

        Assert.Equal ("Open", sb.Subviews [0].Title);
        Assert.Equal ("Save As", sb.Subviews [1].Title);
        Assert.Equal ("Quit", sb.Subviews [^1].Title);
    }

    //[Fact]
    //[AutoInitShutdown]
    //public void CanExecute_ProcessHotKey ()
    //{
    //    Window win = null;

    //    var statusBar = new StatusBar (
    //                                   new Shortcut []
    //                                   {
    //                                       new (
    //                                            KeyCode.CtrlMask | KeyCode.N,
    //                                            "~^N~ New",
    //                                            New,
    //                                            CanExecuteNew
    //                                           ),
    //                                       new (
    //                                            KeyCode.CtrlMask | KeyCode.C,
    //                                            "~^C~ Close",
    //                                            Close,
    //                                            CanExecuteClose
    //                                           )
    //                                   }
    //                                  );
    //    Toplevel top = new ();
    //    top.Add (statusBar);

    //    bool CanExecuteNew () { return win == null; }

    //    void New () { win = new (); }

    //    bool CanExecuteClose () { return win != null; }

    //    void Close () { win = null; }

    //    Application.Begin (top);

    //    Assert.Null (win);
    //    Assert.True (CanExecuteNew ());
    //    Assert.False (CanExecuteClose ());

    //    Assert.True (top.NewKeyDownEvent (Key.N.WithCtrl));
    //    Application.MainLoop.RunIteration ();
    //    Assert.NotNull (win);
    //    Assert.False (CanExecuteNew ());
    //    Assert.True (CanExecuteClose ());
    //    top.Dispose ();
    //}

    [Fact]
    [AutoInitShutdown]
    public void Run_Action_With_Key_And_Mouse ()
    {
        var msg = "";

        var sb = new StatusBar (
                                new Shortcut []
                                {
                                    new (
                                         Application.QuitKey,
                                         $"Quit",
                                         () => msg = "Quiting..."
                                        )
                                }
                               );
        var iteration = 0;

        Application.Iteration += (s, a) =>
                                 {
                                     if (iteration == 0)
                                     {
                                         Assert.Equal ("", msg);
                                         Application.RaiseKeyDownEvent (Application.QuitKey);
                                     }
                                     else if (iteration == 1)
                                     {
                                         Assert.Equal ("Quiting...", msg);
                                         msg = "";
                                         sb.NewMouseEvent (new () { Position = new (0, 0), Flags = MouseFlags.Button1Clicked });
                                     }
                                     else
                                     {
                                         Assert.Equal ("Quiting...", msg);

                                         Application.RequestStop ();
                                     }

                                     iteration++;
                                 };

        Application.Run ().Dispose ();
    }

    [Fact]
    public void StatusBar_Constructor_Default ()
    {
        var sb = new StatusBar ();

        Assert.Empty (sb.Subviews);
        Assert.True (sb.CanFocus);
        Assert.Equal (Colors.ColorSchemes ["Menu"], sb.ColorScheme);
        Assert.Equal (0, sb.X);
        Assert.Equal ("AnchorEnd()", sb.Y.ToString ());
        Assert.Equal (Dim.Fill (), sb.Width);
        Assert.Equal (1, sb.Frame.Height);
    }

    //[Fact]
    //public void RemoveAndThenAddStatusBar_ShouldNotChangeWidth ()
    //{
    //    StatusBar statusBar;
    //    StatusBar statusBar2;

    //    var w = new Window ();
    //    statusBar2 = new StatusBar () { Id = "statusBar2" };
    //    statusBar = new StatusBar () { Id = "statusBar" };
    //    w.Width = Dim.Fill (0);
    //    w.Height = Dim.Fill (0);
    //    w.X = 0;
    //    w.Y = 0;

    //    w.Visible = true;
    //    w.Modal = false;
    //    w.Title = "";
    //    statusBar.Width = Dim.Fill (0);
    //    statusBar.Height = 1;
    //    statusBar.X = 0;
    //    statusBar.Y = 0;
    //    statusBar.Visible = true;
    //    w.Add (statusBar);
    //    Assert.Equal (w.StatusBar, statusBar);

    //    statusBar2.Width = Dim.Fill (0);
    //    statusBar2.Height = 1;
    //    statusBar2.X = 0;
    //    statusBar2.Y = 4;
    //    statusBar2.Visible = true;
    //    w.Add (statusBar2);
    //    Assert.Equal (w.StatusBar, statusBar2);

    //    var menuBars = w.Subviews.OfType<StatusBar> ().ToArray ();
    //    Assert.Equal (2, menuBars.Length);

    //    Assert.Equal (Dim.Fill (0), menuBars [0].Width);
    //    Assert.Equal (Dim.Fill (0), menuBars [1].Width);

    //    // Goes wrong here
    //    w.Remove (statusBar);
    //    w.Remove (statusBar2);

    //    w.Add (statusBar);
    //    w.Add (statusBar2);

    //    // These assertions fail
    //    Assert.Equal (Dim.Fill (0), menuBars [0].Width);
    //    Assert.Equal (Dim.Fill (0), menuBars [1].Width);
    //}

}
