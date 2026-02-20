namespace Terminal.Gui.Views;

/// <summary>
///     A <see cref="PopoverBaseImpl"/>-derived view that provides a cascading menu.
///     Can be used as a context menu or a drop-down menu as part of <see cref="MenuBar"/>.
/// </summary>
/// <remarks>
///     <para>
///         <b>IMPORTANT:</b> Must be registered with <see cref="Application.Popover"/> via
///         <see cref="ApplicationPopover.Register"/> before calling <see cref="MakeVisible"/> or
///         <see cref="ApplicationPopover.Show"/>.
///     </para>
///     <para>
///         <b>Usage Example:</b>
///     </para>
///     <code>
///         var menu = new PopoverMenu ([
///             new MenuItem ("Cut", Command.Cut),
///             new MenuItem ("Copy", Command.Copy),
///             new MenuItem ("Paste", Command.Paste)
///         ]);
///         Application.Popover?.Register (menu);
///         menu.MakeVisible (); // or Application.Popover?.Show (menu);
///     </code>
///     <para>
///         See <see cref="PopoverBaseImpl"/> and <see cref="IPopover"/> for lifecycle, focus, and keyboard handling
///         details.
///     </para>
/// </remarks>
public class PopoverMenu : PopoverBaseImpl, IDesignable
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PopoverMenu"/> class.
    /// </summary>
    public PopoverMenu () : this ((Menu?)null) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PopoverMenu"/> class. If any of the elements of
    ///     <paramref name="menuItems"/> is <see langword="null"/>, a <see cref="Line"/> will be created instead.
    /// </summary>
    /// <param name="menuItems">The views to use as menu items. Null elements become separator lines.</param>
    /// <remarks>
    ///     Remember to call <see cref="ApplicationPopover.Register"/> before calling <see cref="MakeVisible"/>.
    /// </remarks>
    public PopoverMenu (IEnumerable<View>? menuItems) : this (new Menu (menuItems?.Select (item => item ?? new Line ()))) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PopoverMenu"/> class with the specified menu items.
    /// </summary>
    /// <param name="menuItems">The menu items to display in the popover.</param>
    /// <remarks>
    ///     Remember to call <see cref="ApplicationPopover.Register"/> before calling <see cref="MakeVisible"/>.
    /// </remarks>
    public PopoverMenu (IEnumerable<MenuItem>? menuItems) : this (new Menu (menuItems)) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PopoverMenu"/> class with the specified root <see cref="Menu"/>.
    /// </summary>
    /// <param name="root">The root menu that contains the top-level menu items.</param>
    /// <remarks>
    ///     Remember to call <see cref="ApplicationPopover.Register"/> before calling <see cref="MakeVisible"/>.
    /// </remarks>
    public PopoverMenu (Menu? root)
    {
        // Do this to support debugging traces where Title gets set
        base.HotKeySpecifier = (Rune)'\xffff';

        Border?.Settings &= ~BorderSettings.Title;

        Key = DefaultKey;

        base.Visible = false;

        Root = root;

        AddCommand (Command.Right, MoveRight);
        KeyBindings.Add (Key.CursorRight, Command.Right);

        AddCommand (Command.Left, MoveLeft);
        KeyBindings.Add (Key.CursorLeft, Command.Left);

        KeyBindings.Remove (Key.Space);

        // PopoverBaseImpl sets a key binding for Quit, so we
        // don't need to do it here.
        AddCommand (Command.Quit, Quit);

        return;

        bool? Quit (ICommandContext? ctx)
        {
            Logging.Debug ($"{this.ToIdentifyingString ()} {ctx}");

            if (!Visible)
            {
                // If we're not visible, the command is not for us
                return false;
            }

            // This ensures the quit command gets propagated to the owner of the popover.
            // This is important for MenuBarItems to ensure the MenuBar loses focus when
            // the user presses QuitKey to cause the menu to close.
            // Note, we override OnAccepting, which will set Visible to false
            // Logging.Debug ($"{this.ToIdentifyingString ()} Command.Quit - Calling RaiseAccepting {ctx?.Source?.Title}");
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

            if (MostFocused is MenuItem { SuperView: Menu focusedMenu })
            {
                focusedMenu.SuperMenuItem?.SetFocus ();

                return true;
            }

            return AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabStop);
        }

        bool? MoveRight (ICommandContext? ctx)
        {
            if (MostFocused is MenuItem { SubMenu.Visible: true } focused)
            {
                focused.SubMenu.SetFocus ();

                return true;
            }

            return false;
        }
    }

    private Key _key = DefaultKey;

    /// <summary>
    ///     Gets or sets the key that will activate the popover menu when it is registered but not visible.
    /// </summary>
    /// <remarks>
    ///     This key binding works as a global hotkey when the popover is registered with
    ///     <see cref="Application.Popover"/>. The default value is <see cref="DefaultKey"/> (<see cref="Key.F10"/> with
    ///     Shift).
    /// </remarks>
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

    /// <summary>
    ///     Raised when the <see cref="Key"/> property is changed.
    /// </summary>
    public event EventHandler<KeyChangedEventArgs>? KeyChanged;

    /// <summary>
    ///     Gets or sets the default key for activating popover menus. The default value is <see cref="Key.F10"/> with Shift.
    /// </summary>
    /// <remarks>
    ///     This is a configuration property that affects all new <see cref="PopoverMenu"/> instances.
    /// </remarks>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key DefaultKey { get; set; } = Key.F10.WithShift;

    /// <summary>
    ///     The mouse flags that will cause the popover menu to be visible. The default is
    ///     <see cref="MouseFlags.RightButtonClicked"/> which is typically the right mouse button.
    /// </summary>
    public MouseFlags MouseFlags { get; set; } = MouseFlags.RightButtonClicked;

    /// <summary>
    ///     Makes the popover menu visible and locates it at <paramref name="idealScreenPosition"/>. The actual position of the
    ///     menu will be adjusted to ensure the menu fully fits on the screen, with the mouse cursor positioned over
    ///     the first cell of the first <see cref="MenuItem"/>.
    /// </summary>
    /// <param name="idealScreenPosition">
    ///     The ideal screen-relative position for the menu. If <see langword="null"/>, the current mouse position will be
    ///     used.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         IMPORTANT: The popover must be registered with <see cref="Application.Popover"/> before calling this
    ///         method.
    ///         Call <see cref="ApplicationPopover.Register"/> first.
    ///     </para>
    ///     <para>
    ///         This method internally calls <see cref="ApplicationPopover.Show"/>, which will throw
    ///         <see cref="InvalidOperationException"/> if the popover is not registered.
    ///     </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if the popover has not been registered.</exception>
    public void MakeVisible (Point? idealScreenPosition = null)
    {
        if (Visible)
        {
            // Logging.Debug ($"{this.ToIdentifyingString ()} - Already Visible");

            return;
        }

        UpdateKeyBindings ();
        SetPosition (idealScreenPosition);
        App!.Popover?.Show (this);
    }

    /// <summary>
    ///     Sets the position of the popover menu at <paramref name="idealScreenPosition"/>. The actual position will be
    ///     adjusted to ensure the menu fully fits on the screen, with the mouse cursor positioned over the first cell of
    ///     the first <see cref="MenuItem"/> (if possible).
    /// </summary>
    /// <param name="idealScreenPosition">
    ///     The ideal screen-relative position for the menu. If <see langword="null"/>, the current mouse position will be
    ///     used.
    /// </param>
    /// <remarks>
    ///     This method only sets the position; it does not make the popover visible. Use <see cref="MakeVisible"/> to
    ///     both position and show the popover.
    /// </remarks>
    public void SetPosition (Point? idealScreenPosition = null)
    {
        idealScreenPosition ??= App?.Mouse.LastMousePosition;

        if (idealScreenPosition is null || Root is null)
        {
            return;
        }

        Point pos = idealScreenPosition.Value;

        if (!Root.IsInitialized)
        {
            Root.App ??= App;
            Root.BeginInit ();
            Root.EndInit ();

            // BUGBUG: This Layout call is a hack to work around some bug in Layout.
            // BUGBUG: See https://github.com/gui-cs/Terminal.Gui/issues/4522
            Root.Layout ();
        }

        pos = GetMostVisibleLocationForSubMenu (Root, pos);

        Root.X = pos.X;
        Root.Y = pos.Y;
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     When becoming visible, the root menu is added and shown. When becoming hidden, the root menu is removed
    ///     and the popover is hidden via <see cref="ApplicationPopover.Hide"/>.
    /// </remarks>
    protected override void OnVisibleChanged ()
    {
        Logging.Debug ($"{this.ToIdentifyingString ()} - Visible: {Visible}");
        base.OnVisibleChanged ();

        if (Visible)
        {
            AddAndShowSubMenu (_root);
        }
        else
        {
            HideAndRemoveSubMenu (_root);
            App?.Popover?.Hide (this);
        }
    }

    private Menu? _root;
    private bool _isHiding;

    /// <summary>
    ///     Gets or sets the <see cref="Menu"/> that is the root of the popover menu hierarchy.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The root menu contains the top-level menu items. Setting this property updates key bindings and
    ///         event subscriptions for all menus in the hierarchy.
    ///     </para>
    ///     <para>
    ///         When set, all submenus are configured with appropriate event handlers for selection and acceptance.
    ///     </para>
    /// </remarks>
    public Menu? Root
    {
        get => _root;
        set
        {
            if (_root == value)
            {
                return;
            }

#if DEBUG
            Id = $"{Root?.Id}.PopoverMenu";
#endif

            // Unsubscribe from old hierarchy before replacing
            if (_root is { })
            {
                IEnumerable<Menu> oldMenus = GetAllSubMenus ();

                foreach (Menu menu in oldMenus)
                {
                    menu.Accepting -= MenuOnAccepting;
                    menu.Accepted -= MenuAccepted;
                    menu.Activating -= MenuOnActivating;
                    menu.Activated -= MenuOnActivated;
                    menu.SelectedMenuItemChanged -= MenuOnSelectedMenuItemChanged;
                }
            }

            HideAndRemoveSubMenu (_root);

            _root = value;

            _root?.App = App;

            // TODO: This needs to be done whenever any MenuItem in the menu tree changes to support dynamic menus
            // TODO: And it needs to clear the old bindings first
            UpdateKeyBindings ();

            // TODO: This needs to be done whenever any MenuItem in the menu tree changes to support dynamic menus
            IEnumerable<Menu> allMenus = GetAllSubMenus ();

            foreach (Menu menu in allMenus)
            {
                menu.App = App;
                menu.Visible = false;
                menu.Accepting += MenuOnAccepting;
                menu.Accepted += MenuAccepted;
                menu.Activating += MenuOnActivating;
                menu.Activated += MenuOnActivated;
                menu.SelectedMenuItemChanged += MenuOnSelectedMenuItemChanged;
            }
        }
    }

    private void UpdateKeyBindings ()
    {
        IEnumerable<MenuItem> all = GetMenuItemsOfAllSubMenus (mi => mi.Command != Command.NotBound);

        foreach (MenuItem menuItem in all)
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
                key = App?.Keyboard.KeyBindings.GetFirstFromCommands (menuItem.Command);
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

            // Logging.Debug ($"{this.ToIdentifyingString ()} - HotKey: {menuItem.Key}->{menuItem.Command}");
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     This method checks all menu items in the hierarchy for a matching key binding and invokes the
    ///     appropriate menu item if found.
    /// </remarks>
    protected override bool OnKeyDownNotHandled (Key key)
    {
        // See if any of our MenuItems have this key as Key
        IEnumerable<MenuItem> all = GetMenuItemsOfAllSubMenus (mi => key != Application.QuitKey && mi.Key == key);

        foreach (MenuItem menuItem in all)
        {
            // Logging.Debug ($"{this.ToIdentifyingString ()} - key: {key}");

            return menuItem.NewKeyDownEvent (key);
        }

        return base.OnKeyDownNotHandled (key);
    }

    /// <summary>
    ///     Gets all the submenus in the popover menu hierarchy, including the root menu.
    /// </summary>
    /// <returns>An enumerable collection of all <see cref="Menu"/> instances in the hierarchy.</returns>
    /// <remarks>
    ///     This method performs a depth-first traversal of the menu tree, starting from <see cref="Root"/>.
    /// </remarks>
    public IEnumerable<Menu> GetAllSubMenus ()
    {
        List<Menu> result = [];

        if (Root == null)
        {
            return result;
        }

        Stack<Menu> stack = new ();
        stack.Push (Root);

        while (stack.Count > 0)
        {
            Menu currentMenu = stack.Pop ();
            result.Add (currentMenu);

            foreach (View subView in currentMenu.SubViews)
            {
                if (subView is MenuItem { SubMenu: { } } menuItem)
                {
                    stack.Push (menuItem.SubMenu);
                }
            }
        }

        return result;
    }

    /// <summary>
    ///     Gets menu items in the popover menu hierarchy, optionally filtered by <paramref name="predicate"/>.
    /// </summary>
    /// <param name="predicate">
    ///     If provided, only <see cref="MenuItem"/>s matching the predicate are returned.
    ///     If <see langword="null"/>, all menu items are returned.
    /// </param>
    /// <returns>An enumerable of matching <see cref="MenuItem"/> instances across all menus in the hierarchy.</returns>
    /// <remarks>
    ///     This method traverses all menus returned by <see cref="GetAllSubMenus"/> and collects their menu items.
    /// </remarks>
    public IEnumerable<MenuItem> GetMenuItemsOfAllSubMenus (Func<MenuItem, bool>? predicate = null)
    {
        List<MenuItem> result = [];

        foreach (Menu menu in GetAllSubMenus ())
        {
            foreach (View subView in menu.SubViews)
            {
                if (subView is MenuItem menuItem && (predicate is null || predicate (menuItem)))
                {
                    result.Add (menuItem);
                }
            }
        }

        return result;
    }

    /// <summary>
    ///     Shows the submenu of the specified <see cref="MenuItem"/>, if it has one.
    /// </summary>
    /// <param name="menuItem">The menu item whose submenu should be shown.</param>
    /// <remarks>
    ///     <para>
    ///         If another submenu is currently visible at the same level, it will be hidden before showing the new one.
    ///     </para>
    ///     <para>
    ///         The submenu is positioned to the right of the menu item, adjusted to ensure full visibility on screen.
    ///     </para>
    /// </remarks>
    internal void ShowSubMenu (MenuItem? menuItem)
    {
        var menu = menuItem?.SuperView as Menu;

        // Logging.Debug ($"{this.ToIdentifyingString ()} - menuItem: {menuItem?.Title}, menu: {menu?.Title}");
        // BUGBUG: This Layout call is a hack to work around some bug in Layout.
        // BUGBUG: See https://github.com/gui-cs/Terminal.Gui/issues/4522
        menu?.Layout ();

        // If there's a visible peer, remove / hide it
        if (menu?.SubViews.FirstOrDefault (v => v is MenuItem { SubMenu.Visible: true }) is MenuItem visiblePeer)
        {
            HideAndRemoveSubMenu (visiblePeer.SubMenu);
            visiblePeer.ForceFocusColors = false;
        }

        if (menuItem is { SubMenu: { Visible: false } })
        {
            AddAndShowSubMenu (menuItem.SubMenu);

            Point idealLocation = ScreenToViewport (new Point (menuItem.FrameToScreen ().Right - menuItem.SubMenu.GetAdornmentsThickness ().Left,
                                                               menuItem.FrameToScreen ().Top - menuItem.SubMenu.GetAdornmentsThickness ().Top));

            Point pos = GetMostVisibleLocationForSubMenu (menuItem.SubMenu, idealLocation);
            menuItem.SubMenu.X = pos.X;
            menuItem.SubMenu.Y = pos.Y;

            menuItem.ForceFocusColors = true;
        }
    }

    /// <summary>
    ///     Calculates the most visible screen-relative location for the specified <paramref name="menu"/>.
    /// </summary>
    /// <param name="menu">The menu to position.</param>
    /// <param name="idealLocation">The ideal screen-relative location.</param>
    /// <returns>The adjusted screen-relative position that ensures maximum visibility of the menu.</returns>
    /// <remarks>
    ///     This method adjusts the position to keep the menu fully visible on screen, considering screen boundaries.
    /// </remarks>
    internal Point GetMostVisibleLocationForSubMenu (Menu menu, Point idealLocation)
    {
        // Calculate the initial position to the right of the menu item
        GetLocationEnsuringFullVisibility (menu, idealLocation.X, idealLocation.Y, out int nx, out int ny);

        return new Point (nx, ny);
    }

    private void AddAndShowSubMenu (Menu? menu)
    {
        if (menu is not { SuperView: null, Visible: false })
        {
            return;
        }

        // Logging.Debug ($"{this.ToIdentifyingString ()} ({menu?.Title}) - menu.Visible: {menu?.Visible}");

        // TODO: Find the menu item below the mouse, if any, and select it

        if (!menu!.IsInitialized)
        {
            menu.App ??= App;
            menu.BeginInit ();
            menu.EndInit ();
        }

        menu.ClearFocus ();
        Add (menu);

        // IMPORTANT: This must be done after adding the menu to the super view or Add will try
        // to set focus to it.
        menu.Visible = true;

        // BUGBUG: This Layout call is a hack to work around some bug in Layout.
        // BUGBUG: See https://github.com/gui-cs/Terminal.Gui/issues/4522
        menu.Layout ();
    }

    private void HideAndRemoveSubMenu (Menu? menu)
    {
        if (_isHiding || menu is not { Visible: true })
        {
            return;
        }

        _isHiding = true;

        try
        {
            Logging.Debug ($"{this.ToIdentifyingString ()} ({menu?.Title}) - menu.Visible: {menu?.Visible}");

            // If there's a visible submenu, remove / hide it
            if (menu?.SubViews.FirstOrDefault (v => v is MenuItem { SubMenu.Visible: true }) is MenuItem visiblePeer)
            {
                HideAndRemoveSubMenu (visiblePeer.SubMenu);
                visiblePeer.ForceFocusColors = false;
            }

            // Reset ForceFocusColors on the SuperMenuItem that owns this menu
            menu?.SuperMenuItem?.ForceFocusColors = false;

            menu?.Visible = false;
            menu?.ClearFocus ();
            Remove (menu);

            if (menu == Root)
            {
                Visible = false;
            }
        }
        finally
        {
            _isHiding = false;
        }
    }

    private void MenuOnAccepting (object? sender, CommandEventArgs e)
    {
        Logging.Debug ($"{this.ToIdentifyingString ()} ({e})");

        var senderView = sender as View;

        // Logging.Debug ($"{this.ToIdentifyingString ()} ({e.Context?.Source?.Title}) Command: {e.Context?.Command} - Sender: {senderView?.GetType ().Name}");

        if (e.Context?.Command != Command.HotKey)
        {
            Logging.Debug ($"{this.ToIdentifyingString ()} - Setting Visible = false");
            Visible = false;
        }

        if (e.Context?.Binding is not KeyBinding { Key: { } key })
        {
            return;
        }

        if (key == Application.QuitKey && SuperView is { Visible: true })
        {
            // Logging.Debug ($"{this.ToIdentifyingString ()} - Setting e.Handled = true - Application.QuitKey/Command = Command.Quit");
            e.Handled = true;
        }
    }

    private void MenuAccepted (object? sender, CommandEventArgs e)
    {
        Logging.Debug ($"{this.ToIdentifyingString ()} ({e})");

        if (e.Context?.Source?.TryGetTarget (out View? sourceView) == true)
        {
            if (sourceView is MenuItem { SubMenu: null })
            {
                HideAndRemoveSubMenu (_root);
            }
            else if (sourceView is MenuItem { SubMenu: { } } menuItemWithSubMenu)
            {
                ShowSubMenu (menuItemWithSubMenu);
            }
        }
        RaiseAccepted (e.Context);
    }

    private void MenuOnActivating (object? sender, CommandEventArgs e)
    {
        Logging.Debug ($"{this.ToIdentifyingString ()} ({e})");
        var senderView = sender as View;

        if (e.Context?.Command != Command.HotKey)
        {
            Logging.Debug ($"{this.ToIdentifyingString ()} - Setting Visible = false");
            Visible = false;
        }

        if (e.Context?.Binding is not KeyBinding { Key: { } key })
        {
            Logging.Debug ($"{this.ToIdentifyingString ()} ({e}) - not KeyBinding");

            return;
        }

        if (key == Application.QuitKey && SuperView is { Visible: true })
        {
            Logging.Debug ($"{this.ToIdentifyingString ()} ({e}) - KeyBinding, Setting e.Handled");
            e.Handled = true;
        }
    }

    private void MenuOnActivated (object? sender, EventArgs<ICommandContext?> e)
    {
        Logging.Debug ($"{this.ToIdentifyingString ()} ({e})");

        if (e.Value?.Source?.TryGetTarget (out View? sourceView) == true)
        {
            Visible = false;

            if (sourceView is MenuItem { SubMenu: null })
            {
                HideAndRemoveSubMenu (_root);
            }
            else if (sourceView is MenuItem { SubMenu: { } } menuItemWithSubMenu)
            {
                ShowSubMenu (menuItemWithSubMenu);
            }
        }
        RaiseActivated (e.Value);
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     <para>
    ///         When the popover is not visible, only hotkey commands are processed.
    ///     </para>
    ///     <para>
    ///         This method raises <see cref="View.Accepted"/> for commands that originate from menu items in the hierarchy.
    ///     </para>
    /// </remarks>
    protected override bool OnAccepting (CommandEventArgs args)
    {
        Logging.Debug ($"{this.ToIdentifyingString ()} {args}");

        // If we're not visible, ignore any keys that are not hotkeys

        if (!Visible && args.Context?.Binding is KeyBinding { Key: { } key } keyBinding)
        {
            if (!GetMenuItemsOfAllSubMenus (i => i.Key == key).Any ())
            {
                Logging.Debug ($"{this.ToIdentifyingString ()} {args} - ignore any keys that are not hotkeys");

                return false;
            }
        }

        Logging.Debug ($"{this.ToIdentifyingString ()} {args} - calling base.OnAccepting");

        bool? ret = base.OnAccepting (args);

        if (ret is true || args.Handled)
        {
            return args.Handled = true;
        }

        // Only raise Accepted if the command came from one of our MenuItems
        // if (GetMenuItemsOfAllSubMenus ().Contains (args.Context?.Source))
        {
            Logging.Debug ($"{this.ToIdentifyingString ()} {args} - Calling RaiseAccepted");

            RaiseAccepted (args.Context);
        }

        // Always return false to enable accepting to continue propagating
        return false;
    }

    /// <inheritdoc/>
    protected override bool OnActivating (CommandEventArgs args)
    {
        Logging.Debug ($"{this.ToIdentifyingString ()} {args}");

        return base.OnActivating (args);
    }

    private void MenuOnSelectedMenuItemChanged (object? sender, MenuItem? e)
    {
        Logging.Debug ($"{this.ToIdentifyingString ()} {e.ToIdentifyingString ()}");

        ShowSubMenu (e);
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if attempting to add a <see cref="Menu"/> or <see cref="MenuItem"/> directly to the popover.
    /// </exception>
    /// <remarks>
    ///     Do not add <see cref="MenuItem"/> or <see cref="Menu"/> views directly to the popover.
    ///     Use the <see cref="Root"/> property instead.
    /// </remarks>
    protected override void OnSubViewAdded (View view)
    {
        if (Root is null && (view is Menu || view is MenuItem))
        {
            throw new InvalidOperationException ("Do not add MenuItems or Menus directly to a PopoverMenu. Use the Root property.");
        }

        base.OnSubViewAdded (view);
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     This method unsubscribes from all menu events and disposes the root menu.
    /// </remarks>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            IEnumerable<Menu> allMenus = GetAllSubMenus ();

            foreach (Menu menu in allMenus)
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

    /// <summary>
    ///     Enables the popover menu for use in design-time scenarios.
    /// </summary>
    /// <typeparam name="TContext">The type of the target view context.</typeparam>
    /// <param name="targetView">The target view to associate with the menu commands.</param>
    /// <returns><see langword="true"/> if successfully enabled for design; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    ///     This method creates a default set of menu items (Cut, Copy, Paste, Select All, Quit) for design-time use.
    ///     It is primarily used for demonstration and testing purposes.
    /// </remarks>
    public bool EnableForDesign<TContext> (ref TContext targetView) where TContext : notnull
    {
        // Note: This menu is used by unit tests. If you modify it, you'll likely have to update
        // unit tests.

        Root = new Menu ([
                             new MenuItem (targetView as View, Command.Cut),
                             new MenuItem (targetView as View, Command.Copy),
                             new MenuItem (targetView as View, Command.Paste),
                             new Line (),
                             new MenuItem (targetView as View, Command.SelectAll),
                             new Line (),
                             new MenuItem (targetView as View, Command.Quit)
                         ]) { Title = "Popover Demo Root" };

        // NOTE: This is a workaround for the fact that the PopoverMenu is not visible in the designer
        // NOTE: without being activated via App?.Popover. But we want it to be visible.
        // NOTE: If you use PopoverView.EnableForDesign for real Popover scenarios, change back to false
        // NOTE: after calling EnableForDesign.
        //Visible = true;

        return true;
    }
}
