using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class MenuBarTests (ITestOutputHelper output)
{
    [Fact]
    [AutoInitShutdown]
    public void AddMenuBarItem_RemoveMenuItem_Dynamically ()
    {
        var menuBar = new MenuBar ();
        var menuBarItem = new MenuBarItem { Title = "_New" };
        var action = "";
        var menuItem = new MenuItem { Title = "_Item", Action = () => action = "I", Parent = menuBarItem };
        Assert.Equal ("n", menuBarItem.HotKey);
        Assert.Equal ("i", menuItem.HotKey);
        Assert.Empty (menuBar.Menus);
        menuBarItem.AddMenuBarItem (menuBar, menuItem);
        menuBar.Menus = [menuBarItem];
        Assert.Single (menuBar.Menus);
        Assert.Single (menuBar.Menus [0].Children!);
        Assert.Contains (Key.N.WithAlt, menuBar.KeyBindings.Bindings);
        Assert.DoesNotContain (Key.I, menuBar.KeyBindings.Bindings);

        var top = new Toplevel ();
        top.Add (menuBar);
        Application.Begin (top);

        top.NewKeyDownEvent (Key.N.WithAlt);
        Application.MainLoop.RunIteration ();
        Assert.True (menuBar.IsMenuOpen);
        Assert.Equal ("", action);

        top.NewKeyDownEvent (Key.I);
        Application.MainLoop.RunIteration ();
        Assert.False (menuBar.IsMenuOpen);
        Assert.Equal ("I", action);

        menuItem.RemoveMenuItem ();
        Assert.Single (menuBar.Menus);
        Assert.Null (menuBar.Menus [0].Children);
        Assert.Contains (Key.N.WithAlt, menuBar.KeyBindings.Bindings);
        Assert.DoesNotContain (Key.I, menuBar.KeyBindings.Bindings);

        menuBarItem.RemoveMenuItem ();
        Assert.Empty (menuBar.Menus);
        Assert.DoesNotContain (Key.N.WithAlt, menuBar.KeyBindings.Bindings);

        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void AllowNullChecked_Get_Set ()
    {
        var mi = new MenuItem ("Check this out 你", "", null) { CheckType = MenuItemCheckStyle.Checked };
        mi.Action = mi.ToggleChecked;

        var menu = new MenuBar
        {
            Menus =
            [
                new ("Nullable Checked", new [] { mi })
            ]
        };

        //new CheckBox ();
        Toplevel top = new ();
        top.Add (menu);
        Application.Begin (top);

        Assert.False (mi.Checked);
        Assert.True (menu.NewKeyDownEvent (menu.Key));
        Assert.True (menu._openMenu.NewKeyDownEvent (Key.Enter));
        Application.MainLoop.RunIteration ();
        Assert.True (mi.Checked);

        Assert.True (
                     menu.NewMouseEvent (
                                         new () { Position = new (0, 0), Flags = MouseFlags.Button1Pressed, View = menu }
                                        )
                    );

        Assert.True (
                     menu._openMenu.NewMouseEvent (
                                                   new () { Position = new (0, 0), Flags = MouseFlags.Button1Clicked, View = menu._openMenu }
                                                  )
                    );
        Application.MainLoop.RunIteration ();
        Assert.False (mi.Checked);

        mi.AllowNullChecked = true;
        Assert.True (menu.NewKeyDownEvent (menu.Key));
        Assert.True (menu._openMenu.NewKeyDownEvent (Key.Enter));
        Application.MainLoop.RunIteration ();
        Assert.Null (mi.Checked);

        Assert.True (
                     menu.NewMouseEvent (
                                         new () { Position = new (0, 0), Flags = MouseFlags.Button1Pressed, View = menu }
                                        )
                    );
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @$"
 Nullable Checked       
┌──────────────────────┐
│ {CM.Glyphs.CheckStateNone} Check this out 你  │
└──────────────────────┘",
                                                      output
                                                     );

        Assert.True (
                     menu._openMenu.NewMouseEvent (
                                                   new () { Position = new (0, 0), Flags = MouseFlags.Button1Clicked, View = menu._openMenu }
                                                  )
                    );
        Application.MainLoop.RunIteration ();
        Assert.True (mi.Checked);
        Assert.True (menu.NewKeyDownEvent (menu.Key));
        Assert.True (menu._openMenu.NewKeyDownEvent (Key.Enter));
        Application.MainLoop.RunIteration ();
        Assert.False (mi.Checked);

        Assert.True (
                     menu.NewMouseEvent (
                                         new () { Position = new (0, 0), Flags = MouseFlags.Button1Pressed, View = menu }
                                        )
                    );

        Assert.True (
                     menu._openMenu.NewMouseEvent (
                                                   new () { Position = new (0, 0), Flags = MouseFlags.Button1Clicked, View = menu._openMenu }
                                                  )
                    );
        Application.MainLoop.RunIteration ();
        Assert.Null (mi.Checked);

        mi.AllowNullChecked = false;
        Assert.False (mi.Checked);

        mi.CheckType = MenuItemCheckStyle.NoCheck;
        Assert.Throws<InvalidOperationException> (mi.ToggleChecked);

        mi.CheckType = MenuItemCheckStyle.Radio;
        Assert.Throws<InvalidOperationException> (mi.ToggleChecked);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void CanExecute_False_Does_Not_Throws ()
    {
        var menu = new MenuBar
        {
            Menus =
            [
                new ("File", new MenuItem []
                {
                    new ("New", "", null, () => false),
                    null,
                    new ("Quit", "", null)
                })
            ]
        };
        var top = new Toplevel ();
        top.Add (menu);
        Application.Begin (top);

        Assert.True (menu.NewKeyDownEvent (menu.Key));
        Assert.True (menu.IsMenuOpen);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void CanExecute_HotKey ()
    {
        Window win = null;

        var menu = new MenuBar
        {
            Menus =
            [
                new (
                     "_File",
                     new MenuItem []
                     {
                         new ("_New", "", New, CanExecuteNew),
                         new (
                              "_Close",
                              "",
                              Close,
                              CanExecuteClose
                             )
                     }
                    )
            ]
        };
        Toplevel top = new ();
        top.Add (menu);

        bool CanExecuteNew () { return win == null; }

        void New () { win = new (); }

        bool CanExecuteClose () { return win != null; }

        void Close () { win = null; }

        Application.Begin (top);

        Assert.Null (win);
        Assert.True (CanExecuteNew ());
        Assert.False (CanExecuteClose ());

        Assert.True (top.NewKeyDownEvent (Key.F.WithAlt));
        Assert.True (top.NewKeyDownEvent (Key.N.WithAlt));
        Application.MainLoop.RunIteration ();
        Assert.NotNull (win);
        Assert.False (CanExecuteNew ());
        Assert.True (CanExecuteClose ());
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Click_Another_View_Close_An_Open_Menu ()
    {
        var menu = new MenuBar
        {
            Menus =
            [
                new ("File", new MenuItem [] { new ("New", "", null) })
            ]
        };

        var btnClicked = false;
        var btn = new Button { Y = 4, Text = "Test" };
        btn.Accepting += (s, e) => btnClicked = true;
        var top = new Toplevel ();
        top.Add (menu, btn);
        Application.Begin (top);

        Application.RaiseMouseEvent (new () { ScreenPosition = new (0, 4), Flags = MouseFlags.Button1Clicked });
        Assert.True (btnClicked);
        top.Dispose ();
    }

    // TODO: Lots of tests in here really test Menu and MenuItem - Move them to MenuTests.cs

    [Fact]
    public void Constructors_Defaults ()
    {
        var menuBar = new MenuBar ();
        Assert.Equal (KeyCode.F9, menuBar.Key);
        var menu = new Menu { Host = menuBar, X = 0, Y = 0, BarItems = new () };
        Assert.Null (menu.ColorScheme);
        Assert.False (menu.IsInitialized);
        menu.BeginInit ();
        menu.EndInit ();
        Assert.Equal (Colors.ColorSchemes ["Menu"], menu.ColorScheme);
        Assert.True (menu.CanFocus);
        Assert.False (menu.WantContinuousButtonPressed);
        Assert.Equal (LineStyle.Single, menuBar.MenusBorderStyle);

        menuBar = new ();
        Assert.Equal (0, menuBar.X);
        Assert.Equal (0, menuBar.Y);
        Assert.IsType<DimFill> (menuBar.Width);
        Assert.Equal (1, menuBar.Height);
        Assert.Empty (menuBar.Menus);
        Assert.Equal (Colors.ColorSchemes ["Menu"], menuBar.ColorScheme);
        Assert.True (menuBar.WantMousePositionReports);
        Assert.False (menuBar.IsMenuOpen);

        menuBar = new () { Menus = [] };
        Assert.Equal (0, menuBar.X);
        Assert.Equal (0, menuBar.Y);
        Assert.IsType<DimFill> (menuBar.Width);
        Assert.Equal (1, menuBar.Height);
        Assert.Empty (menuBar.Menus);
        Assert.Equal (Colors.ColorSchemes ["Menu"], menuBar.ColorScheme);
        Assert.True (menuBar.WantMousePositionReports);
        Assert.False (menuBar.IsMenuOpen);

        var menuBarItem = new MenuBarItem ();
        Assert.Equal ("", menuBarItem.Title);
        Assert.Null (menuBarItem.Parent);
        Assert.Empty (menuBarItem.Children);

        menuBarItem = new (new MenuBarItem [] { });
        Assert.Equal ("", menuBarItem.Title);
        Assert.Null (menuBarItem.Parent);
        Assert.Empty (menuBarItem.Children);

        menuBarItem = new ("Test", new MenuBarItem [] { });
        Assert.Equal ("Test", menuBarItem.Title);
        Assert.Null (menuBarItem.Parent);
        Assert.Empty (menuBarItem.Children);

        menuBarItem = new ("Test", new List<MenuItem []> ());
        Assert.Equal ("Test", menuBarItem.Title);
        Assert.Null (menuBarItem.Parent);
        Assert.Empty (menuBarItem.Children);

        menuBarItem = new ("Test", "Help", null);
        Assert.Equal ("Test", menuBarItem.Title);
        Assert.Equal ("Help", menuBarItem.Help);
        Assert.Null (menuBarItem.Action);
        Assert.Null (menuBarItem.CanExecute);
        Assert.Null (menuBarItem.Parent);
        Assert.Equal (Key.Empty, menuBarItem.ShortcutKey);
    }

    [Fact]
    [AutoInitShutdown (configLocation: ConfigurationManager.ConfigLocations.DefaultOnly)]
    public void Disabled_MenuBar_Is_Never_Opened ()
    {
        Toplevel top = new ();

        var menu = new MenuBar
        {
            Menus =
            [
                new ("File", new MenuItem [] { new ("New", "", null) })
            ]
        };
        top.Add (menu);
        Application.Begin (top);
        Assert.True (menu.Enabled);
        menu.OpenMenu ();
        Assert.True (menu.IsMenuOpen);

        menu.Enabled = false;
        menu.CloseAllMenus ();
        menu.OpenMenu ();
        Assert.False (menu.IsMenuOpen);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown (configLocation: ConfigurationManager.ConfigLocations.DefaultOnly)]
    public void Disabled_MenuItem_Is_Never_Selected ()
    {
        var menu = new MenuBar
        {
            Menus =
            [
                new (
                     "Menu",
                     new MenuItem []
                     {
                         new ("Enabled 1", "", null),
                         new ("Disabled", "", null, () => false),
                         null,
                         new ("Enabled 2", "", null)
                     }
                    )
            ]
        };

        Toplevel top = new ();
        top.Add (menu);
        Application.Begin (top);

        Attribute [] attributes =
        {
            // 0
            menu.ColorScheme.Normal,

            // 1
            menu.ColorScheme.Focus,

            // 2
            menu.ColorScheme.Disabled
        };

        TestHelpers.AssertDriverAttributesAre (
                                               @"
00000000000000",
                                               Application.Driver,
                                               attributes
                                              );

        Assert.True (
                     menu.NewMouseEvent (
                                         new () { Position = new (0, 0), Flags = MouseFlags.Button1Pressed, View = menu }
                                        )
                    );
        top.Draw ();

        TestHelpers.AssertDriverAttributesAre (
                                               @"
11111100000000
00000000000000
01111111111110
02222222222220
00000000000000
00000000000000
00000000000000",
                                               Application.Driver,
                                               attributes
                                              );

        Assert.True (
                     top.Subviews [1]
                        .NewMouseEvent (
                                        new () { Position = new (0, 2), Flags = MouseFlags.Button1Clicked, View = top.Subviews [1] }
                                       )
                    );
        top.Subviews [1].Draw ();

        TestHelpers.AssertDriverAttributesAre (
                                               @"
11111100000000
00000000000000
01111111111110
02222222222220
00000000000000
00000000000000
00000000000000",
                                               Application.Driver,
                                               attributes
                                              );

        Assert.True (
                     top.Subviews [1]
                        .NewMouseEvent (
                                        new () { Position = new (0, 2), Flags = MouseFlags.ReportMousePosition, View = top.Subviews [1] }
                                       )
                    );
        top.Subviews [1].Draw ();

        TestHelpers.AssertDriverAttributesAre (
                                               @"
11111100000000
00000000000000
01111111111110
02222222222220
00000000000000
00000000000000
00000000000000",
                                               Application.Driver,
                                               attributes
                                              );
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Draw_A_Menu_Over_A_Dialog ()
    {
        // Override CM
        Window.DefaultBorderStyle = LineStyle.Single;
        Dialog.DefaultButtonAlignment = Alignment.Center;
        Dialog.DefaultBorderStyle = LineStyle.Single;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        Toplevel top = new ();
        var win = new Window ();
        top.Add (win);
        RunState rsTop = Application.Begin (top);
        ((FakeDriver)Application.Driver!).SetBufferSize (40, 15);

        Assert.Equal (new (0, 0, 40, 15), win.Frame);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────────────────────────┐
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
└──────────────────────────────────────┘",
                                                      output
                                                     );

        List<string> items = new ()
        {
            "New",
            "Open",
            "Close",
            "Save",
            "Save As",
            "Delete"
        };
        var dialog = new Dialog { X = 2, Y = 2, Width = 15, Height = 4 };
        var menu = new MenuBar { X = Pos.Center (), Width = 10 };

        menu.Menus = new MenuBarItem []
        {
            new (
                 "File",
                 new MenuItem []
                 {
                     new (
                          items [0],
                          "Create a new file",
                          () => ChangeMenuTitle ("New"),
                          null,
                          null,
                          KeyCode.CtrlMask | KeyCode.N
                         ),
                     new (
                          items [1],
                          "Open a file",
                          () => ChangeMenuTitle ("Open"),
                          null,
                          null,
                          KeyCode.CtrlMask | KeyCode.O
                         ),
                     new (
                          items [2],
                          "Close a file",
                          () => ChangeMenuTitle ("Close"),
                          null,
                          null,
                          KeyCode.CtrlMask | KeyCode.C
                         ),
                     new (
                          items [3],
                          "Save a file",
                          () => ChangeMenuTitle ("Save"),
                          null,
                          null,
                          KeyCode.CtrlMask | KeyCode.S
                         ),
                     new (
                          items [4],
                          "Save a file as",
                          () => ChangeMenuTitle ("Save As"),
                          null,
                          null,
                          KeyCode.CtrlMask | KeyCode.A
                         ),
                     new (
                          items [5],
                          "Delete a file",
                          () => ChangeMenuTitle ("Delete"),
                          null,
                          null,
                          KeyCode.CtrlMask | KeyCode.A
                         )
                 }
                )
        };
        dialog.Add (menu);

        void ChangeMenuTitle (string title)
        {
            menu.Menus [0].Title = title;
            menu.SetNeedsDisplay ();
        }

        RunState rsDialog = Application.Begin (dialog);

        Assert.Equal (new (2, 2, 15, 4), dialog.Frame);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────────────────────────┐
│                                      │
│ ┌─────────────┐                      │
│ │  File       │                      │
│ │             │                      │
│ └─────────────┘                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
└──────────────────────────────────────┘",
                                                      output
                                                     );

        Assert.Equal ("File", menu.Menus [0].Title);
        menu.OpenMenu ();
        var firstIteration = false;
        Application.RunIteration (ref rsDialog, ref firstIteration);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────────────────────────┐
│                                      │
│ ┌─────────────┐                      │
│ │  File       │                      │
│ │ ┌──────────────────────────────────┐
│ └─│ New    Create a new file  Ctrl+N │
│   │ Open         Open a file  Ctrl+O │
│   │ Close       Close a file  Ctrl+C │
│   │ Save         Save a file  Ctrl+S │
│   │ Save As   Save a file as  Ctrl+A │
│   │ Delete     Delete a file  Ctrl+A │
│   └──────────────────────────────────┘
│                                      │
│                                      │
└──────────────────────────────────────┘",
                                                      output
                                                     );

        Application.RaiseMouseEvent (new () { ScreenPosition = new (20, 5), Flags = MouseFlags.Button1Clicked });

        firstIteration = false;

        // Need to fool MainLoop into thinking it's running
        Application.MainLoop.Running = true;
        Application.RunIteration (ref rsDialog, ref firstIteration);
        Assert.Equal (items [0], menu.Menus [0].Title);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────────────────────────┐
│                                      │
│ ┌─────────────┐                      │
│ │  New        │                      │
│ │             │                      │
│ └─────────────┘                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
└──────────────────────────────────────┘",
                                                      output
                                                     );

        for (var i = 0; i < items.Count; i++)
        {
            menu.OpenMenu ();

            Application.RaiseMouseEvent (new () { ScreenPosition = new (20, 5 + i), Flags = MouseFlags.Button1Clicked });

            firstIteration = false;
            Application.RunIteration (ref rsDialog, ref firstIteration);
            Assert.Equal (items [i], menu.Menus [0].Title);
        }

        ((FakeDriver)Application.Driver!).SetBufferSize (20, 15);
        menu.OpenMenu ();
        firstIteration = false;
        Application.RunIteration (ref rsDialog, ref firstIteration);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────┐
│                  │
│ ┌─────────────┐  │
│ │  Delete     │  │
│ │ ┌───────────────
│ └─│ New    Create 
│   │ Open         O
│   │ Close       Cl
│   │ Save         S
│   │ Save As   Save
│   │ Delete     Del
│   └───────────────
│                  │
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
    public void Draw_A_Menu_Over_A_Top_Dialog ()
    {
        ((FakeDriver)Application.Driver).SetBufferSize (40, 15);

        // Override CM
        Window.DefaultBorderStyle = LineStyle.Single;
        Dialog.DefaultButtonAlignment = Alignment.Center;
        Dialog.DefaultBorderStyle = LineStyle.Single;
        Dialog.DefaultShadow = ShadowStyle.None;
        Button.DefaultShadow = ShadowStyle.None;

        Assert.Equal (new (0, 0, 40, 15), Application.Driver?.Clip);
        TestHelpers.AssertDriverContentsWithFrameAre (@"", output);

        List<string> items = new ()
        {
            "New",
            "Open",
            "Close",
            "Save",
            "Save As",
            "Delete"
        };
        var dialog = new Dialog { X = 2, Y = 2, Width = 15, Height = 4 };
        var menu = new MenuBar { X = Pos.Center (), Width = 10 };

        menu.Menus = new MenuBarItem []
        {
            new (
                 "File",
                 new MenuItem []
                 {
                     new (
                          items [0],
                          "Create a new file",
                          () => ChangeMenuTitle ("New"),
                          null,
                          null,
                          KeyCode.CtrlMask | KeyCode.N
                         ),
                     new (
                          items [1],
                          "Open a file",
                          () => ChangeMenuTitle ("Open"),
                          null,
                          null,
                          KeyCode.CtrlMask | KeyCode.O
                         ),
                     new (
                          items [2],
                          "Close a file",
                          () => ChangeMenuTitle ("Close"),
                          null,
                          null,
                          KeyCode.CtrlMask | KeyCode.C
                         ),
                     new (
                          items [3],
                          "Save a file",
                          () => ChangeMenuTitle ("Save"),
                          null,
                          null,
                          KeyCode.CtrlMask | KeyCode.S
                         ),
                     new (
                          items [4],
                          "Save a file as",
                          () => ChangeMenuTitle ("Save As"),
                          null,
                          null,
                          KeyCode.CtrlMask | KeyCode.A
                         ),
                     new (
                          items [5],
                          "Delete a file",
                          () => ChangeMenuTitle ("Delete"),
                          null,
                          null,
                          KeyCode.CtrlMask | KeyCode.A
                         )
                 }
                )
        };
        dialog.Add (menu);

        void ChangeMenuTitle (string title)
        {
            menu.Menus [0].Title = title;
            menu.SetNeedsDisplay ();
        }

        RunState rs = Application.Begin (dialog);

        Assert.Equal (new (2, 2, 15, 4), dialog.Frame);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
  ┌─────────────┐
  │  File       │
  │             │
  └─────────────┘",
                                                      output
                                                     );

        Assert.Equal ("File", menu.Menus [0].Title);
        menu.OpenMenu ();
        var firstIteration = false;
        Application.RunIteration (ref rs, ref firstIteration);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
  ┌─────────────┐                       
  │  File       │                       
  │ ┌──────────────────────────────────┐
  └─│ New    Create a new file  Ctrl+N │
    │ Open         Open a file  Ctrl+O │
    │ Close       Close a file  Ctrl+C │
    │ Save         Save a file  Ctrl+S │
    │ Save As   Save a file as  Ctrl+A │
    │ Delete     Delete a file  Ctrl+A │
    └──────────────────────────────────┘",
                                                      output
                                                     );

        Application.RaiseMouseEvent (new () { ScreenPosition = new (20, 5), Flags = MouseFlags.Button1Clicked });

        firstIteration = false;

        // Need to fool MainLoop into thinking it's running
        Application.MainLoop.Running = true;
        Application.RunIteration (ref rs, ref firstIteration);
        Assert.Equal (items [0], menu.Menus [0].Title);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
  ┌─────────────┐
  │  New        │
  │             │
  └─────────────┘",
                                                      output
                                                     );

        for (var i = 1; i < items.Count; i++)
        {
            menu.OpenMenu ();

            Application.RaiseMouseEvent (new () { ScreenPosition = new (20, 5 + i), Flags = MouseFlags.Button1Clicked });

            firstIteration = false;
            Application.RunIteration (ref rs, ref firstIteration);
            Assert.Equal (items [i], menu.Menus [0].Title);
        }

        ((FakeDriver)Application.Driver!).SetBufferSize (20, 15);
        menu.OpenMenu ();
        firstIteration = false;
        Application.RunIteration (ref rs, ref firstIteration);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
  ┌─────────────┐   
  │  Delete     │   
  │ ┌───────────────
  └─│ New    Create 
    │ Open         O
    │ Close       Cl
    │ Save         S
    │ Save As   Save
    │ Delete     Del
    └───────────────",
                                                      output
                                                     );

        Application.End (rs);
        dialog.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void DrawFrame_With_Negative_Positions ()
    {
        var menu = new MenuBar
        {
            X = -1,
            Y = -1,
            Menus =
            [
                new (new MenuItem [] { new ("One", "", null), new ("Two", "", null) })
            ]
        };

        Assert.Equal (new (-1, -1), new Point (menu.Frame.X, menu.Frame.Y));

        Toplevel top = new ();
        Application.Begin (top);
        menu.OpenMenu ();
        Application.Refresh ();

        var expected = @"
──────┐
 One  │
 Two  │
──────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 7, 4), pos);

        menu.CloseAllMenus ();
        menu.Frame = new (-1, -2, menu.Frame.Width, menu.Frame.Height);
        menu.OpenMenu ();
        Application.Refresh ();

        expected = @"
 One  │
 Two  │
