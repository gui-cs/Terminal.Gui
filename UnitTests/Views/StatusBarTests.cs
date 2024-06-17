using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;
public class StatusBarTests (ITestOutputHelper output)
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
    public void Redraw_Output ()
    {
    }

    [Fact]
    [AutoInitShutdown]
    public void Redraw_Output_CTRLQ ()
    {

    }

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
                                         Application.OnKeyDown (Application.QuitKey);
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

}
