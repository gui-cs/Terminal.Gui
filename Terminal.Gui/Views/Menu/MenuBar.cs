using System.ComponentModel;
using System.Diagnostics;

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

        AddCommand (Command.HotKey,
                    ctx =>
                    {
                        // Logging.Debug ($"{Title} - Command.HotKey");

                        if (RaiseHandlingHotKey (ctx) is true)
                        {
                            return true;
                        }

                        if (HideActiveItem ())
                        {
                            return true;
                        }

                        if (SubViews.OfType<MenuBarItem> ().FirstOrDefault (mbi => mbi.PopoverMenu is { }) is { } first)
                        {
                            Active = true;
                            ShowItem (first);

                            return true;
                        }

                        return false;
                    });

        // If we're not focused, Key activates/deactivates
        HotKeyBindings.Add (Key, Command.HotKey);

        KeyBindings.Add (Key, Command.Quit);
        KeyBindings.ReplaceCommands (Application.QuitKey, Command.Quit);

        AddCommand (Command.Quit,
                    ctx =>
                    {
                        // Logging.Debug ($"{Title} - Command.Quit");

                        if (HideActiveItem ())
                        {
                            return true;
                        }

                        if (CanFocus)
                        {
                            CanFocus = false;
                            Active = false;

                            return true;
                        }

                        return false; // RaiseAccepted (ctx);
                    });

        AddCommand (Command.Right, MoveRight);
        KeyBindings.Add (Key.CursorRight, Command.Right);

        AddCommand (Command.Left, MoveLeft);
        KeyBindings.Add (Key.CursorLeft, Command.Left);

        BorderStyle = DefaultBorderStyle;

        ConfigurationManager.Applied += OnConfigurationManagerApplied;

        return;

        bool? MoveLeft (ICommandContext? ctx) => AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabStop);

        bool? MoveRight (ICommandContext? ctx) => AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
    }

    private Key _key = DefaultKey;

    private bool _active;

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

        var bordersCb = new CheckBox { Title = "_Borders", Value = CheckState.Checked };

        var autoSaveCb = new CheckBox { Title = "_Auto Save" };

        var enableOverwriteCb = new CheckBox { Title = "Enable _Overwrite" };

        var mutuallyExclusiveOptionsSelector = new OptionSelector { Labels = ["G_ood", "_Bad", "U_gly"], Value = 0 };

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
                                                                  HelpText = "3 Mutually Exclusive Options",
                                                                  CommandView = mutuallyExclusiveOptionsSelector,
                                                                  Key = Key.F7
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

                foreach (Menu? subMenu in mbi.PopoverMenu.GetAllSubMenus ())
                {
                    if (bordersCb.Value == CheckState.Checked)
                    {
                        subMenu.Border!.Thickness = new Thickness (1);
                    }
                    else
                    {
                        subMenu.Border!.Thickness = new Thickness (0);
                    }
                }
            }
        }

        MenuItem [] ConfigureDetailsSubMenu ()
        {
            var detail = new MenuItem { Title = "_Detail 1", Text = "Some detail #1" };

            var nestedSubMenu = new MenuItem { Title = "_Moar Details", SubMenu = new Menu (ConfigureMoreDetailsSubMenu ()) };

            var editMode = new MenuItem
            {
                Text = "App Binding to Command.Edit", Id = "EditMode", Command = Command.Edit, CommandView = new CheckBox { Title = "E_dit Mode" }
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

    /// <summary>
    ///     Gets or sets whether the menu bar is active or not. When active, the MenuBar can focus and moving the mouse
    ///     over a MenuBarItem will switch focus to that item. Use <see cref="IsOpen"/> to determine if a PopoverMenu of
    ///     a MenuBarItem is open.
    /// </summary>
    /// <returns></returns>
    public bool Active
    {
        get => _active;
        internal set
        {
            if (_active == value)
            {
                return;
            }

            _active = value;

            // Logging.Debug ($"Active set to {_active} - CanFocus: {CanFocus}, HasFocus: {HasFocus}");

            // Change CanFocus based on Active state before hiding Popovers; this way when focus is restored,
            // it won't be to the MenuBar
            CanFocus = value;

            // Logging.Debug ($"Set CanFocus: {CanFocus}, HasFocus: {HasFocus}");

            if (!_active)
            {
                // Hide open Popovers
                HideActiveItem ();
            }
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
            App?.Popover?.Register (mbi?.PopoverMenu);
        }
    }

    /// <summary>
    ///     Gets all menu items with the specified Title, anywhere in the menu hierarchy.
    /// </summary>
    /// <param name="title"></param>
    /// <returns></returns>
    public IEnumerable<MenuItem> GetMenuItemsWithTitle (string title)
    {
        List<MenuItem> menuItems = [];

        if (string.IsNullOrEmpty (title))
        {
            return menuItems;
        }

        foreach (MenuBarItem mbi in SubViews.OfType<MenuBarItem> ())
        {
            if (mbi.PopoverMenu is { })
            {
                menuItems.AddRange (mbi.PopoverMenu.GetMenuItemsOfAllSubMenus ());
            }
        }

        return menuItems.Where (mi => mi.Title == title);
    }

    /// <summary>
    ///     Hides the popover menu associated with the active menu bar item and updates the focus state.
    /// </summary>
    /// <returns><see langword="true"/> if the popover was hidden</returns>
    public bool HideActiveItem () => HideItem (GetActiveItem ());

    /// <summary>
    ///     Hides popover menu associated with the specified menu bar item and updates the focus state.
    /// </summary>
    /// <param name="activeItem"></param>
    /// <returns><see langword="true"/> if the popover was hidden</returns>
    public bool HideItem (MenuBarItem? activeItem)
    {
        // Logging.Debug ($"{Title} ({activeItem?.Title}) - Active: {Active}, CanFocus: {CanFocus}, HasFocus: {HasFocus}");

        if (activeItem is null || !activeItem.PopoverMenu!.Visible)
        {
            // Logging.Debug ($"{Title} No active item.");

            return false;
        }

        // IMPORTANT: Set Visible false before setting Active to false (Active changes Can/HasFocus)
        activeItem.PopoverMenu!.Visible = false;

        Active = false;
        HasFocus = false;

        return true;
    }

    /// <summary>
    ///     Gets whether any of the menu bar items have a visible <see cref="PopoverMenu"/>.
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public bool IsOpen () => SubViews.OfType<MenuBarItem> ().Count (sv => sv is { PopoverMenuOpen: true }) > 0;

    /// <summary>Specifies the key that will activate the context menu.</summary>
    public Key Key
    {
        get => _key;
        set
        {
            Key oldKey = _key;
            _key = value;
            KeyChanged?.Invoke (this, new KeyChangedEventArgs (oldKey, _key));
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

    /// <inheritdoc/>
    protected override void OnAccepted (CommandEventArgs args)
    {
        // Logging.Debug ($"{Title} ({args.Context?.Source?.Title}) Command: {args.Context?.Command}");
        base.OnAccepted (args);

        if (SubViews.OfType<MenuBarItem> ().Contains (args.Context?.Source))
        {
            return;
        }

        Active = false;
    }

    /// <inheritdoc/>
    protected override bool OnAccepting (CommandEventArgs args)
    {
        // Logging.Debug ($"{Title} ({args.Context?.Source?.Title})");

        // TODO: Ensure sourceMenuBar is actually one of our bar items
        if (Visible && Enabled && args.Context?.Source?.TryGetTarget (out View? sourceView) == true && sourceView is MenuBarItem { PopoverMenuOpen: false } sourceMenuBarItem)
        {
            if (!CanFocus)
            {
                Debug.Assert (!Active);

                // We are not Active; change that
                Active = true;

                ShowItem (sourceMenuBarItem);

                if (!sourceMenuBarItem.HasFocus)
                {
                    sourceMenuBarItem.SetFocus ();
                }
            }
            else
            {
                Debug.Assert (Active);
                ShowItem (sourceMenuBarItem);
            }

            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    protected override void OnAccepted (CommandEventArgs args)
    {
        // Logging.Debug ($"{Title} ({args.Context?.Source?.Title}) Command: {args.Context?.Command}");
        base.OnAccepted (args);

        if (args.Context?.Source?.TryGetTarget (out View? sourceView) == true && SubViews.OfType<MenuBarItem> ().Contains (sourceView))
        {
            return;
        }

        // HideActiveItem ();
        base.OnBorderStyleChanged ();

    /// <inheritdoc/>
    protected override void OnHasFocusChanged (bool newHasFocus, View? previousFocusedView, View? focusedView)
    {
        // Logging.Debug ($"CanFocus = {CanFocus}, HasFocus = {HasFocus}");

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
        // Logging.Debug ($"CanFocus = {CanFocus}, HasFocus = {HasFocus}");

        if (!HasFocus)
        {
            Active = true;
        }

        return base.OnMouseEnter (eventArgs);
    }

    /// <inheritdoc/>
    protected override void OnMouseLeave ()
    {
        // Logging.Debug ($"CanFocus = {CanFocus}, HasFocus = {HasFocus}");

        if (!IsOpen ())
        {
            Active = false;
        }

        base.OnMouseLeave ();
    }

    /// <inheritdoc/>
    protected override void OnSelectedMenuItemChanged (MenuItem? selected)
    {
        // Logging.Debug ($"{Title} ({selected?.Title}) - IsOpen: {IsOpen ()}");

        if (IsOpen () && selected is MenuBarItem { PopoverMenuOpen: false } selectedMenuBarItem)
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
            mbi.Accepted += OnMenuBarItemAccepted;
            mbi.PopoverMenuOpenChanged += OnMenuBarItemPopoverMenuOpenChanged;
        }
    }

    /// <inheritdoc/>
    protected override void OnSubViewRemoved (View view)
    {
        base.OnSubViewRemoved (view);

        if (view is MenuBarItem mbi)
        {
            mbi.Accepted -= OnMenuBarItemAccepted;
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

    private MenuBarItem? GetActiveItem () => SubViews.OfType<MenuBarItem> ().FirstOrDefault (sv => sv is { PopoverMenu: { Visible: true } });

    private void OnConfigurationManagerApplied (object? sender, ConfigurationManagerEventArgs e) => BorderStyle = DefaultBorderStyle;

    private void OnMenuBarItemAccepted (object? sender, CommandEventArgs e) =>

        // Logging.Debug ($"{Title} ({e.Context?.Source?.Title}) Command: {e.Context?.Command}");
        RaiseAccepted (e.Context);

    private void OnMenuBarItemPopoverMenuOpenChanged (object? sender, EventArgs<bool> e)
    {
        if (sender is MenuBarItem mbi)
        {
            if (e.Value)
            {
                Active = true;
            }
        }
    }

    /// <summary>
    ///     Shows the specified popover, but only if the menu bar is active.
    /// </summary>
    /// <param name="menuBarItem"></param>
    private void ShowItem (MenuBarItem? menuBarItem)
    {
        // Logging.Debug ($"{Title} - {menuBarItem?.Id}");

        if (!Active || !Visible)
        {
            // Logging.Debug ($"{Title} - {menuBarItem?.Id} - Not Active, not showing.");

            return;
        }

        // TODO: We should init the PopoverMenu in a smarter way
        if (menuBarItem?.PopoverMenu is { IsInitialized: false })
        {
            menuBarItem.PopoverMenu.BeginInit ();
            menuBarItem.PopoverMenu.EndInit ();
        }

        // If the active Application Popover is part of this MenuBar, hide it.
        if (App?.Popover?.GetActivePopover () is PopoverMenu popoverMenu && popoverMenu.Root?.SuperMenuItem?.SuperView == this)
        {
            // Logging.Debug ($"{Title} - Calling App?.Popover?.Hide ({popoverMenu.Title})");
            App?.Popover.Hide (popoverMenu);
        }

        if (menuBarItem is null)
        {
            // Logging.Debug ($"{Title} - menuBarItem is null.");

            return;
        }

        Active = true;
        menuBarItem.SetFocus ();

        if (menuBarItem.PopoverMenu?.Root is { })
        {
            menuBarItem.PopoverMenu.Root.SuperMenuItem = menuBarItem;
            menuBarItem.PopoverMenu.Root.SchemeName = SchemeName;
        }

        // Logging.Debug ($"{Title} - \"{menuBarItem.PopoverMenu?.Title}\".MakeVisible");
        if (menuBarItem.PopoverMenu is { })
        {
            menuBarItem.PopoverMenu.App ??= App;
            menuBarItem.PopoverMenu.MakeVisible (new Point (menuBarItem.FrameToScreen ().X, menuBarItem.FrameToScreen ().Bottom));
        }

        // Subscribe to VisibleChanged to detect when the popover closes
        if (menuBarItem.PopoverMenu is { })
        {
            menuBarItem.PopoverMenu.VisibleChanged += OnPopoverVisibleChanged;
        }

        return;

        void OnPopoverVisibleChanged (object? sender, EventArgs args)
        {
            // Logging.Debug ($"{Title} - OnPopoverVisibleChanged");
            // Unsubscribe from VisibleChanged (the event we subscribed to)
            if (menuBarItem.PopoverMenu is { })
            {
                menuBarItem.PopoverMenu.VisibleChanged -= OnPopoverVisibleChanged;
            }

            if (Active && menuBarItem.PopoverMenu is { Visible: false })
            {
                Active = false;
                HasFocus = false;
            }
        }
    }
}
