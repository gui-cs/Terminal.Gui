using System.Globalization;
using TerminalGuiFluentTesting;
using TerminalGuiFluentTestingXunit;
using Xunit.Abstractions;

namespace IntegrationTests;

/// <summary>
///     Tests for the MenuBar class
/// </summary>
public class MenuBarTests : TestsAllDrivers
{
    private readonly TextWriter _out;

    public MenuBarTests (ITestOutputHelper outputHelper)
    {
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        _out = new TestOutputWriter (outputHelper);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void Initializes_WithNoItems (string d)
    {
        using TestContext c = With.A<Window> (80, 25, d, _out)
                                  .Then (_ =>
                                         {
                                             // Create a menu bar with no items
                                             var menuBar = new MenuBar ();
                                             Assert.Empty (menuBar.SubViews);
                                             Assert.False (menuBar.CanFocus);
                                             Assert.Equal (Orientation.Horizontal, menuBar.Orientation);
                                             Assert.Equal (Key.F9, MenuBar.DefaultKey);
                                         });
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void Initializes_WithItems (string d)
    {
        MenuBarItem [] menuItems = [];

        using TestContext c = With.A<Window> (80, 25, d, _out)
                                  .Then (_ =>
                                         {
                                             // Create items for the menu bar
                                             menuItems =
                                             [
                                                 new MenuBarItem (Strings.menuFile, [new MenuItem (Strings.cmdOpen, "Opens a file", () => { })]),
                                                 new MenuBarItem ("_Edit", [new MenuItem (Strings.cmdCopy, "Copies selection", () => { })])
                                             ];

                                             var menuBar = new MenuBar (menuItems);
                                             Assert.Equal (2, menuBar.SubViews.Count);

                                             // First item should be the File menu
                                             var fileMenu = menuBar.SubViews.ElementAt (0) as MenuBarItem;
                                             Assert.NotNull (fileMenu);
                                             Assert.Equal (Strings.menuFile, fileMenu.Title);

                                             // Second item should be the Edit menu
                                             var editMenu = menuBar.SubViews.ElementAt (1) as MenuBarItem;
                                             Assert.NotNull (editMenu);
                                             Assert.Equal ("_Edit", editMenu.Title);
                                         });
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void AddsItems_WithMenusProperty (string d)
    {
        using TestContext c = With.A<Window> (80, 25, d, _out)
                                  .Then (_ =>
                                         {
                                             var menuBar = new MenuBar ();

                                             // Set items through Menus property
                                             menuBar.Menus = [new MenuBarItem (Strings.menuFile), new MenuBarItem ("_Edit"), new MenuBarItem ("_View")];

                                             Assert.Equal (3, menuBar.SubViews.Count);
                                         });
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void ChangesKey_RaisesEvent (string d)
    {
        using TestContext c = With.A<Window> (80, 25, d, _out)
                                  .Then (_ =>
                                         {
                                             var menuBar = new MenuBar ();

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
    [MemberData (nameof (GetAllDriverNames))]
    public void DefaultKey_Activates (string d)
    {
        MenuBar? menuBar = null;
        View? top = null;

        using TestContext c = With.A<Window> (50, 20, d, _out)
                                  .Then (app =>
                                         {
                                             menuBar = new MenuBar ();
                                             top = app.TopRunnableView!;

                                             top.Add (new View { CanFocus = true, Id = "focusableView" });
                                             menuBar.EnableForDesign (ref top);
                                             app.TopRunnableView!.Add (menuBar);
                                         })
                                  .WaitIteration ()
                                  .AssertIsNotType<MenuItem> (top?.App?.Navigation!.GetFocused ())
                                  .ScreenShot ("MenuBar initial state", _out)
                                  .KeyDown (MenuBar.DefaultKey)
                                  .WaitIteration ()
                                  .ScreenShot ($"After {MenuBar.DefaultKey}", _out)
                                  .AssertEqual ("_New", top?.App?.Navigation!.GetFocused ()!.Title)
                                  .AssertTrue (top?.App?.Popover?.GetActivePopover () is PopoverMenu)
                                  .AssertTrue (menuBar?.IsOpen ());
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void DefaultKey_DeActivates (string d)
    {
        MenuBar? menuBar = null;
        IApplication? app = null;

        using TestContext c = With.A<Window> (50, 20, d, _out)
                                  .Then (a =>
                                         {
                                             app = a;
                                             menuBar = new MenuBar ();
                                             View top = app.TopRunnableView!;

                                             top.Add (new View { CanFocus = true, Id = "focusableView" });
                                             menuBar.EnableForDesign (ref top);
                                             app.TopRunnableView!.Add (menuBar);
                                         })
                                  .WaitIteration ()
                                  .AssertIsNotType<MenuItem> (app?.Navigation!.GetFocused ())
                                  .ScreenShot ("MenuBar initial state", _out)
                                  .KeyDown (MenuBar.DefaultKey)
                                  .ScreenShot ($"After {MenuBar.DefaultKey}", _out)
                                  .AssertEqual ("_New", app?.Navigation!.GetFocused ()!.Title)
                                  .KeyDown (MenuBar.DefaultKey)
                                  .ScreenShot ($"After {MenuBar.DefaultKey}", _out)
                                  .AssertIsNotType<MenuItem> (app?.Navigation!.GetFocused ());
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void EnableForDesign_CreatesMenuItems (string d)
    {
        using TestContext c = With.A<Window> (80, 25, d, _out)
                                  .Then (app =>
                                         {
                                             var menuBar = new MenuBar ();
                                             app.TopRunnableView!.Add (menuBar);

                                             // Call EnableForDesign
                                             View top = app.TopRunnableView!;
                                             bool result = menuBar.EnableForDesign (ref top);

                                             // Should return true
                                             Assert.True (result);

                                             // Should have created menu items
                                             Assert.True (menuBar.SubViews.Count > 0);

                                             // Should have File, Edit and Help menus
                                             View? fileMenu = menuBar.SubViews.FirstOrDefault (v => (v as MenuBarItem)?.Title == Strings.menuFile);
                                             View? editMenu = menuBar.SubViews.FirstOrDefault (v => (v as MenuBarItem)?.Title == "_Edit");
                                             View? helpMenu = menuBar.SubViews.FirstOrDefault (v => (v as MenuBarItem)?.Title == Strings.menuHelp);

                                             Assert.NotNull (fileMenu);
                                             Assert.NotNull (editMenu);
                                             Assert.NotNull (helpMenu);
                                         })
                                  .ScreenShot ("MenuBar EnableForDesign", _out);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void Navigation_Left_Right_Wraps (string d)
    {
        MenuBar? menuBar = null;
        IApplication? app = null;

        using TestContext c = With.A<Window> (50, 20, d, _out)
                                  .Then (a =>
                                         {
                                             app = a;
                                             menuBar = new MenuBar ();
                                             View top = app.TopRunnableView!;
                                             menuBar.EnableForDesign (ref top);
                                             app.TopRunnableView!.Add (menuBar);
                                         })
                                  .WaitIteration ()
                                  .ScreenShot ("MenuBar initial state", _out)
                                  .KeyDown (MenuBar.DefaultKey)
                                  .AssertTrue (app?.Popover?.GetActivePopover () is PopoverMenu)
                                  .AssertTrue (menuBar?.IsOpen ())
                                  .AssertEqual ("_New", app?.Navigation?.GetFocused ()!.Title)
                                  .ScreenShot ($"After {MenuBar.DefaultKey}", _out)
                                  .KeyDown (Key.CursorRight)
                                  .AssertTrue (app?.Popover?.GetActivePopover () is PopoverMenu)
                                  .ScreenShot ("After right arrow", _out)
                                  .AssertEqual ("Cu_t", app?.Navigation?.GetFocused ()!.Title)
                                  .KeyDown (Key.CursorRight)
                                  .ScreenShot ("After second right arrow", _out)
                                  .AssertEqual ("_Online Help...", app?.Navigation?.GetFocused ()!.Title)
                                  .ScreenShot ("After third right arrow", _out)
                                  .KeyDown (Key.CursorRight)
                                  .ScreenShot ("After fourth right arrow", _out)
                                  .AssertEqual ("_New", app?.Navigation?.GetFocused ()!.Title)
                                  .KeyDown (Key.CursorLeft)
                                  .ScreenShot ("After left arrow", _out)
                                  .AssertEqual ("_Online Help...", app?.Navigation?.GetFocused ()!.Title);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void MenuBarItem_With_QuitKey_Open_QuitKey_Restores_Focus_Correctly (string d)
    {
        MenuBar? menuBar = null;
        IApplication? app = null;

        using TestContext c = With.A<Window> (50, 20, d, _out)
                                  .Then (a =>
                                         {
                                             app = a;
                                             menuBar = new MenuBar ();
                                             View top = app.TopRunnableView!;

                                             top.Add (new View { CanFocus = true, Id = "focusableView" });
                                             menuBar.EnableForDesign (ref top);
                                             app.TopRunnableView!.Add (menuBar);
                                         })
                                  .AssertIsNotType<MenuItem> (app!.Navigation!.GetFocused ())
                                  .ScreenShot ("MenuBar initial state", _out)
                                  .KeyDown (MenuBar.DefaultKey)
                                  .AssertEqual ("_New", app.Navigation!.GetFocused ()!.Title)
                                  .AssertTrue (app?.Popover?.GetActivePopover () is PopoverMenu)
                                  .AssertTrue (menuBar?.IsOpen ())
                                  .AssertEqual ("_New", app?.Navigation?.GetFocused ()!.Title)
                                  .ScreenShot ($"After {MenuBar.DefaultKey}", _out)
                                  .KeyDown (Application.QuitKey)
                                  .AssertFalse (app?.Popover?.GetActivePopover () is PopoverMenu)
                                  .AssertIsNotType<MenuItem> (app!.Navigation!.GetFocused ());
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void MenuBarItem_Without_QuitKey_Open_QuitKey_Restores_Focus_Correctly (string d)
    {
        MenuBar? menuBar = null;
        IApplication? app = null;

        using TestContext c = With.A<Window> (50, 20, d, _out)
                                  .Add (new View { CanFocus = true, Id = "focusableView" })
                                  .Then (a =>
                                         {
                                             app = a;
                                             menuBar = new MenuBar ();
                                             View? runnable = app.TopRunnableView;
                                             menuBar.EnableForDesign (ref runnable!);
                                             app.TopRunnableView!.Add (menuBar);
                                         })
                                  .WaitIteration ()
                                  .AssertIsNotType<MenuItem> (app?.Navigation!.GetFocused ())
                                  .ScreenShot ("MenuBar initial state", _out)
                                  .KeyDown (MenuBar.DefaultKey)
                                  .KeyDown (Key.CursorRight)
                                  .AssertEqual ("Cu_t", app?.Navigation!.GetFocused ()!.Title)
                                  .AssertTrue (app?.Popover?.GetActivePopover () is PopoverMenu)
                                  .AssertTrue (menuBar?.IsOpen ())
                                  .AssertEqual ("Cu_t", app?.Navigation?.GetFocused ()!.Title)
                                  .ScreenShot ($"After {MenuBar.DefaultKey}", _out)
                                  .KeyDown (Application.QuitKey)
                                  .AssertFalse (app?.Popover?.GetActivePopover () is PopoverMenu)
                                  .AssertIsNotType<MenuItem> (app?.Navigation?.GetFocused ());
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void MenuBarItem_With_QuitKey_Open_QuitKey_Does_Not_Quit_App (string d)
    {
        MenuBar? menuBar = null;
        IApplication? app = null;

        using TestContext c = With.A<Window> (50, 20, d, _out)
                                  .Then (a =>
                                         {
                                             app = a;
                                             menuBar = new MenuBar ();
                                             View top = app.TopRunnableView!;

                                             top.Add (new View { CanFocus = true, Id = "focusableView" });
                                             menuBar.EnableForDesign (ref top);
                                             app.TopRunnableView!.Add (menuBar);
                                         })
                                  .WaitIteration ()
                                  .AssertIsNotType<MenuItem> (app!.Navigation!.GetFocused ())
                                  .ScreenShot ("MenuBar initial state", _out)
                                  .KeyDown (MenuBar.DefaultKey)
                                  .AssertEqual ("_New", app.Navigation!.GetFocused ()!.Title)
                                  .AssertTrue (app?.TopRunnable!.IsRunning)
                                  .ScreenShot ($"After {MenuBar.DefaultKey}", _out)
                                  .KeyDown (Application.QuitKey)
                                  .AssertFalse (app?.Popover?.GetActivePopover () is PopoverMenu)
                                  .AssertTrue (app!.TopRunnable!.IsRunning);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void MenuBarItem_Without_QuitKey_Open_QuitKey_Does_Not_Quit_MenuBar_SuperView (string d)
    {
        MenuBar? menuBar = null;
        IApplication? app = null;

        using TestContext c = With.A<Window> (50, 20, d, _out)
                                  .Then (a =>
                                         {
                                             app = a;
                                             menuBar = new MenuBar ();
                                             View top = app.TopRunnableView!;

                                             top.Add (new View { CanFocus = true, Id = "focusableView" });
                                             menuBar.EnableForDesign (ref top);
                                             IEnumerable<MenuItem> items = menuBar.GetMenuItemsWith (mi => mi.Title == Strings.cmdQuit);

                                             foreach (MenuItem item in items)
                                             {
                                                 item.Key = Key.Empty;
                                             }

                                             app.TopRunnableView!.Add (menuBar);
                                         })
                                  .WaitIteration ()
                                  .AssertIsNotType<MenuItem> (app?.Navigation!.GetFocused ())
                                  .ScreenShot ("MenuBar initial state", _out)
                                  .KeyDown (MenuBar.DefaultKey)
                                  .AssertEqual ("_New", app?.Navigation!.GetFocused ()!.Title)
                                  .ScreenShot ($"After {MenuBar.DefaultKey}", _out)
                                  .KeyDown (Application.QuitKey)
                                  .AssertFalse (app?.Popover?.GetActivePopover () is PopoverMenu)
                                  .AssertTrue (app?.TopRunnable!.IsRunning);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void MenuBar_Not_Active_DoesNotEat_Space (string d)
    {
        var spaceKeyDownCount = 0;
        var testView = new View { CanFocus = true, Id = "testView" };

        testView.KeyDown += (sender, key) =>
                            {
                                if (key == Key.Space)
                                {
                                    spaceKeyDownCount++;
                                }
                            };

        using TestContext c = With.A<Window> (50, 20, d, _out)
                                  .Then (a =>
                                         {
                                             var menuBar = new MenuBar ();
                                             View top = a.TopRunnableView!;
                                             menuBar.EnableForDesign (ref top);
                                             a.TopRunnableView!.Add (menuBar);
                                         })
                                  .Add (testView)
                                  .WaitIteration ()
                                  .Focus (testView)
                                  .KeyDown (Key.Space)
                                  .AssertEqual (1, spaceKeyDownCount);
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void MenuBar_Not_Active_DoesNotEat_Enter (string d)
    {
        var enterKeyDownCount = 0;
        var testView = new View { CanFocus = true, Id = "testView" };

        testView.KeyDown += (sender, key) =>
                            {
                                if (key == Key.Enter)
                                {
                                    enterKeyDownCount++;
                                }
                            };

        using TestContext c = With.A<Window> (50, 20, d, _out)
                                  .Then (a =>
                                         {
                                             var menuBar = new MenuBar ();
                                             View top = a.TopRunnableView!;
                                             menuBar.EnableForDesign (ref top);
                                             a.TopRunnableView!.Add (menuBar);
                                         })
                                  .Add (testView)
                                  .WaitIteration ()
                                  .Focus (testView)
                                  .KeyDown (Key.Enter)
                                  .AssertEqual (1, enterKeyDownCount);
    }
}