──────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 0, 7, 3), pos);

        menu.CloseAllMenus ();
        menu.Frame = new (0, 0, menu.Frame.Width, menu.Frame.Height);
        ((FakeDriver)Application.Driver!).SetBufferSize (7, 5);
        menu.OpenMenu ();
        Application.Refresh ();

        expected = @"
┌──────
│ One  
│ Two  
└──────
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 1, 7, 4), pos);

        menu.CloseAllMenus ();
        menu.Frame = new (0, 0, menu.Frame.Width, menu.Frame.Height);
        ((FakeDriver)Application.Driver!).SetBufferSize (7, 3);
        menu.OpenMenu ();
        Application.Refresh ();

        expected = @"
┌──────
│ One  
│ Two  
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 7, 3), pos);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void DrawFrame_With_Negative_Positions_Disabled_Border ()
    {
        var menu = new MenuBar
        {
            X = -2,
            Y = -1,
            MenusBorderStyle = LineStyle.None,
            Menus =
            [
                new (new MenuItem [] { new ("One", "", null), new ("Two", "", null) })
            ]
        };

        Assert.Equal (new (-2, -1), new Point (menu.Frame.X, menu.Frame.Y));

        Toplevel top = new ();
        Application.Begin (top);
        menu.OpenMenu ();
        Application.Refresh ();

        var expected = @"
ne
wo
";

        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        menu.CloseAllMenus ();
        menu.Frame = new (-2, -2, menu.Frame.Width, menu.Frame.Height);
        menu.OpenMenu ();
        Application.Refresh ();

        expected = @"
wo
";

        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        menu.CloseAllMenus ();
        menu.Frame = new (0, 0, menu.Frame.Width, menu.Frame.Height);
        ((FakeDriver)Application.Driver!).SetBufferSize (3, 2);
        menu.OpenMenu ();
        Application.Refresh ();

        expected = @"
 On
 Tw
";

        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        menu.CloseAllMenus ();
        menu.Frame = new (0, 0, menu.Frame.Width, menu.Frame.Height);
        ((FakeDriver)Application.Driver!).SetBufferSize (3, 1);
        menu.OpenMenu ();
        Application.Refresh ();

        expected = @"
 On
";

        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void DrawFrame_With_Positive_Positions ()
    {
        var menu = new MenuBar
        {
            Menus =
            [
                new (new MenuItem [] { new ("One", "", null), new ("Two", "", null) })
            ]
        };

        Assert.Equal (Point.Empty, new (menu.Frame.X, menu.Frame.Y));

        Toplevel top = new ();
        Application.Begin (top);
        menu.OpenMenu ();
        Application.Refresh ();

        var expected = @"
┌──────┐
│ One  │
│ Two  │
└──────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 1, 8, 4), pos);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void DrawFrame_With_Positive_Positions_Disabled_Border ()
    {
        var menu = new MenuBar
        {
            MenusBorderStyle = LineStyle.None,
            Menus =
            [
                new (new MenuItem [] { new ("One", "", null), new ("Two", "", null) })
            ]
        };

        Assert.Equal (Point.Empty, new (menu.Frame.X, menu.Frame.Y));

        Toplevel top = new ();
        Application.Begin (top);
        menu.OpenMenu ();
        Application.Refresh ();

        var expected = @"
 One
 Two
";

        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        top.Dispose ();
    }

    [Fact]
    public void Exceptions ()
    {
        Assert.Throws<ArgumentNullException> (() => new MenuBarItem ("Test", (MenuItem [])null));
        Assert.Throws<ArgumentNullException> (() => new MenuBarItem ("Test", (List<MenuItem []>)null));
    }

    [Fact]
    [AutoInitShutdown]
    public void HotKey_MenuBar_OnKeyDown_OnKeyUp_ProcessKeyPressed ()
    {
        var newAction = false;
        var copyAction = false;

        var menu = new MenuBar
        {
            Menus =
            [
                new ("_File", new MenuItem [] { new ("_New", "", () => newAction = true) }),
                new (
                     "_Edit",
                     new MenuItem [] { new ("_Copy", "", () => copyAction = true) }
                    )
            ]
        };

        var top = new Toplevel ();
        top.Add (menu);
        Application.Begin (top);

        Assert.False (newAction);
        Assert.False (copyAction);

#if SUPPORT_ALT_TO_ACTIVATE_MENU
        Assert.False (Application.Top.ProcessKeyDown (new KeyEventArgs (Key.AltMask)));
        Assert.False (Application.Top.ProcessKeyDown (new KeyEventArgs (Key.AltMask)));
        Assert.True (Application.Top.ProcessKeyUp (new KeyEventArgs (Key.AltMask)));
        Assert.True (menu.IsMenuOpen);
        Application.Top.Draw ();

        string expected = @"
 File  Edit
";

        var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 0, 11, 1), pos);

        Assert.True (Application.Top.ProcessKeyDown (new KeyEventArgs (Key.N)));
        Application.MainLoop.RunIteration ();
        Assert.False (newAction); // not yet, hot keys don't work if the item is not visible

        Assert.True (Application.Top.ProcessKeyDown (new KeyEventArgs (Key.F)));
        Application.MainLoop.RunIteration ();
        Assert.True (Application.Top.ProcessKeyDown (new KeyEventArgs (Key.N)));
        Application.MainLoop.RunIteration ();
        Assert.True (newAction);
        Application.Top.Draw ();

        expected = @"
 File  Edit
