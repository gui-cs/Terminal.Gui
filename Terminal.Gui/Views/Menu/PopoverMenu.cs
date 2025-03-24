#nullable enable
using System.Diagnostics;

namespace Terminal.Gui;

/// <summary>
/// </summary>
public class PopoverMenu : PopoverBaseImpl
{
    /// <summary>
    /// </summary>
    public PopoverMenu () : this (null) { }

    /// <summary>
    /// </summary>
    public PopoverMenu (Menuv2? root)
    {
        base.Visible = false;
        base.ColorScheme = Colors.ColorSchemes ["Menu"];

        Root = root;

        AddCommand (Command.Right, MoveRight);

        bool? MoveRight (ICommandContext? ctx)
        {
            if (MostFocused is MenuItemv2 { SubMenu.Visible: true } focused)
            {
                focused.SubMenu.SetFocus ();

                return true;
            }

            return AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
        }

        KeyBindings.Add (Key.CursorRight, Command.Right);

        AddCommand (Command.Left, MoveLeft);

        bool? MoveLeft (ICommandContext? ctx)
        {
            if (MostFocused is MenuItemv2 { SuperView: Menuv2 focusedMenu })
            {
                focusedMenu.SuperMenuItem?.SetFocus ();

                return true;
            }

            return AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabStop);
        }

        KeyBindings.Add (Key.CursorLeft, Command.Left);

        AddCommand (
                    Command.NotBound,
                    ctx =>
                    {
                        Logging.Trace ($"popoverMenu NotBound: {ctx}");

                        return false;
                    });

        KeyBindings.Add (DefaultKey, Command.Quit);
        KeyBindings.ReplaceCommands (Application.QuitKey, Command.Quit);

