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

    // Claude - Opus 4.6
    /// <summary>
    ///     Verifies that clicking the "Error" checkbox in the OptionSelector&lt;Schemes&gt;
    ///     inside File → Preferences SubMenu changes Value from Base (0) to Error (4).
    ///     Exercises the full Shortcut → OptionSelector dispatch path inside a PopoverMenu SubMenu.
    /// </summary>
    [Fact]
    public void OptionSelector_In_SubMenu_Click_Sets_Correct_Value ()
    {
        string d = "ansi";
        MenuBar? menuBar = null;
        IApplication? app = null;
        OptionSelector<Schemes>? optionSelector = null;
        Schemes? capturedNewValue = null;
        var valueChangedCount = 0;

        // Step 1: Set up MenuBar with EnableForDesign
        TestContext c = With.A<Window> (80, 30, d, _out)
                            .Then (a =>
                                   {
                                       app = a;
                                       menuBar = new MenuBar ();
                                       View top = app.TopRunnableView!;
                                       top.Add (new View { CanFocus = true, Id = "focusableView" });
                                       menuBar.EnableForDesign (ref top);
                                       app.TopRunnableView!.Add (menuBar);
                                   });

        c = c.WaitIteration ();

        // Step 2: Open the File menu via F9 then verify it's open
        c = c.KeyDown (MenuBar.DefaultKey);
        Assert.True (menuBar!.IsOpen (), "File menu should be open after F9");

        // Step 3: Navigate down to "Preferences" (it's after several items + a Line)
        // File menu items: New, Open, Save, SaveAs, Line, FileOptions, Line, Preferences, Line, Quit
        c = c.KeyDown (Key.CursorDown); // New → Open
        c = c.KeyDown (Key.CursorDown); // Open → Save
        c = c.KeyDown (Key.CursorDown); // Save → SaveAs
        c = c.KeyDown (Key.CursorDown); // SaveAs → (skips Line) → File Options
        c = c.KeyDown (Key.CursorDown); // File Options → (skips Line) → Preferences

        // Step 4: Open the Preferences SubMenu by pressing Right or Enter
        c = c.KeyDown (Key.CursorRight);

        // Step 5: Find the OptionSelector and subscribe to ValueChanged
        c = c.Then (_ =>
                    {
                        // Find the mutuallyExclusiveOptions MenuItem
                        IEnumerable<MenuItem> allMenuItems = menuBar!.GetMenuItemsWith (mi => mi.Id == "mutuallyExclusiveOptions");
                        MenuItem? optionMenuItem = allMenuItems.FirstOrDefault ();
                        Assert.NotNull (optionMenuItem);

                        optionSelector = optionMenuItem!.CommandView as OptionSelector<Schemes>;
                        Assert.NotNull (optionSelector);
                        Assert.Equal (Schemes.Base, optionSelector!.Value);

                        optionSelector.ValueChanged += (_, args) =>
                                                       {
                                                           capturedNewValue = args.Value;
                                                           valueChangedCount++;
                                                       };
                    });

        // Step 6: Click directly on the Error checkbox WITHOUT keyboard navigation first.
        // This simulates the real user scenario where the user opens the menu and clicks
        // directly on a checkbox without using arrow keys.
        CheckBox? errorCheckBox = null;
        var errorScreenX = 0;
        var errorScreenY = 0;

        c = c.Then (_ =>
                    {
                        errorCheckBox = optionSelector!.SubViews.OfType<CheckBox> ()
                                                       .FirstOrDefault (cb => (int)cb.Data! == (int)Schemes.Error);
                        Assert.NotNull (errorCheckBox);
                        System.Drawing.Point pos = errorCheckBox!.FrameToScreen ().Location;
                        errorScreenX = pos.X;
                        errorScreenY = pos.Y;
                    });

        c = c.LeftClick (errorScreenX, errorScreenY);

        c = c.ScreenShot ("After clicking Error (full driver sequence)", _out);

        // Assert — Value should change from Base (0) to Error (4), not to Menu (1)
        Assert.Equal (Schemes.Error, optionSelector!.Value);

        c.Dispose ();
    }

    // Claude - Opus 4.6
    /// <summary>
    ///     Simpler variant: OptionSelector directly in the root Menu (no SubMenu nesting).
    ///     Isolates whether the bug is SubMenu-specific or general to PopoverMenu.
    /// </summary>
    [Fact]
    public void OptionSelector_In_RootMenu_Space_Sets_Correct_Value ()
    {
        string d = "ansi";
        MenuBar? menuBar = null;
        IApplication? app = null;
        OptionSelector<Schemes>? optionSelector = null;
        Schemes? capturedNewValue = null;
        var valueChangedCount = 0;

        // Step 1: Build a simple MenuBar with an OptionSelector directly in the root Menu
        TestContext c = With.A<Window> (80, 30, d, _out)
                            .Then (a =>
                                   {
                                       app = a;

                                       optionSelector = new OptionSelector<Schemes> { Title = "Scheme", CanFocus = true };

                                       menuBar = new MenuBar
                                       {
                                           Menus =
                                           [
                                               new MenuBarItem ("_Test",
                                                                [
                                                                    new MenuItem
                                                                    {
                                                                        Id = "selectorItem",
                                                                        HelpText = "Pick a scheme",
                                                                        CommandView = optionSelector
                                                                    }
                                                                ])
                                           ]
                                       };

                                       app.TopRunnableView!.Add (menuBar);

                                       optionSelector.ValueChanged += (_, args) =>
                                                                      {
                                                                          capturedNewValue = args.Value;
                                                                          valueChangedCount++;
                                                                      };
                                   });

        c = c.WaitIteration ();

        // Step 2: Open the Test menu
        c = c.KeyDown (MenuBar.DefaultKey);
        Assert.True (menuBar!.IsOpen (), "Menu should be open after F9");

        c = c.ScreenShot ("After F9 - menu open", _out);

        // Step 3: Navigate within the OptionSelector to Error
        // Items: Base(0), Menu(1), Dialog(2), Runnable(3), Error(4)
        c = c.KeyDown (Key.CursorDown); // Base → Menu
        c = c.KeyDown (Key.CursorDown); // Menu → Dialog
        c = c.KeyDown (Key.CursorDown); // Dialog → Runnable
        c = c.KeyDown (Key.CursorDown); // Runnable → Error

        c = c.ScreenShot ("After navigating to Error", _out);

        // Step 4: Press Space to activate
        c = c.KeyDown (Key.Space);

        c = c.ScreenShot ("After Space on Error", _out);

        // Step 5: Assert
        Assert.True (valueChangedCount >= 1, $"ValueChanged should fire (fired {valueChangedCount} times)");
        Assert.Equal (Schemes.Error, capturedNewValue);
        Assert.Equal (Schemes.Error, optionSelector!.Value);

        c.Dispose ();
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
