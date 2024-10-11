﻿using System.Globalization;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class ContextMenuTests (ITestOutputHelper output)
{
    [Fact]
    [AutoInitShutdown]
    public void ContextMenu_Constructors ()
    {
        var cm = new ContextMenu ();
        var top = new Toplevel ();
        Application.Begin (top);

        Assert.Equal (Point.Empty, cm.Position);
        Assert.Null (cm.MenuItems);
        Assert.Null (cm.Host);
        cm.Position = new Point (20, 10);

        var menuItems = new MenuBarItem (
                                         [
                                             new MenuItem ("First", "", null)
                                         ]
                                        );
        cm.Show (menuItems);
        Assert.Equal (new Point (20, 10), cm.Position);
        Assert.Single (cm.MenuItems!.Children);

        cm = new ContextMenu
        {
            Position = new Point (5, 10)
        };

        menuItems = new MenuBarItem (
                                     new [] { new MenuItem ("One", "", null), new MenuItem ("Two", "", null) }
                                    );
        cm.Show (menuItems);
        Assert.Equal (new Point (5, 10), cm.Position);
        Assert.Equal (2, cm.MenuItems!.Children.Length);
        Assert.Null (cm.Host);

        var view = new View { X = 5, Y = 10 };
        top.Add (view);

        cm = new ContextMenu
        {
            Host = view,
            Position = new Point (5, 10)
        };

        menuItems = new MenuBarItem (
                                     new [] { new MenuItem ("One", "", null), new MenuItem ("Two", "", null) }
                                    );
        cm.Show (menuItems);
        Assert.Equal (new Point (5, 10), cm.Position);
        Assert.Equal (2, cm.MenuItems.Children.Length);
        Assert.NotNull (cm.Host);

        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ContextMenu_Is_Closed_If_Another_MenuBar_Is_Open_Or_Vice_Versa ()
    {
        var cm = new ContextMenu
        {
            Position = new Point (10, 5)
        };

        var menuItems = new MenuBarItem (
                                         [
                                             new MenuItem ("One", "", null),
                                             new MenuItem ("Two", "", null)
                                         ]
                                        );

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

        cm.Show (menuItems);
        Assert.True (ContextMenu.IsShow);
        Assert.Equal (cm.MenuBar, Application.MouseGrabView);
        Assert.False (menu.IsMenuOpen);
        Assert.True (menu.NewKeyDownEvent (menu.Key));
        Assert.False (ContextMenu.IsShow);
        Assert.Equal (menu, Application.MouseGrabView);
        Assert.True (menu.IsMenuOpen);

        cm.Show (menuItems);
        Assert.True (ContextMenu.IsShow);
        Assert.Equal (cm.MenuBar, Application.MouseGrabView);
        Assert.False (menu.IsMenuOpen);
#if SUPPORT_ALT_TO_ACTIVATE_MENU
        Assert.True (Application.Top.ProcessKeyUp (new (Key.AltMask)));
        Assert.False (ContextMenu.IsShow);
        Assert.Equal (menu, Application.MouseGrabView);
        Assert.True (menu.IsMenuOpen);
#endif

        cm.Show (menuItems);
        Assert.True (ContextMenu.IsShow);
        Assert.Equal (cm.MenuBar, Application.MouseGrabView);
        Assert.False (menu.IsMenuOpen);
        Assert.False (menu.NewMouseEvent (new MouseEvent { Position = new (1, 0), Flags = MouseFlags.ReportMousePosition, View = menu }));
        Assert.True (ContextMenu.IsShow);
        Assert.Equal (cm.MenuBar, Application.MouseGrabView);
        Assert.False (menu.IsMenuOpen);
        Assert.True (menu.NewMouseEvent (new MouseEvent { Position = new (1, 0), Flags = MouseFlags.Button1Clicked, View = menu }));
        Assert.False (ContextMenu.IsShow);
        Assert.Equal (menu, Application.MouseGrabView);
        Assert.True (menu.IsMenuOpen);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Draw_A_ContextMenu_Over_A_Borderless_Top ()
    {
        ((FakeDriver)Application.Driver!).SetBufferSize (20, 15);

        Assert.Equal (new Rectangle (0, 0, 20, 15), Application.Driver?.Clip);
        TestHelpers.AssertDriverContentsWithFrameAre ("", output);

        var top = new Toplevel { X = 2, Y = 2, Width = 15, Height = 4 };
        top.Add (new TextField { X = Pos.Center (), Width = 10, Text = "Test" });
        RunState rs = Application.Begin (top);

        Assert.Equal (new Rectangle (2, 2, 15, 4), top.Frame);
        Assert.Equal (top, Application.Top);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
    Test",
                                                      output
                                                     );

        Application.OnMouseEvent (new MouseEvent { ScreenPosition = new (8, 2), Flags = MouseFlags.Button3Clicked });

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
                                                      output
                                                     );

        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Draw_A_ContextMenu_Over_A_Dialog ()
    {
        Toplevel top = new ();
        var win = new Window ();
        top.Add (win);
        RunState rsTop = Application.Begin (top);
        ((FakeDriver)Application.Driver!).SetBufferSize (20, 15);

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
                                                      output
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
                                                      output
                                                     );

        Application.OnMouseEvent (new MouseEvent { ScreenPosition = new (9, 3), Flags = MouseFlags.Button3Clicked });

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
                                                      output
                                                     );

        Application.End (rsDialog);
        Application.End (rsTop);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Draw_A_ContextMenu_Over_A_Top_Dialog ()
    {
        ((FakeDriver)Application.Driver!).SetBufferSize (20, 15);

        Assert.Equal (new Rectangle (0, 0, 20, 15), Application.Driver?.Clip);
        TestHelpers.AssertDriverContentsWithFrameAre ("", output);

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
                                                      output
                                                     );

        Application.OnMouseEvent (new MouseEvent { ScreenPosition = new (9, 3), Flags = MouseFlags.Button3Clicked });

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
                                                      output
                                                     );

        Application.End (rs);
        dialog.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ForceMinimumPosToZero_True_False ()
    {
        var cm = new ContextMenu
        {
            Position = new Point (-1, -2)
        };

        var menuItems = new MenuBarItem (
                                         [
                                             new MenuItem ("One", "", null),
                                             new MenuItem ("Two", "", null)
                                         ]
                                        );
        Assert.Equal (new Point (-1, -2), cm.Position);

        Toplevel top = new ();
        Application.Begin (top);

        cm.Show (menuItems);
        Assert.Equal (new Point (-1, -2), cm.Position);
        Application.Refresh ();

        var expected = @"
┌──────┐
│ One  │
│ Two  │
└──────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new Rectangle (0, 1, 8, 4), pos);

        cm.ForceMinimumPosToZero = false;
        cm.Show (menuItems);
        Assert.Equal (new Point (-1, -2), cm.Position);
        Application.Refresh ();

        expected = @"
 One  │
 Two  │