";

        Assert.False (Application.Top.ProcessKeyDown (new KeyEventArgs (Key.AltMask)));
        Assert.True (Application.Top.ProcessKeyUp (new KeyEventArgs (Key.AltMask)));
        Assert.True (Application.Top.ProcessKeyUp (new KeyEventArgs (Key.AltMask)));
        Assert.True (menu.IsMenuOpen);
        Application.Top.Draw ();

        expected = @"
 File  Edit
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 0, 11, 1), pos);

        Assert.True (Application.Top.ProcessKeyDown (new KeyEventArgs (Key.CursorRight)));
        Assert.True (Application.Top.ProcessKeyDown (new KeyEventArgs (Key.C)));
        Application.MainLoop.RunIteration ();
        Assert.True (copyAction);
#endif
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void HotKey_MenuBar_ProcessKeyPressed_Menu_ProcessKey ()
    {
        var newAction = false;
        var copyAction = false;

        // Define the expected menu
        var expectedMenu = new ExpectedMenuBar
        {
            Menus =
            [
                new ("File", new MenuItem [] { new ("New", "", null) }),
                new (
                     "Edit",
                     new MenuItem [] { new ("Copy", "", null) }
                    )
            ]
        };

        // The real menu
        var menu = new MenuBar
        {
            Menus =
            [
                new (
                     "_" + expectedMenu.Menus [0].Title,
                     new MenuItem []
                     {
                         new (
                              "_" + expectedMenu.Menus [0].Children [0].Title,
                              "",
                              () => newAction = true
                             )
                     }
                    ),
                new (
                     "_" + expectedMenu.Menus [1].Title,
                     new MenuItem []
                     {
                         new (
                              "_"
                              + expectedMenu.Menus [1]
                                            .Children [0]
                                            .Title,
                              "",
                              () => copyAction = true
                             )
                     }
                    )
            ]
        };

        var top = new Toplevel ();
        top.Add (menu);
        Application.Begin (top);

        Assert.False (newAction);
        Assert.False (copyAction);

        Assert.True (menu.NewKeyDownEvent (Key.F.WithAlt));
        Assert.True (menu.IsMenuOpen);
        Application.Top.Draw ();
        TestHelpers.AssertDriverContentsAre (expectedMenu.ExpectedSubMenuOpen (0), output);

        Assert.True (Application.Top.Subviews [1].NewKeyDownEvent (Key.N));
        Application.MainLoop.RunIteration ();
        Assert.True (newAction);

        Assert.True (menu.NewKeyDownEvent (Key.E.WithAlt));
        Assert.True (menu.IsMenuOpen);
        Application.Top.Draw ();
        TestHelpers.AssertDriverContentsAre (expectedMenu.ExpectedSubMenuOpen (1), output);

        Assert.True (Application.Top.Subviews [1].NewKeyDownEvent (Key.C));
        Application.MainLoop.RunIteration ();
        Assert.True (copyAction);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Key_Open_And_Close_The_MenuBar ()
    {
        var menu = new MenuBar
        {
            Menus =
            [
                new ("File", new MenuItem [] { new ("New", "", null) })
            ]
        };
        var top = new Toplevel ();
        top.Add (menu);
        Application.Begin (top);

        Assert.True (top.NewKeyDownEvent (menu.Key));
        Assert.True (menu.IsMenuOpen);
        Assert.True (top.NewKeyDownEvent (menu.Key));
        Assert.False (menu.IsMenuOpen);

        menu.Key = Key.F10.WithShift;
        Assert.False (top.NewKeyDownEvent (Key.F9));
        Assert.False (menu.IsMenuOpen);

        Assert.True (top.NewKeyDownEvent (Key.F10.WithShift));
        Assert.True (menu.IsMenuOpen);
        Assert.True (top.NewKeyDownEvent (Key.F10.WithShift));
        Assert.False (menu.IsMenuOpen);
        top.Dispose ();
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData ("_File", "_New", "", KeyCode.Space | KeyCode.CtrlMask)]
    [InlineData ("Closed", "None", "", KeyCode.Space | KeyCode.CtrlMask, KeyCode.Space | KeyCode.CtrlMask)]
    [InlineData ("_File", "_New", "", KeyCode.F9)]
    [InlineData ("Closed", "None", "", KeyCode.F9, KeyCode.F9)]
    [InlineData ("_File", "_Open", "", KeyCode.F9, KeyCode.CursorDown)]
    [InlineData ("_File", "_Save", "", KeyCode.F9, KeyCode.CursorDown, KeyCode.CursorDown)]
    [InlineData ("_File", "_Quit", "", KeyCode.F9, KeyCode.CursorDown, KeyCode.CursorDown, KeyCode.CursorDown)]
    [InlineData (
                    "_File",
                    "_New",
                    "",
                    KeyCode.F9,
                    KeyCode.CursorDown,
                    KeyCode.CursorDown,
                    KeyCode.CursorDown,
                    KeyCode.CursorDown
                )]
    [InlineData ("_File", "_New", "", KeyCode.F9, KeyCode.CursorDown, KeyCode.CursorUp)]
    [InlineData ("_File", "_Quit", "", KeyCode.F9, KeyCode.CursorUp)]
    [InlineData ("_File", "_New", "", KeyCode.F9, KeyCode.CursorUp, KeyCode.CursorDown)]
    [InlineData ("Closed", "None", "Open", KeyCode.F9, KeyCode.CursorDown, KeyCode.Enter)]
    [InlineData ("_Edit", "_Copy", "", KeyCode.F9, KeyCode.CursorRight)]
    [InlineData ("_About", "_About", "", KeyCode.F9, KeyCode.CursorLeft)]
    [InlineData ("_Edit", "_Copy", "", KeyCode.F9, KeyCode.CursorLeft, KeyCode.CursorLeft)]
    [InlineData ("_Edit", "_Select All", "", KeyCode.F9, KeyCode.CursorRight, KeyCode.CursorUp)]
    [InlineData ("_File", "_New", "", KeyCode.F9, KeyCode.CursorRight, KeyCode.CursorDown, KeyCode.CursorLeft)]
    [InlineData ("_About", "_About", "", KeyCode.F9, KeyCode.CursorRight, KeyCode.CursorRight)]
    [InlineData ("Closed", "None", "New", KeyCode.F9, KeyCode.Enter)]
    [InlineData ("Closed", "None", "Quit", KeyCode.F9, KeyCode.CursorUp, KeyCode.Enter)]
    [InlineData ("Closed", "None", "Copy", KeyCode.F9, KeyCode.CursorRight, KeyCode.Enter)]
    [InlineData (
                    "Closed",
                    "None",
                    "Find",
                    KeyCode.F9,
                    KeyCode.CursorRight,
                    KeyCode.CursorUp,
                    KeyCode.CursorUp,
                    KeyCode.Enter
                )]
    [InlineData (
                    "Closed",
                    "None",
                    "Replace",
                    KeyCode.F9,
                    KeyCode.CursorRight,
                    KeyCode.CursorUp,
                    KeyCode.CursorUp,
                    KeyCode.CursorDown,
                    KeyCode.Enter
                )]
    [InlineData (
                    "_Edit",
                    "F_ind",
                    "",
                    KeyCode.F9,
                    KeyCode.CursorRight,
                    KeyCode.CursorUp,
                    KeyCode.CursorUp,
                    KeyCode.CursorLeft,
                    KeyCode.Enter
                )]
    [InlineData ("Closed", "None", "About", KeyCode.F9, KeyCode.CursorRight, KeyCode.CursorRight, KeyCode.Enter)]

    //// Hotkeys
    [InlineData ("_File", "_New", "", KeyCode.AltMask | KeyCode.F)]
    [InlineData ("Closed", "None", "", KeyCode.AltMask | KeyCode.ShiftMask | KeyCode.F)]
    [InlineData ("Closed", "None", "", KeyCode.AltMask | KeyCode.F, KeyCode.Esc)]
    [InlineData ("Closed", "None", "", KeyCode.AltMask | KeyCode.F, KeyCode.AltMask | KeyCode.F)]
    [InlineData ("Closed", "None", "Open", KeyCode.AltMask | KeyCode.F, KeyCode.O)]
    [InlineData ("_File", "_New", "", KeyCode.AltMask | KeyCode.F, KeyCode.ShiftMask | KeyCode.O)]
    [InlineData ("Closed", "None", "Open", KeyCode.AltMask | KeyCode.F, KeyCode.AltMask | KeyCode.O)]
    [InlineData ("_Edit", "_Copy", "", KeyCode.AltMask | KeyCode.E)]
    [InlineData ("_Edit", "F_ind", "", KeyCode.AltMask | KeyCode.E, KeyCode.F)]
    [InlineData ("_Edit", "F_ind", "", KeyCode.AltMask | KeyCode.E, KeyCode.AltMask | KeyCode.F)]
    [InlineData ("Closed", "None", "Replace", KeyCode.AltMask | KeyCode.E, KeyCode.F, KeyCode.R)]
    [InlineData ("Closed", "None", "Copy", KeyCode.AltMask | KeyCode.E, KeyCode.F, KeyCode.C)]
    [InlineData ("_Edit", "_1st", "", KeyCode.AltMask | KeyCode.E, KeyCode.F, KeyCode.D3)]
    [InlineData ("Closed", "None", "1", KeyCode.AltMask | KeyCode.E, KeyCode.F, KeyCode.D3, KeyCode.D1)]
    [InlineData ("Closed", "None", "1", KeyCode.AltMask | KeyCode.E, KeyCode.F, KeyCode.D3, KeyCode.Enter)]
    [InlineData ("Closed", "None", "2", KeyCode.AltMask | KeyCode.E, KeyCode.F, KeyCode.D3, KeyCode.D2)]
    [InlineData ("_Edit", "_5th", "", KeyCode.AltMask | KeyCode.E, KeyCode.F, KeyCode.D3, KeyCode.D4)]
    [InlineData ("Closed", "None", "5", KeyCode.AltMask | KeyCode.E, KeyCode.F, KeyCode.D4, KeyCode.D5)]
    [InlineData ("Closed", "None", "About", KeyCode.AltMask | KeyCode.A)]
    public void KeyBindings_Navigation_Commands (
        string expectedBarTitle,
        string expectedItemTitle,
        string expectedAction,
        params KeyCode [] keys
    )
    {
        var miAction = "";
        MenuItem mbiCurrent = null;
        MenuItem miCurrent = null;

        var menu = new MenuBar ();

        Func<object, bool> fn = s =>
                                {
                                    miAction = s as string;

                                    return true;
                                };
        menu.EnableForDesign (ref fn);

        menu.Key = KeyCode.F9;
        menu.MenuOpening += (s, e) => mbiCurrent = e.CurrentMenu;
        menu.MenuOpened += (s, e) => { miCurrent = e.MenuItem; };

        menu.MenuClosing += (s, e) =>
                            {
                                mbiCurrent = null;
                                miCurrent = null;
                            };
        menu.UseKeysUpDownAsKeysLeftRight = true;
        var top = new Toplevel ();
        top.Add (menu);
        Application.Begin (top);

        foreach (Key key in keys)
        {
            top.NewKeyDownEvent (key);
            Application.MainLoop.RunIteration ();
        }

        Assert.Equal (expectedBarTitle, mbiCurrent != null ? mbiCurrent.Title : "Closed");
        Assert.Equal (expectedItemTitle, miCurrent != null ? miCurrent.Title : "None");
        Assert.Equal (expectedAction, miAction);
        top.Dispose ();
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData ("New", KeyCode.CtrlMask | KeyCode.N)]
    [InlineData ("Quit", KeyCode.CtrlMask | KeyCode.Q)]
    [InlineData ("Copy", KeyCode.CtrlMask | KeyCode.C)]
    [InlineData ("Replace", KeyCode.CtrlMask | KeyCode.H)]
    [InlineData ("1", KeyCode.F1)]
    [InlineData ("5", KeyCode.CtrlMask | KeyCode.D5)]
    public void KeyBindings_Shortcut_Commands (string expectedAction, params KeyCode [] keys)
    {
        var miAction = "";
        MenuItem mbiCurrent = null;
        MenuItem miCurrent = null;

        var menu = new MenuBar ();

        bool FnAction (string s)
        {
            miAction = s;

            return true;
        }

        // Declare a variable for the function
        Func<string, bool> fnActionVariable = FnAction;

        menu.EnableForDesign (ref fnActionVariable);

        menu.Key = KeyCode.F9;
        menu.MenuOpening += (s, e) => mbiCurrent = e.CurrentMenu;
        menu.MenuOpened += (s, e) => { miCurrent = e.MenuItem; };

        menu.MenuClosing += (s, e) =>
                            {
                                mbiCurrent = null;
                                miCurrent = null;
                            };
        menu.UseKeysUpDownAsKeysLeftRight = true;

        var top = new Toplevel ();
        top.Add (menu);
        Application.Begin (top);

        foreach (KeyCode key in keys)
        {
            Assert.True (top.NewKeyDownEvent (new (key)));
            Application.MainLoop!.RunIteration ();
        }

        Assert.Equal (expectedAction, miAction);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Menu_With_Separator ()
    {
        var menu = new MenuBar
        {
            Menus =
            [
                new (
                     "File",
                     new MenuItem []
                     {
                         new (
                              "_Open",
                              "Open a file",
                              () => { },
                              null,
                              null,
                              KeyCode.CtrlMask | KeyCode.O
                             ),
                         null,
                         new ("_Quit", "", null)
                     }
                    )
            ]
        };

        var top = new Toplevel ();
        top.Add (menu);
        Application.Begin (top);

        menu.OpenMenu ();
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 File                         
┌────────────────────────────┐
│ Open   Open a file  Ctrl+O │
├────────────────────────────┤
│ Quit                       │
└────────────────────────────┘",
                                                      output
                                                     );
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Menu_With_Separator_Disabled_Border ()
    {
        var menu = new MenuBar
        {
            MenusBorderStyle = LineStyle.None,
            Menus =
            [
                new (
                     "File",
                     new MenuItem []
                     {
                         new (
                              "_Open",
                              "Open a file",
                              () => { },
                              null,
                              null,
                              KeyCode.CtrlMask | KeyCode.O
                             ),
                         null,
                         new ("_Quit", "", null)
                     }
                    )
            ]
        };

        var top = new Toplevel ();
        top.Add (menu);
        Application.Begin (top);

        menu.OpenMenu ();
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 File                       
 Open   Open a file  Ctrl+O 
────────────────────────────
 Quit                       ",
                                                      output
                                                     );
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void MenuBar_ButtonPressed_Open_The_Menu_ButtonPressed_Again_Close_The_Menu ()
    {
        // Define the expected menu
        var expectedMenu = new ExpectedMenuBar
        {
            Menus =
            [
                new ("File", new MenuItem [] { new ("Open", "", null) }),
                new (
                     "Edit",
                     new MenuItem [] { new ("Copy", "", null) }
                    )
            ]
        };

        // Test without HotKeys first
        var menu = new MenuBar
        {
            Menus =
            [
                new (
                     "_" + expectedMenu.Menus [0].Title,
                     new MenuItem [] { new ("_" + expectedMenu.Menus [0].Children [0].Title, "", null) }
                    ),
                new (
                     "_" + expectedMenu.Menus [1].Title,
                     new MenuItem []
                     {
                         new (
                              "_"
                              + expectedMenu.Menus [1]
                                            .Children [0]
                                            .Title,
                              "",
                              null
                             )
                     }
                    )
            ]
        };

        var top = new Toplevel ();
        top.Add (menu);
        Application.Begin (top);

        Assert.True (menu.NewMouseEvent (new () { Position = new (1, 0), Flags = MouseFlags.Button1Pressed, View = menu }));
        Assert.True (menu.IsMenuOpen);
        top.Draw ();

        TestHelpers.AssertDriverContentsAre (expectedMenu.ExpectedSubMenuOpen (0), output);

        Assert.True (menu.NewMouseEvent (new () { Position = new (1, 0), Flags = MouseFlags.Button1Pressed, View = menu }));
        Assert.False (menu.IsMenuOpen);
        top.Draw ();
        TestHelpers.AssertDriverContentsAre (expectedMenu.ClosedMenuText, output);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void MenuBar_In_Window_Without_Other_Views_With_Top_Init ()
    {
        var win = new Window ();

        var menu = new MenuBar
        {
            Menus =
            [
                new ("File", new MenuItem [] { new ("New", "", null) }),
                new (
                     "Edit",
                     new MenuItem []
                     {
                         new MenuBarItem (
                                          "Delete",
                                          new MenuItem []
                                              { new ("All", "", null), new ("Selected", "", null) }
                                         )
                     }
                    )
            ]
        };
        win.Add (menu);
        Toplevel top = new ();
        top.Add (win);
        Application.Begin (top);
        ((FakeDriver)Application.Driver!).SetBufferSize (40, 8);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────────────────────────┐
│ File  Edit                           │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
└──────────────────────────────────────┘",
                                                      output
                                                     );

        Assert.True (win.NewKeyDownEvent (menu.Key));
        top.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────────────────────────┐
│ File  Edit                           │
│┌──────┐                              │
││ New  │                              │
│└──────┘                              │
│                                      │
│                                      │
└──────────────────────────────────────┘",
                                                      output
                                                     );

        Assert.True (menu.NewKeyDownEvent (Key.CursorRight));
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────────────────────────┐
│ File  Edit                           │
│      ┌─────────┐                     │
│      │ Delete ►│                     │
│      └─────────┘                     │
│                                      │
│                                      │
└──────────────────────────────────────┘",
                                                      output
                                                     );

        Assert.True (menu._openMenu.NewKeyDownEvent (Key.CursorRight));
        top.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────────────────────────┐
│ File  Edit                           │
│      ┌─────────┐                     │
│      │ Delete ►│┌───────────┐        │
│      └─────────┘│ All       │        │
│                 │ Selected  │        │
│                 └───────────┘        │
└──────────────────────────────────────┘",
                                                      output
                                                     );

        Assert.True (menu._openMenu.NewKeyDownEvent (Key.CursorRight));
        top.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────────────────────────┐
