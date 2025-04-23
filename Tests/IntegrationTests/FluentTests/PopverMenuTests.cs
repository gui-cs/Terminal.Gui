using System.Reflection;
using Terminal.Gui;
using TerminalGuiFluentTesting;
using Xunit.Abstractions;

namespace IntegrationTests.FluentTests;

/// <summary>
///     Tests for the PopoverMenu class
/// </summary>
public class PopoverMenuTests (ITestOutputHelper outputHelper)
{
    private readonly TextWriter _out = new TestOutputWriter (outputHelper);

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void EnableForDesign_CreatesMenuItems (V2TestDriver d)
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


    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void Activate_Sets_Application_Navigation_Correctly (V2TestDriver d)
    {
        MenuBarv2? menuBar = null;

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
                                     .Then (() => Assert.False (Application.Popover?.GetActivePopover () is PopoverMenu))
                                     .Then (() => Assert.IsNotType<MenuItemv2> (Application.Navigation!.GetFocused ()))
                                     .ScreenShot ("PopoverMenu initial state", _out)
                                     .Then (() => Application.Popover!.Show (Application.Popover.Popovers.First ()))
                                     .WaitIteration ()
                                     .ScreenShot ($"After Show", _out)
                                     .Then (() => Assert.True (Application.Popover?.GetActivePopover () is PopoverMenu))
                                     .Then (() => Assert.Equal ("Cu_t", Application.Navigation!.GetFocused ()!.Title))
                                     .WriteOutLogs (_out)
                                     .Stop ();
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void QuitKey_Hides (V2TestDriver d)
    {
        MenuBarv2? menuBar = null;

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
                                     .Then (() => Assert.False (Application.Popover?.GetActivePopover () is PopoverMenu))
                                     .Then (() => Application.Popover!.Show (Application.Popover.Popovers.First ()))
                                     .WaitIteration ()
                                     .ScreenShot ($"After Show", _out)
                                     .Then (() => Assert.True (Application.Popover?.GetActivePopover () is PopoverMenu))
                                     .RaiseKeyDownEvent (Application.QuitKey)
                                     .Then (() => Application.LayoutAndDraw (true))
                                     .WaitIteration ()
                                     .WriteOutLogs (_out)
                                     .ScreenShot ($"After {Application.QuitKey}", _out)
                                     .Then (() => Assert.False (Application.Popover!.Popovers.Cast<PopoverMenu> ().FirstOrDefault()!.Visible))
                                     .Then (() => Assert.Null (Application.Popover!.GetActivePopover()))
                                     .Then (() => Assert.True (Application.Top!.Running))
                                     .WriteOutLogs (_out)
                                     .Stop ();
    }

    [Theory]
    [ClassData (typeof (V2TestDrivers))]
    public void QuitKey_Restores_Focus_Correctly (V2TestDriver d)
    {
        MenuBarv2? menuBar = null;

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
                                     .Then (() => Assert.False (Application.Popover?.GetActivePopover () is PopoverMenu))
                                     .Then (() => Assert.IsNotType<MenuItemv2>(Application.Navigation!.GetFocused()))
                                     .Then (() => Application.Popover!.Show (Application.Popover.Popovers.First ()))
                                     .WaitIteration ()
                                     .ScreenShot ($"After Show", _out)
                                     .Then (() => Assert.True (Application.Popover?.GetActivePopover () is PopoverMenu))
                                     .Then (() => Assert.IsType<MenuItemv2>(Application.Navigation!.GetFocused()))
                                     .RaiseKeyDownEvent (Application.QuitKey)
                                     .ScreenShot ($"After {Application.QuitKey}", _out)
                                     .Then (() => Assert.False (Application.Popover?.GetActivePopover () is PopoverMenu))
                                     .Then (() => Assert.IsNotType<MenuItemv2>(Application.Navigation!.GetFocused()))
                                     .WriteOutLogs (_out)
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
                                     .Then (() => Assert.IsNotType<MenuItemv2>(Application.Navigation!.GetFocused()))
                                     .ScreenShot ("PopoverMenu initial state", _out)
                                     .Then (() => Application.Popover!.Show (Application.Popover.Popovers.First ()))
                                     .WaitIteration ()
                                     .ScreenShot ("PopoverMenu after Show", _out)
                                     .Then (() => Assert.Equal ("Cu_t", Application.Navigation!.GetFocused ()!.Title))
                                     .Then (() => Assert.True (Application.Top!.Running))
                                     .RaiseKeyDownEvent (Application.QuitKey)
                                     .Then (() => Application.LayoutAndDraw ())
                                     .ScreenShot ($"After {Application.QuitKey}", _out)
                                     .Then (() => Assert.False (Application.Popover?.GetActivePopover () is PopoverMenu))
                                     .Then (() => Assert.True (Application.Top!.Running))
                                     .WriteOutLogs (_out)
                                     .Stop ();
    }
}
