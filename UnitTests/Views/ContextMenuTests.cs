using System.Globalization;
using Xunit.Abstractions;

//using GraphViewTests = Terminal.Gui.Views.GraphViewTests;

namespace Terminal.Gui.ViewsTests;

public class ContextMenuTests
{
    private readonly ITestOutputHelper _output;
    public ContextMenuTests (ITestOutputHelper output) { _output = output; }

    [Fact]
    [AutoInitShutdown]
    public void ContextMenu_Constructors ()
    {
        var cm = new ContextMenu ();
        Assert.Equal (Point.Empty, cm.Position);
        Assert.Empty (cm.MenuItems.Children);
        Assert.Null (cm.Host);
        cm.Position = new Point (20, 10);

        cm.MenuItems = new MenuBarItem (
                                        [
                                            new MenuItem ("First", "", null)
                                        ]
                                       );
        Assert.Equal (new Point (20, 10), cm.Position);
        Assert.Single (cm.MenuItems.Children);

        cm = new ContextMenu
        {
            Position = new Point (5, 10),
            MenuItems = new MenuBarItem (
                                         new [] { new MenuItem ("One", "", null), new MenuItem ("Two", "", null) }
                                        )
        };
        Assert.Equal (new Point (5, 10), cm.Position);
        Assert.Equal (2, cm.MenuItems.Children.Length);
        Assert.Null (cm.Host);

        cm = new ContextMenu
        {
            Host = new View { X = 5, Y = 10 },
            Position = new Point (5, 10),
            MenuItems = new MenuBarItem (
                                         new [] { new MenuItem ("One", "", null), new MenuItem ("Two", "", null) }
                                        )
        };

        Assert.Equal (new Point (5, 10), cm.Position);
        Assert.Equal (2, cm.MenuItems.Children.Length);
        Assert.NotNull (cm.Host);
    }

    [Fact]
    [AutoInitShutdown]
    public void ContextMenu_Is_Closed_If_Another_MenuBar_Is_Open_Or_Vice_Versa ()
    {
        var cm = new ContextMenu
        {
            Position = new Point (10, 5),
            MenuItems = new MenuBarItem (
                                         [
                                             new MenuItem ("One", "", null),
                                             new MenuItem ("Two", "", null)
                                         ]
                                        )
        };

        var menu = new MenuBar
        {
            Menus =
            [
                new MenuBarItem ("File", "", null),
                new MenuBarItem ("Edit", "", null)
            ]
        };

        var top = new Toplevel ();
        top.Add (menu);
        Application.Begin (top);

        Assert.Null (Application.MouseGrabView);

        cm.Show ();
        Assert.True (ContextMenu.IsShow);
        Assert.Equal (cm.MenuBar, Application.MouseGrabView);
        Assert.False (menu.IsMenuOpen);
        Assert.True (menu.NewKeyDownEvent (menu.Key));
        Assert.False (ContextMenu.IsShow);
        Assert.Equal (menu, Application.MouseGrabView);
        Assert.True (menu.IsMenuOpen);

        cm.Show ();
        Assert.True (ContextMenu.IsShow);
        Assert.Equal (cm.MenuBar, Application.MouseGrabView);
        Assert.False (menu.IsMenuOpen);
#if SUPPORT_ALT_TO_ACTIVATE_MENU
        Assert.True (Application.Top.ProcessKeyUp (new (Key.AltMask)));
        Assert.False (ContextMenu.IsShow);
        Assert.Equal (menu, Application.MouseGrabView);
        Assert.True (menu.IsMenuOpen);
#endif

        cm.Show ();
        Assert.True (ContextMenu.IsShow);
        Assert.Equal (cm.MenuBar, Application.MouseGrabView);
        Assert.False (menu.IsMenuOpen);
        Assert.False (menu.OnMouseEvent (new MouseEvent { X = 1, Flags = MouseFlags.ReportMousePosition, View = menu }));
        Assert.True (ContextMenu.IsShow);
        Assert.Equal (cm.MenuBar, Application.MouseGrabView);
        Assert.False (menu.IsMenuOpen);
        Assert.True (menu.OnMouseEvent (new MouseEvent { X = 1, Flags = MouseFlags.Button1Clicked, View = menu }));
        Assert.False (ContextMenu.IsShow);
        Assert.Equal (menu, Application.MouseGrabView);
        Assert.True (menu.IsMenuOpen);
    }

