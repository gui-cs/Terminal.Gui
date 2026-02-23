using System.ComponentModel;
using Terminal.Gui.Tracing;

namespace Terminal.Gui.Views;

/// <summary>
///     A horizontal list of <see cref="MenuBarItem"/>s. Each <see cref="MenuBarItem"/> can have a
///     <see cref="PopoverMenu"/> that is shown when the <see cref="MenuBarItem"/> is selected.
/// </summary>
/// <remarks>
///     MenuBars may be hosted by any View and will, by default, be positioned the full width across the top of the View's
///     Viewport.
/// </remarks>
public class MenuBar : Menu, IDesignable
{
    /// <inheritdoc/>
    public MenuBar () : this ([]) { }

    /// <inheritdoc/>
    public MenuBar (IEnumerable<MenuBarItem> menuBarItems) : base (menuBarItems)
    {
        CanFocus = false;
        TabStop = TabBehavior.TabGroup;
        Y = 0;
        Width = Dim.Fill ();
        Height = Dim.Auto ();
        Orientation = Orientation.Horizontal;

        Key = DefaultKey;

        // If we're not focused, Key activates/deactivates
        HotKeyBindings.Add (Key, Command.HotKey);

        KeyBindings.Add (Key, Command.Quit);
        KeyBindings.ReplaceCommands (Application.QuitKey, Command.Quit);

        AddCommand (Command.Quit,
                    ctx =>
                    {
                        _popoverBrowsingMode = false;

                        if (HideActiveItem ())
                        {
                            return true;
                        }

                        if (!CanFocus)
                        {
                            return false;
                        }
                        CanFocus = false;
                        Active = false;

                        return true;
                    });

        AddCommand (Command.Right, MoveRight);
        KeyBindings.Add (Key.CursorRight, Command.Right);

        AddCommand (Command.Left, MoveLeft);
        KeyBindings.Add (Key.CursorLeft, Command.Left);

        BorderStyle = DefaultBorderStyle;

        ConfigurationManager.Applied += OnConfigurationManagerApplied;

        return;

        bool? MoveLeft (ICommandContext? ctx)
        {
            _isSwitchingItem = true;

            try
            {
                return AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabStop);
            }
            finally
            {
                _isSwitchingItem = false;
            }
        }

