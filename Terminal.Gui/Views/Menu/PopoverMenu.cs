#nullable enable
namespace Terminal.Gui;

/// <summary>
/// </summary>
public class PopoverMenu : View
{
    /// <summary>
    /// </summary>
    public PopoverMenu () : this (null) { }

    /// <summary>
    /// </summary>
    public PopoverMenu (Menuv2? root)
    {
        CanFocus = true;
        Width = Dim.Fill ();
        Height = Dim.Fill ();
        ViewportSettings = ViewportSettings.Transparent | ViewportSettings.TransparentMouse;

        //base.Visible = false;
        base.ColorScheme = Colors.ColorSchemes ["Menu"];

        Root = root;

        AddCommand (Command.Right, MoveRight);

        bool? MoveRight (ICommandContext? ctx)
        {
            var focused = MostFocused as MenuItemv2;

            if (focused is { SubMenu.Visible: true })
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
                base.Remove (_root);
                _root.Accepting -= RootOnAccepting;
                _root.MenuItemCommandInvoked -= RootOnMenuItemCommandInvoked;
                _root.SelectedMenuItemChanged -= RootOnSelectedMenuItemChanged;
            }

            _root = value;

            if (_root is { })
            {
                base.Add (_root);
                _root.Accepting += RootOnAccepting;
                _root.MenuItemCommandInvoked += RootOnMenuItemCommandInvoked;
                _root.SelectedMenuItemChanged += RootOnSelectedMenuItemChanged;
            }

            return;

            void RootOnMenuItemCommandInvoked (object? sender, CommandEventArgs e) { Logging.Trace ($"RootOnMenuItemCommandInvoked: {e.Context}"); }

            void RootOnAccepting (object? sender, CommandEventArgs e) { Logging.Trace ($"RootOnAccepting: {e.Context}"); }

            void RootOnSelectedMenuItemChanged (object? sender, MenuItemv2? e)
            {
                Logging.Trace ($"RootOnSelectedMenuItemChanged: {e!.Title}");
                ShowSubMenu (e);
            }
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="menuItem"></param>
    public void ShowSubMenu (MenuItemv2? menuItem)
    {
        if (menuItem is null)
        {
            return;
        }

        // Hide any other submenus that might be visible
        // BUBUG: I think this won't work for cascading 
        foreach (MenuItemv2 mi in menuItem!.SuperView!.SubViews.Where (v => v is MenuItemv2 { SubMenu.Visible: true }).Cast<MenuItemv2> ())
        {
            mi.ForceFocusColors = false;
            mi.SubMenu!.Visible = false;
            Remove (mi.SubMenu);
        }

        if (menuItem is { SubMenu: { Visible: false } })
        {
            Add (menuItem.SubMenu);
            menuItem.SubMenu.Layout ();
            Point pos = GetMostVisibleLocationForSubMenu (menuItem);
            menuItem.SubMenu.X = pos.X;
            menuItem.SubMenu.Y = pos.Y;

            menuItem.SubMenu.Visible = true;
            menuItem.ForceFocusColors = true;
        }
    }

    /// <summary>
    ///     Given a <see cref="MenuItemv2"/>, returns the most visible location for the submenu.
    ///     The location is relative to the Frame.
    /// </summary>
    /// <param name="menuItem"></param>
    /// <returns></returns>
    internal Point GetMostVisibleLocationForSubMenu (MenuItemv2 menuItem)
    {
        var pos = Point.Empty;

        // Calculate the initial position to the right of the menu item
        pos.X = menuItem.SuperView!.Frame.X + menuItem.Frame.Width;
        pos.Y = menuItem.SuperView.Frame.Y + menuItem.Frame.Y;

        GetLocationEnsuringFullVisibility (menuItem.SubMenu!, pos.X, pos.Y, out int nx, out int ny);

        return new (nx, ny);
    }
}
