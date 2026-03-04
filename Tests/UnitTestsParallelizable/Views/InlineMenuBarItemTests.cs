// Claude - Opus 4.6

using Terminal.Gui.Tracing;

namespace ViewsTests;

/// <summary>
///     Tests for <see cref="MenuBarItem"/> with <see cref="MenuBarItem.UsePopoverMenu"/> = <see langword="false"/>
///     (inline mode), verifying the behavior previously provided by the deleted InlineMenuBarItem class.
/// </summary>
public class InlineMenuBarItemTests
{
    [Fact]
    public void UsePopoverMenu_Default_IsTrue ()
    {
        MenuBarItem item = new ();
        Assert.True (item.UsePopoverMenu);

        item.Dispose ();
    }

    [Fact]
    public void UsePopoverMenu_False_AllowsSubMenu ()
    {
        // When UsePopoverMenu = false and menu items are provided, EndInit converts
        // the PopoverMenu to a SubMenu.
        MenuBarItem item = new ("_File", [new MenuItem { Title = "Open" }, new MenuItem { Title = "Save" }])
        {
            UsePopoverMenu = false
        };
        item.BeginInit ();
        item.EndInit ();

        Assert.NotNull (item.SubMenu);
        Assert.Null (item.PopoverMenu);
        Assert.Equal (2, item.SubMenu!.SubViews.Count (v => v is MenuItem));

        item.Dispose ();
    }

    [Fact]
    public void Constructor_Default_InlineMode_HasNoSubMenu ()
    {
        MenuBarItem item = new () { UsePopoverMenu = false };
        Assert.Null (item.SubMenu);
        Assert.Null (item.TargetView);

        item.Dispose ();
    }

    [Fact]
    public void Constructor_WithText_InlineMode_SetsTitle ()
    {
        MenuBarItem item = new ("_File") { UsePopoverMenu = false };
        Assert.Equal ("_File", item.Title);

        item.Dispose ();
    }

    [Fact]
    public void SubMenuGlyph_InlineMode_IsSuppressed ()
    {
        MenuBarItem item = new ("_File", [new MenuItem { Title = "Open" }])
        {
            UsePopoverMenu = false
        };
        item.BeginInit ();
        item.EndInit ();

        // SubMenuGlyph returns default when UsePopoverMenu = false,
        // causing KeyView.Text to be empty (glyph slot removed from layout)
        Assert.Equal (string.Empty, item.KeyView.Text);

        item.Dispose ();
    }

    [Fact]
    public void MenuItem_SubMenuGlyph_IsRightArrow ()
    {
        // Verify the base class still uses RightArrow
        MenuItem item = new ("Open", null, new Menu ([new MenuItem { Title = "Sub" }]));
        Assert.Equal ($"{Glyphs.RightArrow}", item.KeyView.Text);

        item.Dispose ();
    }

    [Fact]
    public void IMenuBarEntry_IsMenuOpen_DefaultFalse ()
    {
        MenuBarItem item = new ("_File", [new MenuItem { Title = "Open" }])
        {
            UsePopoverMenu = false
        };
        item.BeginInit ();
        item.EndInit ();
        IMenuBarEntry entry = item;

        Assert.False (entry.IsMenuOpen);

        item.Dispose ();
    }

    [Fact]
    public void IMenuBarEntry_RootMenu_ReturnsSubMenu ()
    {
        MenuBarItem item = new ("_Edit", [new MenuItem { Title = "Item" }])
        {
            UsePopoverMenu = false
        };
        item.BeginInit ();
        item.EndInit ();
        IMenuBarEntry entry = item;

        Assert.NotNull (entry.RootMenu);
        Assert.Same (item.SubMenu, entry.RootMenu);

        item.Dispose ();
    }

    [Fact]
    public void IMenuBarEntry_RootMenu_NullWhenNoSubMenu ()
    {
        MenuBarItem item = new () { UsePopoverMenu = false };
        IMenuBarEntry entry = item;

        Assert.Null (entry.RootMenu);

        item.Dispose ();
    }

    [Fact]
    public void IMenuBarEntry_IsMenuOpen_TogglesSubMenuVisibility ()
    {
        MenuBarItem item = new ("_File", [new MenuItem { Title = "Open" }])
        {
            UsePopoverMenu = false
        };
        item.BeginInit ();
        item.EndInit ();
        IMenuBarEntry entry = item;

        entry.IsMenuOpen = true;
        Assert.True (item.SubMenu!.Visible);

        entry.IsMenuOpen = false;
        Assert.False (item.SubMenu!.Visible);

        item.Dispose ();
    }

