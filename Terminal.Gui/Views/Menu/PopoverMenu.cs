#nullable enable
using System.Diagnostics;

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

            HideAndRemoveSubMenu (_root);

            _root = value;

            AddAndShowSubMenu (_root);
        }
    }

    /// <summary>
    ///     Pops up the submenu of the specified MenuItem, if there is one.
    /// </summary>
    /// <param name="menuItem"></param>
    public void ShowSubMenu (MenuItemv2? menuItem)
    {
        var menu = menuItem?.SuperView as Menuv2;

        // If there's a visible peer, remove / hide it
        
        Debug.Assert(menu is null || menu?.SubViews.Count(v => v is MenuItemv2 { SubMenu.Visible: true }) < 2);
        if (menu?.SubViews.FirstOrDefault (v => v is MenuItemv2 { SubMenu.Visible: true }) is MenuItemv2 visiblePeer)
        {
            HideAndRemoveSubMenu (visiblePeer.SubMenu);
            visiblePeer.ForceFocusColors = false;
        }

        if (menuItem is { SubMenu: { Visible: false } })
        {
            AddAndShowSubMenu (menuItem.SubMenu);

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
        GetLocationEnsuringFullVisibility (
                                           menuItem.SubMenu!,
                                           menuItem.SuperView!.Frame.X + menuItem.Frame.Width,
                                           menuItem.SuperView.Frame.Y + menuItem.Frame.Y,
                                           out int nx,
                                           out int ny);

        return new (nx, ny);
    }

    private void AddAndShowSubMenu (Menuv2? menu)
    {
        if (menu is { })
        {
            base.Add (menu);

            menu.Layout ();

            menu.Accepting += MenuOnAccepting;
            menu.MenuItemCommandInvoked += MenuOnMenuItemCommandInvoked;
            menu.SelectedMenuItemChanged += MenuOnSelectedMenuItemChanged;
        }
    }
    private void HideAndRemoveSubMenu (Menuv2? menu)
    {
        Debug.Assert (menu is null || menu is { Visible: true });
        if (menu is { Visible: true })
        {
            // If there's a visible submenu, remove / hide it
            Debug.Assert (menu?.SubViews.Count (v => v is MenuItemv2 { SubMenu.Visible: true }) < 2);
            if (menu?.SubViews.FirstOrDefault (v => v is MenuItemv2 { SubMenu.Visible: true }) is MenuItemv2 mi)
            {
                HideAndRemoveSubMenu (mi.SubMenu);
                mi.ForceFocusColors = false;
            }

            menu.Visible = false;
            menu.Accepting -= MenuOnAccepting;
            menu.MenuItemCommandInvoked -= MenuOnMenuItemCommandInvoked;
            menu.SelectedMenuItemChanged -= MenuOnSelectedMenuItemChanged;
            base.Remove (menu);
        }
    }

    private void MenuOnAccepting (object? sender, CommandEventArgs e) { Logging.Trace ($"MenuOnSelectedMenuItemChanged: {e.Context}"); }

    private void MenuOnMenuItemCommandInvoked (object? sender, CommandEventArgs e) { Logging.Trace ($"MenuOnMenuItemCommandInvoked: {e.Context}"); }

    private void MenuOnSelectedMenuItemChanged (object? sender, MenuItemv2? e)
    {
        Logging.Trace ($"MenuOnSelectedMenuItemChanged: {e}");
        ShowSubMenu (e);
    }


}