    [Fact]
    [AutoInitShutdown]
    public void ContextMenu_On_Toplevel_With_A_MenuBar_TextField_StatusBar ()
    {
        Thread.CurrentThread.CurrentUICulture = new CultureInfo ("en-US");

        var menu = new MenuBar
        {
            Menus =
            [
                new MenuBarItem ("File", "", null),
                new MenuBarItem ("Edit", "", null)
            ]
        };

        var label = new Label { X = 2, Y = 3, Text = "Label:" };

        var tf = new TextField { X = Pos.Right (label) + 1, Y = Pos.Top (label), Width = 20, Text = "TextField" };

        var statusBar = new StatusBar (
                                       [
                                           new StatusItem (KeyCode.F1, "~F1~ Help", null),
                                           new StatusItem (KeyCode.CtrlMask | KeyCode.Q, "~^Q~ Quit", null)
                                       ]
                                      );

        var top = new Toplevel ();
        top.Add (menu, label, tf, statusBar);
        ((FakeDriver)Application.Driver).SetBufferSize (45, 17);
        Application.Begin (top);

        Assert.Equal (new Rectangle (9, 3, 20, 1), tf.Frame);
        Assert.True (tf.HasFocus);

        tf.ContextMenu.Show ();
        Assert.True (ContextMenu.IsShow);
        Assert.Equal (new Point (9, 3), tf.ContextMenu.Position);
        Application.Top.Draw ();

        var expected = @"
 File  Edit                     
                                
                                
  Label: TextField              
         ┌─────────────────────┐
         │ Select All   Ctrl+T │
         │ Delete All   Ctrl+R │
         │ Copy         Ctrl+C │
         │ Cut          Ctrl+X │
         │ Paste        Ctrl+V │
         │ Undo         Ctrl+Z │
         │ Redo         Ctrl+Y │
         └─────────────────────┘
                                
                                
                                
 F1 Help │ ^Q Quit              
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (1, 0, 32, 17), pos);
    }

    [Fact]
    [AutoInitShutdown]
    public void ContextMenu_On_Toplevel_With_A_MenuBar_Window_TextField_StatusBar ()
    {
        Thread.CurrentThread.CurrentUICulture = new CultureInfo ("en-US");

        var menu = new MenuBar
        {
            Menus =
            [
                new MenuBarItem ("File", "", null),
                new MenuBarItem ("Edit", "", null)
            ]
        };

        var label = new Label { X = 2, Y = 3, Text = "Label:" };

        var tf = new TextField { X = Pos.Right (label) + 1, Y = Pos.Top (label), Width = 20, Text = "TextField" };

        var win = new Window ();
        win.Add (label, tf);

        var statusBar = new StatusBar (
                                       new []
                                       {
                                           new StatusItem (KeyCode.F1, "~F1~ Help", null),
                                           new StatusItem (KeyCode.CtrlMask | KeyCode.Q, "~^Q~ Quit", null)
                                       }
                                      );

        var top = new Toplevel ();
        top.Add (menu, win, statusBar);
        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (44, 17);

        Assert.Equal (new Rectangle (9, 3, 20, 1), tf.Frame);
        Assert.True (tf.HasFocus);

        tf.ContextMenu.Show ();
        Assert.True (ContextMenu.IsShow);
        Assert.Equal (new Point (10, 5), tf.ContextMenu.Position);
        Application.Top.Draw ();

        var expected = @"
 File  Edit                                 
┌──────────────────────────────────────────┐
│                                          │
│                                          │
│                                          │
│  Label: TextField                        │
│         ┌─────────────────────┐          │
│         │ Select All   Ctrl+T │          │
│         │ Delete All   Ctrl+R │          │
│         │ Copy         Ctrl+C │          │
│         │ Cut          Ctrl+X │          │
│         │ Paste        Ctrl+V │          │
│         │ Undo         Ctrl+Z │          │
│         │ Redo         Ctrl+Y │          │
│         └─────────────────────┘          │
└──────────────────────────────────────────┘
 F1 Help │ ^Q Quit                          
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (1, 0, 44, 17), pos);
    }

    [Fact]
    [AutoInitShutdown]
    public void Draw_A_ContextMenu_Over_A_Borderless_Top ()
    {
        ((FakeDriver)Application.Driver).SetBufferSize (20, 15);

        Assert.Equal (new Rectangle (0, 0, 20, 15), Application.Driver.Clip);
        TestHelpers.AssertDriverContentsWithFrameAre ("", _output);

        var top = new Toplevel { X = 2, Y = 2, Width = 15, Height = 4 };
        top.Add (new TextField { X = Pos.Center (), Width = 10, Text = "Test" });
        RunState rs = Application.Begin (top);

        Assert.Equal (new Rectangle (2, 2, 15, 4), top.Frame);
        Assert.Equal (top, Application.Top);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
    Test",
                                                      _output
                                                     );

        Application.OnMouseEvent (
                                  new MouseEventEventArgs (
                                                           new MouseEvent { X = 8, Y = 2, Flags = MouseFlags.Button3Clicked }
                                                          )
                                 );

        var firstIteration = false;
        Application.RunIteration (ref rs, ref firstIteration);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
    Test            
┌───────────────────
│ Select All   Ctrl+
│ Delete All   Ctrl+
│ Copy         Ctrl+
│ Cut          Ctrl+
│ Paste        Ctrl+
│ Undo         Ctrl+
│ Redo         Ctrl+
└───────────────────",
                                                      _output
                                                     );

        Application.End (rs);
    }

    [Fact]
    [AutoInitShutdown]
    public void Draw_A_ContextMenu_Over_A_Dialog ()
    {
        Toplevel top = new ();
        var win = new Window ();
        top.Add (win);
        RunState rsTop = Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (20, 15);

        Assert.Equal (new Rectangle (0, 0, 20, 15), win.Frame);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────┐
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
└──────────────────┘",
                                                      _output
                                                     );

        // Don't use Dialog here as it has more layout logic. Use Window instead.
        var dialog = new Window { X = 2, Y = 2, Width = 15, Height = 4 };
        dialog.Add (new TextField { X = Pos.Center (), Width = 10, Text = "Test" });
        RunState rsDialog = Application.Begin (dialog);

        Assert.Equal (new Rectangle (2, 2, 15, 4), dialog.Frame);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────┐