│ File  Edit                           │
│┌──────┐                              │
││ New  │                              │
│└──────┘                              │
│                                      │
│                                      │
└──────────────────────────────────────┘",
                                                      output
                                                     );
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void MenuBar_In_Window_Without_Other_Views_With_Top_Init_With_Parameterless_Run ()
    {
        var win = new Window ();

        var menu = new MenuBar
        {
            Menus =
            [
                new ("File", new MenuItem [] { new ("New", "", null) }),
                new (
                     "Edit",
                     new MenuItem []
                     {
                         new MenuBarItem (
                                          "Delete",
                                          new MenuItem []
                                              { new ("All", "", null), new ("Selected", "", null) }
                                         )
                     }
                    )
            ]
        };
        win.Add (menu);
        Toplevel top = new ();
        top.Add (win);

        Application.Iteration += (s, a) =>
                                 {
                                     ((FakeDriver)Application.Driver!).SetBufferSize (40, 8);

                                     TestHelpers.AssertDriverContentsWithFrameAre (
                                                                                   @"
┌──────────────────────────────────────┐
│ File  Edit                           │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
└──────────────────────────────────────┘",
                                                                                   output
                                                                                  );

                                     Assert.True (win.NewKeyDownEvent (menu.Key));
                                     top.Draw ();

                                     TestHelpers.AssertDriverContentsWithFrameAre (
                                                                                   @"
┌──────────────────────────────────────┐
│ File  Edit                           │
│┌──────┐                              │
││ New  │                              │
│└──────┘                              │
│                                      │
│                                      │
└──────────────────────────────────────┘",
                                                                                   output
                                                                                  );

                                     Assert.True (menu.NewKeyDownEvent (Key.CursorRight));
                                     Application.Refresh ();

                                     TestHelpers.AssertDriverContentsWithFrameAre (
                                                                                   @"
┌──────────────────────────────────────┐
│ File  Edit                           │
│      ┌─────────┐                     │
│      │ Delete ►│                     │
│      └─────────┘                     │
│                                      │
│                                      │
└──────────────────────────────────────┘",
                                                                                   output
                                                                                  );

                                     Assert.True (menu._openMenu.NewKeyDownEvent (Key.CursorRight));
                                     top.Draw ();

                                     TestHelpers.AssertDriverContentsWithFrameAre (
                                                                                   @"
┌──────────────────────────────────────┐
│ File  Edit                           │
│      ┌─────────┐                     │
│      │ Delete ►│┌───────────┐        │
│      └─────────┘│ All       │        │
│                 │ Selected  │        │
│                 └───────────┘        │
└──────────────────────────────────────┘",
                                                                                   output
                                                                                  );

                                     Assert.True (menu._openMenu.NewKeyDownEvent (Key.CursorRight));
                                     top.Draw ();

                                     TestHelpers.AssertDriverContentsWithFrameAre (
                                                                                   @"
┌──────────────────────────────────────┐
│ File  Edit                           │
│┌──────┐                              │
││ New  │                              │
│└──────┘                              │
│                                      │
│                                      │
└──────────────────────────────────────┘",
                                                                                   output
                                                                                  );

                                     Application.RequestStop ();
                                 };

        Application.Run (top);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void MenuBar_In_Window_Without_Other_Views_Without_Top_Init ()
    {
        var win = new Window ();

        var menu = new MenuBar
        {
            Menus =
            [
                new ("File", new MenuItem [] { new ("New", "", null) }),
                new (
                     "Edit",
                     new MenuItem []
                     {
                         new MenuBarItem (
                                          "Delete",
                                          new MenuItem []
                                              { new ("All", "", null), new ("Selected", "", null) }
                                         )
                     }
                    )
            ]
        };
        win.Add (menu);
        ((FakeDriver)Application.Driver!).SetBufferSize (40, 8);
        Application.Begin (win);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────────────────────────┐
│ File  Edit                           │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
└──────────────────────────────────────┘",
                                                      output
                                                     );

        Assert.True (win.NewKeyDownEvent (menu.Key));
        win.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────────────────────────┐
│ File  Edit                           │
│┌──────┐                              │
││ New  │                              │
│└──────┘                              │
│                                      │
│                                      │
└──────────────────────────────────────┘",
                                                      output
                                                     );

        Assert.True (menu.NewKeyDownEvent (Key.CursorRight));
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────────────────────────┐
│ File  Edit                           │
│      ┌─────────┐                     │
│      │ Delete ►│                     │
│      └─────────┘                     │
│                                      │
│                                      │
└──────────────────────────────────────┘",
                                                      output
                                                     );

        Assert.True (menu._openMenu.NewKeyDownEvent (Key.CursorRight));
        win.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────────────────────────┐
