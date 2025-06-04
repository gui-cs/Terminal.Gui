#nullable enable

namespace Terminal.Gui.Views;

/// <summary>
///     Provides a cascading menu that pops over all other content. Can be used as a context menu or a drop-down
///     all other content. Can be used as a context menu or a drop-down
///     menu as part of <see cref="MenuBarv2"/> as part of <see cref="MenuBarv2"/>.
/// </summary>
/// <remarks>
///     <para>
///         To use as a context menu, register the popover menu with <see cref="Application.Popover"/> and call
///         <see cref="MakeVisible"/>.
///     </para>
/// </remarks>
public class PopoverMenu : PopoverBaseImpl, IDesignable
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PopoverMenu"/> class.
    /// </summary>
    public PopoverMenu () : this ((Menuv2?)null) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PopoverMenu"/> class. If any of the elements of
    ///     <paramref name="menuItems"/> is <see langword="null"/>,
    ///     a see <see cref="Line"/> will be created instead.
    /// </summary>
    public PopoverMenu (IEnumerable<View>? menuItems) : this (
                                                              new Menuv2 (menuItems?.Select (item => item ?? new Line ()))
                                                              {
                                                                  Title = "Popover Root"
                                                              })
    { }

    /// <inheritdoc/>
    public PopoverMenu (IEnumerable<MenuItemv2>? menuItems) : this (
                                                                    new Menuv2 (menuItems)
                                                                    {
                                                                        Title = "Popover Root"
                                                                    })
    { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PopoverMenu"/> class with the specified root <see cref="Menuv2"/>.
    /// </summary>
    public PopoverMenu (Menuv2? root)
    {
        // Do this to support debugging traces where Title gets set
        base.HotKeySpecifier = (Rune)'\xffff';

        if (Border is { })
        {
            Border.Settings &= ~BorderSettings.Title;
        }

        Key = DefaultKey;

        base.Visible = false;

        Root = root;

        AddCommand (Command.Right, MoveRight);
        KeyBindings.Add (Key.CursorRight, Command.Right);

        AddCommand (Command.Left, MoveLeft);
        KeyBindings.Add (Key.CursorLeft, Command.Left);

        // PopoverBaseImpl sets a key binding for Quit, so we
        // don't need to do it here.
        AddCommand (Command.Quit, Quit);

        return;

        bool? Quit (ICommandContext? ctx)
        {
            // Logging.Debug ($"{Title} Command.Quit - {ctx?.Source?.Title}");

            if (!Visible)
            {
                // If we're not visible, the command is not for us
                return false;
            }

            // This ensures the quit command gets propagated to the owner of the popover.
            // This is important for MenuBarItems to ensure the MenuBar loses focus when
            // the user presses QuitKey to cause the menu to close.
            // Note, we override OnAccepting, which will set Visible to false
            // Logging.Debug ($"{Title} Command.Quit - Calling RaiseAccepting {ctx?.Source?.Title}");
            bool? ret = RaiseAccepting (ctx);

            if (Visible && ret is not true)
            {
                Visible = false;

                return true;
            }

            // If we are Visible, returning true will stop the QuitKey from propagating
            // If we are not Visible, returning false will allow the QuitKey to propagate
            return Visible;
        }

        bool? MoveLeft (ICommandContext? ctx)
        {
            if (Focused == Root)
            {
                return false;
            }

            if (MostFocused is MenuItemv2 { SuperView: Menuv2 focusedMenu })
            {
                focusedMenu.SuperMenuItem?.SetFocus ();

                return true;
            }

            return AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabStop);
        }

        bool? MoveRight (ICommandContext? ctx)
        {
            if (MostFocused is MenuItemv2 { SubMenu.Visible: true } focused)
            {
                focused.SubMenu.SetFocus ();

                return true;
            }

            return false;
        }
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

    /// <summary>Raised when <see cref="Key"/> is changed.</summary>
    public event EventHandler<KeyChangedEventArgs>? KeyChanged;

    /// <summary>The default key for activating popover menus.</summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key DefaultKey { get; set; } = Key.F10.WithShift;

    /// <summary>
    ///     The mouse flags that will cause the popover menu to be visible. The default is
    ///     <see cref="MouseFlags.Button3Clicked"/> which is typically the right mouse button.
    /// </summary>
    public MouseFlags MouseFlags { get; set; } = MouseFlags.Button3Clicked;

    /// <summary>
    ///     Makes the popover menu visible and locates it at <paramref name="idealScreenPosition"/>. The actual position of the
    ///     menu
    ///     will be adjusted to
    ///     ensure the menu fully fits on the screen, and the mouse cursor is over the first cell of the
    ///     first MenuItem.
    /// </summary>
    /// <param name="idealScreenPosition">If <see langword="null"/>, the current mouse position will be used.</param>
    public void MakeVisible (Point? idealScreenPosition = null)
    {
        if (Visible)
        {
            // Logging.Debug ($"{Title} - Already Visible");

            return;
        }

        UpdateKeyBindings ();
        SetPosition (idealScreenPosition);
        Application.Popover?.Show (this);
    }

    /// <summary>
    ///     Locates the popover menu at <paramref name="idealScreenPosition"/>. The actual position of the menu will be
    ///     adjusted to
    ///     ensure the menu fully fits on the screen, and the mouse cursor is over the first cell of the
    ///     first MenuItem (if possible).
    /// </summary>
    /// <param name="idealScreenPosition">If <see langword="null"/>, the current mouse position will be used.</param>
    public void SetPosition (Point? idealScreenPosition = null)
    {
        idealScreenPosition ??= Application.GetLastMousePosition ();

        if (idealScreenPosition is null || Root is null)
        {
            return;
        }

        Point pos = idealScreenPosition.Value;

        if (!Root.IsInitialized)
        {
            Root.BeginInit ();
            Root.EndInit ();
            Root.Layout ();
        }

        pos = GetMostVisibleLocationForSubMenu (Root, pos);

        Root.X = pos.X;
        Root.Y = pos.Y;
    }

    /// <inheritdoc/>
    protected override void OnVisibleChanged ()
    {
        // Logging.Debug ($"{Title} - Visible: {Visible}");
        base.OnVisibleChanged ();

        if (Visible)
        {
            AddAndShowSubMenu (_root);
        }
        else
        {
            HideAndRemoveSubMenu (_root);
            Application.Popover?.Hide (this);
        }
    }

    private Menuv2? _root;

    /// <summary>
    ///     Gets or sets the <see cref="Menuv2"/> that is the root of the Popover Menu.
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

            // TODO: This needs to be done whenever any MenuItem in the menu tree changes to support dynamic menus
            // TODO: And it needs to clear the old bindings first
            UpdateKeyBindings ();

            // TODO: This needs to be done whenever any MenuItem in the menu tree changes to support dynamic menus
            IEnumerable<Menuv2> allMenus = GetAllSubMenus ();

            foreach (Menuv2 menu in allMenus)
            {
                menu.Visible = false;
                menu.Accepting += MenuOnAccepting;
                menu.Accepted += MenuAccepted;
                menu.SelectedMenuItemChanged += MenuOnSelectedMenuItemChanged;
            }
        }
    }

    private void UpdateKeyBindings ()
    {
        IEnumerable<MenuItemv2> all = GetMenuItemsOfAllSubMenus ();

        foreach (MenuItemv2 menuItem in all.Where (mi => mi.Command != Command.NotBound))
        {
            Key? key;

            if (menuItem.TargetView is { })
            {
                // A TargetView implies HotKey
                key = menuItem.TargetView.HotKeyBindings.GetFirstFromCommands (menuItem.Command);
            }
            else
            {
                // No TargetView implies Application HotKey
                key = Application.KeyBindings.GetFirstFromCommands (menuItem.Command);
            }

            if (key is not { IsValid: true })
            {
                continue;
            }

            if (menuItem.Key.IsValid)
            {
                //Logging.Warning ("Do not specify a Key for MenuItems where a Command is specified. Key will be determined automatically.");
            }

            menuItem.Key = key;

            // Logging.Debug ($"{Title} - HotKey: {menuItem.Key}->{menuItem.Command}");
        }
    }

    /// <inheritdoc/>
    protected override bool OnKeyDownNotHandled (Key key)
    {
        // See if any of our MenuItems have this key as Key
        IEnumerable<MenuItemv2> all = GetMenuItemsOfAllSubMenus ();

        foreach (MenuItemv2 menuItem in all)
        {
            if (key != Application.QuitKey && menuItem.Key == key)
            {
                // Logging.Debug ($"{Title} - key: {key}");

                return menuItem.NewKeyDownEvent (key);
            }
        }

        return base.OnKeyDownNotHandled (key);
    }

    /// <summary>
    ///     Gets all the submenus in the PopoverMenu.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Menuv2> GetAllSubMenus ()
    {
        List<Menuv2> result = [];

        if (Root == null)
        {
            return result;
        }

        Stack<Menuv2> stack = new ();
        stack.Push (Root);

        while (stack.Count > 0)
        {
            Menuv2 currentMenu = stack.Pop ();
            result.Add (currentMenu);

            foreach (View subView in currentMenu.SubViews)
            {
                if (subView is MenuItemv2 { SubMenu: { } } menuItem)
                {
                    stack.Push (menuItem.SubMenu);
                }
            }
        }

        return result;
    }

    /// <summary>
    ///     Gets all the MenuItems in the PopoverMenu.
    /// </summary>
    /// <returns></returns>
    internal IEnumerable<MenuItemv2> GetMenuItemsOfAllSubMenus ()
    {
        List<MenuItemv2> result = [];

        foreach (Menuv2 menu in GetAllSubMenus ())
        {
            foreach (View subView in menu.SubViews)
            {
                if (subView is MenuItemv2 menuItem)
                {
                    result.Add (menuItem);
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

        // Logging.Debug ($"{Title} - menuItem: {menuItem?.Title}, menu: {menu?.Title}");

        menu?.Layout ();

        // If there's a visible peer, remove / hide it
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
        if (menu is { SuperView: null, Visible: false })
        {
            // Logging.Debug ($"{Title} ({menu?.Title}) - menu.Visible: {menu?.Visible}");

            // TODO: Find the menu item below the mouse, if any, and select it

            if (!menu!.IsInitialized)
            {
                menu.BeginInit ();
                menu.EndInit ();
            }

            menu.ClearFocus ();
            base.Add (menu);

            // IMPORTANT: This must be done after adding the menu to the super view or Add will try
            // to set focus to it.
            menu.Visible = true;

            menu.Layout ();
        }
    }

    private void HideAndRemoveSubMenu (Menuv2? menu)
    {
        if (menu is { Visible: true })
        {
            // Logging.Debug ($"{Title} ({menu?.Title}) - menu.Visible: {menu?.Visible}");

            // If there's a visible submenu, remove / hide it
            if (menu.SubViews.FirstOrDefault (v => v is MenuItemv2 { SubMenu.Visible: true }) is MenuItemv2 visiblePeer)
            {
                HideAndRemoveSubMenu (visiblePeer.SubMenu);
                visiblePeer.ForceFocusColors = false;
            }

            menu.Visible = false;
            menu.ClearFocus ();
            base.Remove (menu);

            if (menu == Root)
            {
                Visible = false;
            }
        }
    }

    private void MenuOnAccepting (object? sender, CommandEventArgs e)
    {
        var senderView = sender as View;
        // Logging.Debug ($"{Title} ({e.Context?.Source?.Title}) Command: {e.Context?.Command} - Sender: {senderView?.GetType ().Name}");

        if (e.Context?.Command != Command.HotKey)
        {
            // Logging.Debug ($"{Title} - Setting Visible = false");
            Visible = false;
        }

        if (e.Context is CommandContext<KeyBinding> keyCommandContext)
        {
            if (keyCommandContext.Binding.Key is { } && keyCommandContext.Binding.Key == Application.QuitKey && SuperView is { Visible: true })
            {
                // Logging.Debug ($"{Title} - Setting e.Handled = true - Application.QuitKey/Command = Command.Quit");
                e.Handled = true;
            }
        }
    }

    private void MenuAccepted (object? sender, CommandEventArgs e)
    {
        // Logging.Debug ($"{Title} ({e.Context?.Source?.Title}) Command: {e.Context?.Command}");

        if (e.Context?.Source is MenuItemv2 { SubMenu: null })
        {
            HideAndRemoveSubMenu (_root);
        }
        else if (e.Context?.Source is MenuItemv2 { SubMenu: { } } menuItemWithSubMenu)
        {
            ShowSubMenu (menuItemWithSubMenu);
        }

        RaiseAccepted (e.Context);
    }

    /// <inheritdoc/>
    protected override bool OnAccepting (CommandEventArgs args)
    {
        // Logging.Debug ($"{Title} ({args.Context?.Source?.Title}) Command: {args.Context?.Command}");

        // If we're not visible, ignore any keys that are not hotkeys
        CommandContext<KeyBinding>? keyCommandContext = args.Context as CommandContext<KeyBinding>? ?? default (CommandContext<KeyBinding>);

        if (!Visible && keyCommandContext is { Binding.Key: { } })
        {
            if (GetMenuItemsOfAllSubMenus ().All (i => i.Key != keyCommandContext.Value.Binding.Key))
            {
                // Logging.Debug ($"{Title} ({args.Context?.Source?.Title}) Command: {args.Context?.Command} - ignore any keys that are not hotkeys");

                return false;
            }
        }

        // Logging.Debug ($"{Title} - calling base.OnAccepting: {args.Context?.Command}");
        bool? ret = base.OnAccepting (args);

        if (ret is true || args.Handled)
        {
            return args.Handled = true;
        }

        // Only raise Accepted if the command came from one of our MenuItems
        //if (GetMenuItemsOfAllSubMenus ().Contains (args.Context?.Source))
        {
            // Logging.Debug ($"{Title} - Calling RaiseAccepted {args.Context?.Command}");
            RaiseAccepted (args.Context);
        }

        // Always return false to enable accepting to continue propagating
        return false;
    }

    /// <summary>
    ///     Raises the <see cref="OnAccepted"/>/<see cref="Accepted"/> event indicating a menu (or submenu)
    ///     was accepted and the Menus in the PopoverMenu were hidden. Use this to determine when to hide the PopoverMenu.
    /// </summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    protected void RaiseAccepted (ICommandContext? ctx)
    {
        // Logging.Debug ($"{Title} - RaiseAccepted: {ctx}");
        CommandEventArgs args = new () { Context = ctx };

        OnAccepted (args);
        Accepted?.Invoke (this, args);
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
        // Logging.Debug ($"{Title} - e.Title: {e?.Title}");
        ShowSubMenu (e);
    }

    /// <inheritdoc/>
    protected override void OnSubViewAdded (View view)
    {
        if (Root is null && (view is Menuv2 || view is MenuItemv2))
        {
            throw new InvalidOperationException ("Do not add MenuItems or Menus directly to a PopoverMenu. Use the Root property.");
        }

        base.OnSubViewAdded (view);
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            IEnumerable<Menuv2> allMenus = GetAllSubMenus ();

            foreach (Menuv2 menu in allMenus)
            {
                menu.Accepting -= MenuOnAccepting;
                menu.Accepted -= MenuAccepted;
                menu.SelectedMenuItemChanged -= MenuOnSelectedMenuItemChanged;
            }

            _root?.Dispose ();
            _root = null;
        }

        base.Dispose (disposing);
    }

    /// <inheritdoc/>
    public bool EnableForDesign<TContext> (ref TContext context) where TContext : notnull
    {
        // Note: This menu is used by unit tests. If you modify it, you'll likely have to update
        // unit tests.

        Root = new (
                    [
                        new MenuItemv2 (context as View, Command.Cut),
                        new MenuItemv2 (context as View, Command.Copy),
                        new MenuItemv2 (context as View, Command.Paste),
                        new Line (),
                        new MenuItemv2 (context as View, Command.SelectAll),
                        new Line (),
                        new MenuItemv2 (context as View, Command.Quit)
                    ])
        {
            Title = "Popover Demo Root"
        };

        // NOTE: This is a workaround for the fact that the PopoverMenu is not visible in the designer
        // NOTE: without being activated via Application.Popover. But we want it to be visible.
        // NOTE: If you use PopoverView.EnableForDesign for real Popover scenarios, change back to false
        // NOTE: after calling EnableForDesign.
        //Visible = true;

        return true;
    }
}
