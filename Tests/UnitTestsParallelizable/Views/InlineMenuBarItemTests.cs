// Claude - Opus 4.6

using Terminal.Gui.Tracing;

namespace ViewsTests;

public class InlineMenuBarItemTests
{
    [Fact]
    public void Constructor_Default_HasNoSubMenu ()
    {
        InlineMenuBarItem item = new ();
        Assert.Null (item.SubMenu);
        Assert.Null (item.TargetView);

        item.Dispose ();
    }

    [Fact]
    public void Constructor_WithText_SetsTitle ()
    {
        InlineMenuBarItem item = new ("_File");
        Assert.Equal ("_File", item.Title);
        Assert.Null (item.SubMenu);

        item.Dispose ();
    }

    [Fact]
    public void Constructor_WithMenuItems_CreatesSubMenu ()
    {
        InlineMenuBarItem item = new ("_File", [new MenuItem { Title = "Open" }, new MenuItem { Title = "Save" }]);
        Assert.NotNull (item.SubMenu);
        Assert.Equal (2, item.SubMenu!.SubViews.Count (v => v is MenuItem));

        item.Dispose ();
    }

    [Fact]
    public void Constructor_WithMenu_SetsSubMenu ()
    {
        Menu subMenu = new ([new MenuItem { Title = "Item1" }]);
        InlineMenuBarItem item = new ("_Edit", subMenu);
        Assert.Same (subMenu, item.SubMenu);

        item.Dispose ();
    }

    [Fact]
    public void SubMenuGlyph_IsDownArrow ()
    {
        InlineMenuBarItem item = new ("_File", [new MenuItem { Title = "Open" }]);

        // SubMenuGlyph is protected, but it manifests through KeyView.Text when SubMenu is set
        Assert.Equal ($"{Glyphs.DownArrow}", item.KeyView.Text);

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
        InlineMenuBarItem item = new ("_File", [new MenuItem { Title = "Open" }]);
        IMenuBarEntry entry = item;

        Assert.False (entry.IsMenuOpen);

        item.Dispose ();
    }

    [Fact]
    public void IMenuBarEntry_RootMenu_ReturnsSubMenu ()
    {
        Menu subMenu = new ([new MenuItem { Title = "Item" }]);
        InlineMenuBarItem item = new ("_Edit", subMenu);
        IMenuBarEntry entry = item;

        Assert.Same (subMenu, entry.RootMenu);

        item.Dispose ();
    }

    [Fact]
    public void IMenuBarEntry_RootMenu_NullWhenNoSubMenu ()
    {
        InlineMenuBarItem item = new ();
        IMenuBarEntry entry = item;

        Assert.Null (entry.RootMenu);

        item.Dispose ();
    }