│                  │
│ ┌─────────────┐  │
│ │ Test        │  │
│ │             │  │
│ └─────────────┘  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
└──────────────────┘",
                                                      _output
                                                     );

        Application.OnMouseEvent (
                                  new MouseEventEventArgs (
                                                           new MouseEvent { X = 9, Y = 3, Flags = MouseFlags.Button3Clicked }
                                                          )
                                 );

        var firstIteration = false;
        Application.RunIteration (ref rsDialog, ref firstIteration);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────┐
│                  │
│ ┌─────────────┐  │
│ │ Test        │  │
┌───────────────────
│ Select All   Ctrl+
│ Delete All   Ctrl+
│ Copy         Ctrl+
│ Cut          Ctrl+
│ Paste        Ctrl+
│ Undo         Ctrl+
│ Redo         Ctrl+
└───────────────────
│                  │
└──────────────────┘",
                                                      _output
                                                     );

        Application.End (rsDialog);
        Application.End (rsTop);
    }

    [Fact]
    [AutoInitShutdown]
    public void Draw_A_ContextMenu_Over_A_Top_Dialog ()
    {
        ((FakeDriver)Application.Driver).SetBufferSize (20, 15);

        Assert.Equal (new Rectangle (0, 0, 20, 15), Application.Driver.Clip);
        TestHelpers.AssertDriverContentsWithFrameAre ("", _output);

        // Don't use Dialog here as it has more layout logic. Use Window instead.
        var dialog = new Window { X = 2, Y = 2, Width = 15, Height = 4 };
        dialog.Add (new TextField { X = Pos.Center (), Width = 10, Text = "Test" });
        RunState rs = Application.Begin (dialog);

        Assert.Equal (new Rectangle (2, 2, 15, 4), dialog.Frame);
        Assert.Equal (dialog, Application.Top);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
  ┌─────────────┐
  │ Test        │
  │             │
  └─────────────┘",
                                                      _output
                                                     );

        Application.OnMouseEvent (
                                  new MouseEventEventArgs (
                                                           new MouseEvent { X = 9, Y = 3, Flags = MouseFlags.Button3Clicked }
                                                          )
                                 );

        var firstIteration = false;
        Application.RunIteration (ref rs, ref firstIteration);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
  ┌─────────────┐   
  │ Test        │   
┌───────────────────
│ Select All   Ctrl+
│ Delete All   Ctrl+
│ Copy         Ctrl+
│ Cut          Ctrl+
│ Paste        Ctrl+
│ Undo         Ctrl+
│ Redo         Ctrl+
└───────────────────",
                                                      _output
                                                     );

        Application.End (rs);
    }

    [Fact]
    [AutoInitShutdown]
    public void ForceMinimumPosToZero_True_False ()
    {
        var cm = new ContextMenu
        {
            Position = new Point (-1, -2),
            MenuItems = new MenuBarItem (
                                         [
                                             new MenuItem ("One", "", null),
                                             new MenuItem ("Two", "", null)
                                         ]
                                        )
        };

        Assert.Equal (new Point (-1, -2), cm.Position);

        Application.Begin (new ());

        cm.Show ();
        Assert.Equal (new Point (-1, -2), cm.Position);
        Application.Refresh ();

        var expected = @"
┌──────┐
│ One  │
│ Two  │
└──────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (0, 1, 8, 4), pos);

        cm.ForceMinimumPosToZero = false;
        cm.Show ();
        Assert.Equal (new Point (-1, -2), cm.Position);
        Application.Refresh ();

        expected = @"
 One  │
 Two  │