    [Fact]
    public void MenuOpenChanged_FiresOnVisibilityChange ()
    {
        MenuBarItem item = new ("_File", [new MenuItem { Title = "Open" }])
        {
            UsePopoverMenu = false
        };
        item.BeginInit ();
        item.EndInit ();
        item.SubscribeToSubMenuVisibility ();

        var firedCount = 0;
        bool? lastNewValue = null;
        item.MenuOpenChanged += (_, args) =>
        {
            firedCount++;
            lastNewValue = args.NewValue;
        };

        IMenuBarEntry entry = item;
        entry.IsMenuOpen = true;
        Assert.Equal (1, firedCount);
        Assert.True (lastNewValue);

        entry.IsMenuOpen = false;
        Assert.Equal (2, firedCount);
        Assert.False (lastNewValue);

        item.Dispose ();
    }

    [Fact]
    public void Dispose_CleansUpSubMenu ()
    {
        MenuBarItem item = new ("_File", [new MenuItem { Title = "Open" }])
        {
            UsePopoverMenu = false
        };
        item.BeginInit ();
        item.EndInit ();
        item.SubscribeToSubMenuVisibility ();
        item.Dispose ();

        // After dispose, SubMenu should be null (cleaned up by base MenuItem.Dispose)
        Assert.Null (item.SubMenu);
    }

    [Fact]
    public void MenuBarItem_Implements_IMenuBarEntry ()
    {
        MenuBarItem mbi = new ();
        Assert.IsAssignableFrom<IMenuBarEntry> (mbi);

        mbi.Dispose ();
    }

    [Fact]
    public void MenuBarItem_IMenuBarEntry_DelegatesToPopoverMenu ()
    {
        MenuBarItem mbi = new ();
        mbi.EnableForDesign ();
        IMenuBarEntry entry = mbi;

        Assert.False (entry.IsMenuOpen);
        Assert.NotNull (entry.RootMenu);

        mbi.Dispose ();
    }

    [Fact]
    public void MenuBar_Accepts_InlineMenuBarItem ()
    {
        MenuBarItem inlineItem = new ("_Inline", [new MenuItem { Title = "A" }])
        {
            UsePopoverMenu = false
        };
        MenuBarItem popoverItem = new ("_Popover", [new MenuItem { Title = "B" }]);

        MenuBar menuBar = new ([popoverItem, inlineItem]);
        Assert.Equal (2, menuBar.SubViews.Count (v => v is IMenuBarEntry));

        menuBar.Dispose ();
    }

    [Fact]
    public void MenuBar_IsOpen_IncludesInlineMenuBarItem ()
    {
        MenuBarItem inlineItem = new ("_Inline", [new MenuItem { Title = "A" }])
        {
            UsePopoverMenu = false
        };

        MenuBar menuBar = new ([inlineItem]);
        menuBar.BeginInit ();
        menuBar.EndInit ();

        Assert.False (menuBar.IsOpen ());

        IMenuBarEntry entry = inlineItem;
        entry.IsMenuOpen = true;
        Assert.True (menuBar.IsOpen ());

        entry.IsMenuOpen = false;
        Assert.False (menuBar.IsOpen ());

        menuBar.Dispose ();
    }

    [Fact]
    public void MenuBar_GetMenuItemsWith_IncludesInlineItems ()
    {
        MenuBarItem inlineItem = new ("_Inline", [new MenuItem { Title = "Target" }])
        {
            UsePopoverMenu = false
        };

        MenuBar menuBar = new ([inlineItem]);
        menuBar.BeginInit ();
        menuBar.EndInit ();

        IEnumerable<MenuItem> found = menuBar.GetMenuItemsWith (mi => mi.Title == "Target");
        Assert.Single (found);

        menuBar.Dispose ();
    }

    [Fact]
    public void MenuBar_GetMenuItemsWith_MixedMode ()
    {
        MenuBarItem inlineItem = new ("_Inline", [new MenuItem { Title = "InlineChild" }])
        {
            UsePopoverMenu = false
        };
        MenuBarItem popoverItem = new ("_Popover", [new MenuItem { Title = "PopoverChild" }]);

        MenuBar menuBar = new ([popoverItem, inlineItem]);
        menuBar.BeginInit ();
        menuBar.EndInit ();

        IEnumerable<MenuItem> all = menuBar.GetMenuItemsWith (_ => true);
        Assert.Equal (2, all.Count ());

        menuBar.Dispose ();
    }

    [Fact]
    public void OnActivating_Toggles_IsMenuOpen ()
    {
        MenuBarItem item = new ("_File", [new MenuItem { Title = "Open" }])
        {
            UsePopoverMenu = false
        };
        item.BeginInit ();
        item.EndInit ();
        IMenuBarEntry entry = item;

        // First activation should open
        item.InvokeCommand (Command.Activate);
        Assert.True (entry.IsMenuOpen);

        // Second activation should close
        item.InvokeCommand (Command.Activate);
        Assert.False (entry.IsMenuOpen);

        item.Dispose ();
    }

