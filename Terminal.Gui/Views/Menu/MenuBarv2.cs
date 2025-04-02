#nullable enable
using System.ComponentModel;
using System.Diagnostics;

namespace Terminal.Gui;

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
        Orientation = Orientation.Horizontal;

        Key = DefaultKey;
        AddCommand (Command.HotKey,
                   () =>
                   {
                       if (HideActiveItem ())
                       {
                           return true;
                       }

                       if (SubViews.FirstOrDefault (sv => sv is MenuBarItemv2 { PopoverMenu: { } }) is MenuBarItemv2 { } first)
                       {
                           _active = true;
                           ShowPopover (first);

                           return true;
                       }

                       return false;
                   });
        HotKeyBindings.Add (Key, Command.HotKey);

        KeyBindings.Add (Key, Command.Quit);
        KeyBindings.ReplaceCommands (Application.QuitKey, Command.Quit);

        AddCommand (
                    Command.Quit,
                    ctx =>
                    {
                        if (HideActiveItem ())
                        {
                            return true;
                        }

                        if (CanFocus)
                        {
                            CanFocus = false;
                            _active = false;

                            return true;
                        }

                        return false;//RaiseAccepted (ctx);
                    });

        AddCommand (Command.Right, MoveRight);
        KeyBindings.Add (Key.CursorRight, Command.Right);

        AddCommand (Command.Left, MoveLeft);
        KeyBindings.Add (Key.CursorLeft, Command.Left);

        return;

        bool? MoveLeft (ICommandContext? ctx) { return AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabStop); }

        bool? MoveRight (ICommandContext? ctx) { return AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop); }
    }

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

    /// <summary>Raised when <see cref="Key"/> is changed.</summary>
    public event EventHandler<KeyChangedEventArgs>? KeyChanged;

    /// <summary>The default key for activating menu bars.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key DefaultKey { get; set; } = Key.F9;

    /// <summary>
    ///     Gets whether any of the menu bar items have a visible <see cref="PopoverMenu"/>.
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public bool IsOpen ()
    {
        return SubViews.Count (sv => sv is MenuBarItemv2 { PopoverMenu: { Visible: true } }) > 0;
    }

    private bool _active;

    /// <summary>
    ///     Returns a value indicating whether the menu bar is active or not. When active, moving the mouse
    ///     over a menu bar item will activate it.
    /// </summary>
    /// <returns></returns>
    public bool IsActive ()
    {
        return _active;
    }

    /// <inheritdoc />
    protected override bool OnMouseEnter (CancelEventArgs eventArgs)
    {
        // If the MenuBar does not have focus and the mouse enters: Enable CanFocus
        // But do NOT show a Popover unless the user clicks or presses a hotkey
        if (!HasFocus)
        {
            CanFocus = true;
        }
        return base.OnMouseEnter (eventArgs);
    }

    /// <inheritdoc />
    protected override void OnMouseLeave ()
    {
        if (!IsOpen ())
        {
            CanFocus = false;
        }
        base.OnMouseLeave ();
    }

    /// <inheritdoc />
    protected override void OnHasFocusChanged (bool newHasFocus, View? previousFocusedView, View? focusedView)
    {
        if (!newHasFocus)
        {
            _active = false;
            CanFocus = false;
        }
    }

    /// <inheritdoc/>
    protected override void OnSelectedMenuItemChanged (MenuItemv2? selected)
    {
        if (selected is MenuBarItemv2 { PopoverMenu.Visible: false } selectedMenuBarItem)
        {
            ShowPopover (selectedMenuBarItem);
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
        Logging.Trace ($"{args.Context?.Source?.Title}");

        if (Visible && args.Context?.Source is MenuBarItemv2 { PopoverMenu.Visible: false } sourceMenuBarItem)
        {
            _active = true;

            if (!CanFocus)
            {
                // Enabling CanFocus will cause focus to change, which will cause OnSelectedMenuItem to change
                // This will call ShowPopover
                CanFocus = true;
                sourceMenuBarItem.SetFocus ();
            }
            else
            {
                ShowPopover (sourceMenuBarItem);
            }

            return true;
        }

        return base.OnAccepting (args);
    }

    /// <summary>
    ///     Shows the specified popover, but only if the menu bar is active.
    /// </summary>
    /// <param name="menuBarItem"></param>
    private void ShowPopover (MenuBarItemv2? menuBarItem)
    {
        Logging.Trace ($"{menuBarItem?.Id}");

        if (!_active || !Visible)
        {
            return;
        }

        //menuBarItem!.PopoverMenu.Id = menuBarItem.Id;

        // TODO: We should init the PopoverMenu in a smarter way
        if (menuBarItem?.PopoverMenu is { IsInitialized: false })
        {
            menuBarItem.PopoverMenu.BeginInit ();
            menuBarItem.PopoverMenu.EndInit ();
        }

        // If the active Application Popover is part of this MenuBar, hide it.
        //HideActivePopover ();
        if (Application.Popover?.GetActivePopover () is PopoverMenu popoverMenu
            && popoverMenu?.Root?.SuperMenuItem?.SuperView == this)
        {
            Application.Popover?.Hide (popoverMenu);
        }

        if (menuBarItem is null)
        {
            return;
        }

        if (menuBarItem.PopoverMenu is { })
        {
            menuBarItem.PopoverMenu.Accepted += (sender, args) =>
                                                {
                                                    if (HasFocus)
                                                    {
                                                        CanFocus = false;
                                                    }
                                                };
        }

        _active = true;
        CanFocus = true;
        menuBarItem.SetFocus ();

        if (menuBarItem.PopoverMenu?.Root is { })
        {
            menuBarItem.PopoverMenu.Root.SuperMenuItem = menuBarItem;
        }

        menuBarItem.PopoverMenu?.MakeVisible (new Point (menuBarItem.FrameToScreen ().X, menuBarItem.FrameToScreen ().Bottom));
    }

    private MenuBarItemv2? GetActiveItem ()
    {
        return SubViews.FirstOrDefault (sv => sv is MenuBarItemv2 { PopoverMenu: { Visible: true } }) as MenuBarItemv2;
    }

    /// <summary>
    ///     Hides the popover menu associated with the active menu bar item and updates the focus state.
    /// </summary>
    /// <returns><see langword="true"/> if the popover was hidden</returns>
    public bool HideActiveItem ()
    {
        return HideItem (GetActiveItem ());
    }

    /// <summary>
    ///     Hides popover menu associated with the specified menu bar item and updates the focus state.
    /// </summary>
    /// <param name="activeItem"></param>
    /// <returns><see langword="true"/> if the popover was hidden</returns>
    public bool HideItem (MenuBarItemv2? activeItem)
    {
        if (activeItem is null || !activeItem.PopoverMenu!.Visible)
        {
            return false;
        }
        _active = false;
        HasFocus = false;
        activeItem.PopoverMenu!.Visible = false;
        CanFocus = false;

        return true;
    }

    /// <inheritdoc/>
    public bool EnableForDesign<TContext> (ref readonly TContext context) where TContext : notnull
    {
        Add (
             new MenuBarItemv2 (
                                "_File",
                                [
                                    new MenuItemv2 (this, Command.New),
                                    new MenuItemv2 (this, Command.Open),
                                    new MenuItemv2 (this, Command.Save),
                                    new MenuItemv2 (this, Command.SaveAs),
                                    new Line (),
                                    new MenuItemv2
                                    {
                                        Title = "_Preferences",
                                        SubMenu = new (
                                                       [
                                                           new MenuItemv2
                                                           {
                                                               CommandView = new CheckBox ()
                                                               {
                                                                   Title = "O_ption",
                                                               },
                                                               HelpText = "Toggle option"
                                                           },
                                                           new MenuItemv2
                                                           {
                                                               Title = "_Settings...",
                                                               HelpText = "More settings",
                                                               Action = () =>  MessageBox.Query ("Settings", "This is the Settings Dialog\n", ["_Ok", "_Cancel"])
                                                           }
                                                       ]
                                                      )
                                    },
                                    new Line (),
                                    new MenuItemv2 (this, Command.Quit)
                                ]
                               )
            );

        Add (
             new MenuBarItemv2 (
                                "_Edit",
                                [
                                    new MenuItemv2 (this, Command.Cut),
                                    new MenuItemv2 (this, Command.Copy),
                                    new MenuItemv2 (this, Command.Paste),
                                    new Line (),
                                    new MenuItemv2 (this, Command.SelectAll)
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
                                        Action = () => MessageBox.Query ("Online Help", "https://gui-cs.github.io/Terminal.GuiV2Docs", "Ok")
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
    }
}