        bool? MoveRight (ICommandContext? ctx)
        {
            _isSwitchingItem = true;

            try
            {
                return AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
            }
            finally
            {
                _isSwitchingItem = false;
            }
        }
    }

    /// <inheritdoc/>
    protected override View? GetDispatchTarget (ICommandContext? ctx) => Focused;

    /// <inheritdoc/>
    protected override bool ConsumeDispatch => true;

    /// <inheritdoc/>
    protected override bool OnActivating (CommandEventArgs args)
    {
        Trace.Command (this, args.Context, "Entry", $"Visible={Visible} Enabled={Enabled} Active={Active}");

        if (!Visible || !Enabled)
        {
            Trace.Command (this, args.Context, "Blocked", "Invisible or disabled");

            return true; // Block activation when invisible or disabled
        }

        // When a MenuBarItem's activation bubbles up, activate the MenuBar and show that item.
        if (args.Context?.Routing == CommandRouting.BubblingUp)
        {
            if (!args.Context.TryGetSource (out View? source) || FindMenuBarItemForSource (source) is not { } sourceMbi)
            {
                Trace.Command (this, args.Context, "BubblingUp", "Source not found or not a MenuBarItem");

                return false;
            }

            Trace.Command (this, args.Context, "BubblingUp", $"Source={sourceMbi.ToIdentifyingString ()} PopoverMenuOpen={sourceMbi.PopoverMenuOpen}");

            if (sourceMbi.PopoverMenuOpen)
            {
                // Guard against intermediate focus traversal: setting Active=true
                // causes CanFocus=true which can focus the first MenuBarItem transiently.
                // _isSwitchingItem prevents OnSelectedMenuItemChanged from auto-opening
                // that intermediate item.
                _isSwitchingItem = true;

                try
                {
                    // MenuBarItem just opened its popover — ensure MenuBar is active
                    if (!Active)
                    {
                        Active = true;
                    }

                    ShowItem (sourceMbi);
                }
                finally
                {
                    _isSwitchingItem = false;
                }
            }
            else
            {
                // MenuBarItem just closed its popover — deactivate MenuBar
                Active = false;
            }

            return true;

            // Non-MenuBarItem SubView bubbling — let normal bubbling proceed.
        }

        if (Active)
        {
            // Already active — toggle off
            Trace.Command (this, args.Context, "ToggleOff", "Already active — deactivating");
            Active = false;

            return true;
        }

        if (SubViews.OfType<MenuBarItem> ().FirstOrDefault (mbi => mbi.PopoverMenu is { }) is not { } first)
        {
            Trace.Command (this, args.Context, "NoItems", "No MenuBarItem with PopoverMenu found");

            return false;
        }

        // Not yet active — activate and show the first MenuBarItem with a PopoverMenu.
        Trace.Command (this, args.Context, "FallbackToFirst", $"Opening first={first.ToIdentifyingString ()}");
        Active = true;
        ShowItem (first);

        return true;
    }

    /// <inheritdoc/>
    protected override void OnActivated (ICommandContext? ctx)
    {
        Trace.Command (this, ctx, "Entry", $"Active={Active} Routing={ctx?.Routing}");
        base.OnActivated (ctx);

        if (ctx is { Routing: CommandRouting.Direct })
        {
            CanFocus = !Active;
        }
    }

    /// <summary>
    ///     Gets or sets whether the menu bar is active or not. When active, the MenuBar can focus and moving the mouse
    ///     over a MenuBarItem will switch focus to that item. Use <see cref="IsOpen"/> to determine if a PopoverMenu of
    ///     a MenuBarItem is open.
    /// </summary>
    /// <returns></returns>
    public bool Active
    {
        get;
        internal set
        {
            if (field == value)
            {
                return;
            }

            Trace.Command (this, "ActiveChanged", $"{field} -> {value}");
            field = value;

            // Change CanFocus based on Active state before hiding Popovers; this way when focus is restored,
            // it won't be to the MenuBar
            CanFocus = value;

            if (field)
            {
                return;
            }
            _popoverBrowsingMode = false;

            HideActiveItem ();
        }
    }

    /// <summary>
    ///     Gets or sets the default Border Style for the MenuBar. The default is <see cref="LineStyle.None"/>.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public new static LineStyle DefaultBorderStyle { get; set; } = LineStyle.None;

    /// <summary>The default key for activating menu bars.</summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key DefaultKey { get; set; } = Key.F9;

    /// <inheritdoc/>
    public override void EndInit ()
    {
        base.EndInit ();

        if (Border is { })
        {
            Border.Thickness = new Thickness (0);
            Border.LineStyle = LineStyle.None;
        }

        // TODO: This needs to be done whenever a menuitem in any MenuBarItem changes
        foreach (MenuBarItem? mbi in SubViews.Select (s => s as MenuBarItem))
        {
            App?.Popovers?.Register (mbi?.PopoverMenu);
        }
    }

    /// <summary>
    ///     Gets all <see cref="MenuItem"/>s in the menu hierarchy that match <paramref name="predicate"/>.
    /// </summary>
    /// <param name="predicate">A function to test each <see cref="MenuItem"/>.</param>
    /// <returns>All matching <see cref="MenuItem"/>s across all <see cref="PopoverMenu"/>s.</returns>
    public IEnumerable<MenuItem> GetMenuItemsWith (Func<MenuItem, bool> predicate)
    {
        List<MenuItem> menuItems = [];

        foreach (MenuBarItem mbi in SubViews.OfType<MenuBarItem> ())
        {
            if (mbi.PopoverMenu is { })
            {
                menuItems.AddRange (mbi.PopoverMenu.GetMenuItemsOfAllSubMenus (predicate));
            }
        }

        return menuItems;
    }

    /// <summary>
    ///     Hides the popover menu associated with the active menu bar item and updates the focus state.
    /// </summary>
    /// <returns><see langword="true"/> if the popover was hidden</returns>
    public bool HideActiveItem ()
    {
        Trace.Command (this, "Entry");

        return HideItem (GetActiveItem ());
    }

    /// <summary>
    ///     Hides popover menu associated with the specified menu bar item and updates the focus state.
    /// </summary>
    /// <param name="activeItem"></param>
    /// <returns><see langword="true"/> if the popover was hidden</returns>
    public bool HideItem (MenuBarItem? activeItem)
    {
        if (activeItem is null || !activeItem.PopoverMenuOpen)
        {
            return false;
        }

        // IMPORTANT: Set Visible false before setting Active to false (Active changes Can/HasFocus)
        activeItem.PopoverMenuOpen = false;

        Active = false;
        HasFocus = false;

        return true;
    }

    /// <summary>
    ///     Guards against deactivation during MenuBarItem switching (arrow keys, mouse hover, ShowItem).
    ///     When switching items, the old popover may close before the new one opens, which would
    ///     trigger deactivation via <see cref="OnMenuBarItemPopoverMenuOpenChanged"/>.
    ///     This flag prevents that false deactivation.
    /// </summary>
    private bool _isSwitchingItem;

    /// <summary>
    ///     Tracks "popover browsing mode" — set when any popover opens, stays true during
    ///     item switching (bridging the brief gap when old popover closes before new one opens).
    ///     Reset only when <see cref="Active"/> goes false or <see cref="Command.Quit"/> is handled.
    /// </summary>
    private bool _popoverBrowsingMode;

    /// <summary>
    ///     Gets whether any of the menu bar items have a visible <see cref="PopoverMenu"/>.
    /// </summary>
    public bool IsOpen () => SubViews.OfType<MenuBarItem> ().Any (m => m.PopoverMenuOpen);

    /// <summary>
    ///     Specifies the key that will activate the MenuBar. The default is <see cref="Key.F9"/> and
    ///     can be configured using the <see cref="DefaultKey"/> configuraiton property.
    /// </summary>
    public Key Key
    {
        get;
        set
        {
            Key oldKey = field;
            field = value;
            KeyChanged?.Invoke (this, new KeyChangedEventArgs (oldKey, field));
        }
    }

    /// <summary>Raised when <see cref="Key"/> is changed.</summary>
    public event EventHandler<KeyChangedEventArgs>? KeyChanged;

    /// <summary>
    ///     Sets the Menu Bar Items for this Menu Bar. This will replace any existing Menu Bar Items.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is a convenience property to help porting from the v1 MenuBar.
    ///     </para>
    /// </remarks>
    public MenuBarItem []? Menus
    {
        set
        {
            RemoveAll ();

            if (value is null)
            {
                return;
            }

            foreach (MenuBarItem mbi in value)
            {
                Add (mbi);
            }
        }
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        base.Dispose (disposing);

        if (disposing)
        {
            ConfigurationManager.Applied -= OnConfigurationManagerApplied;
        }
    }

    /// <summary>
    ///     Finds the MenuBarItem that is an ancestor (or is itself) the source view, and is a direct SubView of this
    ///     MenuBar.
    /// </summary>
    private MenuBarItem? FindMenuBarItemForSource (View? source)
    {
        View? current = source;

        while (current is { })
        {
            if (current is MenuBarItem mbi && mbi.SuperView == this)
            {
                return mbi;
            }

            current = current.SuperView;
        }

        return null;
    }

    /// <inheritdoc/>
    protected override void OnHasFocusChanged (bool newHasFocus, View? previousFocusedView, View? focusedView)
    {
        if (!newHasFocus)
        {
            Active = false;
        }
    }

    /// <inheritdoc/>
    protected override bool OnMouseEnter (CancelEventArgs eventArgs)
    {
        // If the MenuBar does not have focus and the mouse enters: Enable CanFocus
        // But do NOT show a Popover unless the user clicks or presses a hotkey
        if (!HasFocus)
        {
            Active = true;
        }

        return base.OnMouseEnter (eventArgs);
    }

    /// <inheritdoc/>
    protected override void OnMouseLeave ()
    {
        if (!IsOpen ())
        {
            Active = false;
        }

        base.OnMouseLeave ();
    }

    /// <inheritdoc/>
    protected override void OnSelectedMenuItemChanged (MenuItem? selected)
    {
        Trace.Command (this, "Entry", $"Selected={selected?.ToIdentifyingString ()} BrowsingMode={_popoverBrowsingMode} IsSwitching={_isSwitchingItem}");

        // When _isSwitchingItem is true, we're already inside a ShowItem call
        // (e.g., from a HotKey activation that bubbled up). Focus traversal may
        // transiently pass through intermediate MenuBarItems. We must NOT auto-open
        // those items — only the target item should open.
        if (!_isSwitchingItem && _popoverBrowsingMode && selected is MenuBarItem { PopoverMenuOpen: false } selectedMenuBarItem)
        {
            ShowItem (selectedMenuBarItem);
        }
    }

    /// <inheritdoc/>
    protected override void OnSubViewAdded (View view)
    {
        base.OnSubViewAdded (view);

        if (view is MenuBarItem mbi)
        {
            //mbi.Accepted += OnMenuBarItemAccepted;
            mbi.PopoverMenuOpenChanged += OnMenuBarItemPopoverMenuOpenChanged;
        }
    }

    /// <inheritdoc/>
    protected override void OnSubViewRemoved (View view)
    {
        base.OnSubViewRemoved (view);

        if (view is MenuBarItem mbi)
        {
            // mbi.Accepted -= OnMenuBarItemAccepted;
            mbi.PopoverMenuOpenChanged -= OnMenuBarItemPopoverMenuOpenChanged;
        }
    }

    /// <inheritdoc/>
    protected override void OnSuperViewChanged (ValueChangedEventArgs<View?> e)
    {
        if (SuperView is null)
        {
            // BUGBUG: This is a hack for avoiding a race condition in ConfigurationManager.Apply
            // BUGBUG: For some reason in some unit tests, when Top is disposed, MenuBar.Dispose does not get called.
            // BUGBUG: Yet, the MenuBar does get Removed from Top (and it's SuperView set to null).
            // BUGBUG: Related: https://github.com/gui-cs/Terminal.Gui/issues/4021
            ConfigurationManager.Applied -= OnConfigurationManagerApplied;
        }
    }

    private MenuBarItem? GetActiveItem () => SubViews.OfType<MenuBarItem> ().FirstOrDefault (sv => sv is { PopoverMenu.Visible: true });

    private void OnConfigurationManagerApplied (object? sender, ConfigurationManagerEventArgs e) => BorderStyle = DefaultBorderStyle;

    private void OnMenuBarItemPopoverMenuOpenChanged (object? sender, ValueChangedEventArgs<bool> e)
    {
        if (e.NewValue)
        {
            _popoverBrowsingMode = true;

            return;
        }

        // A PopoverMenu just closed. If no others are open, and we're not in the middle of
        // arrow-key navigation (where the old popover closes before the new one opens),
        // deactivate the MenuBar entirely. HotKey switching (Alt+E while File is open)
        // is safe because the new popover opens BEFORE the old one closes.
        if (!_isSwitchingItem && !SubViews.OfType<MenuBarItem> ().Any (m => m.PopoverMenuOpen))
        {
            // During mouse-hover switching, the old MBI's popover closes because
            // MenuItem.OnMouseEnter → SetFocus() moved focus to the new MBI.  At this point
            // the new MBI has _hasFocus=true but hasn't opened its popover yet (we're still
            // inside SetHasFocusTrue).  Deactivating now would set CanFocus=false and cascade
            // focus loss back to the new MBI, violating SetHasFocusTrue's post-condition.
            // Skip deactivation when a *different* MBI already has focus — ShowItem will run
            // via OnSelectedMenuItemChanged once the focus transfer completes.
            bool anotherMbiHasFocus = SubViews.OfType<MenuBarItem> ().Any (m => m != sender && m.HasFocus);

            if (anotherMbiHasFocus)
            {
                return;
            }

            // If no MBI has focus at all, focus is leaving the MenuBar entirely. Don't call
            // Active = false here — it would set CanFocus = false → HasFocus = false re-entrantly,
            // violating SetHasFocusFalse's invariant (Debug.Assert(_hasFocus) at line 908).
            // MenuBar.OnHasFocusChanged will deactivate once the focus traversal completes.
            bool anyMbiHasFocus = SubViews.OfType<MenuBarItem> ().Any (m => m.HasFocus);

            if (!anyMbiHasFocus)
            {
                return;
            }

            Active = false;
        }
    }

    /// <summary>
    ///     Shows the specified popover, but only if the menu bar is active.
    /// </summary>
    /// <param name="menuBarItem"></param>
    private void ShowItem (MenuBarItem? menuBarItem)
    {
        Trace.Command (this, "Entry", $"Item={menuBarItem?.ToIdentifyingString ()} Active={Active}");

        if (!Active || !Visible)
        {
            return;
        }

        // Guard: when switching items, SetFocus() closes the old popover before the new one opens.
        // Without this guard, OnMenuBarItemPopoverMenuOpenChanged would deactivate the MenuBar.
        _isSwitchingItem = true;

        try
        {
            // TODO: We should init the PopoverMenu in a smarter way
            if (menuBarItem?.PopoverMenu is { IsInitialized: false })
            {
                menuBarItem.PopoverMenu.BeginInit ();
                menuBarItem.PopoverMenu.EndInit ();
            }

            if (menuBarItem is null)
            {
                return;
            }

            Active = true;
            menuBarItem.SetFocus ();

            if (menuBarItem.PopoverMenu?.Root is { })
            {
                menuBarItem.PopoverMenu.Root.SuperMenuItem = menuBarItem;
                menuBarItem.PopoverMenu.Root.SchemeName = SchemeName;
            }

            menuBarItem.PopoverMenuOpen = true;
        }
        finally
        {
            _isSwitchingItem = false;
        }
    }

    /// <inheritdoc/>
    public bool EnableForDesign<TContext> (ref TContext targetView) where TContext : notnull
    {
        // Note: This menu is used by unit tests. If you modify it, you'll likely have to update
        // unit tests.

        if (targetView is View target)
        {
            App ??= target.App;
        }

        Id = "DemoBar";

        var bordersCb = new CheckBox
        {
            Title = "_Borders",

            // Shortcut/MenuItem override GettingAttributeForRole to ensure CommandViews with multiple selectable items (like a ListView or Selector)
            // show the selected item distinctly, but for a CommandView with only a single selectable item (like a CheckBox),
            // we want it to look focused when selected, and unfocused when not, so set CanFocus false.
            CanFocus = false,
            Value = DefaultBorderStyle == LineStyle.None ? CheckState.UnChecked : CheckState.Checked
        };

        var autoSaveCb = new CheckBox
        {
            Title = "_Auto Save",

            // Shortcut/MenuItem override GettingAttributeForRole to ensure CommandViews with multiple selectable items (like a ListView or Selector)
            // show the selected item distinctly, but for a CommandView with only a single selectable item (like a CheckBox),
            // we want it to look focused when selected, and unfocused when not, so set CanFocus false.
            CanFocus = false
        };

        var enableOverwriteCb = new CheckBox
        {
            Title = "Enable _Overwrite",

            // Shortcut/MenuItem override GettingAttributeForRole to ensure CommandViews with multiple selectable items (like a ListView or Selector)
            // show the selected item distinctly, but for a CommandView with only a single selectable item (like a CheckBox),
            // we want it to look focused when selected, and unfocused when not, so set CanFocus false.
            CanFocus = false
        };

        OptionSelector<Schemes> mutuallyExclusiveOptionsSelector = new () { Title = "Scheme", CanFocus = true, MouseHighlightStates = MouseState.None };

        var menuBgColorCp = new ColorPicker { Width = 30 };

        menuBgColorCp.ValueChanged += (_, args) =>
                                      {
                                          // BUGBUG: This is weird.
                                          SetScheme (GetScheme () with
                                          {
                                              Normal = new Attribute (GetAttributeForRole (VisualRole.Normal).Foreground,
                                                                      args.NewValue ?? Color.Black,
                                                                      GetAttributeForRole (VisualRole.Normal).Style)
                                          });
                                      };

        Add (new MenuBarItem (Strings.menuFile,
                              [
                                  new MenuItem (targetView as View, Command.New),
                                  new MenuItem (targetView as View, Command.Open),
                                  new MenuItem (targetView as View, Command.Save),
                                  new MenuItem (targetView as View, Command.SaveAs),
                                  new Line (),
                                  new MenuItem
                                  {
                                      Title = "_File Options",
                                      SubMenu =
                                          new Menu ([
                                                        new MenuItem { Id = "AutoSave", Text = "(no Command)", Key = Key.F10, CommandView = autoSaveCb },
                                                        new MenuItem
                                                        {
                                                            Text = "Overwrite",
                                                            Id = "Overwrite",
                                                            Key = Key.W.WithCtrl,
                                                            CommandView = enableOverwriteCb,
                                                            Command = Command.EnableOverwrite,
                                                            TargetView = targetView as View
                                                        },
                                                        new MenuItem
                                                        {
                                                            Title = "_File Settings...",
                                                            HelpText = "More file settings",
                                                            Action =
                                                                () => MessageBox.Query (App!,
                                                                                        "File Settings",
                                                                                        "This is the File Settings Dialog\n",
                                                                                        Strings.btnOk,
                                                                                        Strings.btnCancel)
                                                        }
                                                    ])
                                  },
                                  new Line (),
                                  new MenuItem
                                  {
                                      Title = "_Preferences",
                                      SubMenu = new Menu ([
                                                              new MenuItem
                                                              {
                                                                  CommandView = bordersCb, HelpText = "Toggle Menu Borders", Action = ToggleMenuBorders
                                                              },
                                                              new MenuItem
                                                              {
                                                                  Id = "mutuallyExclusiveOptions",
                                                                  HelpText = "Mutually Exclusive Options",
                                                                  CommandView = mutuallyExclusiveOptionsSelector,
                                                                  Key = Key.F7,
                                                                  MouseHighlightStates = MouseState.None
                                                              },
                                                              new Line (),
                                                              new MenuItem { HelpText = "MenuBar BG Color", CommandView = menuBgColorCp, Key = Key.F8 }
                                                          ])
                                  },
                                  new Line (),
                                  new MenuItem { TargetView = targetView as View, Key = Application.QuitKey, Command = Command.Quit }
                              ]));

        Add (new MenuBarItem ("_Edit",
                              [
                                  new MenuItem (targetView as View, Command.Cut),
                                  new MenuItem (targetView as View, Command.Copy),
                                  new MenuItem (targetView as View, Command.Paste),
                                  new Line (),
                                  new MenuItem (targetView as View, Command.SelectAll),
                                  new Line (),
                                  new MenuItem { Title = "_Details", SubMenu = new Menu (ConfigureDetailsSubMenu ()) }
                              ]));

        Add (new MenuBarItem (Strings.menuHelp,
                              [
                                  new MenuItem
                                  {
                                      Title = "_Online Help...",
                                      Action = () => MessageBox.Query (App!, "Online Help", "https://gui-cs.github.io/Terminal.Gui", Strings.btnOk)
                                  },
                                  new MenuItem { Title = "About...", Action = () => MessageBox.Query (App!, "About", "Something About Mary.", Strings.btnOk) }
                              ]));

        return true;

        void ToggleMenuBorders ()
        {
            foreach (MenuBarItem mbi in SubViews.OfType<MenuBarItem> ())
            {
                if (mbi is not { PopoverMenu: { } })
                {
                    continue;
                }

                foreach (Menu subMenu in mbi.PopoverMenu.GetAllSubMenus ())
                {
                    subMenu.Border?.Thickness = bordersCb.Value == CheckState.Checked ? new Thickness (1) : new Thickness (0);
                    subMenu.Border?.LineStyle = bordersCb.Value == CheckState.Checked ? LineStyle.Rounded : LineStyle.None;
                }
            }
        }

        MenuItem [] ConfigureDetailsSubMenu ()
        {
            var detail = new MenuItem { Title = "_Detail 1", Text = "Some detail #1" };

            var nestedSubMenu = new MenuItem { Title = "_Moar Details", SubMenu = new Menu (ConfigureMoreDetailsSubMenu ()) };

            // This menu item is used to test Application Key binding. See the Menus Scenario.
            // F5 will toggle the Edit Mode checkbox, and the menu item text will update to show the Command it's bound to.
            MenuItem editMode = new ()
            {
                Text = "App Binding to Command.Edit",
                Id = "EditMode",
                Command = Command.Edit,
                CommandView = new CheckBox { Title = "E_dit Mode" },
                Key = Key.F5,
                BindKeyToApplication = true
            };

            return [detail, nestedSubMenu, null!, editMode];

            View [] ConfigureMoreDetailsSubMenu ()
            {
                var deeperDetail = new MenuItem
                {
                    Title = "_Deeper Detail",
                    Text = "Deeper Detail",
                    Action = () => { MessageBox.Query (App!, "Deeper Detail", "Lots of details", Strings.btnOk); }
                };

                var belowLineDetail = new MenuItem { Title = "_Even more detail", Text = "Below the line" };

                // This ensures the checkbox state toggles when the hotkey of Title is pressed.
                // shortcut4.Accepting += (sender, args) => args.Cancel = true;

                return [deeperDetail, new Line (), belowLineDetail];
            }
        }
    }
}