    [Fact]
    public void IMenuBarEntry_IsMenuOpen_TogglesSubMenuVisibility ()
    {
        InlineMenuBarItem item = new ("_File", [new MenuItem { Title = "Open" }]);
        item.SubMenu!.BeginInit ();
        item.SubMenu!.EndInit ();
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
        InlineMenuBarItem item = new ("_File", [new MenuItem { Title = "Open" }]);
        item.SubMenu!.BeginInit ();
        item.SubMenu!.EndInit ();
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
    public void EnableForDesign_CreatesSubMenu ()
    {
        InlineMenuBarItem item = new ();
        bool result = item.EnableForDesign ();

        Assert.True (result);
        Assert.NotNull (item.SubMenu);

        item.Dispose ();
    }

    [Fact]
    public void Dispose_CleansUpSubMenu ()
    {
        InlineMenuBarItem item = new ("_File", [new MenuItem { Title = "Open" }]);
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
        InlineMenuBarItem inlineItem = new ("_Inline", [new MenuItem { Title = "A" }]);
        MenuBarItem popoverItem = new ("_Popover", [new MenuItem { Title = "B" }]);

        MenuBar menuBar = new ([popoverItem, inlineItem]);
        Assert.Equal (2, menuBar.SubViews.Count (v => v is IMenuBarEntry));

        menuBar.Dispose ();
    }

    [Fact]
    public void MenuBar_IsOpen_IncludesInlineMenuBarItem ()
    {
        InlineMenuBarItem inlineItem = new ("_Inline", [new MenuItem { Title = "A" }]);
        inlineItem.SubMenu!.BeginInit ();
        inlineItem.SubMenu!.EndInit ();
        inlineItem.SubscribeToSubMenuVisibility ();

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
        InlineMenuBarItem inlineItem = new ("_Inline", [new MenuItem { Title = "Target" }]);

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
        InlineMenuBarItem inlineItem = new ("_Inline", [new MenuItem { Title = "InlineChild" }]);
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
        InlineMenuBarItem item = new ("_File", [new MenuItem { Title = "Open" }]);
        item.SubMenu!.BeginInit ();
        item.SubMenu!.EndInit ();
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
        InlineMenuBarItem inlineItem = new ("_Inline", [new MenuItem { Title = "A" }]);
        inlineItem.SubMenu!.BeginInit ();
        inlineItem.SubMenu!.EndInit ();
        inlineItem.SubscribeToSubMenuVisibility ();

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
    public void Diagnostic_ClickToggle_TraceCapture ()
    {
        ListBackend traceBackend = new ();
        Trace.Backend = traceBackend;
        Trace.EnabledCategories = TraceCategory.Command | TraceCategory.Keyboard;

        try
        {
            VirtualTimeProvider time = new ();
            using IApplication app = Application.Create (time);
            app.Init (DriverRegistry.Names.ANSI);
            IRunnable runnable = new Runnable ();

            InlineMenuBarItem inlineItem = new ("_Tools",
                                                [
                                                    new MenuItem { Title = "_Compile", HelpText = "Build project" },
                                                    new MenuItem { Title = "_Run", HelpText = "Execute" }
                                                ]);

            MenuBar menuBar = new ([inlineItem]) { Id = "menuBar" };
            ((View)runnable).Add (menuBar);
            app.Begin (runnable);

            IMenuBarEntry entry = inlineItem;

            // --- First activation: open the menu ---
            traceBackend.Clear ();
            app.InjectKey (Key.F9);

            string openTraces = FormatTraces (traceBackend);

            Assert.True (menuBar.Active, $"MenuBar should be active after F9.\nTraces:\n{openTraces}");
            Assert.True (entry.IsMenuOpen, $"InlineItem should be open after F9.\nTraces:\n{openTraces}");

            // --- Second activation: close the menu ---
            traceBackend.Clear ();
            app.InjectKey (Key.F9);

            string closeTraces = FormatTraces (traceBackend);

            Assert.False (entry.IsMenuOpen, $"InlineItem should be closed after second F9.\nTraces:\n{closeTraces}");
            Assert.False (menuBar.Active, $"MenuBar should be inactive after second F9.\nTraces:\n{closeTraces}");
        }
        finally
        {
            Trace.EnabledCategories = TraceCategory.None;
            Trace.Backend = new NullBackend ();
        }
    }

    // Claude - Opus 4.6
    [Fact]
    public void Diagnostic_ActivateToggle_InMenuBar_TraceCapture ()
    {
        ListBackend traceBackend = new ();
        Trace.Backend = traceBackend;
        Trace.EnabledCategories = TraceCategory.Command;

        try
        {
            VirtualTimeProvider time = new ();
            using IApplication app = Application.Create (time);
            app.Init (DriverRegistry.Names.ANSI);
            IRunnable runnable = new Runnable ();

            InlineMenuBarItem inlineItem = new ("_Tools",
                                                [
                                                    new MenuItem { Title = "_Compile" }
                                                ]);

            MenuBar menuBar = new ([inlineItem]) { Id = "menuBar" };
            ((View)runnable).Add (menuBar);
            app.Begin (runnable);

            IMenuBarEntry entry = inlineItem;

            // --- Open via direct Activate command ---
            traceBackend.Clear ();
            inlineItem.InvokeCommand (Command.Activate);

            string openTraces = FormatTraces (traceBackend);

            Assert.True (entry.IsMenuOpen, $"InlineItem should be open after Activate.\nTraces:\n{openTraces}");

            // --- Close via direct Activate command ---
            traceBackend.Clear ();
            inlineItem.InvokeCommand (Command.Activate);

            string closeTraces = FormatTraces (traceBackend);

            Assert.False (entry.IsMenuOpen, $"InlineItem should be closed after second Activate.\nTraces:\n{closeTraces}");
        }
        finally
        {
            Trace.EnabledCategories = TraceCategory.None;
            Trace.Backend = new NullBackend ();
        }
    }

    // Claude - Opus 4.6
    [Fact]
    public void Diagnostic_ArrowRight_PopoverToInline_TraceCapture ()
    {
        ListBackend traceBackend = new ();
        Trace.Backend = traceBackend;
        Trace.EnabledCategories = TraceCategory.Command | TraceCategory.Keyboard;

        try
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

            InlineMenuBarItem inlineItem = new ("_Inline",
                                                [
                                                    new MenuItem { Title = "_Alpha" },
                                                    new MenuItem { Title = "_Beta" }
                                                ]);

            MenuBar menuBar = new ([popoverItem, inlineItem]) { Id = "menuBar" };
            ((View)runnable).Add (menuBar);
            app.Begin (runnable);

            IMenuBarEntry popoverEntry = popoverItem;
            IMenuBarEntry inlineEntry = inlineItem;

            // --- Open popover via F9 ---
            traceBackend.Clear ();
            app.InjectKey (Key.F9);

            string openTraces = FormatTraces (traceBackend);
            Assert.True (menuBar.Active, $"MenuBar should be active after F9.\nTraces:\n{openTraces}");
            Assert.True (popoverEntry.IsMenuOpen, $"Popover should be open after F9.\nTraces:\n{openTraces}");

            // --- Arrow right to switch to inline entry ---
            traceBackend.Clear ();
            app.InjectKey (Key.CursorRight);

            string switchTraces = FormatTraces (traceBackend);
            Assert.True (inlineEntry.IsMenuOpen, $"InlineItem should be open after Right.\nTraces:\n{switchTraces}");
            Assert.False (popoverEntry.IsMenuOpen, $"Popover should be closed after Right.\nTraces:\n{switchTraces}");
        }
        finally
        {
            Trace.EnabledCategories = TraceCategory.None;
            Trace.Backend = new NullBackend ();
        }
    }

    // Claude - Opus 4.6
    [Fact]
    public void Diagnostic_EnterOnMenuItem_InInlineSubMenu_TraceCapture ()
    {
        ListBackend traceBackend = new ();
        Trace.Backend = traceBackend;
        Trace.EnabledCategories = TraceCategory.Command | TraceCategory.Keyboard;

        try
        {
            VirtualTimeProvider time = new ();
            using IApplication app = Application.Create (time);
            app.Init (DriverRegistry.Names.ANSI);
            IRunnable runnable = new Runnable ();

            var actionFiredCount = 0;

            InlineMenuBarItem inlineItem = new ("_Actions",
                                                [
                                                    new MenuItem { Title = "_Do Something", Action = () => actionFiredCount++ }
                                                ]);

            MenuBar menuBar = new ([inlineItem]) { Id = "menuBar" };
            ((View)runnable).Add (menuBar);
            app.Begin (runnable);

            IMenuBarEntry entry = inlineItem;

            // --- Open via F9 ---
            traceBackend.Clear ();
            app.InjectKey (Key.F9);

            string openTraces = FormatTraces (traceBackend);
            Assert.True (entry.IsMenuOpen, $"InlineItem should be open after F9.\nTraces:\n{openTraces}");

            // Check what has focus after opening
            string focusInfo = $"MenuBar.Focused={menuBar.Focused?.ToIdentifyingString ()}, SubMenu.Focused={inlineItem.SubMenu?.Focused?.ToIdentifyingString ()}, SubMenu.Visible={inlineItem.SubMenu?.Visible}";

            // --- Press Enter on the focused MenuItem ---
            traceBackend.Clear ();
            app.InjectKey (Key.Enter);

            string enterTraces = FormatTraces (traceBackend);
            Assert.True (actionFiredCount == 1, $"Action should have fired after Enter. Count={actionFiredCount}\nFocus before Enter: {focusInfo}\nTraces:\n{enterTraces}");
        }
        finally
        {
            Trace.EnabledCategories = TraceCategory.None;
            Trace.Backend = new NullBackend ();
        }
    }

    private static string FormatTraces (ListBackend backend) =>
        string.Join ("\n",
                     backend.Entries.Select (e =>
                                             {
                                                 string dataStr = e.Data switch
                                                                  {
                                                                      (Command cmd, CommandRouting routing) => $"Cmd={cmd} Routing={routing}",
                                                                      Key key => $"Key={key}",
                                                                      ICommandContext ctx => $"Cmd={ctx.Command} Routing={ctx.Routing}",
                                                                      _ => e.Data?.ToString () ?? ""
                                                                  };

                                                 return $"  [{e.Category}:{e.Phase}] {e.Id} ({e.Method}) {e.Message} [{dataStr}]";
                                             }));
}