──────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (1, 0, 7, 3), pos);
    }

    [Fact]
    [AutoInitShutdown]
    public void Hide_Is_Invoke_At_Container_Closing ()
    {
        var cm = new ContextMenu
        {
            Position = new Point (80, 25),
            MenuItems = new MenuBarItem (
                                         [
                                             new MenuItem ("One", "", null),
                                             new MenuItem ("Two", "", null)
                                         ]
                                        )
        };

        Toplevel top = new ();
        Application.Begin (top);
        top.Running = true;

        Assert.False (ContextMenu.IsShow);

        cm.Show ();
        Assert.True (ContextMenu.IsShow);

        top.RequestStop ();
        Assert.False (ContextMenu.IsShow);
    }
    
    [Fact]
    [AutoInitShutdown]
    public void Key_Open_And_Close_The_ContextMenu ()
    {
        var tf = new TextField ();
        var top = new Toplevel ();
        top.Add (tf);
        Application.Begin (top);

        Assert.True (Application.OnKeyDown (ContextMenu.DefaultKey));
        Assert.True (tf.ContextMenu.MenuBar.IsMenuOpen);
        Assert.True (Application.OnKeyDown (ContextMenu.DefaultKey));
        Assert.Null (tf.ContextMenu.MenuBar);
    }

    [Fact]
    [AutoInitShutdown]
    public void KeyChanged_Event ()
    {
        var oldKey = Key.Empty;
        var cm = new ContextMenu ();

        cm.KeyChanged += (s, e) => oldKey = e.OldKey;

        cm.Key = Key.Space.WithCtrl;
        Assert.Equal (Key.Space.WithCtrl, cm.Key);
        Assert.Equal (ContextMenu.DefaultKey, oldKey);
    }

    [Fact]
    [AutoInitShutdown]
    public void MenuItens_Changing ()
    {
        var cm = new ContextMenu
        {
            Position = new Point (10, 5),
            MenuItems = new MenuBarItem (
                                         [
                                             new MenuItem ("One", "", null),
                                             new MenuItem ("Two", "", null)
                                         ]
                                        )
        };

        Application.Begin (new ());
        cm.Show ();
        Application.Refresh ();

        var expected = @"
          ┌──────┐
          │ One  │
          │ Two  │
          └──────┘
";

        TestHelpers.AssertDriverContentsAre (expected, _output);

        cm.MenuItems = new MenuBarItem (
                                        [
                                            new MenuItem ("First", "", null),
                                            new MenuItem ("Second", "", null),
                                            new MenuItem ("Third", "", null)
                                        ]
                                       );

        cm.Show ();
        Application.Refresh ();

        expected = @"
          ┌─────────┐
          │ First   │
          │ Second  │
          │ Third   │
          └─────────┘
";

        TestHelpers.AssertDriverContentsAre (expected, _output);
    }

    [Fact]
    [AutoInitShutdown]
    public void Menus_And_SubMenus_Always_Try_To_Be_On_Screen ()
    {
        var cm = new ContextMenu
        {
            Position = new Point (-1, -2),
            MenuItems = new MenuBarItem (
                                         [
                                             new MenuItem ("One", "", null),
                                             new MenuItem ("Two", "", null),
                                             new MenuItem ("Three", "", null),
                                             new MenuBarItem (
                                                              "Four",
                                                              [
                                                                  new MenuItem ("SubMenu1", "", null),
                                                                  new MenuItem ("SubMenu2", "", null),
                                                                  new MenuItem ("SubMenu3", "", null),
                                                                  new MenuItem ("SubMenu4", "", null),
                                                                  new MenuItem ("SubMenu5", "", null),
                                                                  new MenuItem ("SubMenu6", "", null),
                                                                  new MenuItem ("SubMenu7", "", null)
                                                              ]
                                                             ),
                                             new MenuItem ("Five", "", null),
                                             new MenuItem ("Six", "", null)
                                         ]
                                        )
        };

        Assert.Equal (new Point (-1, -2), cm.Position);

        Toplevel top = new ();
        Application.Begin (top);

        cm.Show ();
        Application.Refresh ();

        Assert.Equal (new Point (-1, -2), cm.Position);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌────────┐
│ One    │
│ Two    │
│ Three  │
│ Four  ►│
│ Five   │
│ Six    │
└────────┘
",
                                                      _output
                                                     );

        Assert.True (
                     top.Subviews [0]
                        .OnMouseEvent (
                                     new MouseEvent { X = 0, Y = 3, Flags = MouseFlags.ReportMousePosition, View = top.Subviews [0] }
                                    )
                    );
        Application.Refresh ();
        Assert.Equal (new Point (-1, -2), cm.Position);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌────────┐             
│ One    │             
│ Two    │             
│ Three  │             
│ Four  ►│┌───────────┐
│ Five   ││ SubMenu1  │
│ Six    ││ SubMenu2  │
└────────┘│ SubMenu3  │
          │ SubMenu4  │
          │ SubMenu5  │
          │ SubMenu6  │
          │ SubMenu7  │
          └───────────┘
",
                                                      _output
                                                     );

        ((FakeDriver)Application.Driver).SetBufferSize (40, 20);
        cm.Position = new Point (41, -2);
        cm.Show ();
        Application.Refresh ();
        Assert.Equal (new Point (41, -2), cm.Position);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
                              ┌────────┐
                              │ One    │
                              │ Two    │
                              │ Three  │
                              │ Four  ►│
                              │ Five   │
                              │ Six    │
                              └────────┘
",
                                                      _output
                                                     );

        Assert.True (
                     top.Subviews [0]
                        .OnMouseEvent (
                                     new MouseEvent { X = 30, Y = 3, Flags = MouseFlags.ReportMousePosition, View = top.Subviews [0] }
                                    )
                    );
        Application.Refresh ();
        Assert.Equal (new Point (41, -2), cm.Position);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
                              ┌────────┐
                              │ One    │
                              │ Two    │
                              │ Three  │
                 ┌───────────┐│ Four  ►│
                 │ SubMenu1  ││ Five   │
                 │ SubMenu2  ││ Six    │
                 │ SubMenu3  │└────────┘
                 │ SubMenu4  │          
                 │ SubMenu5  │          
                 │ SubMenu6  │          
                 │ SubMenu7  │          
                 └───────────┘          
