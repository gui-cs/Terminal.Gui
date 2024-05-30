using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class StatusBarTests (ITestOutputHelper output)
{
    [Fact]
    public void AddItemAt_RemoveItem_Replacing ()
    {
        var sb = new StatusBar (
                                new StatusItem []
                                {
                                    new (KeyCode.CtrlMask | KeyCode.Q, "~^O~ Open", null),
                                    new (KeyCode.CtrlMask | KeyCode.Q, "~^S~ Save", null),
                                    new (KeyCode.CtrlMask | KeyCode.Q, "~^Q~ Quit", null)
                                }
                               );

        sb.AddItemAt (2, new (KeyCode.CtrlMask | KeyCode.Q, "~^C~ Close", null));

        Assert.Equal ("~^O~ Open", sb.Items [0].Title);
        Assert.Equal ("~^S~ Save", sb.Items [1].Title);
        Assert.Equal ("~^C~ Close", sb.Items [2].Title);
        Assert.Equal ("~^Q~ Quit", sb.Items [^1].Title);

        Assert.Equal ("~^S~ Save", sb.RemoveItem (1).Title);

        Assert.Equal ("~^O~ Open", sb.Items [0].Title);
        Assert.Equal ("~^C~ Close", sb.Items [1].Title);
        Assert.Equal ("~^Q~ Quit", sb.Items [^1].Title);

        sb.Items [1] = new (KeyCode.CtrlMask | KeyCode.A, "~^A~ Save As", null);

        Assert.Equal ("~^O~ Open", sb.Items [0].Title);
        Assert.Equal ("~^A~ Save As", sb.Items [1].Title);
        Assert.Equal ("~^Q~ Quit", sb.Items [^1].Title);
    }

    [Fact]
    [AutoInitShutdown]
    public void CanExecute_ProcessHotKey ()
    {
        Window win = null;

        var statusBar = new StatusBar (
                                       new StatusItem []
                                       {
                                           new (
                                                KeyCode.CtrlMask | KeyCode.N,
                                                "~^N~ New",
                                                New,
                                                CanExecuteNew
                                               ),
                                           new (
                                                KeyCode.CtrlMask | KeyCode.C,
                                                "~^C~ Close",
                                                Close,
                                                CanExecuteClose
                                               )
                                       }
                                      );
        Toplevel top = new ();
        top.Add (statusBar);

        bool CanExecuteNew () { return win == null; }

        void New () { win = new (); }

        bool CanExecuteClose () { return win != null; }

        void Close () { win = null; }

        Application.Begin (top);

        Assert.Null (win);
        Assert.True (CanExecuteNew ());
        Assert.False (CanExecuteClose ());

        Assert.True (top.NewKeyDownEvent (Key.N.WithCtrl));
        Application.MainLoop.RunIteration ();
        Assert.NotNull (win);
        Assert.False (CanExecuteNew ());
        Assert.True (CanExecuteClose ());
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Redraw_Output ()
    {
        var sb = new StatusBar (
                                new StatusItem []
                                {
                                    new (KeyCode.CtrlMask | KeyCode.O, "~^O~ Open", null),
                                    new (Application.QuitKey, $"{Application.QuitKey} to Quit!", null)
                                }
                               );
        var top = new Toplevel ();
        top.Add (sb);

        sb.OnDrawContent (sb.Viewport);

        var expected = @$"
^O Open {
    CM.Glyphs.VLine
} Ctrl+Q to Quit!
";
        TestHelpers.AssertDriverContentsAre (expected, output);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Redraw_Output_CTRLQ ()
    {
        var sb = new StatusBar (
                                new StatusItem []
                                {
                                    new (KeyCode.CtrlMask | KeyCode.O, "~CTRL-O~ Open", null),
                                    new (KeyCode.CtrlMask | KeyCode.Q, "~CTRL-Q~ Quit", null)
                                }
                               );
        var top = new Toplevel ();
        top.Add (sb);
        sb.OnDrawContent (sb.Viewport);

        var expected = @$"
CTRL-O Open {
    CM.Glyphs.VLine
} CTRL-Q Quit
";

        TestHelpers.AssertDriverContentsAre (expected, output);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Run_Action_With_Key_And_Mouse ()
    {
        var msg = "";

        var sb = new StatusBar (
                                new StatusItem []
                                {
                                    new (
                                         Application.QuitKey,
                                         $"{Application.QuitKey} to Quit",
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
                                         sb.NewKeyDownEvent (Key.Q.WithCtrl);
                                     }
                                     else if (iteration == 1)
                                     {
                                         Assert.Equal ("Quiting...", msg);
                                         msg = "";
                                         sb.NewMouseEvent (new() { Position = new (1, 24), Flags = MouseFlags.Button1Clicked });
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

        Assert.Empty (sb.Items);
        Assert.False (sb.CanFocus);
        Assert.Equal (Colors.ColorSchemes ["Menu"], sb.ColorScheme);
        Assert.Equal (0, sb.X);
        Assert.Equal ("AnchorEnd(1)", sb.Y.ToString ());
        Assert.Equal (Dim.Fill (), sb.Width);
        Assert.Equal (1, sb.Height);
    }

    [Fact]
    public void StatusItem_Constructor ()
    {
        Application.Init ();
        var si = new StatusItem (Application.QuitKey, $"{Application.QuitKey} to Quit", null);
        Assert.Equal (KeyCode.CtrlMask | KeyCode.Q, si.Shortcut);
        Assert.Equal ($"{Application.QuitKey} to Quit", si.Title);
        Assert.Null (si.Action);
        si = new (Application.QuitKey, $"{Application.QuitKey} to Quit", () => { });
        Assert.NotNull (si.Action);
        Application.Shutdown ();
    }
}