│ File  Edit                           │
│      ┌─────────┐                     │
│      │ Delete ►│┌───────────┐        │
│      └─────────┘│ All       │        │
│                 │ Selected  │        │
│                 └───────────┘        │
└──────────────────────────────────────┘",
                                                      output
                                                     );

        Assert.True (menu._openMenu.NewKeyDownEvent (Key.CursorRight));
        win.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌──────────────────────────────────────┐
│ File  Edit                           │
│┌──────┐                              │
││ New  │                              │
│└──────┘                              │
│                                      │
│                                      │
└──────────────────────────────────────┘",
                                                      output
                                                     );
        win.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void MenuBar_In_Window_Without_Other_Views_Without_Top_Init_With_Run_T ()
    {
        ((FakeDriver)Application.Driver!).SetBufferSize (40, 8);

        Application.Iteration += (s, a) =>
                                 {
                                     Toplevel top = Application.Top;

                                     TestHelpers.AssertDriverContentsWithFrameAre (
                                                                                   @"
┌──────────────────────────────────────┐
│ File  Edit                           │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
└──────────────────────────────────────┘",
                                                                                   output
                                                                                  );

                                     Assert.True (top.NewKeyDownEvent (Key.F9));
                                     top.Draw ();

                                     TestHelpers.AssertDriverContentsWithFrameAre (
                                                                                   @"
┌──────────────────────────────────────┐
│ File  Edit                           │
│┌──────┐                              │
││ New  │                              │
│└──────┘                              │
│                                      │
│                                      │
└──────────────────────────────────────┘",
                                                                                   output
                                                                                  );

                                     Assert.True (top.Subviews [0].NewKeyDownEvent (Key.CursorRight));
                                     Application.Refresh ();

                                     TestHelpers.AssertDriverContentsWithFrameAre (
                                                                                   @"
┌──────────────────────────────────────┐
│ File  Edit                           │
│      ┌─────────┐                     │
│      │ Delete ►│                     │
│      └─────────┘                     │
│                                      │
│                                      │
└──────────────────────────────────────┘",
                                                                                   output
                                                                                  );

                                     Assert.True (
                                                  ((MenuBar)top.Subviews [0])._openMenu.NewKeyDownEvent (Key.CursorRight)
                                                 );
                                     top.Draw ();

                                     TestHelpers.AssertDriverContentsWithFrameAre (
                                                                                   @"
┌──────────────────────────────────────┐
│ File  Edit                           │
│      ┌─────────┐                     │
│      │ Delete ►│┌───────────┐        │
│      └─────────┘│ All       │        │
│                 │ Selected  │        │
│                 └───────────┘        │
└──────────────────────────────────────┘",
                                                                                   output
                                                                                  );

                                     Assert.True (
                                                  ((MenuBar)top.Subviews [0])._openMenu.NewKeyDownEvent (Key.CursorRight)
                                                 );
                                     top.Draw ();

                                     TestHelpers.AssertDriverContentsWithFrameAre (
                                                                                   @"
┌──────────────────────────────────────┐
│ File  Edit                           │
│┌──────┐                              │
││ New  │                              │
│└──────┘                              │
│                                      │
│                                      │
└──────────────────────────────────────┘",
                                                                                   output
                                                                                  );

                                     Application.RequestStop ();
                                 };

        Application.Run<CustomWindow> ().Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void MenuBar_Position_And_Size_With_HotKeys_Is_The_Same_As_Without_HotKeys ()
    {
        // Define the expected menu
        var expectedMenu = new ExpectedMenuBar
        {
            Menus =
            [
                new ("File", new MenuItem [] { new ("12", "", null) }),
                new (
                     "Edit",
                     new MenuItem [] { new ("Copy", "", null) }
                    )
            ]
        };

        // Test without HotKeys first
        var menu = new MenuBar
        {
            Menus =
            [
                new (
                     expectedMenu.Menus [0].Title,
                     new MenuItem [] { new (expectedMenu.Menus [0].Children [0].Title, "", null) }
                    ),
                new (
                     expectedMenu.Menus [1].Title,
                     new MenuItem []
                     {
                         new (
                              expectedMenu.Menus [1].Children [0].Title,
                              "",
                              null
                             )
                     }
                    )
            ]
        };

        var top = new Toplevel ();
        top.Add (menu);
        Application.Begin (top);

        // Open first
        Assert.True (menu.NewKeyDownEvent (menu.Key));
        Assert.True (menu.IsMenuOpen);
        top.Draw ();
        TestHelpers.AssertDriverContentsAre (expectedMenu.ExpectedSubMenuOpen (0), output);

        // Open second
        Assert.True (Application.Top.Subviews [1].NewKeyDownEvent (Key.CursorRight));
        Assert.True (menu.IsMenuOpen);
        top.Draw ();
        TestHelpers.AssertDriverContentsAre (expectedMenu.ExpectedSubMenuOpen (1), output);

        // Close menu
        Assert.True (menu.NewKeyDownEvent (menu.Key));
        Assert.False (menu.IsMenuOpen);
        top.Draw ();
        TestHelpers.AssertDriverContentsAre (expectedMenu.ClosedMenuText, output);

        top.Remove (menu);

        // Now test WITH HotKeys
        menu = new ()
        {
            Menus =
            [
                new (
                     "_" + expectedMenu.Menus [0].Title,
                     new MenuItem [] { new ("_" + expectedMenu.Menus [0].Children [0].Title, "", null) }
                    ),
                new (
                     "_" + expectedMenu.Menus [1].Title,
                     new MenuItem []
                     {
                         new (
                              "_" + expectedMenu.Menus [1].Children [0].Title,
                              "",
                              null
                             )
                     }
                    )
            ]
        };

        top.Add (menu);

        // Open first
        Assert.True (menu.NewKeyDownEvent (menu.Key));
        Assert.True (menu.IsMenuOpen);
        top.Draw ();
        TestHelpers.AssertDriverContentsAre (expectedMenu.ExpectedSubMenuOpen (0), output);

        // Open second
        Assert.True (top.Subviews [1].NewKeyDownEvent (Key.CursorRight));
        Assert.True (menu.IsMenuOpen);
        Application.Top.Draw ();
        TestHelpers.AssertDriverContentsAre (expectedMenu.ExpectedSubMenuOpen (1), output);

        // Close menu
        Assert.True (menu.NewKeyDownEvent (menu.Key));
        Assert.False (menu.IsMenuOpen);
        top.Draw ();
        TestHelpers.AssertDriverContentsAre (expectedMenu.ClosedMenuText, output);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void MenuBar_Submenus_Alignment_Correct ()
    {
        // Define the expected menu
        var expectedMenu = new ExpectedMenuBar
        {
            Menus =
            [
                new (
                     "File",
                     new MenuItem []
                     {
                         new (
                              "Really Long Sub Menu",
                              "",
                              null
                             )
                     }
                    ),
                new (
                     "123",
                     new MenuItem [] { new ("Copy", "", null) }
                    ),
                new (
                     "Format",
                     new MenuItem [] { new ("Word Wrap", "", null) }
                    ),
                new (
                     "Help",
                     new MenuItem [] { new ("About", "", null) }
                    ),
                new (
                     "1",
                     new MenuItem [] { new ("2", "", null) }
                    ),
                new (
                     "3",
                     new MenuItem [] { new ("2", "", null) }
                    ),
                new (
                     "Last one",
                     new MenuItem [] { new ("Test", "", null) }
                    )
            ]
        };

        MenuBarItem [] items = new MenuBarItem [expectedMenu.Menus.Length];

        for (var i = 0; i < expectedMenu.Menus.Length; i++)
        {
            items [i] = new (
                             expectedMenu.Menus [i].Title,
                             new MenuItem [] { new (expectedMenu.Menus [i].Children [0].Title, "", null) }
                            );
        }

        var menu = new MenuBar { Menus = items };

        var top = new Toplevel ();
        top.Add (menu);
        Application.Begin (top);

        top.Draw ();
        TestHelpers.AssertDriverContentsAre (expectedMenu.ClosedMenuText, output);

        for (var i = 0; i < expectedMenu.Menus.Length; i++)
        {
            menu.OpenMenu (i);
            Assert.True (menu.IsMenuOpen);
            top.Draw ();
            TestHelpers.AssertDriverContentsAre (expectedMenu.ExpectedSubMenuOpen (i), output);
        }

        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void MenuBar_With_Action_But_Without_MenuItems_Not_Throw ()
    {
        var menu = new MenuBar
        {
            Menus =
            [
                new () { Title = "Test 1", Action = () => { } },

                new () { Title = "Test 2", Action = () => { } }
            ]
        };

        var top = new Toplevel ();
        top.Add (menu);
        Application.Begin (top);

#if SUPPORT_ALT_TO_ACTIVATE_MENU
        Assert.True (
                     Application.OnKeyUp (
                                          new KeyEventArgs (
                                                            Key.AltMask
                                                           )
                                         )
                    ); // changed to true because Alt activates menu bar
#endif
        Assert.True (menu.NewKeyDownEvent (Key.CursorRight));
        Assert.True (menu.NewKeyDownEvent (Key.CursorRight));
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void MenuBarItem_Children_Null_Does_Not_Throw ()
    {
        var menu = new MenuBar
        {
            Menus =
            [
                new ("Test", "", null)
            ]
        };
        var top = new Toplevel ();
        top.Add (menu);

        Exception exception = Record.Exception (() => menu.NewKeyDownEvent (Key.Space));
        Assert.Null (exception);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void MenuOpened_On_Disabled_MenuItem ()
    {
        MenuItem parent = null;
        MenuItem miCurrent = null;
        Menu mCurrent = null;

        var menu = new MenuBar
        {
            Menus =
            [
                new (
                     "_File",
                     new MenuItem []
                     {
                         new MenuBarItem (
                                          "_New",
                                          new MenuItem []
                                          {
                                              new (
                                                   "_New doc",
                                                   "Creates new doc.",
                                                   null,
                                                   () => false
                                                  )
                                          }
                                         ),
                         null,
                         new ("_Save", "Saves the file.", null)
                     }
                    )
            ]
        };

        menu.MenuOpened += (s, e) =>
                           {
                               parent = e.Parent;
                               miCurrent = e.MenuItem;
                               mCurrent = menu._openMenu;
                           };
        menu.UseKeysUpDownAsKeysLeftRight = true;
        var top = new Toplevel ();
        top.Add (menu);
        Application.Begin (top);

        // open the menu
        Assert.True (
                     menu.NewMouseEvent (
                                         new () { Position = new (1, 0), Flags = MouseFlags.Button1Pressed, View = menu }
                                        )
                    );
        Assert.True (menu.IsMenuOpen);
        Assert.Equal ("_File", parent.Title);
        Assert.Equal ("_File", miCurrent.Parent.Title);
        Assert.Equal ("_New", miCurrent.Title);

        Assert.True (
                     mCurrent.NewMouseEvent (
                                             new () { Position = new (1, 1), Flags = MouseFlags.ReportMousePosition, View = mCurrent }
                                            )
                    );
        Assert.True (menu.IsMenuOpen);
        Assert.Equal ("_File", parent.Title);
        Assert.Equal ("_File", miCurrent.Parent.Title);
        Assert.Equal ("_New", miCurrent.Title);

        Assert.True (
                     mCurrent.NewMouseEvent (
                                             new () { Position = new (1, 1), Flags = MouseFlags.ReportMousePosition, View = mCurrent }
                                            )
                    );
        Assert.True (menu.IsMenuOpen);
        Assert.Equal ("_File", parent.Title);
        Assert.Equal ("_File", miCurrent.Parent.Title);
        Assert.Equal ("_New", miCurrent.Title);

        Assert.True (
                     mCurrent.NewMouseEvent (
                                             new () { Position = new (1, 2), Flags = MouseFlags.ReportMousePosition, View = mCurrent }
                                            )
                    );
        Assert.True (menu.IsMenuOpen);
        Assert.Equal ("_File", parent.Title);
        Assert.Equal ("_File", miCurrent.Parent.Title);
        Assert.Equal ("_Save", miCurrent.Title);

        // close the menu
        Assert.True (
                     menu.NewMouseEvent (
                                         new () { Position = new (1, 0), Flags = MouseFlags.Button1Pressed, View = menu }
                                        )
                    );
        Assert.False (menu.IsMenuOpen);

        // open the menu
        Assert.True (menu.NewKeyDownEvent (menu.Key));
        Assert.True (menu.IsMenuOpen);

        // The _New doc is enabled but the sub-menu isn't enabled. Is show but can't be selected and executed
        Assert.Equal ("_New", parent.Title);
        Assert.Equal ("_New", miCurrent.Parent.Title);
        Assert.Equal ("_New doc", miCurrent.Title);

        Assert.True (mCurrent.NewKeyDownEvent (Key.CursorDown));
        Assert.True (menu.IsMenuOpen);
        Assert.Equal ("_File", parent.Title);
        Assert.Equal ("_File", miCurrent.Parent.Title);
        Assert.Equal ("_Save", miCurrent.Title);

        Assert.True (mCurrent.NewKeyDownEvent (Key.CursorUp));
        Assert.True (menu.IsMenuOpen);
        Assert.Equal ("_File", parent.Title);
        Assert.Null (miCurrent);

        // close the menu
        Assert.True (menu.NewKeyDownEvent (menu.Key));
        Assert.False (menu.IsMenuOpen);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void MenuOpening_MenuOpened_MenuClosing_Events ()
    {
        var miAction = "";
        var isMenuClosed = true;
        var cancelClosing = false;

        var menu = new MenuBar
        {
            Menus =
            [
                new ("_File", new MenuItem [] { new ("_New", "Creates new file.", New) })
            ]
        };

        menu.MenuOpening += (s, e) =>
                            {
                                Assert.Equal ("_File", e.CurrentMenu.Title);
                                Assert.Equal ("_New", e.CurrentMenu.Children [0].Title);
                                Assert.Equal ("Creates new file.", e.CurrentMenu.Children [0].Help);
                                Assert.Equal (New, e.CurrentMenu.Children [0].Action);
                                e.CurrentMenu.Children [0].Action ();
                                Assert.Equal ("New", miAction);

                                e.NewMenuBarItem = new (
                                                        "_Edit",
                                                        new MenuItem [] { new ("_Copy", "Copies the selection.", Copy) }
                                                       );
                            };

        menu.MenuOpened += (s, e) =>
                           {
                               MenuItem mi = e.MenuItem;

                               Assert.Equal ("_Edit", mi.Parent.Title);
                               Assert.Equal ("_Copy", mi.Title);
                               Assert.Equal ("Copies the selection.", mi.Help);
                               Assert.Equal (Copy, mi.Action);
                               mi.Action ();
                               Assert.Equal ("Copy", miAction);
                           };

        menu.MenuClosing += (s, e) =>
                            {
                                Assert.False (isMenuClosed);

                                if (cancelClosing)
                                {
                                    e.Cancel = true;
                                    isMenuClosed = false;
                                }
                                else
                                {
                                    isMenuClosed = true;
                                }
                            };
        var top = new Toplevel ();
        top.Add (menu);
        Application.Begin (top);

        Assert.True (menu.NewKeyDownEvent (menu.Key));
        Assert.True (menu.IsMenuOpen);
        isMenuClosed = !menu.IsMenuOpen;
        Assert.False (isMenuClosed);
        top.Draw ();

        var expected = @"
Edit
┌──────────────────────────────┐
│ Copy   Copies the selection. │
└──────────────────────────────┘
";
        TestHelpers.AssertDriverContentsAre (expected, output);

        cancelClosing = true;
        Assert.True (menu.NewKeyDownEvent (menu.Key));
        Assert.True (menu.IsMenuOpen);
        Assert.False (isMenuClosed);
        top.Draw ();

        expected = @"
Edit
┌──────────────────────────────┐
│ Copy   Copies the selection. │
└──────────────────────────────┘
";
        TestHelpers.AssertDriverContentsAre (expected, output);

        cancelClosing = false;
        Assert.True (menu.NewKeyDownEvent (menu.Key));
        Assert.False (menu.IsMenuOpen);
        Assert.True (isMenuClosed);
        top.Draw ();

        expected = @"
Edit
";
        TestHelpers.AssertDriverContentsAre (expected, output);

        void New () { miAction = "New"; }

        void Copy () { miAction = "Copy"; }

        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void MouseEvent_Test ()
    {
        MenuItem miCurrent = null;
        Menu mCurrent = null;

        var menu = new MenuBar
        {
            Menus =
            [
                new (
                     "_File",
                     new MenuItem [] { new ("_New", "", null), new ("_Open", "", null), new ("_Save", "", null) }
                    ),
                new (
                     "_Edit",
                     new MenuItem [] { new ("_Copy", "", null), new ("C_ut", "", null), new ("_Paste", "", null) }
                    )
            ]
        };

        menu.MenuOpened += (s, e) =>
                           {
                               miCurrent = e.MenuItem;
                               mCurrent = menu.OpenCurrentMenu;
                           };
        var top = new Toplevel ();
        top.Add (menu);
        Application.Begin (top);

        // Click on Edit
        Assert.True (
                     menu.NewMouseEvent (
                                         new () { Position = new (10, 0), Flags = MouseFlags.Button1Pressed, View = menu }
                                        )
                    );
        Assert.True (menu.IsMenuOpen);
        Assert.Equal ("_Edit", miCurrent.Parent.Title);
        Assert.Equal ("_Copy", miCurrent.Title);

        // Click on Paste
        Assert.True (
                     mCurrent.NewMouseEvent (
                                             new () { Position = new (10, 2), Flags = MouseFlags.ReportMousePosition, View = mCurrent }
                                            )
                    );
        Assert.True (menu.IsMenuOpen);
        Assert.Equal ("_Edit", miCurrent.Parent.Title);
        Assert.Equal ("_Paste", miCurrent.Title);

        for (var i = 2; i >= -1; i--)
        {
            if (i == -1)
            {
                // Edit menu is open. Click on the menu at Y = -1, which is outside the menu.
                Assert.False (
                              mCurrent.NewMouseEvent (
                                                      new () { Position = new (10, i), Flags = MouseFlags.ReportMousePosition, View = menu }
                                                     )
                             );
            }
            else
            {
                // Edit menu is open. Click on the menu at Y = i.
                Assert.True (
                             mCurrent.NewMouseEvent (
                                                     new () { Position = new (10, i), Flags = MouseFlags.ReportMousePosition, View = mCurrent }
                                                    )
                            );
            }

            Assert.True (menu.IsMenuOpen);

            if (i == 2)
            {
                Assert.Equal ("_Edit", miCurrent.Parent.Title);
                Assert.Equal ("_Paste", miCurrent.Title);
            }
            else if (i == 1)
            {
                Assert.Equal ("_Edit", miCurrent.Parent.Title);
                Assert.Equal ("C_ut", miCurrent.Title);
            }
            else if (i == 0)
            {
                Assert.Equal ("_Edit", miCurrent.Parent.Title);
                Assert.Equal ("_Copy", miCurrent.Title);
            }
            else
            {
                Assert.Equal ("_Edit", miCurrent.Parent.Title);
                Assert.Equal ("_Copy", miCurrent.Title);
            }
        }

        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Parent_MenuItem_Stay_Focused_If_Child_MenuItem_Is_Empty_By_Keyboard ()
    {
        var expectedMenu = new ExpectedMenuBar
        {
            Menus =
            [
                new ("File", new MenuItem [] { new ("New", "", null) }),
                new ("Edit", Array.Empty<MenuItem> ()),
                new (
                     "Format",
                     new MenuItem [] { new ("Wrap", "", null) }
                    )
            ]
        };

        MenuBarItem [] items = new MenuBarItem [expectedMenu.Menus.Length];

        for (var i = 0; i < expectedMenu.Menus.Length; i++)
        {
            items [i] = new (
                             expectedMenu.Menus [i].Title,
                             expectedMenu.Menus [i].Children.Length > 0
                                 ? new MenuItem [] { new (expectedMenu.Menus [i].Children [0].Title, "", null) }
                                 : Array.Empty<MenuItem> ()
                            );
        }

        var menu = new MenuBar { Menus = items };

        var tf = new TextField { Y = 2, Width = 10 };
        var top = new Toplevel ();
        top.Add (menu, tf);

        Application.Begin (top);
        Assert.True (tf.HasFocus);
        Assert.True (menu.NewKeyDownEvent (menu.Key));
        Assert.True (menu.IsMenuOpen);
        Assert.False (tf.HasFocus);
        top.Draw ();
        TestHelpers.AssertDriverContentsAre (expectedMenu.ExpectedSubMenuOpen (0), output);

        // Right - Edit has no sub menu; this tests that no sub menu shows
        Assert.True (menu._openMenu.NewKeyDownEvent (Key.CursorRight));
        Assert.True (menu.IsMenuOpen);
        Assert.False (tf.HasFocus);
        Assert.Equal (1, menu._selected);
        Assert.Equal (-1, menu._selectedSub);
        Assert.Null (menu._openSubMenu);
        top.Draw ();
        TestHelpers.AssertDriverContentsAre (expectedMenu.ExpectedSubMenuOpen (1), output);

        // Right - Format
        Assert.True (menu._openMenu.NewKeyDownEvent (Key.CursorRight));
        Assert.True (menu.IsMenuOpen);
        Assert.False (tf.HasFocus);
        top.Draw ();
        TestHelpers.AssertDriverContentsAre (expectedMenu.ExpectedSubMenuOpen (2), output);

        // Left - Edit
        Assert.True (menu._openMenu.NewKeyDownEvent (Key.CursorLeft));
        Assert.True (menu.IsMenuOpen);
        Assert.False (tf.HasFocus);
        top.Draw ();
        TestHelpers.AssertDriverContentsAre (expectedMenu.ExpectedSubMenuOpen (1), output);

        Assert.True (menu._openMenu.NewKeyDownEvent (Key.CursorLeft));
        Assert.True (menu.IsMenuOpen);
        Assert.False (tf.HasFocus);
        top.Draw ();
        TestHelpers.AssertDriverContentsAre (expectedMenu.ExpectedSubMenuOpen (0), output);

        Assert.True (Application.RaiseKeyDownEvent (menu.Key));
        Assert.False (menu.IsMenuOpen);
        Assert.True (tf.HasFocus);
        top.Draw ();
        TestHelpers.AssertDriverContentsAre (expectedMenu.ClosedMenuText, output);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Parent_MenuItem_Stay_Focused_If_Child_MenuItem_Is_Empty_By_Mouse ()
    {
        // File  Edit  Format
        //┌──────┐    ┌───────┐         
        //│ New  │    │ Wrap  │         
        //└──────┘    └───────┘         

        // Define the expected menu
        var expectedMenu = new ExpectedMenuBar
        {
            Menus =
            [
                new ("File", new MenuItem [] { new ("New", "", null) }),
                new ("Edit", new MenuItem [] { }),
                new (
                     "Format",
                     new MenuItem [] { new ("Wrap", "", null) }
                    )
            ]
        };

        var menu = new MenuBar
        {
            Menus =
            [
                new (
                     expectedMenu.Menus [0].Title,
                     new MenuItem [] { new (expectedMenu.Menus [0].Children [0].Title, "", null) }
                    ),
                new (expectedMenu.Menus [1].Title, new MenuItem [] { }),
                new (
                     expectedMenu.Menus [2].Title,
                     new MenuItem []
                     {
                         new (
                              expectedMenu.Menus [2].Children [0].Title,
                              "",
                              null
                             )
                     }
                    )
            ]
        };

        var tf = new TextField { Y = 2, Width = 10 };
        var top = new Toplevel ();
        top.Add (menu, tf);
        Application.Begin (top);

        Assert.True (tf.HasFocus);
        Assert.True (menu.NewMouseEvent (new () { Position = new (1, 0), Flags = MouseFlags.Button1Pressed, View = menu }));
        Assert.True (menu.IsMenuOpen);
        Assert.False (tf.HasFocus);
        top.Draw ();
        TestHelpers.AssertDriverContentsAre (expectedMenu.ExpectedSubMenuOpen (0), output);

        Assert.True (
                     menu.NewMouseEvent (
                                         new () { Position = new (8, 0), Flags = MouseFlags.ReportMousePosition, View = menu }
                                        )
                    );
        Assert.True (menu.IsMenuOpen);
        Assert.False (tf.HasFocus);
        top.Draw ();
        TestHelpers.AssertDriverContentsAre (expectedMenu.ExpectedSubMenuOpen (1), output);

        Assert.True (
                     menu.NewMouseEvent (
                                         new () { Position = new (15, 0), Flags = MouseFlags.ReportMousePosition, View = menu }
                                        )
                    );
        Assert.True (menu.IsMenuOpen);
        Assert.False (tf.HasFocus);
        top.Draw ();
        TestHelpers.AssertDriverContentsAre (expectedMenu.ExpectedSubMenuOpen (2), output);

        Assert.True (
                     menu.NewMouseEvent (
                                         new () { Position = new (8, 0), Flags = MouseFlags.ReportMousePosition, View = menu }
                                        )
                    );
        Assert.True (menu.IsMenuOpen);
        Assert.False (tf.HasFocus);
        top.Draw ();
        TestHelpers.AssertDriverContentsAre (expectedMenu.ClosedMenuText, output);

        Assert.True (
                     menu.NewMouseEvent (
                                         new () { Position = new (1, 0), Flags = MouseFlags.ReportMousePosition, View = menu }
                                        )
                    );
        Assert.True (menu.IsMenuOpen);
        Assert.False (tf.HasFocus);
        top.Draw ();
        TestHelpers.AssertDriverContentsAre (expectedMenu.ExpectedSubMenuOpen (0), output);

        Assert.True (menu.NewMouseEvent (new () { Position = new (8, 0), Flags = MouseFlags.Button1Pressed, View = menu }));
        Assert.False (menu.IsMenuOpen);
        Assert.True (tf.HasFocus);
        top.Draw ();
        TestHelpers.AssertDriverContentsAre (expectedMenu.ClosedMenuText, output);
        top.Dispose ();
    }

    [Fact]
    public void RemoveAndThenAddMenuBar_ShouldNotChangeWidth ()
    {
        MenuBar menuBar;
        MenuBar menuBar2;

        // TODO: When https: //github.com/gui-cs/Terminal.Gui/issues/3136 is fixed, 
        // TODO: Change this to Window
        var w = new View ();
        menuBar2 = new ();
        menuBar = new ();
        w.Width = Dim.Fill ();
        w.Height = Dim.Fill ();
        w.X = 0;
        w.Y = 0;

        w.Visible = true;

        // TODO: When https: //github.com/gui-cs/Terminal.Gui/issues/3136 is fixed, 
        // TODO: uncomment this.
        //w.Modal = false;
        w.Title = "";
        menuBar.Width = Dim.Fill ();
        menuBar.Height = 1;
        menuBar.X = 0;
        menuBar.Y = 0;
        menuBar.Visible = true;
        w.Add (menuBar);

        menuBar2.Width = Dim.Fill ();
        menuBar2.Height = 1;
        menuBar2.X = 0;
        menuBar2.Y = 4;
        menuBar2.Visible = true;
        w.Add (menuBar2);

        MenuBar [] menuBars = w.Subviews.OfType<MenuBar> ().ToArray ();
        Assert.Equal (2, menuBars.Length);

        Assert.Equal (Dim.Fill (), menuBars [0].Width);
        Assert.Equal (Dim.Fill (), menuBars [1].Width);

        // Goes wrong here
        w.Remove (menuBar);
        w.Remove (menuBar2);

        w.Add (menuBar);
        w.Add (menuBar2);

        // These assertions fail
        Assert.Equal (Dim.Fill (), menuBars [0].Width);
        Assert.Equal (Dim.Fill (), menuBars [1].Width);
    }

    [Fact]
    [AutoInitShutdown]
    public void Resizing_Close_Menus ()
    {
        var menu = new MenuBar
        {
            Menus =
            [
                new (
                     "File",
                     new MenuItem []
                     {
                         new (
                              "Open",
                              "Open a file",
                              () => { },
                              null,
                              null,
                              KeyCode.CtrlMask | KeyCode.O
                             )
                     }
                    )
            ]
        };
        var top = new Toplevel ();
        top.Add (menu);
        RunState rs = Application.Begin (top);

        menu.OpenMenu ();
        var firstIteration = false;
        Application.RunIteration (ref rs, ref firstIteration);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 File                         
┌────────────────────────────┐
│ Open   Open a file  Ctrl+O │
└────────────────────────────┘",
                                                      output
                                                     );

        ((FakeDriver)Application.Driver!).SetBufferSize (20, 15);
        firstIteration = false;
        Application.RunIteration (ref rs, ref firstIteration);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 File",
                                                      output
                                                     );

        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    public void Separator_Does_Not_Throws_Pressing_Menu_Hotkey ()
    {
        var menu = new MenuBar
        {
            Menus =
            [
                new (
                     "File",
                     new MenuItem [] { new ("_New", "", null), null, new ("_Quit", "", null) }
                    )
            ]
        };
        Assert.False (menu.NewKeyDownEvent (Key.Q.WithAlt));
    }

    [Fact]
    public void SetMenus_With_Same_HotKey_Does_Not_Throws ()
    {
        var mb = new MenuBar ();

        var i1 = new MenuBarItem ("_heey", "fff", () => { }, () => true);

        mb.Menus = new [] { i1 };
        mb.Menus = new [] { i1 };

        Assert.Equal (Key.H, mb.Menus [0].HotKey);
    }

    [Fact]
    [AutoInitShutdown]
    public void ShortCut_Activates ()
    {
        var saveAction = false;

        var menu = new MenuBar
        {
            Menus =
            [
                new (
                     "_File",
                     new MenuItem []
                     {
                         new (
                              "_Save",
                              "Saves the file.",
                              () => { saveAction = true; },
                              null,
                              null,
                              (KeyCode)Key.S.WithCtrl
                             )
                     }
                    )
            ]
        };

        var top = new Toplevel ();
        top.Add (menu);
        Application.Begin (top);

        Application.RaiseKeyDownEvent (Key.S.WithCtrl);
        Application.MainLoop.RunIteration ();

        Assert.True (saveAction);
        top.Dispose ();
    }

    [Fact]
    public void Update_ShortcutKey_KeyBindings_Old_ShortcutKey_Is_Removed ()
    {
        var menuBar = new MenuBar
        {
            Menus =
            [
                new (
                     "_File",
                     new MenuItem []
                     {
                         new ("New", "Create New", null, null, null, Key.A.WithCtrl)
                     }
                    )
            ]
        };

        Assert.Contains (Key.A.WithCtrl, menuBar.KeyBindings.Bindings);

        menuBar.Menus [0].Children! [0].ShortcutKey = Key.B.WithCtrl;

        Assert.DoesNotContain (Key.A.WithCtrl, menuBar.KeyBindings.Bindings);
        Assert.Contains (Key.B.WithCtrl, menuBar.KeyBindings.Bindings);
    }

    [Fact]
    public void UseKeysUpDownAsKeysLeftRight_And_UseSubMenusSingleFrame_Cannot_Be_Both_True ()
    {
        var menu = new MenuBar ();
        Assert.False (menu.UseKeysUpDownAsKeysLeftRight);
        Assert.False (menu.UseSubMenusSingleFrame);

        menu.UseKeysUpDownAsKeysLeftRight = true;
        Assert.True (menu.UseKeysUpDownAsKeysLeftRight);
        Assert.False (menu.UseSubMenusSingleFrame);

        menu.UseSubMenusSingleFrame = true;
        Assert.False (menu.UseKeysUpDownAsKeysLeftRight);
        Assert.True (menu.UseSubMenusSingleFrame);
    }

    [Fact]
    [AutoInitShutdown]
    public void UseSubMenusSingleFrame_False_By_Keyboard ()
    {
        var menu = new MenuBar
        {
            Menus = new MenuBarItem []
            {
                new (
                     "Numbers",
                     new MenuItem []
                     {
                         new ("One", "", null),
                         new MenuBarItem (
                                          "Two",
                                          new MenuItem []
                                          {
                                              new ("Sub-Menu 1", "", null),
                                              new ("Sub-Menu 2", "", null)
                                          }
                                         ),
                         new ("Three", "", null)
                     }
                    )
            }
        };
        menu.UseKeysUpDownAsKeysLeftRight = true;
        var top = new Toplevel ();
        top.Add (menu);
        Application.Begin (top);

        Assert.Equal (Point.Empty, new (menu.Frame.X, menu.Frame.Y));
        Assert.False (menu.UseSubMenusSingleFrame);

        top.Draw ();

        var expected = @"
 Numbers
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        Assert.True (menu.NewKeyDownEvent (menu.Key));
        top.Draw ();

        expected = @"
 Numbers  
┌────────┐
│ One    │
│ Two   ►│
│ Three  │
└────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        Assert.True (Application.Top.Subviews [1].NewKeyDownEvent (Key.CursorDown));
        top.Draw ();

        expected = @"
 Numbers                 
┌────────┐               
│ One    │               
│ Two   ►│┌─────────────┐
│ Three  ││ Sub-Menu 1  │
└────────┘│ Sub-Menu 2  │
          └─────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        Assert.True (Application.Top.Subviews [2].NewKeyDownEvent (Key.CursorLeft));
        top.Draw ();

        expected = @"
 Numbers  
┌────────┐
│ One    │
│ Two   ►│
│ Three  │
└────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        Assert.True (Application.Top.Subviews [1].NewKeyDownEvent (Key.Esc));
        top.Draw ();

        expected = @"
 Numbers
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void UseSubMenusSingleFrame_False_By_Mouse ()
    {
        var menu = new MenuBar
        {
            Menus =
            [
                new (
                     "Numbers",
                     new MenuItem []
                     {
                         new ("One", "", null),
                         new MenuBarItem (
                                          "Two",
                                          new MenuItem []
                                          {
                                              new (
                                                   "Sub-Menu 1",
                                                   "",
                                                   null
                                                  ),
                                              new (
                                                   "Sub-Menu 2",
                                                   "",
                                                   null
                                                  )
                                          }
                                         ),
                         new ("Three", "", null)
                     }
                    )
            ]
        };

        var top = new Toplevel ();
        top.Add (menu);
        Application.Begin (top);

        Assert.Equal (Point.Empty, new (menu.Frame.X, menu.Frame.Y));
        Assert.False (menu.UseSubMenusSingleFrame);

        top.Draw ();

        var expected = @"
 Numbers
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 0, 8, 1), pos);

        menu.NewMouseEvent (
                            new () { Position = new (1, 0), Flags = MouseFlags.Button1Pressed, View = menu }
                           );
        top.Draw ();

        expected = @"
 Numbers  
┌────────┐
│ One    │
│ Two   ►│
│ Three  │
└────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 0, 10, 6), pos);

        menu.NewMouseEvent (
                            new ()
                            {
                                Position = new (1, 2), Flags = MouseFlags.ReportMousePosition, View = Application.Top.Subviews [1]
                            }
                           );
        top.Draw ();

        expected = @"
 Numbers                 
┌────────┐               
│ One    │               
│ Two   ►│┌─────────────┐
│ Three  ││ Sub-Menu 1  │
└────────┘│ Sub-Menu 2  │
          └─────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 0, 25, 7), pos);

        Assert.False (
                      menu.NewMouseEvent (
                                          new ()
                                          {
                                              Position = new (1, 1), Flags = MouseFlags.ReportMousePosition, View = Application.Top.Subviews [1]
                                          }
                                         )
                     );
        top.Draw ();

        expected = @"
 Numbers  
┌────────┐
│ One    │
│ Two   ►│
│ Three  │
└────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 0, 10, 6), pos);

        menu.NewMouseEvent (
                            new () { Position = new (70, 2), Flags = MouseFlags.Button1Clicked, View = Application.Top }
                           );
        top.Draw ();

        expected = @"
 Numbers
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 0, 8, 1), pos);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void UseSubMenusSingleFrame_False_Disabled_Border ()
    {
        var menu = new MenuBar
        {
            MenusBorderStyle = LineStyle.None,
            Menus =
            [
                new (
                     "Numbers",
                     new MenuItem []
                     {
                         new ("One", "", null),
                         new MenuBarItem (
                                          "Two",
                                          new MenuItem []
                                          {
                                              new (
                                                   "Sub-Menu 1",
                                                   "",
                                                   null
                                                  ),
                                              new (
                                                   "Sub-Menu 2",
                                                   "",
                                                   null
                                                  )
                                          }
                                         ),
                         new ("Three", "", null)
                     }
                    )
            ]
        };

        menu.UseKeysUpDownAsKeysLeftRight = true;
        menu.BeginInit ();
        menu.EndInit ();

        menu.OpenMenu ();
        menu.ColorScheme = menu._openMenu.ColorScheme = new (Attribute.Default);
        Assert.True (menu.IsMenuOpen);

        menu.Draw ();
        menu._openMenu.Draw ();

        var expected = @"
 Numbers
 One    
 Two   ►
 Three  ";

        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        Assert.True (menu._openMenu.NewKeyDownEvent (Key.CursorDown));
        menu.Draw ();
        menu._openMenu.Draw ();
        menu.OpenCurrentMenu.Draw ();

        expected = @"
 Numbers           
 One               
 Two   ► Sub-Menu 1
 Three   Sub-Menu 2";

        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void UseSubMenusSingleFrame_True_By_Keyboard ()
    {
        var menu = new MenuBar
        {
            Menus =
            [
                new (
                     "Numbers",
                     new MenuItem []
                     {
                         new ("One", "", null),
                         new MenuBarItem (
                                          "Two",
                                          new MenuItem []
                                          {
                                              new (
                                                   "Sub-Menu 1",
                                                   "",
                                                   null
                                                  ),
                                              new (
                                                   "Sub-Menu 2",
                                                   "",
                                                   null
                                                  )
                                          }
                                         ),
                         new ("Three", "", null)
                     }
                    )
            ]
        };

        var top = new Toplevel ();
        top.Add (menu);
        Application.Begin (top);

        Assert.Equal (Point.Empty, new (menu.Frame.X, menu.Frame.Y));
        Assert.False (menu.UseSubMenusSingleFrame);
        menu.UseSubMenusSingleFrame = true;
        Assert.True (menu.UseSubMenusSingleFrame);

        top.Draw ();

        var expected = @"
 Numbers
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 0, 8, 1), pos);

        Assert.True (menu.NewKeyDownEvent (menu.Key));
        top.Draw ();

        expected = @"
 Numbers  
