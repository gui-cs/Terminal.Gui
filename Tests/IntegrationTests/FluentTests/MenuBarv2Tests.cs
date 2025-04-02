using System.Reflection;
using Terminal.Gui;
using TerminalGuiFluentTesting;
using Xunit.Abstractions;

namespace IntegrationTests.FluentTests;

/// <summary>
///     Tests for the MenuBarv2 class
/// </summary>
public class MenuBarv2Tests
{
    private readonly TextWriter _out;

    public MenuBarv2Tests (ITestOutputHelper outputHelper) { _out = new BasicFluentAssertionTests.TestOutputWriter (outputHelper); }

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
                                                Assert.Equal (0, menuBar.SubViews.Count);
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
                                                fileMenuItem.PopoverMenu.Visible = true;

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
                                                var menuBar = new MenuBarv2 ();
                                                Application.Top.Add (menuBar);

                                                // Call EnableForDesign
                                                bool result = menuBar.EnableForDesign ();

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
    public void Navigation_BetweenItems (V2TestDriver d)
    {
        var menuBarActivated = false;

        using GuiTestContext c = With.A<Window> (80, 25, d)
                                     .Then (
                                            () =>
                                            {
                                                // Create menu items
                                                var fileMenu = new MenuBarItemv2 (
                                                                                  "_File",
                                                                                  [
                                                                                      new MenuItemv2 ("_Open", string.Empty, null),
                                                                                      new MenuItemv2 ("_Save", string.Empty, null)
                                                                                  ]);

                                                var editMenu = new MenuBarItemv2 (
                                                                                  "_Edit",
                                                                                  [
                                                                                      new MenuItemv2 ("_Cut", string.Empty, null),
                                                                                      new MenuItemv2 ("_Copy", string.Empty, null)
                                                                                  ]);

                                                // Create menu bar and add to window
                                                var menuBar = new MenuBarv2 ([fileMenu, editMenu]);
                                                Application.Top.Add (menuBar);

                                                // Set menu bar to active state using reflection
                                                FieldInfo? activeField = typeof (MenuBarv2).GetField (
                                                                                                      "_active",
                                                                                                      BindingFlags.NonPublic | BindingFlags.Instance);
                                                activeField?.SetValue (menuBar, true);
                                                menuBar.CanFocus = true;
                                                menuBarActivated = true;

                                                // Give focus to the first menu item
                                                fileMenu.SetFocus ();
                                                Assert.True (fileMenu.HasFocus);

                                                Application.LayoutAndDraw ();
                                            })
                                     .ScreenShot ("MenuBar initial state", _out)
                                     .Then (
                                            () =>
                                            {
                                                if (!menuBarActivated)
                                                {
                                                    // Skip further tests if activation failed
                                                }

                                                // Move right to select the edit menu
                                                // This simulates navigation between menu items
                                            })
                                     .Right ()
                                     .ScreenShot ("After right arrow", _out)
                                     .Right ()
                                     .ScreenShot ("After second right arrow (should wrap)", _out)
                                     .Left ()
                                     .ScreenShot ("After left arrow", _out)
                                     .WriteOutLogs (_out)
                                     .Stop ();
    }
}