    [Fact]
    public void HideItem_Works_WithIMenuBarEntry ()
    {
        MenuBarItem inlineItem = new ("_Inline", [new MenuItem { Title = "A" }])
        {
            UsePopoverMenu = false
        };

        MenuBar menuBar = new ([inlineItem]);
        menuBar.BeginInit ();
        menuBar.EndInit ();
        menuBar.Active = true;

        IMenuBarEntry entry = inlineItem;
        entry.IsMenuOpen = true;
        Assert.True (menuBar.IsOpen ());

        menuBar.HideItem (entry);
        Assert.False (entry.IsMenuOpen);

        menuBar.Dispose ();
    }

    // Claude - Opus 4.6
    [Fact]
    public void ClickToggle_OpensAndClosesInlineMenu ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuBarItem inlineItem = new ("_Tools",
                                      [
                                          new MenuItem { Title = "_Compile", HelpText = "Build project" },
                                          new MenuItem { Title = "_Run", HelpText = "Execute" }
                                      ])
        {
            UsePopoverMenu = false
        };

        MenuBar menuBar = new ([inlineItem]) { Id = "menuBar" };
        ((View)runnable).Add (menuBar);
        app.Begin (runnable);

        IMenuBarEntry entry = inlineItem;

        // --- First activation: open the menu ---
        app.InjectKey (Key.F9);

        Assert.True (menuBar.Active, "MenuBar should be active after F9.");
        Assert.True (entry.IsMenuOpen, "InlineItem should be open after F9.");

        // --- Second activation: close the menu ---
        app.InjectKey (Key.F9);

        Assert.False (entry.IsMenuOpen, "InlineItem should be closed after second F9.");
        Assert.False (menuBar.Active, "MenuBar should be inactive after second F9.");
    }

    // Claude - Opus 4.6
    [Fact]
    public void ActivateToggle_InMenuBar ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuBarItem inlineItem = new ("_Tools",
                                      [
                                          new MenuItem { Title = "_Compile" }
                                      ])
        {
            UsePopoverMenu = false
        };

        MenuBar menuBar = new ([inlineItem]) { Id = "menuBar" };
        ((View)runnable).Add (menuBar);
        app.Begin (runnable);

        IMenuBarEntry entry = inlineItem;

        // --- Open via direct Activate command ---
        inlineItem.InvokeCommand (Command.Activate);

        Assert.True (entry.IsMenuOpen, "InlineItem should be open after Activate.");

        // --- Close via direct Activate command ---
        inlineItem.InvokeCommand (Command.Activate);

        Assert.False (entry.IsMenuOpen, "InlineItem should be closed after second Activate.");
    }

    // Claude - Opus 4.6
    [Fact]
    public void ArrowRight_PopoverToInline ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        MenuBarItem popoverItem = new ("_File",
                                       [
                                           new MenuItem { Title = "_New" },
                                           new MenuItem { Title = "_Open" }
                                       ]);

        MenuBarItem inlineItem = new ("_Inline",
                                      [
                                          new MenuItem { Title = "_Alpha" },
                                          new MenuItem { Title = "_Beta" }
                                      ])
        {
            UsePopoverMenu = false
        };

        MenuBar menuBar = new ([popoverItem, inlineItem]) { Id = "menuBar" };
        ((View)runnable).Add (menuBar);
        app.Begin (runnable);

        IMenuBarEntry popoverEntry = popoverItem;
        IMenuBarEntry inlineEntry = inlineItem;

        // --- Open popover via F9 ---
        app.InjectKey (Key.F9);

        Assert.True (menuBar.Active, "MenuBar should be active after F9.");
        Assert.True (popoverEntry.IsMenuOpen, "Popover should be open after F9.");

        // --- Arrow right to switch to inline entry ---
        app.InjectKey (Key.CursorRight);

        Assert.True (inlineEntry.IsMenuOpen, "InlineItem should be open after Right.");
        Assert.False (popoverEntry.IsMenuOpen, "Popover should be closed after Right.");
    }

    // Claude - Opus 4.6
    [Fact]
    public void EnterOnMenuItem_InInlineSubMenu ()
    {
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        var actionFiredCount = 0;

        MenuBarItem inlineItem = new ("_Actions",
                                      [
                                          new MenuItem { Title = "_Do Something", Action = () => actionFiredCount++ }
                                      ])
        {
            UsePopoverMenu = false
        };

        MenuBar menuBar = new ([inlineItem]) { Id = "menuBar" };
        ((View)runnable).Add (menuBar);
        app.Begin (runnable);

        IMenuBarEntry entry = inlineItem;

        // --- Open via F9 ---
        app.InjectKey (Key.F9);

        Assert.True (entry.IsMenuOpen, "InlineItem should be open after F9.");

        // --- Press Enter on the focused MenuItem ---
        app.InjectKey (Key.Enter);

        Assert.Equal (1, actionFiredCount);
    }
}