",
                                                      _output
                                                     );

        cm.Position = new Point (41, 9);
        cm.Show ();
        Application.Refresh ();
        Assert.Equal (new Point (41, 9), cm.Position);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
                              ┌────────┐
                              │ One    │
                              │ Two    │
                              │ Three  │
                              │ Four  ►│
                              │ Five   │
                              │ Six    │
                              └────────┘
",
                                                      _output
                                                     );

        Assert.True (
                     top.Subviews [0]
                        .OnMouseEvent (
                                     new MouseEvent { X = 30, Y = 3, Flags = MouseFlags.ReportMousePosition, View = top.Subviews [0] }
                                    )
                    );
        Application.Refresh ();
        Assert.Equal (new Point (41, 9), cm.Position);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
                              ┌────────┐
                 ┌───────────┐│ One    │
                 │ SubMenu1  ││ Two    │
                 │ SubMenu2  ││ Three  │
                 │ SubMenu3  ││ Four  ►│
                 │ SubMenu4  ││ Five   │
                 │ SubMenu5  ││ Six    │
                 │ SubMenu6  │└────────┘
                 │ SubMenu7  │          
                 └───────────┘          
",
                                                      _output
                                                     );

        cm.Position = new Point (41, 22);
        cm.Show ();
        Application.Refresh ();
        Assert.Equal (new Point (41, 22), cm.Position);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
                              ┌────────┐
                              │ One    │
                              │ Two    │
                              │ Three  │
                              │ Four  ►│
                              │ Five   │
                              │ Six    │
                              └────────┘
",
                                                      _output
                                                     );

        Assert.True (
                     top.Subviews [0]
                        .OnMouseEvent (
                                     new MouseEvent { X = 30, Y = 3, Flags = MouseFlags.ReportMousePosition, View = top.Subviews [0] }
                                    )
                    );
        Application.Refresh ();
        Assert.Equal (new Point (41, 22), cm.Position);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
                 ┌───────────┐          
                 │ SubMenu1  │┌────────┐
                 │ SubMenu2  ││ One    │
                 │ SubMenu3  ││ Two    │
                 │ SubMenu4  ││ Three  │
                 │ SubMenu5  ││ Four  ►│
                 │ SubMenu6  ││ Five   │
                 │ SubMenu7  ││ Six    │
                 └───────────┘└────────┘
",
                                                      _output
                                                     );

        ((FakeDriver)Application.Driver).SetBufferSize (18, 8);
        cm.Position = new Point (19, 10);
        cm.Show ();
        Application.Refresh ();
        Assert.Equal (new Point (19, 10), cm.Position);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
        ┌────────┐
        │ One    │
        │ Two    │
        │ Three  │
        │ Four  ►│
        │ Five   │
        │ Six    │
        └────────┘
",
                                                      _output
                                                     );

        Assert.True (
                     top.Subviews [0]
                        .OnMouseEvent (
                                     new MouseEvent { X = 30, Y = 3, Flags = MouseFlags.ReportMousePosition, View = top.Subviews [0] }
                                    )
                    );
        Application.Refresh ();
        Assert.Equal (new Point (19, 10), cm.Position);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌───────────┐────┐