        AddCommand (
                    Command.Quit,
                    ctx =>
                    {
                        Visible = false;

                        return RaiseAccepted (ctx);
                    });
    }

    /// <summary>
    ///     The mouse flags that will cause the popover menu to be visible. The default is
    ///     <see cref="MouseFlags.Button3Clicked"/> which is typically the right mouse button.
    /// </summary>
    public static MouseFlags MouseFlags { get; set; } = MouseFlags.Button3Clicked;

    /// <summary>The default key for activating the popover menu.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key DefaultKey { get; set; } = Key.F10.WithShift;

    /// <summary>
    ///     Makes the menu visible and locates it at <paramref name="idealScreenPosition"/>. The actual position of the menu
    ///     will be adjusted to
    ///     ensure the menu fully fits on the screen, and the mouse cursor is over the first cell of the
    ///     first MenuItem.
    /// </summary>
    /// <param name="idealScreenPosition">If <see langword="null"/>, the current mouse position will be used.</param>
    public void MakeVisible (Point? idealScreenPosition = null)
    {
        Visible = true;
        SetPosition (idealScreenPosition);
    }

    /// <summary>
    ///     Locates the menu at <paramref name="idealScreenPosition"/>. The actual position of the menu will be adjusted to
    ///     ensure the menu fully fits on the screen, and the mouse cursor is over the first cell of the
    ///     first MenuItem (if possible).
    /// </summary>
    /// <param name="idealScreenPosition">If <see langword="null"/>, the current mouse position will be used.</param>
    public void SetPosition (Point? idealScreenPosition = null)
    {
        idealScreenPosition ??= Application.GetLastMousePosition ();

        if (idealScreenPosition is { } && Root is { })
        {
            Point pos = idealScreenPosition.Value;
            pos.Offset (-Root.GetAdornmentsThickness ().Left, -Root.GetAdornmentsThickness ().Top);

            pos = GetMostVisibleLocationForSubMenu (Root, ScreenToViewport (pos));

            Root.X = pos.X;
            Root.Y = pos.Y;
        }
    }

    /// <inheritdoc/>
    protected override void OnVisibleChanged ()
    {
        base.OnVisibleChanged ();

        if (Visible)
        {
            AddAndShowSubMenu (_root);
        }
        else
        {
            HideAndRemoveSubMenu (_root);
        }
    }

    private Menuv2? _root;

    /// <summary>
    ///     Gets or sets the <seealso cref="Menuv2"/> that is the root of the Popover Menu.
    /// </summary>
    public Menuv2? Root
    {
        get => _root;
        set
        {
            if (_root == value)
            {
                return;
            }

            if (_root is { })
            {
                _root.Accepting -= MenuOnAccepting;
            }

            HideAndRemoveSubMenu (_root);

            _root = value;

            if (_root is { })
            {
                _root.Accepting += MenuOnAccepting;
            }

            //AddAndShowSubMenu (_root);

            // TODO: This needs to be done whenever any MenuItem in the menu tree changes to support dynamic menus
            // TODO: And it needs to clear them first
            IEnumerable<MenuItemv2> all = GetMenuItemsOfAllSubMenus ();

            foreach (MenuItemv2 menu in all)
            {
                if (menu.Key.IsValid)
                {
                    Logging.Trace ($"{menu.Key}->{menu.Command}");
                    KeyBindings.Add (menu.Key, menu.Command);
                }
            }
        }
    }

    internal IEnumerable<MenuItemv2> GetMenuItemsOfAllSubMenus ()
    {
        List<MenuItemv2> result = [];

        if (Root == null)
        {
            return result;
        }

        Stack<Menuv2> stack = new ();
        stack.Push (Root);

        while (stack.Count > 0)
        {
            Menuv2 currentMenu = stack.Pop ();

            foreach (View subView in currentMenu.SubViews)
            {
                if (subView is MenuItemv2 menuItem)
                {
                    result.Add (menuItem);

                    if (menuItem.SubMenu != null)
                    {
                        stack.Push (menuItem.SubMenu);
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    ///     Pops up the submenu of the specified MenuItem, if there is one.
    /// </summary>
    /// <param name="menuItem"></param>
    internal void ShowSubMenu (MenuItemv2? menuItem)
    {
        var menu = menuItem?.SuperView as Menuv2;

        // If there's a visible peer, remove / hide it

        Debug.Assert (menu is null || menu?.SubViews.Count (v => v is MenuItemv2 { SubMenu.Visible: true }) < 2);

        if (menu?.SubViews.FirstOrDefault (v => v is MenuItemv2 { SubMenu.Visible: true }) is MenuItemv2 visiblePeer)
        {
            HideAndRemoveSubMenu (visiblePeer.SubMenu);
            visiblePeer.ForceFocusColors = false;
        }

        if (menuItem is { SubMenu: { Visible: false } })
        {
            AddAndShowSubMenu (menuItem.SubMenu);

            Point idealLocation = ScreenToViewport (
                                                    new (
                                                         menuItem.FrameToScreen ().Right - menuItem.SubMenu.GetAdornmentsThickness ().Left,
                                                         menuItem.FrameToScreen ().Top - menuItem.SubMenu.GetAdornmentsThickness ().Top));

            Point pos = GetMostVisibleLocationForSubMenu (menuItem.SubMenu, idealLocation);
            menuItem.SubMenu.X = pos.X;
            menuItem.SubMenu.Y = pos.Y;

            menuItem.SubMenu.Visible = true;
            menuItem.ForceFocusColors = true;
        }
    }

    /// <summary>
    ///     Gets the most visible screen-relative location for <paramref name="menu"/>.
    /// </summary>
    /// <param name="menu">The menu to locate.</param>
    /// <param name="idealLocation">Ideal screen-relative location.</param>
    /// <returns></returns>
    internal Point GetMostVisibleLocationForSubMenu (Menuv2 menu, Point idealLocation)
    {
        var pos = Point.Empty;

        // Calculate the initial position to the right of the menu item
        GetLocationEnsuringFullVisibility (
                                           menu,
                                           idealLocation.X,
                                           idealLocation.Y,
                                           out int nx,
                                           out int ny);

        return new (nx, ny);
    }

    private void AddAndShowSubMenu (Menuv2? menu)
    {
        if (menu is { SuperView: null })
        {
            base.Add (menu);
            menu.Visible = true;
            menu.Layout ();

            menu.Accepting += MenuOnAccepting;
            menu.Accepted += MenuAccepted;
            menu.SelectedMenuItemChanged += MenuOnSelectedMenuItemChanged;

            // TODO: Find the menu item below the mouse, if any, and select it
        }
    }

    private void HideAndRemoveSubMenu (Menuv2? menu)
    {
        if (menu is { Visible: true })
        {
            // If there's a visible submenu, remove / hide it
            Debug.Assert (menu.SubViews.Count (v => v is MenuItemv2 { SubMenu.Visible: true }) <= 1);

            if (menu.SubViews.FirstOrDefault (v => v is MenuItemv2 { SubMenu.Visible: true }) is MenuItemv2 visiblePeer)
            {
                HideAndRemoveSubMenu (visiblePeer.SubMenu);
                visiblePeer.ForceFocusColors = false;
            }

            menu.Visible = false;
            menu.Accepting -= MenuOnAccepting;
            menu.Accepted -= MenuAccepted;
            menu.SelectedMenuItemChanged -= MenuOnSelectedMenuItemChanged;
            base.Remove (menu);
        }
    }

    private void MenuOnAccepting (object? sender, CommandEventArgs e)
    {
        //Logging.Trace ($"{e.Context?.Source?.Title}");
    }

    private void MenuAccepted (object? sender, CommandEventArgs e)
    {
        Logging.Trace ($"{e.Context?.Source?.Title}");

        if (e.Context?.Source is MenuItemv2 { SubMenu: null })
        {
            HideAndRemoveSubMenu (_root);
            RaiseAccepted (e.Context);
        }
    }

    /// <summary>
    ///     Riases the <see cref="OnAccepted"/>/<see cref="Accepted"/> event indicating a menu (or submenu)
    ///     was accepted and the Menus in the PopoverMenu were hidden. Use this to determine when to hide the PopoverMenu.
    /// </summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    protected bool? RaiseAccepted (ICommandContext? ctx)
    {
        Logging.Trace ($"RaiseAccepted: {ctx}");
        CommandEventArgs args = new () { Context = ctx };

        OnAccepted (args);
        Accepted?.Invoke (this, args);

        return true;
    }

    /// <summary>
    ///     Called when the user has accepted an item in this menu (or submenu. This is used to determine when to hide the
    ///     menu.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="args"></param>
    protected virtual void OnAccepted (CommandEventArgs args) { }

    /// <summary>
    ///     Raised when the user has accepted an item in this menu (or submenu. This is used to determine when to hide the
    ///     menu.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         See <see cref="RaiseAccepted"/> for more information.
    ///     </para>
    /// </remarks>
    public event EventHandler<CommandEventArgs>? Accepted;

    private void MenuOnSelectedMenuItemChanged (object? sender, MenuItemv2? e)
    {
        //Logging.Trace ($"{e}");
        ShowSubMenu (e);
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            _root?.Dispose ();
            _root = null;
        }

        base.Dispose (disposing);
    }
}
