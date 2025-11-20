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
                                     .Then ((app) =>
                                            {
                                                PopoverMenu popoverMenu = new ();
                                                app.Current!.Add (popoverMenu);

                                                // Call EnableForDesign
                                                Toplevel top = app.Current;
                                                bool result = popoverMenu.EnableForDesign (ref top);

                                                // Should return true
                                                Assert.True (result);

                                                // Should have created menu items
                                                Assert.NotNull (popoverMenu.Root);
                                                Assert.Equal (7, popoverMenu.Root.SubViews.Count);

                                                // Should have Cut menu item
                                                View? cutMenuItem = popoverMenu.GetMenuItemsOfAllSubMenus ().FirstOrDefault (v => v?.Title == "Cu_t");

                                                Assert.NotNull (cutMenuItem);
                                            });
    }

    private static readonly object o = new ();

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void Activate_Sets_Application_Navigation_Correctly (TestDriver d)
    {
        lock (o)
        {
            IApplication? app = null;
            using GuiTestContext c = With.A<Window> (50, 20, d)
                                         .Then ((a) =>
                                                {
                                                    app = a;
                                                    PopoverMenu popoverMenu = new ()
                                                    {
                                                        App = app
                                                    };

                                                    // Call EnableForDesign
                                                    Toplevel top = app.Current!;
                                                    popoverMenu.EnableForDesign (ref top);

                                                    var view = new View
                                                    {
                                                        CanFocus = true,
                                                        Height = Dim.Auto (),
                                                        Width = Dim.Auto (),
                                                        Id = "focusableView",
                                                        Text = "View"
                                                    };
                                                    app.Current!.Add (view);

                                                    // EnableForDesign sets to true; undo that
                                                    popoverMenu.Visible = false;

                                                    app?.Popover!.Register (popoverMenu);

                                                    view.SetFocus ();
                                                })
                                         .AssertFalse (app?.Popover?.GetActivePopover () is PopoverMenu)
                                         .AssertIsNotType<MenuItem> (app?.Navigation!.GetFocused ())
                                         .ScreenShot ("PopoverMenu initial state", _out)
                                         .Then ((_) => app?.Popover!.Show (app?.Popover.Popovers.First ()))
                                         .ScreenShot ("After Show", _out)
                                         .AssertTrue (app?.Popover?.GetActivePopover () is PopoverMenu)
                                         .AssertEqual ("Cu_t", app?.Navigation!.GetFocused ()!.Title);
        }
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void QuitKey_Hides (TestDriver d)
    {
        IApplication? app = null;
        using GuiTestContext c = With.A<Window> (50, 20, d)
                                     .Then ((a) =>
                                            {
                                                app = a;
                                                PopoverMenu popoverMenu = new ()
                                                {
                                                    App = app
                                                };

                                                // Call EnableForDesign
                                                Toplevel top = app.Current!;
                                                bool result = popoverMenu.EnableForDesign (ref top);

                                                var view = new View
                                                {
                                                    CanFocus = true,
                                                    Height = Dim.Auto (),
                                                    Width = Dim.Auto (),
                                                    Id = "focusableView",
                                                    Text = "View"
                                                };
                                                app.Current!.Add (view);

                                                // EnableForDesign sets to true; undo that
                                                popoverMenu.Visible = false;

                                                app?.Popover!.Register (popoverMenu);

                                                view.SetFocus ();
                                            })
                                     .ScreenShot ("PopoverMenu initial state", _out)
                                     .AssertFalse (app?.Popover?.GetActivePopover () is PopoverMenu)
                                     .Then ((_) => app?.Popover!.Show (app?.Popover.Popovers.First ()))
                                     .ScreenShot ("After Show", _out)
                                     .AssertTrue (app?.Popover?.GetActivePopover () is PopoverMenu)
                                     .EnqueueKeyEvent (Application.QuitKey)
                                     .ScreenShot ($"After {Application.QuitKey}", _out)
                                     .AssertFalse (app?.Popover!.Popovers.Cast<PopoverMenu> ().FirstOrDefault ()!.Visible)
                                     .AssertNull (app?.Popover!.GetActivePopover ())
                                     .AssertTrue (app?.Current!.Running);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void QuitKey_Restores_Focus_Correctly (TestDriver d)
    {
        IApplication? app = null;
        using GuiTestContext c = With.A<Window> (50, 20, d)
                                     .Then ((a) =>
                                            {
                                                app = a;
                                                PopoverMenu popoverMenu = new ()
                                                {
                                                    App = app
                                                };

                                                // Call EnableForDesign
                                                Toplevel top = app.Current!;
                                                bool result = popoverMenu.EnableForDesign (ref top);

                                                var view = new View
                                                {
                                                    CanFocus = true,
                                                    Height = Dim.Auto (),
                                                    Width = Dim.Auto (),
                                                    Id = "focusableView",
                                                    Text = "View"
                                                };
                                                app.Current!.Add (view);

                                                // EnableForDesign sets to true; undo that
                                                popoverMenu.Visible = false;

                                                app?.Popover!.Register (popoverMenu);

                                                view.SetFocus ();
                                            })
                                     .ScreenShot ("PopoverMenu initial state", _out)
                                     .AssertFalse (app?.Popover?.GetActivePopover () is PopoverMenu)
                                     .AssertIsNotType<MenuItem> (app?.Navigation!.GetFocused ())
                                     .Then ((_) => app?.Popover!.Show (app?.Popover.Popovers.First ()))
                                     .ScreenShot ("After Show", _out)
                                     .AssertTrue (app?.Popover?.GetActivePopover () is PopoverMenu)
                                     .AssertIsType<MenuItem> (app?.Navigation!.GetFocused ())
                                     .EnqueueKeyEvent (Application.QuitKey)
                                     .ScreenShot ($"After {Application.QuitKey}", _out)
                                     .AssertFalse (app?.Popover?.GetActivePopover () is PopoverMenu)
                                     .AssertIsNotType<MenuItem> (app?.Navigation!.GetFocused ());
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void MenuBarItem_With_QuitKey_Open_QuitKey_Does_Not_Quit_App (TestDriver d)
    {
        IApplication? app = null;

        using GuiTestContext c = With.A<Window> (50, 20, d)
                                     .Then ((a) =>
                                            {
                                                app = a;
                                                PopoverMenu popoverMenu = new ()
                                                {
                                                    App = app
                                                };

                                                // Call EnableForDesign
                                                Toplevel top = app.Current!;
                                                bool result = popoverMenu.EnableForDesign (ref top);

                                                var view = new View
                                                {
                                                    CanFocus = true,
                                                    Height = Dim.Auto (),
                                                    Width = Dim.Auto (),
                                                    Id = "focusableView",
                                                    Text = "View"
                                                };
                                                app.Current!.Add (view);

                                                // EnableForDesign sets to true; undo that
                                                popoverMenu.Visible = false;

                                                app?.Popover!.Register (popoverMenu);

                                                view.SetFocus ();
                                            })
                                     .AssertIsNotType<MenuItem> (app?.Navigation!.GetFocused ())
                                     .ScreenShot ("PopoverMenu initial state", _out)
                                     .Then ((_) => app?.Popover!.Show (app?.Popover.Popovers.First ()))
                                     .ScreenShot ("PopoverMenu after Show", _out)
                                     .AssertEqual ("Cu_t", app?.Navigation!.GetFocused ()!.Title)
                                     .AssertTrue (app?.Current!.Running)
                                     .EnqueueKeyEvent (Application.QuitKey)
                                     .ScreenShot ($"After {Application.QuitKey}", _out)
                                     .AssertFalse (app?.Popover?.GetActivePopover () is PopoverMenu)
                                     .AssertTrue (app?.Current!.Running);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void Not_Active_DoesNotEat_Space (TestDriver d)
    {
        var spaceKeyDownCount = 0;

        var testView = new View
        {
            CanFocus = true,
            Id = "testView"
        };

        testView.KeyDown += (sender, key) =>
                            {
                                if (key == Key.Space)
                                {
                                    spaceKeyDownCount++;
                                }
                            };

        IApplication? app = null;
        using GuiTestContext c = With.A<Window> (50, 20, d)
                                     .Then ((a) =>
                                            {
                                                app = a;
                                                PopoverMenu popoverMenu = new ()
                                                {
                                                    App = app
                                                };
                                                Toplevel top = app.Current!;
                                                popoverMenu.EnableForDesign (ref top);
                                                app?.Popover!.Register (popoverMenu);
                                            })
                                     .Add (testView)
                                     .Focus (testView)
                                     .EnqueueKeyEvent (Key.Space)
                                     .AssertEqual (1, spaceKeyDownCount);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void Not_Active_DoesNotEat_Enter (TestDriver d)
    {
        var enterKeyDownCount = 0;

        var testView = new View
        {
            CanFocus = true,
            Id = "testView"
        };

        testView.KeyDown += (sender, key) =>
                            {
                                if (key == Key.Enter)
                                {
                                    enterKeyDownCount++;
                                }
                            };

        IApplication? app = null;

        using GuiTestContext c = With.A<Window> (50, 20, d)
                                     .Then ((a) =>
                                            {
                                                app = a;
                                                PopoverMenu popoverMenu = new ()
                                                {
                                                    App = app
                                                };
                                                Toplevel top = app.Current!;
                                                popoverMenu.EnableForDesign (ref top);
                                                app?.Popover!.Register (popoverMenu);
                                            })
                                     .Add (testView)
                                     .Focus (testView)
                                     .EnqueueKeyEvent (Key.Enter)
                                     .AssertEqual (1, enterKeyDownCount);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void Not_Active_DoesNotEat_QuitKey (TestDriver d)
    {
        var quitKeyDownCount = 0;

        var testView = new View
        {
            CanFocus = true,
            Id = "testView"
        };

        testView.KeyDown += (sender, key) =>
                            {
                                if (key == Application.QuitKey)
                                {
                                    quitKeyDownCount++;
                                }
                            };

        IApplication? app = null;
        using GuiTestContext c = With.A<Window> (50, 20, d)
                                     .Then ((a) =>
                                            {
                                                app = a;
                                                PopoverMenu popoverMenu = new ()
                                                {
                                                    App = app
                                                };
                                                Toplevel top = app.Current!;
                                                popoverMenu.EnableForDesign (ref top);
                                                app?.Popover!.Register (popoverMenu);
                                            })
                                     .Add (testView)
                                     .EnqueueKeyEvent (Application.QuitKey)
                                     .AssertEqual (1, quitKeyDownCount);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void ContextMenu_CrashesOnRight (TestDriver d)
    {
        var clicked = false;

        MenuItem [] menuItems = [new ("_New File", string.Empty, () => { clicked = true; })];

        IApplication? app = null;
        using GuiTestContext c = With.A<Window> (40, 10, d, _out)
                                     .Then ((a) => app = a)
                                     .WithContextMenu (new (menuItems) { App = app })
                                     .ScreenShot ("Before open menu", _out)

                                     // Click in main area inside border
                                     .RightClick (1, 1)
                                     .Then ((_) =>
                                            {
                                                // Test depends on menu having a border
                                                IPopover? popover = app?.Popover!.GetActivePopover ();
                                                Assert.NotNull (popover);
                                                var popoverMenu = popover as PopoverMenu;
                                                popoverMenu!.Root!.BorderStyle = LineStyle.Single;
                                            })
                                     .ScreenShot ("After open menu", _out)
                                     .LeftClick (2, 2)
                                     .AssertTrue(clicked);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void ContextMenu_OpenSubmenu (TestDriver d)
    {
        var clicked = false;

        MenuItem [] menuItems =
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

        IApplication? app = null;

        using GuiTestContext c = With.A<Window> (40, 10, d)
                                     .Then ((a) => app = a)
                                     .WithContextMenu (new (menuItems) { App = app })
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
                                     .ScreenShot ("Menu should be closed after selecting", _out);
        Assert.True (clicked);
    }
}