│ SubMenu1  │    │
│ SubMenu2  │    │
│ SubMenu3  │ee  │
│ SubMenu4  │r  ►│
│ SubMenu5  │e   │
│ SubMenu6  │    │
│ SubMenu7  │────┘
",
                                                      _output
                                                     );
    }

    [Fact]
    [AutoInitShutdown]
    public void MouseFlags_Changing ()
    {
        var lbl = new Label { Text = "Original" };

        var cm = new ContextMenu ();

        lbl.MouseClick += (s, e) =>
                          {
                              if (e.MouseEvent.Flags == cm.MouseFlags)
                              {
                                  lbl.Text = "Replaced";
                                  e.Handled = true;
                              }
                          };

        Toplevel top = new ();
        top.Add (lbl);
        Application.Begin (top);

        Assert.True (lbl.OnMouseEvent (new MouseEvent { Flags = cm.MouseFlags }));
        Assert.Equal ("Replaced", lbl.Text);

        lbl.Text = "Original";
        cm.MouseFlags = MouseFlags.Button2Clicked;
        Assert.True (lbl.OnMouseEvent (new MouseEvent { Flags = cm.MouseFlags }));
        Assert.Equal ("Replaced", lbl.Text);
    }

    [Fact]
    [AutoInitShutdown]
    public void MouseFlagsChanged_Event ()
    {
        var oldMouseFlags = new MouseFlags ();
        var cm = new ContextMenu ();

        cm.MouseFlagsChanged += (s, e) => oldMouseFlags = e.OldValue;

        cm.MouseFlags = MouseFlags.Button2Clicked;
        Assert.Equal (MouseFlags.Button2Clicked, cm.MouseFlags);
        Assert.Equal (MouseFlags.Button3Clicked, oldMouseFlags);
    }

    [Fact]
    [AutoInitShutdown]
    public void Position_Changing ()
    {
        var cm = new ContextMenu
        {
            Position = new Point (10, 5),
            MenuItems = new MenuBarItem (
                                         [
                                             new MenuItem ("One", "", null),
                                             new MenuItem ("Two", "", null)
                                         ]
                                        )
        };

        Application.Begin (new ());
        cm.Show ();
        Application.Refresh ();

        var expected = @"
          ┌──────┐
          │ One  │
          │ Two  │
          └──────┘
";

        TestHelpers.AssertDriverContentsAre (expected, _output);

        cm.Position = new Point (5, 10);

        cm.Show ();
        Application.Refresh ();

        expected = @"
     ┌──────┐
     │ One  │
     │ Two  │
     └──────┘
";

        TestHelpers.AssertDriverContentsAre (expected, _output);
    }

    [Fact]
    [AutoInitShutdown]
    public void RequestStop_While_ContextMenu_Is_Open_Does_Not_Throws ()
    {
        ContextMenu cm = Create_ContextMenu_With_Two_MenuItem (10, 5);
        Toplevel top = new ();
        var isMenuAllClosed = false;
        MenuBarItem mi = null;
        int iterations = -1;

        Application.Iteration += (s, a) =>
                                 {
                                     iterations++;

                                     if (iterations == 0)
                                     {
                                         cm.Show ();
                                         Assert.True (ContextMenu.IsShow);
                                         mi = cm.MenuBar.Menus [0];

                                         mi.Action = () =>
                                                     {
                                                         var dialog1 = new Dialog ();
                                                         Application.Run (dialog1);
                                                         dialog1.Dispose ();
                                                         Assert.False (ContextMenu.IsShow);
                                                         Assert.True (isMenuAllClosed);
                                                     };
                                         cm.MenuBar.MenuAllClosed += (_, _) => isMenuAllClosed = true;
                                     }
                                     else if (iterations == 1)
                                     {
                                         mi.Action ();
                                     }
                                     else if (iterations == 2)
                                     {
                                         Application.RequestStop ();
                                     }
                                     else if (iterations == 3)
                                     {
                                         isMenuAllClosed = false;
                                         cm.Show ();
                                         Assert.True (ContextMenu.IsShow);
                                         cm.MenuBar.MenuAllClosed += (_, _) => isMenuAllClosed = true;
                                     }
                                     else if (iterations == 4)
                                     {
                                         Exception exception = Record.Exception (() => Application.RequestStop ());
                                         Assert.Null (exception);
                                     }
                                     else
                                     {
                                         Application.RequestStop ();
                                     }
                                 };

        var isTopClosed = false;

        top.Closing += (_, _) =>
                       {
                           var dialog2 = new Dialog ();
                           Application.Run (dialog2);
                           dialog2.Dispose ();
                           Assert.False (ContextMenu.IsShow);
                           Assert.True (isMenuAllClosed);
                           isTopClosed = true;
                       };

        Application.Run (top);

        Assert.True (isTopClosed);
        Assert.False (ContextMenu.IsShow);
        Assert.True (isMenuAllClosed);
    }

    [Fact]
    [AutoInitShutdown]
    public void Show_Display_At_Zero_If_The_Toplevel_Height_Is_Less_Than_The_Menu_Height ()
    {
        ((FakeDriver)Application.Driver).SetBufferSize (80, 3);

        var cm = new ContextMenu
        {
            Position = Point.Empty,
            MenuItems = new MenuBarItem (
                                         [
                                             new MenuItem ("One", "", null),
                                             new MenuItem ("Two", "", null)
                                         ]
                                        )
        };

        Assert.Equal (Point.Empty, cm.Position);

        Application.Begin (new ());
        cm.Show ();
        Assert.Equal (Point.Empty, cm.Position);
        Application.Refresh ();

        var expected = @"
┌──────┐
│ One  │
│ Two  │";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (0, 0, 8, 3), pos);

        cm.Hide ();
        Assert.Equal (Point.Empty, cm.Position);
    }

    [Fact]
    [AutoInitShutdown]
    public void Show_Display_At_Zero_If_The_Toplevel_Width_Is_Less_Than_The_Menu_Width ()
    {
        ((FakeDriver)Application.Driver).SetBufferSize (5, 25);

        var cm = new ContextMenu
        {
            Position = Point.Empty,
            MenuItems = new MenuBarItem (
                                         [
                                             new MenuItem ("One", "", null),
                                             new MenuItem ("Two", "", null)
                                         ]
                                        )
        };

        Assert.Equal (Point.Empty, cm.Position);

        Application.Begin (new ());
        cm.Show ();
        Assert.Equal (Point.Empty, cm.Position);
        Application.Refresh ();

        var expected = @"
┌────
│ One
│ Two
└────";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (0, 1, 5, 4), pos);

        cm.Hide ();
        Assert.Equal (Point.Empty, cm.Position);
    }

    [Fact]
    [AutoInitShutdown]
    public void Show_Display_Below_The_Bottom_Host_If_Has_Enough_Space ()
    {
        var view = new View
        {
            X = 10,
            Y = 5,
            Width = 10,
            Height = 1,
            Text = "View"
        };

        var cm = new ContextMenu
        {
            Host = view,
            Position = new Point (10, 5),
            MenuItems = new MenuBarItem (
                                         [
                                             new MenuItem ("One", "", null),
                                             new MenuItem ("Two", "", null)
                                         ]
                                        )
        };

        var top = new Toplevel ();
        top.Add (view);
        Application.Begin (top);

        Assert.Equal (new Point (10, 5), cm.Position);

        cm.Show ();
        top.Draw ();
        Assert.Equal (new Point (10, 5), cm.Position);

        var expected = @"
          View    
          ┌──────┐
          │ One  │
          │ Two  │
          └──────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (10, 5, 18, 5), pos);

        cm.Hide ();
        Assert.Equal (new Point (10, 5), cm.Position);
        cm.Host.X = 5;
        cm.Host.Y = 10;
        cm.Host.Height = 3;

        cm.Show ();
        Application.Top.Draw ();
        Assert.Equal (new Point (5, 12), cm.Position);

        expected = @"
     View    
             
             
     ┌──────┐
     │ One  │
     │ Two  │
     └──────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (5, 10, 13, 7), pos);

        cm.Hide ();
        Assert.Equal (new Point (5, 12), cm.Position);
    }

    [Fact]
    [AutoInitShutdown]
    public void Show_Ensures_Display_Inside_The_Container_But_Preserves_Position ()
    {
        var cm = new ContextMenu
        {
            Position = new Point (80, 25),
            MenuItems = new MenuBarItem (
                                         [
                                             new MenuItem ("One", "", null),
                                             new MenuItem ("Two", "", null)
                                         ]
                                        )
        };

        Assert.Equal (new Point (80, 25), cm.Position);

        Application.Begin (new ());
        cm.Show ();
        Assert.Equal (new Point (80, 25), cm.Position);
        Application.Refresh ();

        var expected = @"
                                                                        ┌──────┐
                                                                        │ One  │
                                                                        │ Two  │
                                                                        └──────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (72, 21, 80, 4), pos);

        cm.Hide ();
        Assert.Equal (new Point (80, 25), cm.Position);
    }

    [Fact]
    [AutoInitShutdown]
    public void Show_Ensures_Display_Inside_The_Container_Without_Overlap_The_Host ()
    {
        var view = new View
        {
            X = Pos.AnchorEnd (10),
            Y = Pos.AnchorEnd (1),
            Width = 10,
            Height = 1,
            Text = "View"
        };

        var cm = new ContextMenu
        {
            Host = view,
            MenuItems = new MenuBarItem (
                                         [
                                             new MenuItem ("One", "", null),
                                             new MenuItem ("Two", "", null)
                                         ]
                                        )
        };

        var top = new Toplevel ();
        top.Add (view);
        Application.Begin (top);

        Assert.Equal (new Rectangle (70, 24, 10, 1), view.Frame);
        Assert.Equal (Point.Empty, cm.Position);

        cm.Show ();
        Assert.Equal (new Point (70, 24), cm.Position);
        top.Draw ();

        var expected = @"
                                                                      ┌──────┐
                                                                      │ One  │
                                                                      │ Two  │
                                                                      └──────┘
                                                                      View    
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (70, 20, 78, 5), pos);

        cm.Hide ();
        Assert.Equal (new Point (70, 24), cm.Position);
    }

    [Fact]
    [AutoInitShutdown]
    public void Show_Hide_IsShow ()
    {
        ContextMenu cm = Create_ContextMenu_With_Two_MenuItem (10, 5);

        Application.Begin (new ());
        cm.Show ();
        Assert.True (ContextMenu.IsShow);
        Application.Refresh ();

        var expected = @"
          ┌──────┐
          │ One  │
          │ Two  │
          └──────┘
";

        TestHelpers.AssertDriverContentsAre (expected, _output);

        cm.Hide ();
        Assert.False (ContextMenu.IsShow);

        Application.Refresh ();

        expected = "";

        TestHelpers.AssertDriverContentsAre (expected, _output);
    }

    [Fact]
    [AutoInitShutdown]
    public void UseSubMenusSingleFrame_True_By_Mouse ()
    {
        var cm = new ContextMenu
        {
            Position = new Point (5, 10),
            MenuItems = new MenuBarItem (
                                         "Numbers",
                                         [
                                             new MenuItem ("One", "", null),
                                             new MenuBarItem (
                                                              "Two",
                                                              [
                                                                  new MenuItem (
                                                                                "Sub-Menu 1",
                                                                                "",
                                                                                null
                                                                               ),
                                                                  new MenuItem ("Sub-Menu 2", "", null)
                                                              ]
                                                             ),
                                             new MenuItem ("Three", "", null)
                                         ]
                                        ),
            UseSubMenusSingleFrame = true
        };

        RunState rs = Application.Begin (new ());
        cm.Show ();
        Assert.Equal (new Rectangle (5, 11, 10, 5), Application.Top.Subviews [0].Frame);
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
     ┌────────┐
     │ One    │
     │ Two   ►│
     │ Three  │
     └────────┘",
                                                      _output
                                                     );

        // X=5 is the border and so need to use at least one more
        Application.OnMouseEvent (
                                  new MouseEventEventArgs (
                                                           new MouseEvent { X = 6, Y = 13, Flags = MouseFlags.Button1Clicked }
                                                          )
                                 );

        var firstIteration = false;
        Application.RunIteration (ref rs, ref firstIteration);
        Assert.Equal (new Rectangle (5, 11, 10, 5), Application.Top.Subviews [0].Frame);
        Assert.Equal (new Rectangle (5, 11, 15, 6), Application.Top.Subviews [1].Frame);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
     ┌─────────────┐
     │◄    Two     │
     ├─────────────┤
     │ Sub-Menu 1  │
     │ Sub-Menu 2  │
     └─────────────┘",
                                                      _output
                                                     );

        Application.OnMouseEvent (
                                  new MouseEventEventArgs (
                                                           new MouseEvent { X = 6, Y = 12, Flags = MouseFlags.Button1Clicked }
                                                          )
                                 );

        firstIteration = false;
        Application.RunIteration (ref rs, ref firstIteration);
        Assert.Equal (new Rectangle (5, 11, 10, 5), Application.Top.Subviews [0].Frame);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
     ┌────────┐
     │ One    │
     │ Two   ►│
     │ Three  │
     └────────┘",
                                                      _output
                                                     );

        Application.End (rs);
    }

        [Fact]
    [AutoInitShutdown]
    public void UseSubMenusSingleFrame_False_By_Mouse ()
    {
        var cm = new ContextMenu
        {
            Position = new Point (5, 10),
            MenuItems = new MenuBarItem (
                                         "Numbers",
                                         [
                                             new MenuItem ("One", "", null),
                                             new MenuBarItem (
                                                              "Two",
                                                              [
                                                                  new MenuItem (
                                                                                "Two-Menu 1",
                                                                                "",
                                                                                null
                                                                               ),
                                                                  new MenuItem ("Two-Menu 2", "", null)
                                                              ]
                                                             ),
                                             new MenuBarItem ("Three",
                                                              [
                                                                  new MenuItem (
                                                                                "Three-Menu 1",
                                                                                "",
                                                                                null
                                                                               ),
                                                                  new MenuItem ("Three-Menu 2", "", null)
                                                              ]
                                                             )
                                         ]
                                        )
        };

        RunState rs = Application.Begin (new ());
        cm.Show ();

        Assert.Equal (new Rectangle (5, 11, 10, 5), Application.Top.Subviews [0].Frame);
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
     ┌────────┐
     │ One    │
     │ Two   ►│
     │ Three ►│
     └────────┘",
                                                      _output
                                                     );

        Application.OnMouseEvent (
                                  new MouseEventEventArgs (
                                                           new MouseEvent { X = 6, Y = 13, Flags = MouseFlags.ReportMousePosition }
                                                          )
                                 );

        var firstIteration = false;
        Application.RunIteration (ref rs, ref firstIteration);
        Assert.Equal (new Rectangle (5, 11, 10, 5), Application.Top.Subviews [0].Frame);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
     ┌────────┐               
     │ One    │               
     │ Two   ►│┌─────────────┐
     │ Three ►││ Two-Menu 1  │
     └────────┘│ Two-Menu 2  │
               └─────────────┘",
                                                      _output
                                                     );

        Application.OnMouseEvent (
                                  new MouseEventEventArgs (
                                                           new MouseEvent { X = 6, Y = 14, Flags = MouseFlags.ReportMousePosition }
                                                          )
                                 );

        firstIteration = false;
        Application.RunIteration (ref rs, ref firstIteration);
        Assert.Equal (new Rectangle (5, 11, 10, 5), Application.Top.Subviews [0].Frame);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
     ┌────────┐                 
     │ One    │                 
     │ Two   ►│                 
     │ Three ►│┌───────────────┐
     └────────┘│ Three-Menu 1  │
               │ Three-Menu 2  │
               └───────────────┘",
                                                      _output
                                                     );

        Application.OnMouseEvent (
                                  new MouseEventEventArgs (
                                                           new MouseEvent { X = 6, Y = 13, Flags = MouseFlags.ReportMousePosition }
                                                          )
                                 );

        firstIteration = false;
        Application.RunIteration (ref rs, ref firstIteration);
        Assert.Equal (new Rectangle (5, 11, 10, 5), Application.Top.Subviews [0].Frame);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
     ┌────────┐               
     │ One    │               
     │ Two   ►│┌─────────────┐
     │ Three ►││ Two-Menu 1  │
     └────────┘│ Two-Menu 2  │
               └─────────────┘",
                                                      _output
                                                     );

        Application.End (rs);
    }

    private ContextMenu Create_ContextMenu_With_Two_MenuItem (int x, int y)
    {
        return new ContextMenu
        {
            Position = new Point (x, y),
            MenuItems = new MenuBarItem (
                                         new MenuItem [] { new ("One", "", null), new ("Two", "", null) }
                                        )
        };
    }
}
