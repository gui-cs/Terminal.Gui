using System.Globalization;
using TerminalGuiFluentTesting;
using TerminalGuiFluentTestingXunit;
using Xunit.Abstractions;

namespace IntegrationTests.FluentTests;

/// <summary>
///     Tests for the PopoverMenu class
/// </summary>
public class PopoverMenuTests
{
    private readonly TextWriter _out;

    public PopoverMenuTests (ITestOutputHelper outputHelper)
    {
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        _out = new TestOutputWriter (outputHelper);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void EnableForDesign_CreatesMenuItems (TestDriver d)
    {
        using GuiTestContext c = With.A<Window> (80, 25, d)
                                     .Then (
                                            () =>
                                            {
                                                var popoverMenu = new PopoverMenu ();
                                                Application.Top!.Add (popoverMenu);

                                                // Call EnableForDesign
                                                Toplevel top = Application.Top;
                                                bool result = popoverMenu.EnableForDesign (ref top);

                                                // Should return true
                                                Assert.True (result);

                                                // Should have created menu items
                                                Assert.NotNull (popoverMenu.Root);
                                                Assert.Equal (7, popoverMenu.Root.SubViews.Count);

                                                // Should have Cut menu item
                                                View? cutMenuItem = popoverMenu.GetMenuItemsOfAllSubMenus ().FirstOrDefault (v => v?.Title == "Cu_t");

                                                Assert.NotNull (cutMenuItem);
                                            })
                                     .Stop ();
    }

    private static object o = new  ();

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void Activate_Sets_Application_Navigation_Correctly (TestDriver d)
    {
        lock (o)
        {
            using GuiTestContext c = With.A<Window> (50, 20, d)
                                     .Then (
                                            () =>
                                            {
                                                var popoverMenu = new PopoverMenu ();

                                                // Call EnableForDesign
                                                Toplevel top = Application.Top!;
                                                popoverMenu.EnableForDesign (ref top);

                                                View? view = new View ()
                                                {
                                                    CanFocus = true,
                                                    Height = Dim.Auto (),
                                                    Width = Dim.Auto (),
                                                    Id = "focusableView",
                                                    Text = "View",
                                                };
                                                Application.Top!.Add (view);

                                                // EnableForDesign sets to true; undo that
                                                popoverMenu.Visible = false;

                                                Application.Popover!.Register (popoverMenu);

                                                view.SetFocus ();
                                            })
                                     .WaitIteration ()
                                     .AssertFalse (Application.Popover?.GetActivePopover () is PopoverMenu)
                                     .AssertIsNotType<MenuItemv2> (Application.Navigation!.GetFocused ())
                                     .ScreenShot ("PopoverMenu initial state", _out)
                                     .Then (() => Application.Popover!.Show (Application.Popover.Popovers.First ()))
                                     .WaitIteration ()
                                     .ScreenShot ($"After Show", _out)
                                     .AssertTrue (Application.Popover?.GetActivePopover () is PopoverMenu)
                                     .AssertEqual ("Cu_t", Application.Navigation!.GetFocused ()!.Title)
                                     .WriteOutLogs (_out)
                                     .Stop ();
        }
        
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void QuitKey_Hides (TestDriver d)
    {
        using GuiTestContext c = With.A<Window> (50, 20, d)
                                     .Then (
                                            () =>
                                            {
                                                var popoverMenu = new PopoverMenu ();

                                                // Call EnableForDesign
                                                Toplevel top = Application.Top!;
                                                bool result = popoverMenu.EnableForDesign (ref top);

                                                View? view = new View ()
                                                {
                                                    CanFocus = true,
                                                    Height = Dim.Auto (),
                                                    Width = Dim.Auto (),
                                                    Id = "focusableView",
                                                    Text = "View",
                                                };
                                                Application.Top!.Add (view);

                                                // EnableForDesign sets to true; undo that
                                                popoverMenu.Visible = false;

                                                Application.Popover!.Register (popoverMenu);

                                                view.SetFocus ();
                                            })
                                     .WaitIteration ()
                                     .ScreenShot ("PopoverMenu initial state", _out)
                                     .AssertFalse (Application.Popover?.GetActivePopover () is PopoverMenu)
                                     .Then (() => Application.Popover!.Show (Application.Popover.Popovers.First ()))
                                     .WaitIteration ()
                                     .ScreenShot ($"After Show", _out)
                                     .AssertTrue (Application.Popover?.GetActivePopover () is PopoverMenu)
                                     .EnqueueKeyEvent (Application.QuitKey)
                                     .WaitIteration ()
                                     .WriteOutLogs (_out)
                                     .ScreenShot ($"After {Application.QuitKey}", _out)
                                     .AssertFalse (Application.Popover!.Popovers.Cast<PopoverMenu> ().FirstOrDefault()!.Visible)
                                     .AssertNull (Application.Popover!.GetActivePopover())
                                     .AssertTrue (Application.Top!.Running)
                                     .WriteOutLogs (_out)
                                     .Stop ();
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void QuitKey_Restores_Focus_Correctly (TestDriver d)
    {
        using GuiTestContext c = With.A<Window> (50, 20, d)
                                     .Then (
                                            () =>
                                            {
                                                var popoverMenu = new PopoverMenu ();

                                                // Call EnableForDesign
                                                Toplevel top = Application.Top!;
                                                bool result = popoverMenu.EnableForDesign (ref top);

                                                View? view = new View ()
                                                {
                                                    CanFocus = true,
                                                    Height = Dim.Auto (),
                                                    Width = Dim.Auto (),
                                                    Id = "focusableView",
                                                    Text = "View",
                                                };
                                                Application.Top!.Add (view);

                                                // EnableForDesign sets to true; undo that
                                                popoverMenu.Visible = false;

                                                Application.Popover!.Register (popoverMenu);

                                                view.SetFocus ();
                                            })
                                     .WaitIteration ()
                                     .ScreenShot ("PopoverMenu initial state", _out)
                                     .AssertFalse (Application.Popover?.GetActivePopover () is PopoverMenu)
                                     .AssertIsNotType<MenuItemv2>(Application.Navigation!.GetFocused())
                                     .Then (() => Application.Popover!.Show (Application.Popover.Popovers.First ()))
                                     .WaitIteration ()
                                     .ScreenShot ($"After Show", _out)
                                     .AssertTrue (Application.Popover?.GetActivePopover () is PopoverMenu)
                                     .AssertIsType<MenuItemv2>(Application.Navigation!.GetFocused())
                                     .EnqueueKeyEvent (Application.QuitKey)
                                     .ScreenShot ($"After {Application.QuitKey}", _out)
                                     .AssertFalse (Application.Popover?.GetActivePopover () is PopoverMenu)
                                     .AssertIsNotType<MenuItemv2>(Application.Navigation!.GetFocused())
                                     .WriteOutLogs (_out)
                                     .Stop ();
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void MenuBarItem_With_QuitKey_Open_QuitKey_Does_Not_Quit_App (TestDriver d)
    {
        using GuiTestContext c = With.A<Window> (50, 20, d)
                                     .Then (
                                            () =>
                                            {
                                                var popoverMenu = new PopoverMenu ();

                                                // Call EnableForDesign
                                                Toplevel top = Application.Top!;
                                                bool result = popoverMenu.EnableForDesign (ref top);

                                                View? view = new View ()
                                                {
                                                    CanFocus = true,
                                                    Height = Dim.Auto (),
                                                    Width = Dim.Auto (),
                                                    Id = "focusableView",
                                                    Text = "View",
                                                };
                                                Application.Top!.Add (view);

                                                // EnableForDesign sets to true; undo that
                                                popoverMenu.Visible = false;

                                                Application.Popover!.Register (popoverMenu);

                                                view.SetFocus ();
                                            })
                                     .WaitIteration ()
                                     .AssertIsNotType<MenuItemv2>(Application.Navigation!.GetFocused())
                                     .ScreenShot ("PopoverMenu initial state", _out)
                                     .Then (() => Application.Popover!.Show (Application.Popover.Popovers.First ()))
                                     .WaitIteration ()
                                     .ScreenShot ("PopoverMenu after Show", _out)
                                     .AssertEqual ("Cu_t", Application.Navigation!.GetFocused ()!.Title)
                                     .AssertTrue (Application.Top!.Running)
                                     .EnqueueKeyEvent (Application.QuitKey)
                                     .WaitIteration ()
                                     .ScreenShot ($"After {Application.QuitKey}", _out)
                                     .AssertFalse (Application.Popover?.GetActivePopover () is PopoverMenu)
                                     .AssertTrue (Application.Top!.Running)
                                     .WriteOutLogs (_out)
                                     .Stop ();
    }


    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void Not_Active_DoesNotEat_Space (TestDriver d)
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
                                                var popoverMenu = new PopoverMenu();
                                                Toplevel top = Application.Top!;
                                                popoverMenu.EnableForDesign (ref top);
                                                Application.Popover!.Register (popoverMenu);
                                            })
                                     .Add (testView)
                                     .WaitIteration ()
                                     .Focus (testView)
                                     .EnqueueKeyEvent (Key.Space)
                                     .AssertEqual (1, spaceKeyDownCount)
                                     .WriteOutLogs (_out)
                                     .Stop ();
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void Not_Active_DoesNotEat_Enter (TestDriver d)
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
                                                var popoverMenu = new PopoverMenu ();
                                                Toplevel top = Application.Top!;
                                                popoverMenu.EnableForDesign (ref top);
                                                Application.Popover!.Register (popoverMenu);
                                            })
                                     .Add (testView)
                                     .WaitIteration ()
                                     .Focus (testView)
                                     .EnqueueKeyEvent (Key.Enter)
                                     .AssertEqual (1, enterKeyDownCount)
                                     .WriteOutLogs (_out)
                                     .Stop ();
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void Not_Active_DoesNotEat_QuitKey (TestDriver d)
    {
        int quitKeyDownCount = 0;
        View testView = new View ()
        {
            CanFocus = true,
            Id = "testView",
        };

        testView.KeyDown += (sender, key) =>
                            {
                                if (key == Application.QuitKey)
                                {
                                    quitKeyDownCount++;
                                }
                            };

        using GuiTestContext c = With.A<Window> (50, 20, d)
                                     .Then (
                                            () =>
                                            {
                                                var popoverMenu = new PopoverMenu ();
                                                Toplevel top = Application.Top!;
                                                popoverMenu.EnableForDesign (ref top);
                                                Application.Popover!.Register (popoverMenu);
                                            })
                                     .Add (testView)
                                     .WaitIteration ()
                                     .Focus (testView)
                                     .EnqueueKeyEvent (Application.QuitKey)
                                     .AssertEqual (1, quitKeyDownCount)
                                     .WriteOutLogs (_out)
                                     .Stop ();
    }


    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void ContextMenu_CrashesOnRight (TestDriver d)
    {
        var clicked = false;

        MenuItemv2 [] menuItems = [new ("_New File", string.Empty, () => { clicked = true; })];

        using GuiTestContext c = With.A<Window> (40, 10, d, _out)
                                     .WithContextMenu (new (menuItems))
                                     .ScreenShot ("Before open menu", _out)

                                     // Click in main area inside border
                                     .RightClick (1, 1)
                                     .Then (() =>
                                     {
                                         // Test depends on menu having a border
                                         IPopover? popover = Application.Popover!.GetActivePopover ();
                                         Assert.NotNull (popover);
                                         var popoverMenu = popover as PopoverMenu;
                                         popoverMenu!.Root!.BorderStyle = LineStyle.Single;
                                     })
                                     .WaitIteration ()
                                     .ScreenShot ("After open menu", _out)
                                     .LeftClick (2, 2)
                                     .Stop ()
                                     .WriteOutLogs (_out);
        Assert.True (clicked);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void ContextMenu_OpenSubmenu (TestDriver d)
    {
        var clicked = false;

        MenuItemv2 [] menuItems =
        [
            new ("One", "", null),
            new ("Two", "", null),
            new ("Three", "", null),
            new (
                 "Four",
                 "",
                 new (
                      [
                          new ("SubMenu1", "", null),
                          new ("SubMenu2", "", () => clicked = true),
                          new ("SubMenu3", "", null),
                          new ("SubMenu4", "", null),
                          new ("SubMenu5", "", null),
                          new ("SubMenu6", "", null),
                          new ("SubMenu7", "", null)
                      ])),
            new ("Five", "", null),
            new ("Six", "", null)
        ];

        using GuiTestContext c = With.A<Window> (40, 10, d)
                                     .WithContextMenu (new (menuItems))
                                     .ScreenShot ("Before open menu", _out)

                                     // Click in main area inside border
                                     .RightClick (1, 1)
                                     .ScreenShot ("After open menu", _out)
                                     .EnqueueKeyEvent (Key.CursorDown)
                                     .EnqueueKeyEvent (Key.CursorDown)
                                     .EnqueueKeyEvent (Key.CursorDown)
                                     .EnqueueKeyEvent (Key.CursorRight)
                                     .ScreenShot ("After open submenu", _out)
                                     .EnqueueKeyEvent (Key.CursorDown)
                                     .EnqueueKeyEvent (Key.Enter)
                                     .ScreenShot ("Menu should be closed after selecting", _out)
                                     .Stop ()
                                     .WriteOutLogs (_out);
        Assert.True (clicked);
    }

}
