using System.Globalization;
using System.Reflection;
using TerminalGuiFluentTesting;
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
    [ClassData (typeof (V2TestDrivers))]
    public void Initializes_WithNoItems (V2TestDriver d)
    {
        using GuiTestContext c = With.A<Window> (80, 25, d)
                                     .Then (
                                            () =>
                                            {
                                                // Create a menu bar with no items
                                                var menuBar = new MenuBarv2 ();
                                                Assert.Empty (menuBar.SubViews);
                                                Assert.False (menuBar.CanFocus);
                                                Assert.Equal (Orientation.Horizontal, menuBar.Orientation);
                                                Assert.Equal (Key.F9, MenuBarv2.DefaultKey);
                                            })
                                     .WriteOutLogs (_out)
                                     .Stop ();
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void Initializes_WithItems (V2TestDriver d)
    {
        MenuBarItemv2 [] menuItems = [];

        using GuiTestContext c = With.A<Window> (80, 25, d)
                                     .Then (
                                            () =>
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
                                            })
                                     .WriteOutLogs (_out)
                                     .Stop ();
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void AddsItems_WithMenusProperty (V2TestDriver d)
    {
        using GuiTestContext c = With.A<Window> (80, 25, d)
                                     .Then (
                                            () =>
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
                                            })
                                     .WriteOutLogs (_out)
                                     .Stop ();
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void ChangesKey_RaisesEvent (V2TestDriver d)
    {
        using GuiTestContext c = With.A<Window> (80, 25, d)
                                     .Then (
                                            () =>
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
                                            })
                                     .WriteOutLogs (_out)
                                     .Stop ();
    }


    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void DefaultKey_Activates (V2TestDriver d)
    {
        MenuBarv2? menuBar = null;

        using GuiTestContext c = With.A<Window> (50, 20, d)
                                     .Then (
                                            () =>
                                            {
                                                menuBar = new MenuBarv2 ();
                                                Toplevel top = Application.Top!;

                                                top.Add (
                                                         new View ()
                                                         {
                                                             CanFocus = true,
                                                             Id = "focusableView",

                                                         });
                                                menuBar.EnableForDesign (ref top);
                                                Application.Top!.Add (menuBar);
                                            })
                                     .WaitIteration ()
                                     .Then (() => Assert.IsNotType<MenuItemv2> (Application.Navigation!.GetFocused ()))
                                     .ScreenShot ("MenuBar initial state", _out)
                                     .RaiseKeyDownEvent (MenuBarv2.DefaultKey)
                                     .WaitIteration ()
                                     .ScreenShot ($"After {MenuBarv2.DefaultKey}", _out)
                                     .WriteOutLogs (_out)
                                     .Then (() => Assert.Equal ("_New file", Application.Navigation!.GetFocused ()!.Title))
                                     .Then (() => Assert.True (Application.Popover?.GetActivePopover () is PopoverMenu))
                                     .Then (() => Assert.True (menuBar?.IsOpen ()))
                                     .WriteOutLogs (_out)
                                     .Stop ();
    }


    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void DefaultKey_DeActivates (V2TestDriver d)
    {
        MenuBarv2? menuBar = null;

        using GuiTestContext c = With.A<Window> (50, 20, d)
                                     .Then (
                                            () =>
                                            {
                                                menuBar = new MenuBarv2 ();
                                                Toplevel top = Application.Top!;

                                                top.Add (
                                                         new View ()
                                                         {
                                                             CanFocus = true,
                                                             Id = "focusableView",

                                                         });
                                                menuBar.EnableForDesign (ref top);
                                                Application.Top!.Add (menuBar);
                                            })
                                     .WaitIteration ()
                                     .Then (() => Assert.IsNotType<MenuItemv2>(Application.Navigation!.GetFocused()))
                                     .ScreenShot ("MenuBar initial state", _out)
                                     .RaiseKeyDownEvent (MenuBarv2.DefaultKey)
                                     .ScreenShot ($"After {MenuBarv2.DefaultKey}", _out)
                                     .Then (() => Assert.Equal ("_New file", Application.Navigation!.GetFocused ()!.Title))
                                     .RaiseKeyDownEvent (MenuBarv2.DefaultKey)
                                     .ScreenShot ($"After {MenuBarv2.DefaultKey}", _out)
                                     .Then (() => Assert.IsNotType<MenuItemv2>(Application.Navigation!.GetFocused()))
                                     .WriteOutLogs (_out)
                                     .Stop ();
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void ShowHidePopovers (V2TestDriver d)
    {
        using GuiTestContext c = With.A<Window> (80, 25, d)
                                     .Then (
                                            () =>
                                            {
                                                // Create a menu bar with items that have submenus
                                                var fileMenuItem = new MenuBarItemv2 (
                                                                                      "_File",
                                                                                      [
                                                                                          new MenuItemv2 ("_Open", string.Empty, null),
                                                                                          new MenuItemv2 ("_Save", string.Empty, null)
                                                                                      ]);

                                                var menuBar = new MenuBarv2 ([fileMenuItem]);

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
                                            })
                                     .WriteOutLogs (_out)
                                     .Stop ();
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void EnableForDesign_CreatesMenuItems (V2TestDriver d)
    {
        using GuiTestContext c = With.A<Window> (80, 25, d)
                                     .Then (
                                            () =>
                                            {
                                                var menuBar = new MenuBarv2 ();
                                                Application.Top!.Add (menuBar);

                                                // Call EnableForDesign
                                                Toplevel top = Application.Top!;
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
                                     .ScreenShot ("MenuBarv2 EnableForDesign", _out)
                                     .WriteOutLogs (_out)
                                     .Stop ();
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void Navigation_Left_Right_Wraps (V2TestDriver d)
    {
        MenuBarv2? menuBar = null;

        using GuiTestContext c = With.A<Window> (50, 20, d)
                                     .Then (
                                            () =>
                                            {
                                                menuBar = new MenuBarv2 ();
                                                Toplevel top = Application.Top!;
                                                menuBar.EnableForDesign (ref top);
                                                Application.Top!.Add (menuBar);
                                            })
                                     .WaitIteration ()
                                     .ScreenShot ("MenuBar initial state", _out)
                                     .RaiseKeyDownEvent (MenuBarv2.DefaultKey)
                                     .Then (() => Assert.True (Application.Popover?.GetActivePopover () is PopoverMenu))
                                     .Then (() => Assert.True (menuBar?.IsOpen ()))
                                     .Then (() => Assert.Equal ("_New file", Application.Navigation?.GetFocused ()!.Title))
                                     .ScreenShot ($"After {MenuBarv2.DefaultKey}", _out)
                                     .Right ()
                                     .Then (() => Assert.True (Application.Popover?.GetActivePopover () is PopoverMenu))
                                     .ScreenShot ("After right arrow", _out)
                                     .Then (() => Assert.Equal ("Cu_t", Application.Navigation?.GetFocused ()!.Title))
                                     .Right ()
                                     .ScreenShot ("After second right arrow", _out)
                                     .Then (() => Assert.Equal ("_Online Help...", Application.Navigation?.GetFocused ()!.Title))
                                     .ScreenShot ("After third right arrow", _out)
                                     .Right ()
                                     .ScreenShot ("After fourth right arrow", _out)
                                     .Then (() => Assert.Equal ("_New file", Application.Navigation?.GetFocused ()!.Title))
                                     .Left ()
                                     .ScreenShot ("After left arrow", _out)
                                     .Then (() => Assert.Equal ("_Online Help...", Application.Navigation?.GetFocused ()!.Title))
                                     .WriteOutLogs (_out)
                                     .Stop ();
    }


    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void MenuBarItem_With_QuitKey_Open_QuitKey_Restores_Focus_Correctly (V2TestDriver d)
    {
        MenuBarv2? menuBar = null;

        using GuiTestContext c = With.A<Window> (50, 20, d)
                                     .Then (
                                            () =>
                                            {
                                                menuBar = new MenuBarv2 ();
                                                Toplevel top = Application.Top!;

                                                top.Add (
                                                         new View ()
                                                         {
                                                             CanFocus = true,
                                                             Id = "focusableView",

                                                         });
                                                menuBar.EnableForDesign (ref top);
                                                Application.Top!.Add (menuBar);
                                            })
                                     .WaitIteration ()
                                     .Then (() => Assert.IsNotType<MenuItemv2>(Application.Navigation!.GetFocused()))
                                     .ScreenShot ("MenuBar initial state", _out)
                                     .RaiseKeyDownEvent (MenuBarv2.DefaultKey)
                                     .Then (() => Assert.Equal ("_New file", Application.Navigation!.GetFocused ()!.Title))
                                     .Then (() => Assert.True (Application.Popover?.GetActivePopover () is PopoverMenu))
                                     .Then (() => Assert.True (menuBar?.IsOpen ()))
                                     .Then (() => Assert.Equal ("_New file", Application.Navigation?.GetFocused ()!.Title))
                                     .ScreenShot ($"After {MenuBarv2.DefaultKey}", _out)
                                     .RaiseKeyDownEvent (Application.QuitKey)
                                     .Then (() => Assert.False (Application.Popover?.GetActivePopover () is PopoverMenu))
                                     .Then (() => Assert.IsNotType<MenuItemv2> (Application.Navigation!.GetFocused ()))
                                     .WriteOutLogs (_out)
                                     .Stop ();
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void MenuBarItem_Without_QuitKey_Open_QuitKey_Restores_Focus_Correctly (V2TestDriver d)
    {
        MenuBarv2? menuBar = null;

        using GuiTestContext c = With.A<Window> (50, 20, d)
                                     .Then (
                                            () =>
                                            {
                                                menuBar = new MenuBarv2 ();
                                                Toplevel top = Application.Top!;

                                                top.Add (
                                                         new View ()
                                                         {
                                                             CanFocus = true,
                                                             Id = "focusableView",

                                                         });
                                                menuBar.EnableForDesign (ref top);
                                                Application.Top!.Add (menuBar);
                                            })
                                     .WaitIteration ()
                                     .Then (() => Assert.IsNotType<MenuItemv2> (Application.Navigation!.GetFocused ()))
                                     .ScreenShot ("MenuBar initial state", _out)
                                     .RaiseKeyDownEvent (MenuBarv2.DefaultKey)
                                     .RaiseKeyDownEvent (Key.CursorRight)
                                     .Then (() => Assert.Equal ("Cu_t", Application.Navigation!.GetFocused ()!.Title))
                                     .Then (() => Assert.True (Application.Popover?.GetActivePopover () is PopoverMenu))
                                     .Then (() => Assert.True (menuBar?.IsOpen ()))
                                     .Then (() => Assert.Equal ("Cu_t", Application.Navigation?.GetFocused ()!.Title))
                                     .ScreenShot ($"After {MenuBarv2.DefaultKey}", _out)
                                     .RaiseKeyDownEvent (Application.QuitKey)
                                     .WriteOutLogs (_out)
                                     .Then (() => Assert.False (Application.Popover?.GetActivePopover () is PopoverMenu))
                                     .Then (() => Assert.IsNotType<MenuItemv2> (Application.Navigation!.GetFocused ()))
                                     .Stop ();
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void MenuBarItem_With_QuitKey_Open_QuitKey_Does_Not_Quit_App (V2TestDriver d)
    {
        MenuBarv2? menuBar = null;

        using GuiTestContext c = With.A<Window> (50, 20, d)
                                     .Then (
                                            () =>
                                            {
                                                menuBar = new MenuBarv2 ();
                                                Toplevel top = Application.Top!;

                                                top.Add (
                                                         new View ()
                                                         {
                                                             CanFocus = true,
                                                             Id = "focusableView",

                                                         });
                                                menuBar.EnableForDesign (ref top);
                                                Application.Top!.Add (menuBar);
                                            })
                                     .WaitIteration ()
                                     .Then (() => Assert.IsNotType<MenuItemv2> (Application.Navigation!.GetFocused ()))
                                     .ScreenShot ("MenuBar initial state", _out)
                                     .RaiseKeyDownEvent (MenuBarv2.DefaultKey)
                                     .Then (() => Assert.Equal ("_New file", Application.Navigation!.GetFocused ()!.Title))
                                     .Then (() => Assert.True (Application.Top!.Running))
                                     .ScreenShot ($"After {MenuBarv2.DefaultKey}", _out)
                                     .RaiseKeyDownEvent (Application.QuitKey)
                                     .Then (() => Assert.False (Application.Popover?.GetActivePopover () is PopoverMenu))
                                     .Then (() => Assert.True (Application.Top!.Running))
                                     .WriteOutLogs (_out)
                                     .Stop ();
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void MenuBarItem_Without_QuitKey_Open_QuitKey_Does_Not_Quit_MenuBar_SuperView (V2TestDriver d)
    {
        MenuBarv2? menuBar = null;

        using GuiTestContext c = With.A<Window> (50, 20, d)
                                     .Then (
                                            () =>
                                            {
                                                menuBar = new MenuBarv2 ();
                                                Toplevel top = Application.Top!;

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
                                                Application.Top!.Add (menuBar);
                                            })
                                     .WaitIteration ()
                                     .Then (() => Assert.IsNotType<MenuItemv2> (Application.Navigation!.GetFocused ()))
                                     .ScreenShot ("MenuBar initial state", _out)
                                     .RaiseKeyDownEvent (MenuBarv2.DefaultKey)
                                     .Then (() => Assert.Equal ("_New file", Application.Navigation!.GetFocused ()!.Title))
                                     .ScreenShot ($"After {MenuBarv2.DefaultKey}", _out)
                                     .RaiseKeyDownEvent (Application.QuitKey)
                                     .Then (() => Assert.False (Application.Popover?.GetActivePopover () is PopoverMenu))
                                     .Then (() => Assert.True (Application.Top!.Running))
                                     .WriteOutLogs (_out)
                                     .Stop ();
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void MenuBar_Not_Active_DoesNotEat_Space (V2TestDriver d)
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

        using GuiTestContext c = With.A<Window> (50, 20, d)
                                     .Then (
                                            () =>
                                            {
                                                var menuBar = new MenuBarv2 ();
                                                Toplevel top = Application.Top!;
                                                menuBar.EnableForDesign (ref top);
                                                Application.Top!.Add (menuBar);
                                            })
                                     .Add (testView)
                                     .WaitIteration ()
                                     .Focus (testView)
                                     .RaiseKeyDownEvent (Key.Space)
                                     .Then (() => Assert.Equal (1, spaceKeyDownCount))
                                     .WriteOutLogs (_out)
                                     .Stop ();
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void MenuBar_Not_Active_DoesNotEat_Enter (V2TestDriver d)
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

        using GuiTestContext c = With.A<Window> (50, 20, d)
                                     .Then (
                                            () =>
                                            {
                                                var menuBar = new MenuBarv2 ();
                                                Toplevel top = Application.Top!;
                                                menuBar.EnableForDesign (ref top);
                                                Application.Top!.Add (menuBar);
                                            })
                                     .Add (testView)
                                     .WaitIteration ()
                                     .Focus (testView)
                                     .RaiseKeyDownEvent (Key.Enter)
                                     .Then (() => Assert.Equal (1, enterKeyDownCount))
                                     .WriteOutLogs (_out)
                                     .Stop ();
    }

}