┌────────┐
│ One    │
│ Two   ►│
│ Three  │
└────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 0, 10, 6), pos);

        Assert.True (Application.Top.Subviews [1].NewKeyDownEvent (Key.CursorDown));
        Assert.True (Application.Top.Subviews [1].NewKeyDownEvent (Key.Enter));
        top.Draw ();

        expected = @"
 Numbers       
┌─────────────┐
│◄    Two     │
├─────────────┤
│ Sub-Menu 1  │
│ Sub-Menu 2  │
└─────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 0, 15, 7), pos);

        Assert.True (Application.Top.Subviews [2].NewKeyDownEvent (Key.Enter));
        top.Draw ();

        expected = @"
 Numbers  
┌────────┐
│ One    │
│ Two   ►│
│ Three  │
└────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 0, 10, 6), pos);

        Assert.True (Application.Top.Subviews [1].NewKeyDownEvent (Key.Esc));
        top.Draw ();

        expected = @"
 Numbers
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 0, 8, 1), pos);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void UseSubMenusSingleFrame_True_By_Mouse ()
    {
        var menu = new MenuBar
        {
            Menus =
            [
                new (
                     "Numbers",
                     new MenuItem []
                     {
                         new ("One", "", null),
                         new MenuBarItem (
                                          "Two",
                                          new MenuItem []
                                          {
                                              new (
                                                   "Sub-Menu 1",
                                                   "",
                                                   null
                                                  ),
                                              new (
                                                   "Sub-Menu 2",
                                                   "",
                                                   null
                                                  )
                                          }
                                         ),
                         new ("Three", "", null)
                     }
                    )
            ]
        };

        var top = new Toplevel ();
        top.Add (menu);
        Application.Begin (top);

        Assert.Equal (Point.Empty, new (menu.Frame.X, menu.Frame.Y));
        Assert.False (menu.UseSubMenusSingleFrame);
        menu.UseSubMenusSingleFrame = true;
        Assert.True (menu.UseSubMenusSingleFrame);

        top.Draw ();

        var expected = @"
 Numbers
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 0, 8, 1), pos);

        Assert.True (menu.NewMouseEvent (new () { Position = new (1, 0), Flags = MouseFlags.Button1Pressed, View = menu }));
        top.Draw ();

        expected = @"
 Numbers  