──────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new Rectangle (1, 0, 7, 3), pos);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Hide_Is_Invoke_At_Container_Closing ()
    {
        var cm = new ContextMenu
        {
            Position = new Point (80, 25)
        };

        var menuItems = new MenuBarItem (
                                         [
                                             new MenuItem ("One", "", null),
                                             new MenuItem ("Two", "", null)
                                         ]
                                        );
        Toplevel top = new ();
        Application.Begin (top);
        top.Running = true;

        Assert.False (ContextMenu.IsShow);

        cm.Show (menuItems);
        Assert.True (ContextMenu.IsShow);

        top.RequestStop ();
        Assert.False (ContextMenu.IsShow);
        top.Dispose ();
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
        Assert.True (tf.ContextMenu.MenuBar!.IsMenuOpen);
        Assert.True (Application.OnKeyDown (ContextMenu.DefaultKey));

        // The last context menu bar opened is always preserved
        Assert.NotNull (tf.ContextMenu.MenuBar);
        top.Dispose ();
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
            Position = new Point (10, 5)
        };

        var menuItems = new MenuBarItem (
                                         [
                                             new MenuItem ("One", "", null),
                                             new MenuItem ("Two", "", null)
                                         ]
                                        );
        Toplevel top = new ();
        Application.Begin (top);
        cm.Show (menuItems);
        Application.Refresh ();

        var expected = @"
          ┌──────┐
          │ One  │
          │ Two  │
          └──────┘
";

        TestHelpers.AssertDriverContentsAre (expected, output);

        menuItems = new MenuBarItem (
                                     [
                                         new MenuItem ("First", "", null),
                                         new MenuItem ("Second", "", null),
                                         new MenuItem ("Third", "", null)
                                     ]
                                    );

        cm.Show (menuItems);
        Application.Refresh ();

        expected = @"
          ┌─────────┐
          │ First   │
          │ Second  │
          │ Third   │
          └─────────┘
";

        TestHelpers.AssertDriverContentsAre (expected, output);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Menus_And_SubMenus_Always_Try_To_Be_On_Screen ()
    {
        var cm = new ContextMenu
        {
            Position = new Point (-1, -2)
        };

        var menuItems = new MenuBarItem (
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
                                        );
        Assert.Equal (new Point (-1, -2), cm.Position);

        Toplevel top = new ();
        Application.Begin (top);

        cm.Show (menuItems);
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
                                                      output
                                                     );

        Assert.True (
                     top.Subviews [0]
                        .NewMouseEvent (
                                        new MouseEvent { Position = new (0, 3), Flags = MouseFlags.ReportMousePosition, View = top.Subviews [0] }
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
                                                      output
                                                     );

        ((FakeDriver)Application.Driver!).SetBufferSize (40, 20);
        cm.Position = new Point (41, -2);
        cm.Show (menuItems);
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
                                                      output
                                                     );

        Assert.True (
                     top.Subviews [0]
                        .NewMouseEvent (
                                        new MouseEvent { Position = new (30, 3), Flags = MouseFlags.ReportMousePosition, View = top.Subviews [0] }
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
                                                      output
                                                     );

        cm.Position = new Point (41, 9);
        cm.Show (menuItems);
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
                                                      output
                                                     );

        Assert.True (
                     top.Subviews [0]
                        .NewMouseEvent (
                                        new MouseEvent { Position = new (30, 3), Flags = MouseFlags.ReportMousePosition, View = top.Subviews [0] }
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
                                                      output
                                                     );

        cm.Position = new Point (41, 22);
        cm.Show (menuItems);
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
                                                      output
                                                     );

        Assert.True (
                     top.Subviews [0]
                        .NewMouseEvent (
                                        new MouseEvent { Position = new (30, 3), Flags = MouseFlags.ReportMousePosition, View = top.Subviews [0] }
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
                                                      output
                                                     );

        ((FakeDriver)Application.Driver!).SetBufferSize (18, 8);
        cm.Position = new Point (19, 10);
        cm.Show (menuItems);
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
                                                      output
                                                     );

        Assert.True (
                     top.Subviews [0]
                        .NewMouseEvent (
                                        new MouseEvent { Position = new (30, 3), Flags = MouseFlags.ReportMousePosition, View = top.Subviews [0] }
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
                                                      output
                                                     );
        top.Dispose ();
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

        Assert.True (lbl.NewMouseEvent (new MouseEvent { Flags = cm.MouseFlags }));
        Assert.Equal ("Replaced", lbl.Text);

        lbl.Text = "Original";
        cm.MouseFlags = MouseFlags.Button2Clicked;
        Assert.True (lbl.NewMouseEvent (new MouseEvent { Flags = cm.MouseFlags }));
        Assert.Equal ("Replaced", lbl.Text);
        top.Dispose ();
    }

    [Fact]
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
            Position = new Point (10, 5)
        };

        var menuItems = new MenuBarItem (
                                         [
                                             new MenuItem ("One", "", null),
                                             new MenuItem ("Two", "", null)
                                         ]
                                        );
        Toplevel top = new ();
        Application.Begin (top);
        cm.Show (menuItems);
        Application.Refresh ();

        var expected = @"
          ┌──────┐
          │ One  │
          │ Two  │
          └──────┘
