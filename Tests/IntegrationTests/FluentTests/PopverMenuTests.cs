using System.Reflection;
using Terminal.Gui;
using TerminalGuiFluentTesting;
using Xunit.Abstractions;

namespace IntegrationTests.FluentTests;

/// <summary>
///     Tests for the PopoverMenu class
/// </summary>
public class PopoverMenuTests
{
    private readonly TextWriter _out;

    public PopoverMenuTests (ITestOutputHelper outputHelper) { _out = new TestOutputWriter (outputHelper); }

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
                                                Assert.False (menuBar.IsActive ());

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
                                                Assert.True (menuBar.IsActive ());

                                                // Test if we can hide the popover menu
                                                fileMenuItem.PopoverMenu!.Visible = true;

                                                Assert.True (menuBar.HideActiveItem ());

                                                // Menu should no longer be open or active
                                                Assert.False (menuBar.IsActive ());
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
                                                var popverMenu = new PopoverMenu();
                                                Application.Top!.Add (popverMenu);

                                                // Call EnableForDesign
                                                bool result = popverMenu.EnableForDesign ();

                                                // Should return true
                                                Assert.True (result);

                                                // Should have created menu items
                                                Assert.True (popverMenu.SubViews.Count > 0);

                                                // Should have Cut menu item
                                                View? cutMenuItem = popverMenu.GetMenuItemsOfAllSubMenus().FirstOrDefault (v => v?.Title == "Cu_t");

                                                Assert.NotNull (cutMenuItem);
                                            })
                                     .ScreenShot ("PopoverMenu EnableForDesign", _out)
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
                                                var popverMenu = new PopoverMenu ();

                                                // Call EnableForDesign
                                                bool result = popverMenu.EnableForDesign ();

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
                                                popverMenu.Visible = false;

                                                Application.Popover!.Register (popverMenu);

                                                view.SetFocus ();
                                            })
                                     .WaitIteration ()
                                     .Then (() => Assert.False (Application.Popover?.GetActivePopover () is PopoverMenu))
                                     .Then (() => Assert.True (Application.Navigation!.GetFocused ()!.Id == "focusableView"))
                                     .Then(() => Application.Popover!.Show(Application.Popover.Popovers.First()))
                                     .ScreenShot ("PopoverMenu initial state", _out)
                                     .SendKey (PopoverMenu.DefaultKey)
                                     .ScreenShot ($"After {PopoverMenu.DefaultKey}", _out)
                                     .Then (() => Assert.True (Application.Popover?.GetActivePopover () is PopoverMenu))
                                     .Then (() => Assert.False (Application.Navigation!.GetFocused ()!.Id == "focusableView"))
                                     .SendKey (Application.QuitKey)
                                     .ScreenShot ($"After {Application.QuitKey}", _out)
                                     .Then (() => Assert.False (Application.Popover?.GetActivePopover () is PopoverMenu))
                                     .Then (() => Assert.True (Application.Navigation!.GetFocused ()!.Id == "focusableView"))
                                     .WriteOutLogs (_out)
                                     .Stop ();
    }
}