┌────────┐
│ One    │
│ Two   ►│
│ Three  │
└────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 0, 10, 6), pos);

        Assert.False (menu.NewMouseEvent (new () { Position = new (1, 2), Flags = MouseFlags.Button1Clicked, View = Application.Top.Subviews [1] }));
        top.Draw ();

        expected = @"
 Numbers       
┌─────────────┐
│◄    Two     │
├─────────────┤
│ Sub-Menu 1  │
│ Sub-Menu 2  │
└─────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 0, 15, 7), pos);

        menu.NewMouseEvent (new () { Position = new (1, 1), Flags = MouseFlags.Button1Clicked, View = Application.Top.Subviews [2] });
        top.Draw ();

        expected = @"
 Numbers  
┌────────┐
│ One    │
│ Two   ►│
│ Three  │
└────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 0, 10, 6), pos);

        Assert.False (menu.NewMouseEvent (new () { Position = new (70, 2), Flags = MouseFlags.Button1Clicked, View = Application.Top }));
        top.Draw ();

        expected = @"
 Numbers
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 0, 8, 1), pos);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void UseSubMenusSingleFrame_True_Disabled_Border ()
    {
        var menu = new MenuBar
        {
            MenusBorderStyle = LineStyle.None,
            Menus =
            [
                new (
                     "Numbers",
                     new MenuItem []
                     {
                         new ("One", "", null),
                         new MenuBarItem (
                                          "Two",
                                          new MenuItem []
                                          {
                                              new (
                                                   "Sub-Menu 1",
                                                   "",
                                                   null
                                                  ),
                                              new (
                                                   "Sub-Menu 2",
                                                   "",
                                                   null
                                                  )
                                          }
                                         ),
                         new ("Three", "", null)
                     }
                    )
            ]
        };

        menu.UseSubMenusSingleFrame = true;
        menu.BeginInit ();
        menu.EndInit ();

        menu.OpenMenu ();
        Assert.True (menu.IsMenuOpen);

        menu.Draw ();
        menu.ColorScheme = menu._openMenu.ColorScheme = new (Attribute.Default);
        menu._openMenu.Draw ();

        var expected = @"
 Numbers
 One    
 Two   ►
 Three  ";

        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        Assert.True (menu._openMenu.NewKeyDownEvent (Key.CursorDown));
        Assert.True (menu._openMenu.NewKeyDownEvent (Key.Enter));
        menu.Draw ();
        menu._openMenu.Draw ();
        menu.OpenCurrentMenu.Draw ();

        expected = @"
 Numbers     
◄    Two     
─────────────
 Sub-Menu 1  
 Sub-Menu 2  ";

        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void UseSubMenusSingleFrame_True_Without_Border ()
    {
        var menu = new MenuBar
        {
            UseSubMenusSingleFrame = true,
            MenusBorderStyle = LineStyle.None,
            Menus =
            [
                new (
                     "Numbers",
                     new MenuItem []
                     {
                         new ("One", "", null),
                         new MenuBarItem (
                                          "Two",
                                          new MenuItem []
                                          {
                                              new (
                                                   "Sub-Menu 1",
                                                   "",
                                                   null
                                                  ),
                                              new (
                                                   "Sub-Menu 2",
                                                   "",
                                                   null
                                                  )
                                          }
                                         ),
                         new ("Three", "", null)
                     }
                    )
            ]
        };

        var top = new Toplevel ();
        top.Add (menu);
        Application.Begin (top);

        Assert.Equal (Point.Empty, new (menu.Frame.X, menu.Frame.Y));
        Assert.True (menu.UseSubMenusSingleFrame);
        Assert.Equal (LineStyle.None, menu.MenusBorderStyle);

        top.Draw ();

        var expected = @"
 Numbers
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 0, 8, 1), pos);

        Assert.True (
                     menu.NewMouseEvent (
                                         new () { Position = new (1, 0), Flags = MouseFlags.Button1Pressed, View = menu }
                                        )
                    );
        top.Draw ();

        expected = @"
 Numbers
 One    
 Two   ►
 Three  
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 0, 8, 4), pos);

        menu.NewMouseEvent (
                            new () { Position = new (1, 2), Flags = MouseFlags.Button1Clicked, View = Application.Top.Subviews [1] }
                           );
        top.Draw ();

        expected = @"
 Numbers     