";

        TestHelpers.AssertDriverContentsAre (expected, output);

        cm.Position = new Point (5, 10);

        cm.Show (menuItems);
        Application.Refresh ();

        expected = @"
     ┌──────┐
     │ One  │
     │ Two  │
     └──────┘
";

        TestHelpers.AssertDriverContentsAre (expected, output);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void RequestStop_While_ContextMenu_Is_Open_Does_Not_Throws ()
    {
        ContextMenu cm = new ContextMenu
        {
            Position = new Point (10, 5)
        };

        var menuItems = new MenuBarItem (
                                         new MenuItem [] { new ("One", "", null), new ("Two", "", null) }
                                        );
        Toplevel top = new ();
        var isMenuAllClosed = false;
        MenuBarItem mi = null;
        int iterations = -1;

        Application.Iteration += (s, a) =>
                                 {
                                     iterations++;

                                     if (iterations == 0)
                                     {
                                         cm.Show (menuItems);
                                         Assert.True (ContextMenu.IsShow);
                                         mi = cm.MenuBar.Menus [0];

                                         mi.Action = () =>
                                                     {
                                                         Assert.True (ContextMenu.IsShow);

                                                         var dialog1 = new Dialog () { Id = "dialog1" };
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
                                         cm.Show (menuItems);
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
                           var dialog2 = new Dialog () { Id = "dialog2" };
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
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Show_Display_At_Zero_If_The_Toplevel_Height_Is_Less_Than_The_Menu_Height ()
    {
        ((FakeDriver)Application.Driver!).SetBufferSize (80, 3);

        var cm = new ContextMenu
        {
            Position = Point.Empty
        };

        var menuItems = new MenuBarItem (
                                         [
                                             new MenuItem ("One", "", null),
                                             new MenuItem ("Two", "", null)
                                         ]
                                        );
        Assert.Equal (Point.Empty, cm.Position);

        Toplevel top = new ();
        Application.Begin (top);
        cm.Show (menuItems);
        Assert.Equal (Point.Empty, cm.Position);
        Application.Refresh ();

        var expected = @"
┌──────┐
│ One  │
│ Two  │";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new Rectangle (0, 0, 8, 3), pos);

        cm.Hide ();
        Assert.Equal (Point.Empty, cm.Position);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Show_Display_At_Zero_If_The_Toplevel_Width_Is_Less_Than_The_Menu_Width ()
    {
        ((FakeDriver)Application.Driver!).SetBufferSize (5, 25);

        var cm = new ContextMenu
        {
            Position = Point.Empty
        };

        var menuItems = new MenuBarItem (
                                         [
                                             new MenuItem ("One", "", null),
                                             new MenuItem ("Two", "", null)
                                         ]
                                        );
        Assert.Equal (Point.Empty, cm.Position);

        Toplevel top = new ();
        Application.Begin (top);
        cm.Show (menuItems);
        Assert.Equal (Point.Empty, cm.Position);
        Application.Refresh ();

        var expected = @"
┌────
│ One
│ Two
└────";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new Rectangle (0, 1, 5, 4), pos);

        cm.Hide ();
        Assert.Equal (Point.Empty, cm.Position);
        top.Dispose ();
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
            Position = new Point (10, 5)
        };

        var menuItems = new MenuBarItem (
                                         [
                                             new MenuItem ("One", "", null),
                                             new MenuItem ("Two", "", null)
                                         ]
                                        );
        var top = new Toplevel ();
        top.Add (view);
        Application.Begin (top);

        Assert.Equal (new Point (10, 5), cm.Position);

        cm.Show (menuItems);
        top.Draw ();
        Assert.Equal (new Point (10, 5), cm.Position);

        var expected = @"
          View    
          ┌──────┐
          │ One  │
          │ Two  │
          └──────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new Rectangle (10, 5, 18, 5), pos);

        cm.Hide ();
        Assert.Equal (new Point (10, 5), cm.Position);
        cm.Host.X = 5;
        cm.Host.Y = 10;
        cm.Host.Height = 3;

        cm.Show (menuItems);
        Application.Top.Draw ();
        Assert.Equal (new Point (5, 12), cm.Position);

        expected = @"
     View    
             
             
     ┌──────┐
     │ One  │
     │ Two  │
     └──────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new Rectangle (5, 10, 13, 7), pos);

        cm.Hide ();
        Assert.Equal (new Point (5, 12), cm.Position);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Show_Ensures_Display_Inside_The_Container_But_Preserves_Position ()
    {
        var cm = new ContextMenu
        {
            Position = new Point (80, 25)
        };

        var menuItems = new MenuBarItem (
                                         [
                                             new MenuItem ("One", "", null),
                                             new MenuItem ("Two", "", null)
                                         ]
                                        );
        Assert.Equal (new Point (80, 25), cm.Position);

        Toplevel top = new ();
        Application.Begin (top);
        cm.Show (menuItems);
        Assert.Equal (new Point (80, 25), cm.Position);
        Application.Refresh ();

        var expected = @"
                                                                        ┌──────┐
                                                                        │ One  │
                                                                        │ Two  │
                                                                        └──────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new Rectangle (72, 21, 80, 4), pos);

        cm.Hide ();
        Assert.Equal (new Point (80, 25), cm.Position);
        top.Dispose ();
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
            Host = view
        };

        var menuItems = new MenuBarItem (
                                         [
                                             new MenuItem ("One", "", null),
                                             new MenuItem ("Two", "", null)
                                         ]
                                        );
        var top = new Toplevel ();
        top.Add (view);
        Application.Begin (top);

        Assert.Equal (new Rectangle (70, 24, 10, 1), view.Frame);
        Assert.Equal (Point.Empty, cm.Position);

        cm.Show (menuItems);
        Assert.Equal (new Point (70, 24), cm.Position);
        top.Draw ();

        var expected = @"
                                                                      ┌──────┐
                                                                      │ One  │
                                                                      │ Two  │
                                                                      └──────┘
                                                                      View    
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new Rectangle (70, 20, 78, 5), pos);

        cm.Hide ();
        Assert.Equal (new Point (70, 24), cm.Position);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Show_Hide_IsShow ()
    {
        ContextMenu cm = new ContextMenu
        {
            Position = new Point (10, 5)
        };

        var menuItems = new MenuBarItem (
                                         new MenuItem [] { new ("One", "", null), new ("Two", "", null) }
                                        );

        Toplevel top = new ();
        Application.Begin (top);
        cm.Show (menuItems);
        Assert.True (ContextMenu.IsShow);
        Application.Refresh ();

        var expected = @"
          ┌──────┐
          │ One  │
          │ Two  │
          └──────┘
";

        TestHelpers.AssertDriverContentsAre (expected, output);

        cm.Hide ();
        Assert.False (ContextMenu.IsShow);

        Application.Refresh ();

        expected = "";

        TestHelpers.AssertDriverContentsAre (expected, output);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void UseSubMenusSingleFrame_True_By_Mouse ()
    {
        var cm = new ContextMenu
        {
            Position = new Point (5, 10),
            UseSubMenusSingleFrame = true
        };

        var menuItems = new MenuBarItem (
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
                                        );
        Toplevel top = new ();
        RunState rs = Application.Begin (top);
        cm.Show (menuItems);
        Assert.Equal (new Rectangle (5, 11, 10, 5), Application.Top!.Subviews [0].Frame);
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
     ┌────────┐
     │ One    │
     │ Two   ►│
     │ Three  │
     └────────┘",
                                                      output
                                                     );

        // X=5 is the border and so need to use at least one more
        Application.OnMouseEvent (new MouseEvent { ScreenPosition = new (6, 13), Flags = MouseFlags.Button1Clicked });

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
                                                      output
                                                     );

        Application.OnMouseEvent (new MouseEvent { ScreenPosition = new (6, 12), Flags = MouseFlags.Button1Clicked });

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
                                                      output
                                                     );

        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void UseSubMenusSingleFrame_False_By_Mouse ()
    {
        var cm = new ContextMenu
        {
            Position = new Point (5, 10)
        };

        var menuItems = new MenuBarItem (
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
                                             new MenuBarItem (
                                                              "Three",
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
                                        );
        Toplevel top = new ();
        RunState rs = Application.Begin (top);
        cm.Show (menuItems);

        Assert.Equal (new Rectangle (5, 11, 10, 5), Application.Top.Subviews [0].Frame);
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
     ┌────────┐
     │ One    │
     │ Two   ►│
     │ Three ►│
     └────────┘",
                                                      output
                                                     );

        Application.OnMouseEvent (new MouseEvent { ScreenPosition = new (6, 13), Flags = MouseFlags.ReportMousePosition });

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
                                                      output
                                                     );

        Application.OnMouseEvent (new MouseEvent { ScreenPosition = new (6, 14), Flags = MouseFlags.ReportMousePosition });

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
                                                      output
                                                     );

        Application.OnMouseEvent (new MouseEvent { ScreenPosition = new (6, 13), Flags = MouseFlags.ReportMousePosition });

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
                                                      output
                                                     );

        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Handling_TextField_With_Opened_ContextMenu_By_Mouse_HasFocus ()
    {
        var tf1 = new TextField { Width = 10, Text = "TextField 1" };
        var tf2 = new TextField { Y = 2, Width = 10, Text = "TextField 2" };
        var win = new Window ();
        win.Add (tf1, tf2);
        var rs = Application.Begin (win);

        Assert.True (tf1.HasFocus);
        Assert.False (tf2.HasFocus);
        Assert.Equal (4, win.Subviews.Count); // TF & TV add autocomplete popup's to their superviews.
        Assert.Empty (Application._cachedViewsUnderMouse);

        // Right click on tf2 to open context menu
        Application.OnMouseEvent (new () { ScreenPosition = new (1, 3), Flags = MouseFlags.Button3Clicked });
        Assert.False (tf1.HasFocus);
        Assert.False (tf2.HasFocus);
        Assert.Equal (5, win.Subviews.Count);
        Assert.True (tf2.ContextMenu.MenuBar.IsMenuOpen);
        Assert.True (win.Focused is Menu);
        Assert.True (Application.MouseGrabView is MenuBar);
        Assert.Equal (tf2, Application._cachedViewsUnderMouse.LastOrDefault ());

        // Click on tf1 to focus it, which cause context menu being closed
        Application.OnMouseEvent (new () { ScreenPosition = new (1, 1), Flags = MouseFlags.Button1Clicked });
        Assert.True (tf1.HasFocus);
        Assert.False (tf2.HasFocus);
        Assert.Equal (4, win.Subviews.Count);

        // The last context menu bar opened is always preserved
        Assert.NotNull (tf2.ContextMenu.MenuBar);
        Assert.Equal (win.Focused, tf1);
        Assert.Null (Application.MouseGrabView);
        Assert.Equal (tf1, Application._cachedViewsUnderMouse.LastOrDefault ());

        // Click on tf2 to focus it
        Application.OnMouseEvent (new () { ScreenPosition = new (1, 3), Flags = MouseFlags.Button1Clicked });
        Assert.False (tf1.HasFocus);
        Assert.True (tf2.HasFocus);
        Assert.Equal (4, win.Subviews.Count);

        // The last context menu bar opened is always preserved
        Assert.NotNull (tf2.ContextMenu.MenuBar);
        Assert.Equal (win.Focused, tf2);
        Assert.Null (Application.MouseGrabView);
        Assert.Equal (tf2, Application._cachedViewsUnderMouse.LastOrDefault ());

        Application.End (rs);
        win.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Empty_Menus_Items_Children_Does_Not_Open_The_Menu ()
    {
        var cm = new ContextMenu ();
        Assert.Null (cm.MenuItems);

        var top = new Toplevel ();
        Application.Begin (top);

        cm.Show (cm.MenuItems);
        Assert.Null (cm.MenuBar);

        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void KeyBindings_Removed_On_Close_ContextMenu ()
    {
        var newFile = false;
        var renameFile = false;
        var deleteFile = false;

        var cm = new ContextMenu ();

        var menuItems = new MenuBarItem (
                                         [
                                             new MenuItem ("New File", string.Empty, New, null, null, Key.N.WithCtrl),
                                             new MenuItem ("Rename File", string.Empty, Rename, null, null, Key.R.WithCtrl),
                                             new MenuItem ("Delete File", string.Empty, Delete, null, null, Key.D.WithCtrl)
                                         ]
                                        );
        var top = new Toplevel ();
        Application.Begin (top);

        Assert.Null (cm.MenuBar);
        Assert.False (Application.OnKeyDown (Key.N.WithCtrl));
        Assert.False (Application.OnKeyDown (Key.R.WithCtrl));
        Assert.False (Application.OnKeyDown (Key.D.WithCtrl));
        Assert.False (newFile);
        Assert.False (renameFile);
        Assert.False (deleteFile);

        cm.Show (menuItems);
        Assert.True (cm.MenuBar!.KeyBindings.Bindings.ContainsKey (Key.N.WithCtrl));
        Assert.True (cm.MenuBar.KeyBindings.Bindings.ContainsKey (Key.R.WithCtrl));
        Assert.True (cm.MenuBar.KeyBindings.Bindings.ContainsKey (Key.D.WithCtrl));

        Assert.True (Application.OnKeyDown (Key.N.WithCtrl));
        Application.MainLoop!.RunIteration ();
        Assert.True (newFile);
        Assert.False (cm.MenuBar!.IsMenuOpen);
        cm.Show (menuItems);
        Assert.True (Application.OnKeyDown (Key.R.WithCtrl));
        Application.MainLoop!.RunIteration ();
        Assert.True (renameFile);
        Assert.False (cm.MenuBar.IsMenuOpen);
        cm.Show (menuItems);
        Assert.True (Application.OnKeyDown (Key.D.WithCtrl));
        Application.MainLoop!.RunIteration ();
        Assert.True (deleteFile);
        Assert.False (cm.MenuBar.IsMenuOpen);

        Assert.False (cm.MenuBar.KeyBindings.Bindings.ContainsKey (Key.N.WithCtrl));
        Assert.False (cm.MenuBar.KeyBindings.Bindings.ContainsKey (Key.R.WithCtrl));
        Assert.False (cm.MenuBar.KeyBindings.Bindings.ContainsKey (Key.D.WithCtrl));

        newFile = false;
        renameFile = false;
        deleteFile = false;
        Assert.False (Application.OnKeyDown (Key.N.WithCtrl));
        Assert.False (Application.OnKeyDown (Key.R.WithCtrl));
        Assert.False (Application.OnKeyDown (Key.D.WithCtrl));
        Assert.False (newFile);
        Assert.False (renameFile);
        Assert.False (deleteFile);

        top.Dispose ();

        void New () { newFile = true; }

        void Rename () { renameFile = true; }

        void Delete () { deleteFile = true; }
    }

    [Fact]
    [AutoInitShutdown]
    public void KeyBindings_With_ContextMenu_And_MenuBar ()
    {
        var newFile = false;
        var renameFile = false;

        var menuBar = new MenuBar
        {
            Menus =
            [
                new (
                     "File",
                     new MenuItem []
                     {
                         new ("New", string.Empty, New, null, null, Key.N.WithCtrl)
                     })
            ]
        };
        var cm = new ContextMenu ();

        var menuItems = new MenuBarItem (
                                         [
                                             new ("Rename File", string.Empty, Rename, null, null, Key.R.WithCtrl),
                                         ]
                                        );
        var top = new Toplevel ();
        top.Add (menuBar);
        Application.Begin (top);

        Assert.True (menuBar.KeyBindings.Bindings.ContainsKey (Key.N.WithCtrl));
        Assert.False (menuBar.KeyBindings.Bindings.ContainsKey (Key.R.WithCtrl));
        Assert.Null (cm.MenuBar);

        Assert.True (Application.OnKeyDown (Key.N.WithCtrl));
        Assert.False (Application.OnKeyDown (Key.R.WithCtrl));
        Application.MainLoop!.RunIteration ();
        Assert.True (newFile);
        Assert.False (renameFile);

        newFile = false;

        cm.Show (menuItems);
        Assert.True (menuBar.KeyBindings.Bindings.ContainsKey (Key.N.WithCtrl));
        Assert.False (menuBar.KeyBindings.Bindings.ContainsKey (Key.R.WithCtrl));
        Assert.False (cm.MenuBar!.KeyBindings.Bindings.ContainsKey (Key.N.WithCtrl));
        Assert.True (cm.MenuBar.KeyBindings.Bindings.ContainsKey (Key.R.WithCtrl));

        Assert.True (cm.MenuBar.IsMenuOpen);
        Assert.True (Application.OnKeyDown (Key.N.WithCtrl));
        Application.MainLoop!.RunIteration ();
        Assert.True (newFile);
        Assert.False (cm.MenuBar!.IsMenuOpen);
        cm.Show (menuItems);
        Assert.True (Application.OnKeyDown (Key.R.WithCtrl));
        Application.MainLoop!.RunIteration ();
        Assert.True (renameFile);
        Assert.False (cm.MenuBar.IsMenuOpen);

        Assert.True (menuBar.KeyBindings.Bindings.ContainsKey (Key.N.WithCtrl));
        Assert.False (menuBar.KeyBindings.Bindings.ContainsKey (Key.R.WithCtrl));
        Assert.False (cm.MenuBar.KeyBindings.Bindings.ContainsKey (Key.N.WithCtrl));
        Assert.False (cm.MenuBar.KeyBindings.Bindings.ContainsKey (Key.R.WithCtrl));

        newFile = false;
        renameFile = false;
        Assert.True (Application.OnKeyDown (Key.N.WithCtrl));
        Assert.False (Application.OnKeyDown (Key.R.WithCtrl));
        Application.MainLoop!.RunIteration ();
        Assert.True (newFile);
        Assert.False (renameFile);

        top.Dispose ();

        void New () { newFile = true; }

        void Rename () { renameFile = true; }
    }

    [Fact]
    [AutoInitShutdown]
    public void KeyBindings_With_Same_Shortcut_ContextMenu_And_MenuBar ()
    {
        var newMenuBar = false;
        var newContextMenu = false;

        var menuBar = new MenuBar
        {
            Menus =
            [
                new (
                     "File",
                     new MenuItem []
                     {
                         new ("New", string.Empty, NewMenuBar, null, null, Key.N.WithCtrl)
                     })
            ]
        };
        var cm = new ContextMenu ();

        var menuItems = new MenuBarItem (
                                         [
                                             new ("New File", string.Empty, NewContextMenu, null, null, Key.N.WithCtrl),
                                         ]
                                        );
        var top = new Toplevel ();
        top.Add (menuBar);
        Application.Begin (top);

        Assert.True (menuBar.KeyBindings.Bindings.ContainsKey (Key.N.WithCtrl));
        Assert.Null (cm.MenuBar);

        Assert.True (Application.OnKeyDown (Key.N.WithCtrl));
        Application.MainLoop!.RunIteration ();
        Assert.True (newMenuBar);
        Assert.False (newContextMenu);

        newMenuBar = false;

        cm.Show (menuItems);
        Assert.True (menuBar.KeyBindings.Bindings.ContainsKey (Key.N.WithCtrl));
        Assert.True (cm.MenuBar!.KeyBindings.Bindings.ContainsKey (Key.N.WithCtrl));

        Assert.True (cm.MenuBar.IsMenuOpen);
        Assert.True (Application.OnKeyDown (Key.N.WithCtrl));
        Application.MainLoop!.RunIteration ();
        Assert.False (newMenuBar);

        // The most focused shortcut is executed
        Assert.True (newContextMenu);
        Assert.False (cm.MenuBar!.IsMenuOpen);

        Assert.True (menuBar.KeyBindings.Bindings.ContainsKey (Key.N.WithCtrl));
        Assert.False (cm.MenuBar.KeyBindings.Bindings.ContainsKey (Key.N.WithCtrl));

        newMenuBar = false;
        newContextMenu = false;
        Assert.True (Application.OnKeyDown (Key.N.WithCtrl));
        Application.MainLoop!.RunIteration ();
        Assert.True (newMenuBar);
        Assert.False (newContextMenu);

        top.Dispose ();

        void NewMenuBar () { newMenuBar = true; }

        void NewContextMenu () { newContextMenu = true; }
    }

    [Fact]
    [AutoInitShutdown]
    public void HotKeys_Removed_On_Close_ContextMenu ()
    {
        var newFile = false;
        var renameFile = false;
        var deleteFile = false;

        var cm = new ContextMenu ();

        var menuItems = new MenuBarItem (
                                         [
                                             new ("_New File", string.Empty, New, null, null),
                                             new ("_Rename File", string.Empty, Rename, null, null),
                                             new ("_Delete File", string.Empty, Delete, null, null)
                                         ]
                                        );
        var top = new Toplevel ();
        Application.Begin (top);

        Assert.Null (cm.MenuBar);
        Assert.False (Application.OnKeyDown (Key.N.WithAlt));
        Assert.False (Application.OnKeyDown (Key.R.WithAlt));
        Assert.False (Application.OnKeyDown (Key.D.WithAlt));
        Assert.False (newFile);
        Assert.False (renameFile);
        Assert.False (deleteFile);

        cm.Show (menuItems);
        Assert.True (cm.MenuBar!.IsMenuOpen);
        Assert.False (cm.MenuBar!.KeyBindings.Bindings.ContainsKey (Key.N.WithAlt));
        Assert.False (cm.MenuBar!.KeyBindings.Bindings.ContainsKey (Key.N.NoShift));
        Assert.False (cm.MenuBar.KeyBindings.Bindings.ContainsKey (Key.R.WithAlt));
        Assert.False (cm.MenuBar.KeyBindings.Bindings.ContainsKey (Key.R.NoShift));
        Assert.False (cm.MenuBar.KeyBindings.Bindings.ContainsKey (Key.D.WithAlt));
        Assert.False (cm.MenuBar.KeyBindings.Bindings.ContainsKey (Key.D.NoShift));
        Assert.Single (Application.Top!.Subviews);
        View [] menus = Application.Top!.Subviews.Where (v => v is Menu m && m.Host == cm.MenuBar).ToArray ();
        Assert.True (menus [0].KeyBindings.Bindings.ContainsKey (Key.N.WithAlt));
        Assert.True (menus [0].KeyBindings.Bindings.ContainsKey (Key.N.NoShift));
        Assert.True (menus [0].KeyBindings.Bindings.ContainsKey (Key.R.WithAlt));
        Assert.True (menus [0].KeyBindings.Bindings.ContainsKey (Key.R.NoShift));
        Assert.True (menus [0].KeyBindings.Bindings.ContainsKey (Key.D.WithAlt));
        Assert.True (menus [0].KeyBindings.Bindings.ContainsKey (Key.D.NoShift));

        Assert.True (Application.OnKeyDown (Key.N.WithAlt));
        Assert.False (cm.MenuBar!.IsMenuOpen);
        Application.MainLoop!.RunIteration ();
        Assert.True (newFile);
        cm.Show (menuItems);
        Assert.True (Application.OnKeyDown (Key.R.WithAlt));
        Assert.False (cm.MenuBar.IsMenuOpen);
        Application.MainLoop!.RunIteration ();
        Assert.True (renameFile);
        cm.Show (menuItems);
        Assert.True (Application.OnKeyDown (Key.D.WithAlt));
        Assert.False (cm.MenuBar.IsMenuOpen);
        Application.MainLoop!.RunIteration ();
        Assert.True (deleteFile);

        Assert.False (cm.MenuBar.KeyBindings.Bindings.ContainsKey (Key.N.WithAlt));
        Assert.False (cm.MenuBar.KeyBindings.Bindings.ContainsKey (Key.N.NoShift));
        Assert.False (cm.MenuBar.KeyBindings.Bindings.ContainsKey (Key.R.WithAlt));
        Assert.False (cm.MenuBar.KeyBindings.Bindings.ContainsKey (Key.R.NoShift));
        Assert.False (cm.MenuBar.KeyBindings.Bindings.ContainsKey (Key.D.WithAlt));
        Assert.False (cm.MenuBar.KeyBindings.Bindings.ContainsKey (Key.D.NoShift));

        newFile = false;
        renameFile = false;
        deleteFile = false;
        Assert.False (Application.OnKeyDown (Key.N.WithAlt));
        Assert.False (Application.OnKeyDown (Key.R.WithAlt));
        Assert.False (Application.OnKeyDown (Key.D.WithAlt));
        Assert.False (newFile);
        Assert.False (renameFile);
        Assert.False (deleteFile);

        top.Dispose ();

        void New () { newFile = true; }

        void Rename () { renameFile = true; }

        void Delete () { deleteFile = true; }
    }

    [Fact]
    [AutoInitShutdown]
    public void HotKeys_With_ContextMenu_And_MenuBar ()
    {
        var newFile = false;
        var renameFile = false;

        var menuBar = new MenuBar
        {
            Menus =
            [
                new (
                     "_File",
                     new MenuItem []
                     {
                         new ("_New", string.Empty, New)
                     })
            ]
        };
        var cm = new ContextMenu ();

        var menuItems = new MenuBarItem (
                                         [
                                             new MenuBarItem (
                                                              "_Edit",
                                                              new MenuItem []
                                                              {
                                                                  new ("_Rename File", string.Empty, Rename)
                                                              }
                                                             )
                                         ]
                                        );
        var top = new Toplevel ();
        top.Add (menuBar);
        Application.Begin (top);

        Assert.True (menuBar.KeyBindings.Bindings.ContainsKey (Key.F.WithAlt));
        Assert.False (menuBar.KeyBindings.Bindings.ContainsKey (Key.N.WithAlt));
        Assert.False (menuBar.KeyBindings.Bindings.ContainsKey (Key.R.WithAlt));
        View [] menus = Application.Top!.Subviews.Where (v => v is Menu m && m.Host == menuBar).ToArray ();
        Assert.Empty (menus);
        Assert.Null (cm.MenuBar);

        Assert.True (Application.OnKeyDown (Key.F.WithAlt));
        Assert.True (menuBar.IsMenuOpen);
        Assert.Equal (2, Application.Top!.Subviews.Count);
        menus = Application.Top!.Subviews.Where (v => v is Menu m && m.Host == menuBar).ToArray ();
        Assert.True (menus [0].KeyBindings.Bindings.ContainsKey (Key.N.WithAlt));
        Assert.True (Application.OnKeyDown (Key.N.WithAlt));
        Assert.False (menuBar.IsMenuOpen);
        Assert.False (Application.OnKeyDown (Key.R.WithAlt));
        Application.MainLoop!.RunIteration ();
        Assert.True (newFile);
        Assert.False (renameFile);

        newFile = false;

        cm.Show (menuItems);
        Assert.True (menuBar.KeyBindings.Bindings.ContainsKey (Key.F.WithAlt));
        Assert.True (menuBar.KeyBindings.Bindings.ContainsKey (Key.F.NoShift));
        Assert.False (menuBar.KeyBindings.Bindings.ContainsKey (Key.N.WithAlt));
        Assert.False (menuBar.KeyBindings.Bindings.ContainsKey (Key.N.NoShift));
        Assert.False (menuBar.KeyBindings.Bindings.ContainsKey (Key.E.WithAlt));
        Assert.False (menuBar.KeyBindings.Bindings.ContainsKey (Key.E.NoShift));
        Assert.False (menuBar.KeyBindings.Bindings.ContainsKey (Key.R.WithAlt));
        Assert.False (menuBar.KeyBindings.Bindings.ContainsKey (Key.R.NoShift));
        Assert.True (cm.MenuBar!.IsMenuOpen);
        Assert.False (cm.MenuBar!.KeyBindings.Bindings.ContainsKey (Key.F.WithAlt));
        Assert.False (cm.MenuBar!.KeyBindings.Bindings.ContainsKey (Key.F.NoShift));
        Assert.False (cm.MenuBar!.KeyBindings.Bindings.ContainsKey (Key.N.WithAlt));
        Assert.False (cm.MenuBar!.KeyBindings.Bindings.ContainsKey (Key.N.NoShift));
        Assert.False (cm.MenuBar!.KeyBindings.Bindings.ContainsKey (Key.E.WithAlt));
        Assert.False (cm.MenuBar!.KeyBindings.Bindings.ContainsKey (Key.E.NoShift));
        Assert.False (cm.MenuBar.KeyBindings.Bindings.ContainsKey (Key.R.WithAlt));
        Assert.False (cm.MenuBar.KeyBindings.Bindings.ContainsKey (Key.R.NoShift));
        Assert.Equal (3, Application.Top!.Subviews.Count);
        menus = Application.Top!.Subviews.Where (v => v is Menu m && m.Host == cm.MenuBar).ToArray ();
        Assert.True (menus [0].KeyBindings.Bindings.ContainsKey (Key.E.WithAlt));
        Assert.True (menus [0].KeyBindings.Bindings.ContainsKey (Key.E.NoShift));
        Assert.True (menus [1].KeyBindings.Bindings.ContainsKey (Key.R.WithAlt));
        Assert.True (menus [1].KeyBindings.Bindings.ContainsKey (Key.R.NoShift));
        Assert.True (cm.MenuBar.IsMenuOpen);
        Assert.True (Application.OnKeyDown (Key.F.WithAlt));
        Assert.False (cm.MenuBar.IsMenuOpen);
        Assert.True (Application.OnKeyDown (Key.N.WithAlt));
        Application.MainLoop!.RunIteration ();
        Assert.True (newFile);

        cm.Show (menuItems);
        Assert.True (cm.MenuBar.IsMenuOpen);
        Assert.Equal (3, Application.Top!.Subviews.Count);
        menus = Application.Top!.Subviews.Where (v => v is Menu m && m.Host == cm.MenuBar).ToArray ();
        Assert.True (menus [0].KeyBindings.Bindings.ContainsKey (Key.E.WithAlt));
        Assert.True (menus [0].KeyBindings.Bindings.ContainsKey (Key.E.NoShift));
        Assert.False (menus [0].KeyBindings.Bindings.ContainsKey (Key.R.WithAlt));
        Assert.False (menus [0].KeyBindings.Bindings.ContainsKey (Key.R.NoShift));
        Assert.False (menus [1].KeyBindings.Bindings.ContainsKey (Key.E.WithAlt));
        Assert.False (menus [1].KeyBindings.Bindings.ContainsKey (Key.E.NoShift));
        Assert.True (menus [1].KeyBindings.Bindings.ContainsKey (Key.R.WithAlt));
        Assert.True (menus [1].KeyBindings.Bindings.ContainsKey (Key.R.NoShift));
        Assert.True (Application.OnKeyDown (Key.E.NoShift));
        Assert.True (Application.OnKeyDown (Key.R.WithAlt));
        Assert.False (cm.MenuBar.IsMenuOpen);
        Application.MainLoop!.RunIteration ();
        Assert.True (renameFile);

        Assert.Single (Application.Top!.Subviews);
        Assert.True (menuBar.KeyBindings.Bindings.ContainsKey (Key.F.WithAlt));
        Assert.True (menuBar.KeyBindings.Bindings.ContainsKey (Key.F.NoShift));
        Assert.False (menuBar.KeyBindings.Bindings.ContainsKey (Key.N.WithAlt));
        Assert.False (menuBar.KeyBindings.Bindings.ContainsKey (Key.N.NoShift));
        Assert.False (cm.MenuBar.KeyBindings.Bindings.ContainsKey (Key.E.WithAlt));
        Assert.False (cm.MenuBar.KeyBindings.Bindings.ContainsKey (Key.E.NoShift));
        Assert.False (cm.MenuBar.KeyBindings.Bindings.ContainsKey (Key.R.WithAlt));
        Assert.False (cm.MenuBar.KeyBindings.Bindings.ContainsKey (Key.R.NoShift));

        newFile = false;
        renameFile = false;
        Assert.True (Application.OnKeyDown (Key.F.WithAlt));
        Assert.True (Application.OnKeyDown (Key.N.WithAlt));
        Assert.False (Application.OnKeyDown (Key.R.WithAlt));
        Application.MainLoop!.RunIteration ();
        Assert.True (newFile);
        Assert.False (renameFile);

        top.Dispose ();

        void New () { newFile = true; }

        void Rename () { renameFile = true; }
    }

    [Fact]
    [AutoInitShutdown]
    public void Opened_MenuBar_Is_Closed_When_Another_MenuBar_Is_Opening_Also_By_HotKey ()
    {
        var menuBar = new MenuBar
        {
            Menus =
            [
                new (
                     "_File",
                     new MenuItem []
                     {
                         new ("_New", string.Empty, null)
                     })
            ]
        };
        var cm = new ContextMenu ();

        var menuItems = new MenuBarItem (
                                         [
                                             new MenuBarItem (
                                                              "_Edit",
                                                              new MenuItem []
                                                              {
                                                                  new ("_Rename File", string.Empty, null)
                                                              }
                                                             )
                                         ]
                                        );
        var top = new Toplevel ();
        top.Add (menuBar);
        Application.Begin (top);

        Assert.True (Application.OnKeyDown (Key.F.WithAlt));
        Assert.True (menuBar.IsMenuOpen);

        cm.Show (menuItems);
        Assert.False (menuBar.IsMenuOpen);
        Assert.True (cm.MenuBar!.IsMenuOpen);

        Assert.True (Application.OnKeyDown (Key.F.WithAlt));
        Assert.True (menuBar.IsMenuOpen);
        Assert.False (cm.MenuBar!.IsMenuOpen);

        top.Dispose ();
    }
}