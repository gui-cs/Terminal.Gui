#nullable enable
using System.ComponentModel;
using System.Diagnostics;

namespace Terminal.Gui.Views;

/// <summary>
///     A horizontal list of <see cref="MenuBarItemv2"/>s. Each <see cref="MenuBarItemv2"/> can have a
///     <see cref="PopoverMenu"/> that is shown when the <see cref="MenuBarItemv2"/> is selected.
/// </summary>
/// <remarks>
///     MenuBars may be hosted by any View and will, by default, be positioned the full width across the top of the View's
///     Viewport.
/// </remarks>
public class MenuBarv2 : Menuv2, IDesignable
{
    /// <inheritdoc/>
    public MenuBarv2 () : this ([]) { }

    /// <inheritdoc/>
    public MenuBarv2 (IEnumerable<MenuBarItemv2> menuBarItems) : base (menuBarItems)
    {
        CanFocus = false;
        TabStop = TabBehavior.TabGroup;
        Y = 0;
        Width = Dim.Fill ();
        Height = Dim.Auto ();
        Orientation = Orientation.Horizontal;

        Key = DefaultKey;

        AddCommand (
                    Command.HotKey,
                    () =>
                    {
                        // Logging.Debug ($"{Title} - Command.HotKey");

                        if (RaiseHandlingHotKey () is true)
                        {
                            return true;
                        }

                        if (HideActiveItem ())
                        {
                            return true;
                        }

                        if (SubViews.OfType<MenuBarItemv2> ().FirstOrDefault (mbi => mbi.PopoverMenu is { }) is { } first)
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

        AddCommand (
                    Command.Quit,
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

                        return false; //RaiseAccepted (ctx);
                    });

        AddCommand (Command.Right, MoveRight);
        KeyBindings.Add (Key.CursorRight, Command.Right);

        AddCommand (Command.Left, MoveLeft);
        KeyBindings.Add (Key.CursorLeft, Command.Left);

        BorderStyle = DefaultBorderStyle;

        ConfigurationManager.Applied += OnConfigurationManagerApplied;
        SuperViewChanged += OnSuperViewChanged;

        return;

        bool? MoveLeft (ICommandContext? ctx) { return AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabStop); }

        bool? MoveRight (ICommandContext? ctx) { return AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop); }
    }

    private void OnSuperViewChanged (object? sender, SuperViewChangedEventArgs e)
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

    private void OnConfigurationManagerApplied (object? sender, ConfigurationManagerEventArgs e) { BorderStyle = DefaultBorderStyle; }

    /// <inheritdoc/>
    protected override bool OnBorderStyleChanged ()
    {
        //HideActiveItem ();

        return base.OnBorderStyleChanged ();
    }

    /// <summary>
    ///     Gets or sets the default Border Style for the MenuBar. The default is <see cref="LineStyle.None"/>.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public new static LineStyle DefaultBorderStyle { get; set; } = LineStyle.None;

    private Key _key = DefaultKey;

    /// <summary>Specifies the key that will activate the context menu.</summary>
    public Key Key
    {
        get => _key;
        set
        {
            Key oldKey = _key;
            _key = value;
            KeyChanged?.Invoke (this, new (oldKey, _key));
        }
    }

    /// <summary>
    ///     Sets the Menu Bar Items for this Menu Bar. This will replace any existing Menu Bar Items.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is a convenience property to help porting from the v1 MenuBar.
    ///     </para>
    /// </remarks>
    public MenuBarItemv2 []? Menus
    {
        set
        {
            RemoveAll ();

            if (value is null)
            {
                return;
            }

            foreach (MenuBarItemv2 mbi in value)
            {
                Add (mbi);
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnSubViewAdded (View view)
    {
        base.OnSubViewAdded (view);

        if (view is MenuBarItemv2 mbi)
        {
            mbi.Accepted += OnMenuBarItemAccepted;
            mbi.PopoverMenuOpenChanged += OnMenuBarItemPopoverMenuOpenChanged;
        }
    }

    /// <inheritdoc/>
    protected override void OnSubViewRemoved (View view)
    {
        base.OnSubViewRemoved (view);

        if (view is MenuBarItemv2 mbi)
        {
            mbi.Accepted -= OnMenuBarItemAccepted;
            mbi.PopoverMenuOpenChanged -= OnMenuBarItemPopoverMenuOpenChanged;
        }
    }

    private void OnMenuBarItemPopoverMenuOpenChanged (object? sender, EventArgs<bool> e)
    {
        if (sender is MenuBarItemv2 mbi)
        {
            if (e.Value)
            {
                Active = true;
            }
        }
    }

    private void OnMenuBarItemAccepted (object? sender, CommandEventArgs e)
    {
        // Logging.Debug ($"{Title} ({e.Context?.Source?.Title}) Command: {e.Context?.Command}");

        RaiseAccepted (e.Context);
    }

    /// <summary>Raised when <see cref="Key"/> is changed.</summary>
    public event EventHandler<KeyChangedEventArgs>? KeyChanged;

    /// <summary>The default key for activating menu bars.</summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key DefaultKey { get; set; } = Key.F9;

    /// <summary>
    ///     Gets whether any of the menu bar items have a visible <see cref="PopoverMenu"/>.
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public bool IsOpen () { return SubViews.OfType<MenuBarItemv2> ().Count (sv => sv is { PopoverMenuOpen: true }) > 0; }

    private bool _active;

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

            if (!_active)
            {
                // Hide open Popovers
                HideActiveItem ();
            }

            CanFocus = value;
            // Logging.Debug ($"Set CanFocus: {CanFocus}, HasFocus: {HasFocus}");
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
    protected override void OnHasFocusChanged (bool newHasFocus, View? previousFocusedView, View? focusedView)
    {
        // Logging.Debug ($"CanFocus = {CanFocus}, HasFocus = {HasFocus}");

        if (!newHasFocus)
        {
            Active = false;
        }
    }

    /// <inheritdoc/>
    protected override void OnSelectedMenuItemChanged (MenuItemv2? selected)
    {
        // Logging.Debug ($"{Title} ({selected?.Title}) - IsOpen: {IsOpen ()}");

        if (IsOpen () && selected is MenuBarItemv2 { PopoverMenuOpen: false } selectedMenuBarItem)
        {
            ShowItem (selectedMenuBarItem);
        }
    }

    /// <inheritdoc/>
    public override void EndInit ()
    {
        base.EndInit ();

        if (Border is { })
        {
            Border.Thickness = new (0);
            Border.LineStyle = LineStyle.None;
        }

        // TODO: This needs to be done whenever a menuitem in any MenuBarItem changes
        foreach (MenuBarItemv2? mbi in SubViews.Select (s => s as MenuBarItemv2))
        {
            Application.Popover?.Register (mbi?.PopoverMenu);
        }
    }

    /// <inheritdoc/>
    protected override bool OnAccepting (CommandEventArgs args)
    {
        // Logging.Debug ($"{Title} ({args.Context?.Source?.Title})");

        // TODO: Ensure sourceMenuBar is actually one of our bar items
        if (Visible && Enabled && args.Context?.Source is MenuBarItemv2 { PopoverMenuOpen: false } sourceMenuBarItem)
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

        if (SubViews.OfType<MenuBarItemv2> ().Contains (args.Context?.Source))
        {
            return;
        }

        Active = false;
    }

    /// <summary>
    ///     Shows the specified popover, but only if the menu bar is active.
    /// </summary>
    /// <param name="menuBarItem"></param>
    private void ShowItem (MenuBarItemv2? menuBarItem)
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
        if (Application.Popover?.GetActivePopover () is PopoverMenu popoverMenu
            && popoverMenu.Root?.SuperMenuItem?.SuperView == this)
        {
            // Logging.Debug ($"{Title} - Calling Application.Popover?.Hide ({popoverMenu.Title})");
            Application.Popover.Hide (popoverMenu);
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
        menuBarItem.PopoverMenu?.MakeVisible (new Point (menuBarItem.FrameToScreen ().X, menuBarItem.FrameToScreen ().Bottom));

        menuBarItem.Accepting += OnMenuItemAccepted;

        return;

        void OnMenuItemAccepted (object? sender, EventArgs args)
        {
            // Logging.Debug ($"{Title} - OnMenuItemAccepted");
            if (menuBarItem.PopoverMenu is { })
            {
                menuBarItem.PopoverMenu.VisibleChanged -= OnMenuItemAccepted;
            }

            if (Active && menuBarItem.PopoverMenu is { Visible: false })
            {
                Active = false;
                HasFocus = false;
            }
        }
    }

    private MenuBarItemv2? GetActiveItem () { return SubViews.OfType<MenuBarItemv2> ().FirstOrDefault (sv => sv is { PopoverMenu: { Visible: true } }); }

    /// <summary>
    ///     Hides the popover menu associated with the active menu bar item and updates the focus state.
    /// </summary>
    /// <returns><see langword="true"/> if the popover was hidden</returns>
    public bool HideActiveItem () { return HideItem (GetActiveItem ()); }

    /// <summary>
    ///     Hides popover menu associated with the specified menu bar item and updates the focus state.
    /// </summary>
    /// <param name="activeItem"></param>
    /// <returns><see langword="true"/> if the popover was hidden</returns>
    public bool HideItem (MenuBarItemv2? activeItem)
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
    ///     Gets all menu items with the specified Title, anywhere in the menu hierarchy.
    /// </summary>
    /// <param name="title"></param>
    /// <returns></returns>
    public IEnumerable<MenuItemv2> GetMenuItemsWithTitle (string title)
    {
        List<MenuItemv2> menuItems = new ();

        if (string.IsNullOrEmpty (title))
        {
            return menuItems;
        }

        foreach (MenuBarItemv2 mbi in SubViews.OfType<MenuBarItemv2> ())
        {
            if (mbi.PopoverMenu is { })
            {
                menuItems.AddRange (mbi.PopoverMenu.GetMenuItemsOfAllSubMenus ());
            }
        }

        return menuItems.Where (mi => mi.Title == title);
    }

    /// <inheritdoc/>
    public bool EnableForDesign<TContext> (ref TContext context) where TContext : notnull
    {
        // Note: This menu is used by unit tests. If you modify it, you'll likely have to update
        // unit tests.

        Id = "DemoBar";

        var bordersCb = new CheckBox
        {
            Title = "_Borders",
            CheckedState = CheckState.Checked
        };

        var autoSaveCb = new CheckBox
        {
            Title = "_Auto Save"
        };

        var enableOverwriteCb = new CheckBox
        {
            Title = "Enable _Overwrite"
        };

        var mutuallyExclusiveOptionsSelector = new OptionSelector
        {
            Options = ["G_ood", "_Bad", "U_gly"],
            SelectedItem = 0
        };

        var menuBgColorCp = new ColorPicker
        {
            Width = 30
        };

        menuBgColorCp.ColorChanged += (sender, args) =>
                                      {
                                          // BUGBUG: This is weird.
                                          SetScheme (
                                                     GetScheme () with
                                                     {
                                                         Normal = new (
                                                                       GetAttributeForRole (VisualRole.Normal).Foreground,
                                                                       args.Result,
                                                                       GetAttributeForRole (VisualRole.Normal).Style)
                                                     });
                                      };

        Add (
             new MenuBarItemv2 (
                                "_File",
                                [
                                    new MenuItemv2 (context as View, Command.New),
                                    new MenuItemv2 (context as View, Command.Open),
                                    new MenuItemv2 (context as View, Command.Save),
                                    new MenuItemv2 (context as View, Command.SaveAs),
                                    new Line (),
                                    new MenuItemv2
                                    {
                                        Title = "_File Options",
                                        SubMenu = new (
                                                       [
                                                           new ()
                                                           {
                                                               Id = "AutoSave",
                                                               Text = "(no Command)",
                                                               Key = Key.F10,
                                                               CommandView = autoSaveCb
                                                           },
                                                           new ()
                                                           {
                                                               Text = "Overwrite",
                                                               Id = "Overwrite",
                                                               Key = Key.W.WithCtrl,
                                                               CommandView = enableOverwriteCb,
                                                               Command = Command.EnableOverwrite,
                                                               TargetView = context as View
                                                           },
                                                           new ()
                                                           {
                                                               Title = "_File Settings...",
                                                               HelpText = "More file settings",
                                                               Action = () => MessageBox.Query (
                                                                                                "File Settings",
                                                                                                "This is the File Settings Dialog\n",
                                                                                                "_Ok",
                                                                                                "_Cancel")
                                                           }
                                                       ]
                                                      )
                                    },
                                    new Line (),
                                    new MenuItemv2
                                    {
                                        Title = "_Preferences",
                                        SubMenu = new (
                                                       [
                                                           new MenuItemv2
                                                           {
                                                               CommandView = bordersCb,
                                                               HelpText = "Toggle Menu Borders",
                                                               Action = ToggleMenuBorders
                                                           },
                                                           new MenuItemv2
                                                           {
                                                               HelpText = "3 Mutually Exclusive Options",
                                                               CommandView = mutuallyExclusiveOptionsSelector,
                                                               Key = Key.F7
                                                           },
                                                           new Line (),
                                                           new MenuItemv2
                                                           {
                                                               HelpText = "MenuBar BG Color",
                                                               CommandView = menuBgColorCp,
                                                               Key = Key.F8
                                                           }
                                                       ]
                                                      )
                                    },
                                    new Line (),
                                    new MenuItemv2
                                    {
                                        TargetView = context as View,
                                        Key = Application.QuitKey,
                                        Command = Command.Quit
                                    }
                                ]
                               )
            );

        Add (
             new MenuBarItemv2 (
                                "_Edit",
                                [
                                    new MenuItemv2 (context as View, Command.Cut),
                                    new MenuItemv2 (context as View, Command.Copy),
                                    new MenuItemv2 (context as View, Command.Paste),
                                    new Line (),
                                    new MenuItemv2 (context as View, Command.SelectAll),
                                    new Line (),
                                    new MenuItemv2
                                    {
                                        Title = "_Details",
                                        SubMenu = new (ConfigureDetailsSubMenu ())
                                    }
                                ]
                               )
            );

        Add (
             new MenuBarItemv2 (
                                "_Help",
                                [
                                    new MenuItemv2
                                    {
                                        Title = "_Online Help...",
                                        Action = () => MessageBox.Query ("Online Help", "https://gui-cs.github.io/Terminal.Gui", "Ok")
                                    },
                                    new MenuItemv2
                                    {
                                        Title = "About...",
                                        Action = () => MessageBox.Query ("About", "Something About Mary.", "Ok")
                                    }
                                ]
                               )
            );

        return true;

        void ToggleMenuBorders ()
        {
            foreach (MenuBarItemv2 mbi in SubViews.OfType<MenuBarItemv2> ())
            {
                if (mbi is not { PopoverMenu: { } })
                {
                    continue;
                }

                foreach (Menuv2? subMenu in mbi.PopoverMenu.GetAllSubMenus ())
                {
                    if (bordersCb.CheckedState == CheckState.Checked)
                    {
                        subMenu.Border!.Thickness = new (1);
                    }
                    else
                    {
                        subMenu.Border!.Thickness = new (0);
                    }
                }
            }
        }

        MenuItemv2 [] ConfigureDetailsSubMenu ()
        {
            var detail = new MenuItemv2
            {
                Title = "_Detail 1",
                Text = "Some detail #1"
            };

            var nestedSubMenu = new MenuItemv2
            {
                Title = "_Moar Details",
                SubMenu = new (ConfigureMoreDetailsSubMenu ())
            };

            var editMode = new MenuItemv2
            {
                Text = "App Binding to Command.Edit",
                Id = "EditMode",
                Command = Command.Edit,
                CommandView = new CheckBox
                {
                    Title = "E_dit Mode"
                }
            };

            return [detail, nestedSubMenu, null!, editMode];

            View [] ConfigureMoreDetailsSubMenu ()
            {
                var deeperDetail = new MenuItemv2
                {
                    Title = "_Deeper Detail",
                    Text = "Deeper Detail",
                    Action = () => { MessageBox.Query ("Deeper Detail", "Lots of details", "_Ok"); }
                };

                var belowLineDetail = new MenuItemv2
                {
                    Title = "_Even more detail",
                    Text = "Below the line"
                };

                // This ensures the checkbox state toggles when the hotkey of Title is pressed.
                //shortcut4.Accepting += (sender, args) => args.Cancel = true;

                return [deeperDetail, new Line (), belowLineDetail];
            }
        }
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        base.Dispose (disposing);

        if (disposing)
        {
            SuperViewChanged += OnSuperViewChanged;
            ConfigurationManager.Applied -= OnConfigurationManagerApplied;
        }
    }
}