◄    Two     
─────────────
 Sub-Menu 1  
 Sub-Menu 2  
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 0, 13, 5), pos);

        menu.NewMouseEvent (
                            new () { Position = new (1, 1), Flags = MouseFlags.Button1Clicked, View = Application.Top.Subviews [2] }
                           );
        top.Draw ();

        expected = @"
 Numbers
 One    
 Two   ►
 Three  
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 0, 8, 4), pos);

        menu.NewMouseEvent (
                            new () { Position = new (70, 2), Flags = MouseFlags.Button1Clicked, View = Application.Top }
                           );
        top.Draw ();

        expected = @"
 Numbers
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 0, 8, 1), pos);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Visible_False_Key_Does_Not_Open_And_Close_All_Opened_Menus ()
    {
        var menu = new MenuBar
        {
            Menus =
            [
                new ("File", new MenuItem [] { new ("New", "", null) })
            ]
        };
        var top = new Toplevel ();
        top.Add (menu);
        Application.Begin (top);

        Assert.True (menu.Visible);
        Assert.True (menu.NewKeyDownEvent (menu.Key));
        Assert.True (menu.IsMenuOpen);

        menu.Visible = false;
        Assert.False (menu.IsMenuOpen);

        Assert.True (menu.NewKeyDownEvent (menu.Key));
        Assert.False (menu.IsMenuOpen);
        top.Dispose ();
    }

    // Defines the expected strings for a Menu. Currently supports 
    //   - MenuBar with any number of MenuItems 
    //   - Each top-level MenuItem can have a SINGLE sub-menu
    //
    // TODO: Enable multiple sub-menus
    // TODO: Enable checked sub-menus
    // TODO: Enable sub-menus with sub-menus (perhaps better to put this in a separate class with focused unit tests?)
    //
    // E.g: 
    //
    // File  Edit
    //  New    Copy
    public class ExpectedMenuBar : MenuBar
    {
        private FakeDriver _d = (FakeDriver)Application.Driver;

        // The expected strings when the menu is closed
        public string ClosedMenuText => MenuBarText + "\n";

        public string ExpectedBottomRow (int i)
        {
            return $"{CM.Glyphs.LLCorner}{new (CM.Glyphs.HLine.ToString () [0], Menus [i].Children [0].TitleLength + 3)}{CM.Glyphs.LRCorner}  \n";
        }

        // The 3 spaces at end are a result of Menu.cs line 1062 where `pos` is calculated (` + spacesAfterTitle`)
        public string ExpectedMenuItemRow (int i) { return $"{CM.Glyphs.VLine} {Menus [i].Children [0].Title}  {CM.Glyphs.VLine}   \n"; }

        // The full expected string for an open sub menu
        public string ExpectedSubMenuOpen (int i)
        {
            return ClosedMenuText
                   + (Menus [i].Children.Length > 0
                          ? ExpectedPadding (i)
                            + ExpectedTopRow (i)
                            + ExpectedPadding (i)
                            + ExpectedMenuItemRow (i)
                            + ExpectedPadding (i)
                            + ExpectedBottomRow (i)
                          : "");
        }

        // Define expected menu frame
        // "┌──────┐"
        // "│ New  │"
        // "└──────┘"
        // 
        // The width of the Frame is determined in Menu.cs line 144, where `Width` is calculated
        //   1 space before the Title and 2 spaces after the Title/Check/Help
        public string ExpectedTopRow (int i)
        {
            return $"{CM.Glyphs.ULCorner}{new (CM.Glyphs.HLine.ToString () [0], Menus [i].Children [0].TitleLength + 3)}{CM.Glyphs.URCorner}  \n";
        }

        // Each MenuBar title has a 1 space pad on each side
        // See `static int leftPadding` and `static int rightPadding` on line 1037 of Menu.cs
        public string MenuBarText
        {
            get
            {
                var txt = string.Empty;

                foreach (MenuBarItem m in Menus)
                {
                    txt += " " + m.Title + " ";
                }

                return txt;
            }
        }

        // Padding for the X of the sub menu Frame
        // Menu.cs - Line 1239 in `internal void OpenMenu` is where the Menu is created
        private string ExpectedPadding (int i)
        {
            var n = 0;

            while (i > 0)
            {
                n += Menus [i - 1].TitleLength + 2;
                i--;
            }

            return new (' ', n);
        }
    }

    private class CustomWindow : Window
    {
        public CustomWindow ()
        {
            var menu = new MenuBar
            {
                Menus =
                [
                    new ("File", new MenuItem [] { new ("New", "", null) }),
                    new (
                         "Edit",
                         new MenuItem []
                         {
                             new MenuBarItem (
                                              "Delete",
                                              new MenuItem []
                                                  { new ("All", "", null), new ("Selected", "", null) }
                                             )
                         }
                        )
                ]
            };
            Add (menu);
        }
    }
}
