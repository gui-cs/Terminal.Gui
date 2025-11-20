using System.Globalization;
using System.Reflection;
using TerminalGuiFluentTesting;
using TerminalGuiFluentTestingXunit;
using Xunit.Abstractions;

namespace IntegrationTests.FluentTests;

/// <summary>
///     Tests for the MenuBarv2 class
/// </summary>
public class MenuBarv2Tests
{
    private readonly TextWriter _out;

    public MenuBarv2Tests (ITestOutputHelper outputHelper)
    {
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        _out = new TestOutputWriter (outputHelper);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void Initializes_WithNoItems (TestDriver d)
    {
        using GuiTestContext c = With.A<Window> (80, 25, d, _out)
                                     .Then ((_) =>
                                            {
                                                // Create a menu bar with no items
                                                var menuBar = new MenuBarv2 ();
                                                Assert.Empty (menuBar.SubViews);
                                                Assert.False (menuBar.CanFocus);
                                                Assert.Equal (Orientation.Horizontal, menuBar.Orientation);
                                                Assert.Equal (Key.F9, MenuBarv2.DefaultKey);
                                            });
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void Initializes_WithItems (TestDriver d)
    {
        MenuBarItemv2 [] menuItems = [];

        using GuiTestContext c = With.A<Window> (80, 25, d, _out)
                                     .Then ((_) =>
                                            {
                                                // Create items for the menu bar
                                                menuItems =
                                                [
                                                    new (
                                                         "_File",
                                                         [
                                                             new MenuItemv2 ("_Open", "Opens a file", () => { })
                                                         ]),
                                                    new (
                                                         "_Edit",
                                                         [
                                                             new MenuItemv2 ("_Copy", "Copies selection", () => { })
                                                         ])
                                                ];

                                                var menuBar = new MenuBarv2 (menuItems);
                                                Assert.Equal (2, menuBar.SubViews.Count);

                                                // First item should be the File menu
                                                var fileMenu = menuBar.SubViews.ElementAt (0) as MenuBarItemv2;
                                                Assert.NotNull (fileMenu);
                                                Assert.Equal ("_File", fileMenu.Title);

                                                // Second item should be the Edit menu
                                                var editMenu = menuBar.SubViews.ElementAt (1) as MenuBarItemv2;
                                                Assert.NotNull (editMenu);
                                                Assert.Equal ("_Edit", editMenu.Title);
                                            });
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void AddsItems_WithMenusProperty (TestDriver d)
    {
        using GuiTestContext c = With.A<Window> (80, 25, d, _out)
                                     .Then ((_) =>
                                            {
                                                var menuBar = new MenuBarv2 ();

                                                // Set items through Menus property
                                                menuBar.Menus =
                                                [
                                                    new ("_File"),
                                                    new ("_Edit"),
                                                    new ("_View")
                                                ];

                                                Assert.Equal (3, menuBar.SubViews.Count);
                                            });
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void ChangesKey_RaisesEvent (TestDriver d)
    {
        using GuiTestContext c = With.A<Window> (80, 25, d, _out)
                                     .Then ((_) =>
                                            {
                                                var menuBar = new MenuBarv2 ();

                                                var oldKeyValue = Key.Empty;
                                                var newKeyValue = Key.Empty;
                                                var eventRaised = false;

                                                menuBar.KeyChanged += (_, args) =>
                                                                      {
                                                                          eventRaised = true;
                                                                          oldKeyValue = args.OldKey;
                                                                          newKeyValue = args.NewKey;
                                                                      };

                                                // Default key should be F9
                                                Assert.Equal (Key.F9, menuBar.Key);

                                                // Change key to F1
                                                menuBar.Key = Key.F1;

                                                // Verify event was raised
                                                Assert.True (eventRaised);
                                                Assert.Equal (Key.F9, oldKeyValue);
                                                Assert.Equal (Key.F1, newKeyValue);

                                                // Verify key was changed
                                                Assert.Equal (Key.F1, menuBar.Key);
                                            });
    }


    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void DefaultKey_Activates (TestDriver d)
    {
        MenuBarv2? menuBar = null;
        Toplevel? top = null;

        using GuiTestContext c = With.A<Window> (50, 20, d, _out)
                                     .Then ((app) =>
                                            {
                                                menuBar = new MenuBarv2 ();
                                                top = app.Current!;

                                                top.Add (
                                                         new View ()
                                                         {
                                                             CanFocus = true,
                                                             Id = "focusableView",

                                                         });
                                                menuBar.EnableForDesign (ref top);
                                                app.Current!.Add (menuBar);
                                            })
                                     .WaitIteration ()
                                     .AssertIsNotType<MenuItemv2> (top?.App?.Navigation!.GetFocused ())
                                     .ScreenShot ("MenuBar initial state", _out)
                                     .EnqueueKeyEvent (MenuBarv2.DefaultKey)
                                     .WaitIteration ()
                                     .ScreenShot ($"After {MenuBarv2.DefaultKey}", _out)
                                     .AssertEqual ("_New file", top?.App?.Navigation!.GetFocused ()!.Title)
                                     .AssertTrue (top?.App?.Popover?.GetActivePopover () is PopoverMenu)
                                     .AssertTrue (menuBar?.IsOpen ());
    }


    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void DefaultKey_DeActivates (TestDriver d)
    {
        MenuBarv2? menuBar = null;
        IApplication? app = null;
        using GuiTestContext c = With.A<Window> (50, 20, d, _out)
                                     .Then ((a) =>
                                            {
                                                app = a;
                                                menuBar = new MenuBarv2 ();
                                                Toplevel top = app.Current!;

                                                top.Add (
                                                         new View ()
                                                         {
                                                             CanFocus = true,
                                                             Id = "focusableView",

                                                         });
                                                menuBar.EnableForDesign (ref top);
                                                app.Current!.Add (menuBar);
                                            })
                                     .WaitIteration ()
                                     .AssertIsNotType<MenuItemv2> (app?.Navigation!.GetFocused ())
                                     .ScreenShot ("MenuBar initial state", _out)
                                     .EnqueueKeyEvent (MenuBarv2.DefaultKey)
                                     .ScreenShot ($"After {MenuBarv2.DefaultKey}", _out)
                                     .AssertEqual ("_New file", app?.Navigation!.GetFocused ()!.Title)
                                     .EnqueueKeyEvent (MenuBarv2.DefaultKey)
                                     .ScreenShot ($"After {MenuBarv2.DefaultKey}", _out)
                                     .AssertIsNotType<MenuItemv2> (app?.Navigation!.GetFocused ());
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void ShowHidePopovers (TestDriver d)
    {
        IApplication? app = null;
        using GuiTestContext c = With.A<Window> (80, 25, d, _out)
                                     .Then ((a) =>
                                            {
                                                app = a;
                                                // Create a menu bar with items that have submenus
                                                var fileMenuItem = new MenuBarItemv2 (
                                                                                      "_File",
                                                                                      [
                                                                                          new MenuItemv2 ("_Open", string.Empty, null),
                                                                                          new MenuItemv2 ("_Save", string.Empty, null)
                                                                                      ]);

                                                var menuBar = new MenuBarv2 ([fileMenuItem]) { App = app };

                                                // Initially, no menu should be open
                                                Assert.False (menuBar.IsOpen ());
                                                Assert.False (menuBar.Active);

                                                // Initialize the menu bar
                                                menuBar.BeginInit ();
                                                menuBar.EndInit ();

                                                // Simulate showing a popover menu by manipulating the first menu item
                                                MethodInfo? showPopoverMethod = typeof (MenuBarv2).GetMethod (
                                                     "ShowPopover",
                                                     BindingFlags.NonPublic | BindingFlags.Instance);

                                                // Set menu bar to active state using reflection
                                                FieldInfo? activeField = typeof (MenuBarv2).GetField (
                                                                                                      "_active",
                                                                                                      BindingFlags.NonPublic | BindingFlags.Instance);
                                                activeField?.SetValue (menuBar, true);
                                                menuBar.CanFocus = true;

                                                // Show the popover menu
                                                showPopoverMethod?.Invoke (menuBar, new object? [] { fileMenuItem });

                                                // Should be active now
                                                Assert.True (menuBar.Active);

                                                // Test if we can hide the popover menu
                                                fileMenuItem.PopoverMenu!.Visible = true;

                                                Assert.True (menuBar.HideActiveItem ());

                                                // Menu should no longer be open or active
                                                Assert.False (menuBar.Active);
                                                Assert.False (menuBar.IsOpen ());
                                                Assert.False (menuBar.CanFocus);
                                            });
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void EnableForDesign_CreatesMenuItems (TestDriver d)
    {
        using GuiTestContext c = With.A<Window> (80, 25, d, _out)
                                     .Then ((app) =>
                                            {
                                                var menuBar = new MenuBarv2 ();
                                                app.Current!.Add (menuBar);

                                                // Call EnableForDesign
                                                Toplevel top = app.Current!;
                                                bool result = menuBar.EnableForDesign (ref top);

                                                // Should return true
                                                Assert.True (result);

                                                // Should have created menu items
                                                Assert.True (menuBar.SubViews.Count > 0);

                                                // Should have File, Edit and Help menus
                                                View? fileMenu = menuBar.SubViews.FirstOrDefault (v => (v as MenuBarItemv2)?.Title == "_File");
                                                View? editMenu = menuBar.SubViews.FirstOrDefault (v => (v as MenuBarItemv2)?.Title == "_Edit");
                                                View? helpMenu = menuBar.SubViews.FirstOrDefault (v => (v as MenuBarItemv2)?.Title == "_Help");

                                                Assert.NotNull (fileMenu);
                                                Assert.NotNull (editMenu);
                                                Assert.NotNull (helpMenu);
                                            })
                                     .ScreenShot ("MenuBarv2 EnableForDesign", _out);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void Navigation_Left_Right_Wraps (TestDriver d)
    {
        MenuBarv2? menuBar = null;
        IApplication? app = null;

        using GuiTestContext c = With.A<Window> (50, 20, d, _out)
                                     .Then ((a) =>
                                            {
                                                app = a;
                                                menuBar = new MenuBarv2 ();
                                                Toplevel top = app.Current!;
                                                menuBar.EnableForDesign (ref top);
                                                app.Current!.Add (menuBar);
                                            })
                                     .WaitIteration ()
                                     .ScreenShot ("MenuBar initial state", _out)
                                     .EnqueueKeyEvent (MenuBarv2.DefaultKey)
                                     .AssertTrue (app?.Popover?.GetActivePopover () is PopoverMenu)
                                     .AssertTrue (menuBar?.IsOpen ())
                                     .AssertEqual ("_New file", app?.Navigation?.GetFocused ()!.Title)
                                     .ScreenShot ($"After {MenuBarv2.DefaultKey}", _out)
                                     .EnqueueKeyEvent (Key.CursorRight)
                                     .AssertTrue (app?.Popover?.GetActivePopover () is PopoverMenu)
                                     .ScreenShot ("After right arrow", _out)
                                     .AssertEqual ("Cu_t", app?.Navigation?.GetFocused ()!.Title)
                                     .EnqueueKeyEvent (Key.CursorRight)
                                     .ScreenShot ("After second right arrow", _out)
                                     .AssertEqual ("_Online Help...", app?.Navigation?.GetFocused ()!.Title)
                                     .ScreenShot ("After third right arrow", _out)
                                     .EnqueueKeyEvent (Key.CursorRight)
                                     .ScreenShot ("After fourth right arrow", _out)
                                     .AssertEqual ("_New file", app?.Navigation?.GetFocused ()!.Title)
                                     .EnqueueKeyEvent (Key.CursorLeft)
                                     .ScreenShot ("After left arrow", _out)
                                     .AssertEqual ("_Online Help...", app?.Navigation?.GetFocused ()!.Title);
    }


    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void MenuBarItem_With_QuitKey_Open_QuitKey_Restores_Focus_Correctly (TestDriver d)
    {
        MenuBarv2? menuBar = null;
        IApplication? app = null;

        using GuiTestContext c = With.A<Window> (50, 20, d, _out)
                                     .Then ((a) =>
                                            {
                                                app = a;
                                                menuBar = new MenuBarv2 ();
                                                Toplevel top = app.Current!;

                                                top.Add (
                                                         new View ()
                                                         {
                                                             CanFocus = true,
                                                             Id = "focusableView",

                                                         });
                                                menuBar.EnableForDesign (ref top);
                                                app.Current!.Add (menuBar);
                                            })
                                     .AssertIsNotType<MenuItemv2> (app!.Navigation!.GetFocused ())
                                     .ScreenShot ("MenuBar initial state", _out)
                                     .EnqueueKeyEvent (MenuBarv2.DefaultKey)
                                     .AssertEqual ("_New file", app.Navigation!.GetFocused ()!.Title)
                                     .AssertTrue (app?.Popover?.GetActivePopover () is PopoverMenu)
                                     .AssertTrue (menuBar?.IsOpen ())
                                     .AssertEqual ("_New file", app?.Navigation?.GetFocused ()!.Title)
                                     .ScreenShot ($"After {MenuBarv2.DefaultKey}", _out)
                                     .EnqueueKeyEvent (Application.QuitKey)
                                     .AssertFalse (app?.Popover?.GetActivePopover () is PopoverMenu)
                                     .AssertIsNotType<MenuItemv2> (app!.Navigation!.GetFocused ());
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void MenuBarItem_Without_QuitKey_Open_QuitKey_Restores_Focus_Correctly (TestDriver d)
    {
        MenuBarv2? menuBar = null;
        IApplication? app = null;

        using GuiTestContext c = With.A<Window> (50, 20, d, _out)
                                     .Add (
                                           new View ()
                                           {
                                               CanFocus = true,
                                               Id = "focusableView",

                                           })
                                     .Then ((a) =>
                                            {
                                                app = a;
                                                menuBar = new MenuBarv2 ();
                                                Toplevel? toplevel = app.Current;
                                                menuBar.EnableForDesign (ref toplevel!);
                                                app.Current!.Add (menuBar);
                                            })
                                     .WaitIteration ()
                                     .AssertIsNotType<MenuItemv2> (app?.Navigation!.GetFocused ())
                                     .ScreenShot ("MenuBar initial state", _out)
                                     .EnqueueKeyEvent (MenuBarv2.DefaultKey)
                                     .EnqueueKeyEvent (Key.CursorRight)
                                     .AssertEqual ("Cu_t", app?.Navigation!.GetFocused ()!.Title)
                                     .AssertTrue (app?.Popover?.GetActivePopover () is PopoverMenu)
                                     .AssertTrue (menuBar?.IsOpen ())
                                     .AssertEqual ("Cu_t", app?.Navigation?.GetFocused ()!.Title)
                                     .ScreenShot ($"After {MenuBarv2.DefaultKey}", _out)
                                     .EnqueueKeyEvent (Application.QuitKey)
                                     .AssertFalse (app?.Popover?.GetActivePopover () is PopoverMenu)
                                     .AssertIsNotType<MenuItemv2> (app?.Navigation?.GetFocused ());
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void MenuBarItem_With_QuitKey_Open_QuitKey_Does_Not_Quit_App (TestDriver d)
    {
        MenuBarv2? menuBar = null;
        IApplication? app = null;

        using GuiTestContext c = With.A<Window> (50, 20, d, _out)
                                     .Then ((a) =>
                                            {
                                                app = a;
                                                menuBar = new MenuBarv2 ();
                                                Toplevel top = app.Current!;

                                                top.Add (
                                                         new View ()
                                                         {
                                                             CanFocus = true,
                                                             Id = "focusableView",

                                                         });
                                                menuBar.EnableForDesign (ref top);
                                                app.Current!.Add (menuBar);
                                            })
                                     .WaitIteration ()
                                     .AssertIsNotType<MenuItemv2> (app!.Navigation!.GetFocused ())
                                     .ScreenShot ("MenuBar initial state", _out)
                                     .EnqueueKeyEvent (MenuBarv2.DefaultKey)
                                     .AssertEqual ("_New file", app.Navigation!.GetFocused ()!.Title)
                                     .AssertTrue (app?.Current!.IsRunning)
                                     .ScreenShot ($"After {MenuBarv2.DefaultKey}", _out)
                                     .EnqueueKeyEvent (Application.QuitKey)
                                     .AssertFalse (app?.Popover?.GetActivePopover () is PopoverMenu)
                                     .AssertTrue (app!.Current!.IsRunning);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void MenuBarItem_Without_QuitKey_Open_QuitKey_Does_Not_Quit_MenuBar_SuperView (TestDriver d)
    {
        MenuBarv2? menuBar = null;
        IApplication? app = null;

        using GuiTestContext c = With.A<Window> (50, 20, d, _out)
                                     .Then ((a) =>
                                            {
                                                app = a;
                                                menuBar = new MenuBarv2 ();
                                                Toplevel top = app.Current!;

                                                top.Add (
                                                         new View ()
                                                         {
                                                             CanFocus = true,
                                                             Id = "focusableView",

                                                         });
                                                menuBar.EnableForDesign (ref top);
                                                IEnumerable<MenuItemv2> items = menuBar.GetMenuItemsWithTitle ("_Quit");

                                                foreach (MenuItemv2 item in items)
                                                {
                                                    item.Key = Key.Empty;
                                                }

                                                app.Current!.Add (menuBar);
                                            })
                                     .WaitIteration ()
                                     .AssertIsNotType<MenuItemv2> (app?.Navigation!.GetFocused ())
                                     .ScreenShot ("MenuBar initial state", _out)
                                     .EnqueueKeyEvent (MenuBarv2.DefaultKey)
                                     .AssertEqual ("_New file", app?.Navigation!.GetFocused ()!.Title)
                                     .ScreenShot ($"After {MenuBarv2.DefaultKey}", _out)
                                     .EnqueueKeyEvent (Application.QuitKey)
                                     .AssertFalse (app?.Popover?.GetActivePopover () is PopoverMenu)
                                     .AssertTrue (app?.Current!.IsRunning);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void MenuBar_Not_Active_DoesNotEat_Space (TestDriver d)
    {
        int spaceKeyDownCount = 0;
        View testView = new View ()
        {
            CanFocus = true,
            Id = "testView",
        };

        testView.KeyDown += (sender, key) =>
                            {
                                if (key == Key.Space)
                                {
                                    spaceKeyDownCount++;
                                }
                            };

        using GuiTestContext c = With.A<Window> (50, 20, d, _out)
                                     .Then ((a) =>
                                            {
                                                var menuBar = new MenuBarv2 ();
                                                Toplevel top = a.Current!;
                                                menuBar.EnableForDesign (ref top);
                                                a.Current!.Add (menuBar);
                                            })
                                     .Add (testView)
                                     .WaitIteration ()
                                     .Focus (testView)
                                     .EnqueueKeyEvent (Key.Space)
                                     .AssertEqual (1, spaceKeyDownCount);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void MenuBar_Not_Active_DoesNotEat_Enter (TestDriver d)
    {
        int enterKeyDownCount = 0;
        View testView = new View ()
        {
            CanFocus = true,
            Id = "testView",
        };

        testView.KeyDown += (sender, key) =>
                            {
                                if (key == Key.Enter)
                                {
                                    enterKeyDownCount++;
                                }
                            };

        using GuiTestContext c = With.A<Window> (50, 20, d, _out)
                                     .Then ((a) =>
                                            {
                                                var menuBar = new MenuBarv2 ();
                                                Toplevel top = a.Current!;
                                                menuBar.EnableForDesign (ref top);
                                                a.Current!.Add (menuBar);
                                            })
                                     .Add (testView)
                                     .WaitIteration ()
                                     .Focus (testView)
                                     .EnqueueKeyEvent (Key.Enter)
                                     .AssertEqual (1, enterKeyDownCount);
    }

}
