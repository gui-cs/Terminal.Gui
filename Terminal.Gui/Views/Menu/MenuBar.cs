namespace Terminal.Gui;

/// <summary>
///     <see cref="MenuBarItem"/> is a menu item on  <see cref="MenuBar"/>. MenuBarItems do not support
///     <see cref="MenuItem.Shortcut"/>.
/// </summary>
public class MenuBarItem : MenuItem
{
    /// <summary>Initializes a new <see cref="MenuBarItem"/> as a <see cref="MenuItem"/>.</summary>
    /// <param name="title">Title for the menu item.</param>
    /// <param name="help">Help text to display. Will be displayed next to the Title surrounded by parentheses.</param>
    /// <param name="action">Action to invoke when the menu item is activated.</param>
    /// <param name="canExecute">Function to determine if the action can currently be executed.</param>
    /// <param name="parent">The parent <see cref="MenuItem"/> of this if exist, otherwise is null.</param>
    public MenuBarItem (
        string title,
        string help,
        Action action,
        Func<bool> canExecute = null,
        MenuItem parent = null
    ) : base (title, help, action, canExecute, parent)
    {
        SetInitialProperties (title, null, null, true);
    }

    /// <summary>Initializes a new <see cref="MenuBarItem"/>.</summary>
    /// <param name="title">Title for the menu item.</param>
    /// <param name="children">The items in the current menu.</param>
    /// <param name="parent">The parent <see cref="MenuItem"/> of this if exist, otherwise is null.</param>
    public MenuBarItem (string title, MenuItem [] children, MenuItem parent = null) { SetInitialProperties (title, children, parent); }

    /// <summary>Initializes a new <see cref="MenuBarItem"/> with separate list of items.</summary>
    /// <param name="title">Title for the menu item.</param>
    /// <param name="children">The list of items in the current menu.</param>
    /// <param name="parent">The parent <see cref="MenuItem"/> of this if exist, otherwise is null.</param>
    public MenuBarItem (string title, List<MenuItem []> children, MenuItem parent = null) { SetInitialProperties (title, children, parent); }

    /// <summary>Initializes a new <see cref="MenuBarItem"/>.</summary>
    /// <param name="children">The items in the current menu.</param>
    public MenuBarItem (MenuItem [] children) : this ("", children) { }

    /// <summary>Initializes a new <see cref="MenuBarItem"/>.</summary>
    public MenuBarItem () : this (new MenuItem [] { }) { }

    /// <summary>
    ///     Gets or sets an array of <see cref="MenuItem"/> objects that are the children of this
    ///     <see cref="MenuBarItem"/>
    /// </summary>
    /// <value>The children.</value>
    public MenuItem [] Children { get; set; }

    internal bool IsTopLevel => Parent is null && (Children is null || Children.Length == 0) && Action != null;

    /// <summary>Get the index of a child <see cref="MenuItem"/>.</summary>
    /// <param name="children"></param>
    /// <returns>Returns a greater than -1 if the <see cref="MenuItem"/> is a child.</returns>
    public int GetChildrenIndex (MenuItem children)
    {
        var i = 0;

        if (Children is { })
        {
            foreach (MenuItem child in Children)
            {
                if (child == children)
                {
                    return i;
                }

                i++;
            }
        }

        return -1;
    }

    /// <summary>Check if a <see cref="MenuItem"/> is a submenu of this MenuBar.</summary>
    /// <param name="menuItem"></param>
    /// <returns>Returns <c>true</c> if it is a submenu. <c>false</c> otherwise.</returns>
    public bool IsSubMenuOf (MenuItem menuItem)
    {
        foreach (MenuItem child in Children)
        {
            if (child == menuItem && child.Parent == menuItem.Parent)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Check if a <see cref="MenuItem"/> is a <see cref="MenuBarItem"/>.</summary>
    /// <param name="menuItem"></param>
    /// <returns>Returns a <see cref="MenuBarItem"/> or null otherwise.</returns>
    public MenuBarItem SubMenu (MenuItem menuItem) { return menuItem as MenuBarItem; }

    internal void AddKeyBindings (MenuBar menuBar)
    {
        if (Children is null)
        {
            return;
        }

        foreach (MenuItem menuItem in Children.Where (m => m is { }))
        {
            if (menuItem.HotKey != default (Rune))
            {
                menuBar.KeyBindings.Add ((KeyCode)menuItem.HotKey.Value, Command.ToggleExpandCollapse);

                menuBar.KeyBindings.Add (
                                         (KeyCode)menuItem.HotKey.Value | KeyCode.AltMask,
                                         KeyBindingScope.HotKey,
                                         Command.ToggleExpandCollapse
                                        );
            }

            if (menuItem.Shortcut != KeyCode.Null)
            {
                menuBar.KeyBindings.Add (menuItem.Shortcut, KeyBindingScope.HotKey, Command.Select);
            }

            SubMenu (menuItem)?.AddKeyBindings (menuBar);
        }
    }

    private void SetInitialProperties (string title, object children, MenuItem parent = null, bool isTopLevel = false)
    {
        if (!isTopLevel && children is null)
        {
            throw new ArgumentNullException (
                                             nameof (children),
                                             "The parameter cannot be null. Use an empty array instead."
                                            );
        }

        SetTitle (title ?? "");

        if (parent is { })
        {
            Parent = parent;
        }

        if (children is List<MenuItem []> childrenList)
        {
            MenuItem [] newChildren = { };

            foreach (MenuItem [] grandChild in childrenList)
            {
                foreach (MenuItem child in grandChild)
                {
                    SetParent (grandChild);
                    Array.Resize (ref newChildren, newChildren.Length + 1);
                    newChildren [newChildren.Length - 1] = child;
                }
            }

            Children = newChildren;
        }
        else if (children is MenuItem [] items)
        {
            SetParent (items);
            Children = items;
        }
        else
        {
            Children = null;
        }
    }

    private void SetParent (MenuItem [] children)
    {
        foreach (MenuItem child in children)
        {
            if (child is { Parent: null })
            {
                child.Parent = this;
            }
        }
    }

    private void SetTitle (string title)
    {
        title ??= string.Empty;
        Title = title;
    }
}

/// <summary>
///     <para>Provides a menu bar that spans the top of a <see cref="Toplevel"/> View with drop-down and cascading menus.</para>
///     <para>
///         By default, any sub-sub-menus (sub-menus of the <see cref="MenuItem"/>s added to <see cref="MenuBarItem"/>s)
///         are displayed in a cascading manner, where each sub-sub-menu pops out of the sub-menu frame (either to the
///         right or left, depending on where the sub-menu is relative to the edge of the screen). By setting
///         <see cref="UseSubMenusSingleFrame"/> to <see langword="true"/>, this behavior can be changed such that all
///         sub-sub-menus are drawn within a single frame below the MenuBar.
///     </para>
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="MenuBar"/> appears on the first row of the <see cref="Toplevel"/> SuperView and uses the full
///         width.
///     </para>
///     <para>See also: <see cref="ContextMenu"/></para>
///     <para>The <see cref="MenuBar"/> provides global hot keys for the application. See <see cref="MenuItem.HotKey"/>.</para>
///     <para>
///         When the menu is created key bindings for each menu item and its sub-menu items are added for each menu
///         item's hot key (both alone AND with AltMask) and shortcut, if defined.
///     </para>
///     <para>
///         If a key press matches any of the menu item's hot keys or shortcuts, the menu item's action is invoked or
///         sub-menu opened.
///     </para>
///     <para>
///         * If the menu bar is not open * Any shortcut defined within the menu will be invoked * Only hot keys defined
///         for the menu bar items will be invoked, and only if Alt is pressed too. * If the menu bar is open * Un-shifted
///         hot keys defined for the menu bar items will be invoked, only if the menu they belong to is open (the menu bar
///         item's text is visible). * Alt-shifted hot keys defined for the menu bar items will be invoked, only if the
///         menu they belong to is open (the menu bar item's text is visible). * If there is a visible hot key that
///         duplicates a shortcut (e.g. _File and Alt-F), the hot key wins.
///     </para>
/// </remarks>
public class MenuBar : View
{
    // Spaces before the Title
    private static readonly int _leftPadding = 1;

    // Spaces after the submenu Title, before Help
    private static readonly int _parensAroundHelp = 3;

    // Spaces after the Title
    private static readonly int _rightPadding = 1;

    // The column where the MenuBar starts
    private static readonly int _xOrigin = 0;
    internal bool _isMenuClosing;
    internal bool _isMenuOpening;

    // BUGBUG: Hack
    internal Menu _openMenu;
    internal List<Menu> _openSubMenu;
    internal int _selected;
    internal int _selectedSub;

    private bool _initialCanFocus;
    private bool _isCleaning;
    private View _lastFocused;
    private Menu _ocm;
    private View _previousFocused;
    private bool _reopen;
    private bool _useSubMenusSingleFrame;

    /// <summary>Initializes a new instance of the <see cref="MenuBar"/>.</summary>
    public MenuBar ()
    {
        X = 0;
        Y = 0;
        Width = Dim.Fill ();
        Height = 1;
        Menus = new MenuBarItem [] { };

        //CanFocus = true;
        _selected = -1;
        _selectedSub = -1;
        ColorScheme = Colors.ColorSchemes ["Menu"];
        WantMousePositionReports = true;
        IsMenuOpen = false;

        Added += MenuBar_Added;

        // Things this view knows how to do
        AddCommand (
                    Command.Left,
                    () =>
                    {
                        MoveLeft ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Right,
                    () =>
                    {
                        MoveRight ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Cancel,
                    () =>
                    {
                        CloseMenuBar ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Accept,
                    () =>
                    {
                        ProcessMenu (_selected, Menus [_selected]);

                        return true;
                    }
                   );

        AddCommand (Command.ToggleExpandCollapse, () => SelectOrRun ());
        AddCommand (Command.Select, () => Run (_menuItemToSelect?.Action));

        // Default key bindings for this view
        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.Esc, Command.Cancel);
        KeyBindings.Add (Key.CursorDown, Command.Accept);
        KeyBindings.Add (Key.Enter, Command.Accept);
        KeyBindings.Add (Key, KeyBindingScope.HotKey, Command.ToggleExpandCollapse);

        KeyBindings.Add (
                         KeyCode.CtrlMask | KeyCode.Space,
                         KeyBindingScope.HotKey,
                         Command.ToggleExpandCollapse
                        );
    }

    /// <summary><see langword="true"/> if the menu is open; otherwise <see langword="true"/>.</summary>
    public bool IsMenuOpen { get; protected set; }

    /// <summary>Gets the view that was last focused before opening the menu.</summary>
    public View LastFocused { get; private set; }

    /// <summary>
    ///     Gets or sets the array of <see cref="MenuBarItem"/>s for the menu. Only set this after the
    ///     <see cref="MenuBar"/> is visible.
    /// </summary>
    /// <value>The menu array.</value>
    public MenuBarItem [] Menus
    {
        get => _menus;
        set
        {
            _menus = value;

            if (Menus is null)
            {
                return;
            }

            // TODO: Bindings (esp for hotkey) should be added across and then down. This currently does down then across. 
            // TODO: As a result, _File._Save will have precedence over in "_File _Edit _ScrollbarView"
            // TODO: Also: Hotkeys should not work for sub-menus if they are not visible!
            foreach (MenuBarItem menuBarItem in Menus?.Where (m => m is { })!)
            {
                if (menuBarItem.HotKey != default (Rune))
                {
                    KeyBindings.Add (
                                     (KeyCode)menuBarItem.HotKey.Value,
                                     Command.ToggleExpandCollapse
                                    );

                    KeyBindings.Add (
                                     (KeyCode)menuBarItem.HotKey.Value | KeyCode.AltMask,
                                     KeyBindingScope.HotKey,
                                     Command.ToggleExpandCollapse
                                    );
                }

                if (menuBarItem.Shortcut != KeyCode.Null)
                {
                    // Technically this will never run because MenuBarItems don't have shortcuts
                    KeyBindings.Add (menuBarItem.Shortcut, KeyBindingScope.HotKey, Command.Select);
                }

                menuBarItem.AddKeyBindings (this);
            }
#if SUPPORT_ALT_TO_ACTIVATE_MENU
            // Enable the Alt key as a menu activator
            Initialized += (s, e) =>
                           {
                               if (SuperView is { })
                               {
                                   SuperView.KeyUp += SuperView_KeyUp;
                               }
                           };
#endif
        }
    }

    /// <summary>
    ///     The default <see cref="LineStyle"/> for <see cref="Menus"/>'s border. The default is
    ///     <see cref="LineStyle.Single"/>.
    /// </summary>
    public LineStyle MenusBorderStyle { get; set; } = LineStyle.Single;

    /// <summary>
    ///     Gets or sets if the sub-menus must be displayed in a single or multiple frames.
    ///     <para>
    ///         By default any sub-sub-menus (sub-menus of the main <see cref="MenuItem"/>s) are displayed in a cascading
    ///         manner, where each sub-sub-menu pops out of the sub-menu frame (either to the right or left, depending on where
    ///         the sub-menu is relative to the edge of the screen). By setting <see cref="UseSubMenusSingleFrame"/> to
    ///         <see langword="true"/>, this behavior can be changed such that all sub-sub-menus are drawn within a single
    ///         frame below the MenuBar.
    ///     </para>
    /// </summary>
    public bool UseSubMenusSingleFrame
    {
        get => _useSubMenusSingleFrame;
        set
        {
            _useSubMenusSingleFrame = value;

            if (value && UseKeysUpDownAsKeysLeftRight)
            {
                _useKeysUpDownAsKeysLeftRight = false;
                SetNeedsDisplay ();
            }
        }
    }

    /// <inheritdoc/>
    public override bool Visible
    {
        get => base.Visible;
        set
        {
            base.Visible = value;

            if (!value)
            {
                CloseAllMenus ();
            }
        }
    }

    internal Menu openCurrentMenu
    {
        get => _ocm;
        set
        {
            if (_ocm != value)
            {
                _ocm = value;

                if (_ocm is { } && _ocm._currentChild > -1)
                {
                    OnMenuOpened ();
                }
            }
        }
    }

    /// <summary>Closes the Menu programmatically if open and not canceled (as though F9 were pressed).</summary>
    public bool CloseMenu (bool ignoreUseSubMenusSingleFrame = false) { return CloseMenu (false, false, ignoreUseSubMenusSingleFrame); }

    /// <summary>Raised when all the menu is closed.</summary>
    public event EventHandler MenuAllClosed;

    /// <summary>Raised when a menu is closing passing <see cref="MenuClosingEventArgs"/>.</summary>
    public event EventHandler<MenuClosingEventArgs> MenuClosing;

    /// <summary>Raised when a menu is opened.</summary>
    public event EventHandler<MenuOpenedEventArgs> MenuOpened;

    /// <summary>Raised as a menu is opening.</summary>
    public event EventHandler<MenuOpeningEventArgs> MenuOpening;

    /// <inheritdoc/>
    public override void OnDrawContent (Rectangle viewport)
    {
        Driver.SetAttribute (GetNormalColor ());

        Clear ();

        var pos = 0;

        for (var i = 0; i < Menus.Length; i++)
        {
            MenuBarItem menu = Menus [i];
            Move (pos, 0);
            Attribute hotColor, normalColor;

            if (i == _selected && IsMenuOpen)
            {
                hotColor = i == _selected ? ColorScheme.HotFocus : ColorScheme.HotNormal;
                normalColor = i == _selected ? ColorScheme.Focus : GetNormalColor ();
            }
            else
            {
                hotColor = ColorScheme.HotNormal;
                normalColor = GetNormalColor ();
            }

            // Note Help on MenuBar is drawn with parens around it
            DrawHotString (
                           string.IsNullOrEmpty (menu.Help) ? $" {menu.Title} " : $" {menu.Title} ({menu.Help}) ",
                           hotColor,
                           normalColor
                          );

            pos += _leftPadding
                   + menu.TitleLength
                   + (menu.Help.GetColumns () > 0
                          ? _leftPadding + menu.Help.GetColumns () + _parensAroundHelp
                          : 0)
                   + _rightPadding;
        }

        PositionCursor ();
    }

    /// <summary>Virtual method that will invoke the <see cref="MenuAllClosed"/>.</summary>
    public virtual void OnMenuAllClosed () { MenuAllClosed?.Invoke (this, EventArgs.Empty); }

    /// <summary>Virtual method that will invoke the <see cref="MenuClosing"/>.</summary>
    /// <param name="currentMenu">The current menu to be closed.</param>
    /// <param name="reopen">Whether the current menu will be reopen.</param>
    /// <param name="isSubMenu">Whether is a sub-menu or not.</param>
    public virtual MenuClosingEventArgs OnMenuClosing (MenuBarItem currentMenu, bool reopen, bool isSubMenu)
    {
        var ev = new MenuClosingEventArgs (currentMenu, reopen, isSubMenu);
        MenuClosing?.Invoke (this, ev);

        return ev;
    }

    /// <summary>Virtual method that will invoke the <see cref="MenuOpened"/> event if it's defined.</summary>
    public virtual void OnMenuOpened ()
    {
        MenuItem mi = null;
        MenuBarItem parent;

        if (openCurrentMenu.BarItems.Children != null
            && openCurrentMenu.BarItems!.Children.Length > 0
            && openCurrentMenu?._currentChild > -1)
        {
            parent = openCurrentMenu.BarItems;
            mi = parent.Children [openCurrentMenu._currentChild];
        }
        else if (openCurrentMenu!.BarItems.IsTopLevel)
        {
            parent = null;
            mi = openCurrentMenu.BarItems;
        }
        else
        {
            parent = _openMenu.BarItems;
            mi = parent.Children?.Length > 0 ? parent.Children [_openMenu._currentChild] : null;
        }

        MenuOpened?.Invoke (this, new (parent, mi));
    }

    /// <summary>Virtual method that will invoke the <see cref="MenuOpening"/> event if it's defined.</summary>
    /// <param name="currentMenu">The current menu to be replaced.</param>
    /// <returns>Returns the <see cref="MenuOpeningEventArgs"/></returns>
    public virtual MenuOpeningEventArgs OnMenuOpening (MenuBarItem currentMenu)
    {
        var ev = new MenuOpeningEventArgs (currentMenu);
        MenuOpening?.Invoke (this, ev);

        return ev;
    }

    /// <summary>Opens the Menu programatically, as though the F9 key were pressed.</summary>
    public void OpenMenu ()
    {
        MenuBar mbar = GetMouseGrabViewInstance (this);

        if (mbar is { })
        {
            mbar.CleanUp ();
        }

        if (!Enabled || _openMenu is { })
        {
            return;
        }

        _selected = 0;
        SetNeedsDisplay ();

        _previousFocused = SuperView is null ? Application.Current?.Focused : SuperView.Focused;
        OpenMenu (_selected);

        if (!SelectEnabledItem (
                                openCurrentMenu.BarItems.Children,
                                openCurrentMenu._currentChild,
                                out openCurrentMenu._currentChild
                               )
            && !CloseMenu (false))
        {
            return;
        }

        if (!openCurrentMenu.CheckSubMenu ())
        {
            return;
        }

        Application.GrabMouse (this);
    }

    /// <inheritdoc/>
    public override Point? PositionCursor ()
    {
        if (_selected == -1 && HasFocus && Menus.Length > 0)
        {
            _selected = 0;
        }

        var pos = 0;

        for (var i = 0; i < Menus.Length; i++)
        {
            if (i == _selected)
            {
                pos++;
                Move (pos + 1, 0);

                return new (pos +1, 0);
            }

            pos += _leftPadding
                   + Menus [i].TitleLength
                   + (Menus [i].Help.GetColumns () > 0
                          ? Menus [i].Help.GetColumns () + _parensAroundHelp
                          : 0)
                   + _rightPadding;
        }
        return null;
    }

    // Activates the menu, handles either first focus, or activating an entry when it was already active
    // For mouse events.
    internal void Activate (int idx, int sIdx = -1, MenuBarItem subMenu = null)
    {
        _selected = idx;
        _selectedSub = sIdx;

        if (_openMenu is null)
        {
            _previousFocused = SuperView is null ? Application.Current?.Focused ?? null : SuperView.Focused;
        }

        OpenMenu (idx, sIdx, subMenu);
        SetNeedsDisplay ();
    }

    internal void CleanUp ()
    {
        _isCleaning = true;

        if (_openMenu is { })
        {
            CloseAllMenus ();
        }

        _openedByAltKey = false;
        _openedByHotKey = false;
        IsMenuOpen = false;
        _selected = -1;
        CanFocus = _initialCanFocus;

        if (_lastFocused is { })
        {
            _lastFocused.SetFocus ();
        }

        SetNeedsDisplay ();
        Application.UngrabMouse ();
        _isCleaning = false;
    }

    internal void CloseAllMenus ()
    {
        if (!_isMenuOpening && !_isMenuClosing)
        {
            if (_openSubMenu is { } && !CloseMenu (false, true, true))
            {
                return;
            }

            if (!CloseMenu (false))
            {
                return;
            }

            if (LastFocused is { } && LastFocused != this)
            {
                _selected = -1;
            }

            Application.UngrabMouse ();
        }

        if (openCurrentMenu is { })
        {
            openCurrentMenu = null;
        }

        IsMenuOpen = false;
        _openedByAltKey = false;
        _openedByHotKey = false;
        OnMenuAllClosed ();
    }

    internal bool CloseMenu (bool reopen = false, bool isSubMenu = false, bool ignoreUseSubMenusSingleFrame = false)
    {
        MenuBarItem mbi = isSubMenu ? openCurrentMenu.BarItems : _openMenu?.BarItems;

        if (UseSubMenusSingleFrame && mbi is { } && !ignoreUseSubMenusSingleFrame && mbi.Parent is { })
        {
            return false;
        }

        _isMenuClosing = true;
        _reopen = reopen;
        MenuClosingEventArgs args = OnMenuClosing (mbi, reopen, isSubMenu);

        if (args.Cancel)
        {
            _isMenuClosing = false;

            if (args.CurrentMenu.Parent is { })
            {
                _openMenu._currentChild =
                    ((MenuBarItem)args.CurrentMenu.Parent).Children.IndexOf (args.CurrentMenu);
            }

            return false;
        }

        switch (isSubMenu)
        {
            case false:
                if (_openMenu is { })
                {
                    Application.Current.Remove (_openMenu);
                }

                SetNeedsDisplay ();

                if (_previousFocused is { } && _previousFocused is Menu && _openMenu is { } && _previousFocused.ToString () != openCurrentMenu.ToString ())
                {
                    _previousFocused.SetFocus ();
                }

                _openMenu?.Dispose ();
                _openMenu = null;

                if (_lastFocused is Menu || _lastFocused is MenuBar)
                {
                    _lastFocused = null;
                }

                LastFocused = _lastFocused;
                _lastFocused = null;

                if (LastFocused is { } && LastFocused.CanFocus)
                {
                    if (!reopen)
                    {
                        _selected = -1;
                    }

                    if (_openSubMenu is { })
                    {
                        _openSubMenu = null;
                    }

                    if (openCurrentMenu is { })
                    {
                        Application.Current.Remove (openCurrentMenu);
                        openCurrentMenu.Dispose ();
                        openCurrentMenu = null;
                    }

                    LastFocused.SetFocus ();
                }
                else if (_openSubMenu is null || _openSubMenu.Count == 0)
                {
                    CloseAllMenus ();
                }
                else
                {
                    SetFocus ();
                    PositionCursor ();
                }

                IsMenuOpen = false;

                break;

            case true:
                _selectedSub = -1;
                SetNeedsDisplay ();
                RemoveAllOpensSubMenus ();
                openCurrentMenu._previousSubFocused.SetFocus ();
                _openSubMenu = null;
                IsMenuOpen = true;

                break;
        }

        _reopen = false;
        _isMenuClosing = false;

        return true;
    }

    /// <summary>Gets the superview location offset relative to the <see cref="ConsoleDriver"/> location.</summary>
    /// <returns>The location offset.</returns>
    internal Point GetScreenOffset ()
    {
        if (Driver is null)
        {
            return Point.Empty;
        }

        Rectangle superViewFrame = SuperView is null ? Driver.Screen : SuperView.Frame;
        View sv = SuperView is null ? Application.Current : SuperView;
        Point viewportOffset = sv.GetViewportOffsetFromFrame ();

        return new (
                    superViewFrame.X - sv.Frame.X - viewportOffset.X,
                    superViewFrame.Y - sv.Frame.Y - viewportOffset.Y
                   );
    }

    /// <summary>
    ///     Gets the <see cref="Application.Current"/> location offset relative to the <see cref="ConsoleDriver"/>
    ///     location.
    /// </summary>
    /// <returns>The location offset.</returns>
    internal Point GetScreenOffsetFromCurrent ()
    {
        Rectangle screen = Driver.Screen;
        Rectangle currentFrame = Application.Current.Frame;
        Point viewportOffset = Application.Top.GetViewportOffsetFromFrame ();

        return new (screen.X - currentFrame.X - viewportOffset.X, screen.Y - currentFrame.Y - viewportOffset.Y);
    }

    internal void NextMenu (bool isSubMenu = false, bool ignoreUseSubMenusSingleFrame = false)
    {
        switch (isSubMenu)
        {
            case false:
                if (_selected == -1)
                {
                    _selected = 0;
                }
                else if (_selected + 1 == Menus.Length)
                {
                    _selected = 0;
                }
                else
                {
                    _selected++;
                }

                if (_selected > -1 && !CloseMenu (true, ignoreUseSubMenusSingleFrame))
                {
                    return;
                }

                OpenMenu (_selected);

                SelectEnabledItem (
                                   openCurrentMenu.BarItems.Children,
                                   openCurrentMenu._currentChild,
                                   out openCurrentMenu._currentChild
                                  );

                break;
            case true:
                if (UseKeysUpDownAsKeysLeftRight)
                {
                    if (CloseMenu (false, true, ignoreUseSubMenusSingleFrame))
                    {
                        NextMenu (false, ignoreUseSubMenusSingleFrame);
                    }
                }
                else
                {
                    MenuBarItem subMenu = openCurrentMenu._currentChild > -1 && openCurrentMenu.BarItems.Children.Length > 0
                                              ? openCurrentMenu.BarItems.SubMenu (
                                                                                  openCurrentMenu.BarItems.Children [openCurrentMenu._currentChild]
                                                                                 )
                                              : null;

                    if ((_selectedSub == -1 || _openSubMenu is null || _openSubMenu?.Count - 1 == _selectedSub) && subMenu is null)
                    {
                        if (_openSubMenu is { } && !CloseMenu (false, true))
                        {
                            return;
                        }

                        NextMenu (false, ignoreUseSubMenusSingleFrame);
                    }
                    else if (subMenu != null
                             || (openCurrentMenu._currentChild > -1
                                 && !openCurrentMenu.BarItems
                                                    .Children [openCurrentMenu._currentChild]
                                                    .IsFromSubMenu))
                    {
                        _selectedSub++;
                        openCurrentMenu.CheckSubMenu ();
                    }
                    else
                    {
                        if (CloseMenu (false, true, ignoreUseSubMenusSingleFrame))
                        {
                            NextMenu (false, ignoreUseSubMenusSingleFrame);
                        }

                        return;
                    }

                    SetNeedsDisplay ();

                    if (UseKeysUpDownAsKeysLeftRight)
                    {
                        openCurrentMenu.CheckSubMenu ();
                    }
                }

                break;
        }
    }

    internal void OpenMenu (int index, int sIndex = -1, MenuBarItem subMenu = null)
    {
        _isMenuOpening = true;
        MenuOpeningEventArgs newMenu = OnMenuOpening (Menus [index]);

        if (newMenu.Cancel)
        {
            _isMenuOpening = false;

            return;
        }

        if (newMenu.NewMenuBarItem is { })
        {
            Menus [index] = newMenu.NewMenuBarItem;
        }

        var pos = 0;

        switch (subMenu)
        {
            case null:
                // Open a submenu below a MenuBar
                _lastFocused ??= SuperView is null ? Application.Current?.MostFocused : SuperView.MostFocused;

                if (_openSubMenu is { } && !CloseMenu (false, true))
                {
                    return;
                }

                if (_openMenu is { })
                {
                    Application.Current.Remove (_openMenu);
                    _openMenu.Dispose ();
                    _openMenu = null;
                }

                // This positions the submenu horizontally aligned with the first character of the
                // text belonging to the menu 
                for (var i = 0; i < index; i++)
                {
                    pos += Menus [i].TitleLength + (Menus [i].Help.GetColumns () > 0 ? Menus [i].Help.GetColumns () + 2 : 0) + _leftPadding + _rightPadding;
                }

                var locationOffset = Point.Empty;

                // if SuperView is null then it's from a ContextMenu
                if (SuperView is null)
                {
                    locationOffset = GetScreenOffset ();
                }

                if (SuperView is { } && SuperView != Application.Current)
                {
                    locationOffset.X += SuperView.Border.Thickness.Left;
                    locationOffset.Y += SuperView.Border.Thickness.Top;
                }

                _openMenu = new()
                {
                    Host = this,
                    X = Frame.X + pos + locationOffset.X,
                    Y = Frame.Y + 1 + locationOffset.Y,
                    BarItems = Menus [index],
                    Parent = null
                };
                openCurrentMenu = _openMenu;
                openCurrentMenu._previousSubFocused = _openMenu;

                Application.Current.Add (_openMenu);
                _openMenu.SetFocus ();

                break;
            default:
                // Opens a submenu next to another submenu (openSubMenu)
                if (_openSubMenu is null)
                {
                    _openSubMenu = new ();
                }

                if (sIndex > -1)
                {
                    RemoveSubMenu (sIndex);
                }
                else
                {
                    Menu last = _openSubMenu.Count > 0 ? _openSubMenu.Last () : _openMenu;

                    if (!UseSubMenusSingleFrame)
                    {
                        locationOffset = GetLocationOffset ();

                        openCurrentMenu = new()
                        {
                            Host = this,
                            X = last.Frame.Left + last.Frame.Width + locationOffset.X,
                            Y = last.Frame.Top + locationOffset.Y + last._currentChild,
                            BarItems = subMenu,
                            Parent = last
                        };
                    }
                    else
                    {
                        Menu first = _openSubMenu.Count > 0 ? _openSubMenu.First () : _openMenu;

                        // 2 is for the parent and the separator
                        MenuItem [] mbi = new MenuItem [2 + subMenu.Children.Length];
                        mbi [0] = new() { Title = subMenu.Title, Parent = subMenu };
                        mbi [1] = null;

                        for (var j = 0; j < subMenu.Children.Length; j++)
                        {
                            mbi [j + 2] = subMenu.Children [j];
                        }

                        var newSubMenu = new MenuBarItem (mbi) { Parent = subMenu };

                        openCurrentMenu = new()
                        {
                            Host = this, X = first.Frame.Left, Y = first.Frame.Top, BarItems = newSubMenu
                        };
                        last.Visible = false;
                        Application.GrabMouse (openCurrentMenu);
                    }

                    openCurrentMenu._previousSubFocused = last._previousSubFocused;
                    _openSubMenu.Add (openCurrentMenu);
                    Application.Current.Add (openCurrentMenu);
                }

                _selectedSub = _openSubMenu.Count - 1;

                if (_selectedSub > -1
                    && SelectEnabledItem (
                                          openCurrentMenu.BarItems.Children,
                                          openCurrentMenu._currentChild,
                                          out openCurrentMenu._currentChild
                                         ))
                {
                    openCurrentMenu.SetFocus ();
                }

                break;
        }

        _isMenuOpening = false;
        IsMenuOpen = true;
    }

    internal void PreviousMenu (bool isSubMenu = false, bool ignoreUseSubMenusSingleFrame = false)
    {
        switch (isSubMenu)
        {
            case false:
                if (_selected <= 0)
                {
                    _selected = Menus.Length - 1;
                }
                else
                {
                    _selected--;
                }

                if (_selected > -1 && !CloseMenu (true, false, ignoreUseSubMenusSingleFrame))
                {
                    return;
                }

                OpenMenu (_selected);

                if (!SelectEnabledItem (
                                        openCurrentMenu.BarItems.Children,
                                        openCurrentMenu._currentChild,
                                        out openCurrentMenu._currentChild,
                                        false
                                       ))
                {
                    openCurrentMenu._currentChild = 0;
                }

                break;
            case true:
                if (_selectedSub > -1)
                {
                    _selectedSub--;
                    RemoveSubMenu (_selectedSub, ignoreUseSubMenusSingleFrame);
                    SetNeedsDisplay ();
                }
                else
                {
                    PreviousMenu ();
                }

                break;
        }
    }

    internal void RemoveAllOpensSubMenus ()
    {
        if (_openSubMenu is { })
        {
            foreach (Menu item in _openSubMenu)
            {
                Application.Current.Remove (item);
                item.Dispose ();
            }
        }
    }

    internal bool Run (Action action)
    {
        if (action is null)
        {
            return false;
        }

        Application.MainLoop.AddIdle (
                                      () =>
                                      {
                                          action ();

                                          return false;
                                      }
                                     );

        return true;
    }

    internal bool SelectEnabledItem (
        IEnumerable<MenuItem> chldren,
        int current,
        out int newCurrent,
        bool forward = true
    )
    {
        if (chldren is null)
        {
            newCurrent = -1;

            return true;
        }

        IEnumerable<MenuItem> childrens;

        if (forward)
        {
            childrens = chldren;
        }
        else
        {
            childrens = chldren.Reverse ();
        }

        int count;

        if (forward)
        {
            count = -1;
        }
        else
        {
            count = childrens.Count ();
        }

        foreach (MenuItem child in childrens)
        {
            if (forward)
            {
                if (++count < current)
                {
                    continue;
                }
            }
            else
            {
                if (--count > current)
                {
                    continue;
                }
            }

            if (child is null || !child.IsEnabled ())
            {
                if (forward)
                {
                    current++;
                }
                else
                {
                    current--;
                }
            }
            else
            {
                newCurrent = current;

                return true;
            }
        }

        newCurrent = -1;

        return false;
    }

    /// <summary>Called when an item is selected; Runs the action.</summary>
    /// <param name="item"></param>
    internal bool SelectItem (MenuItem item)
    {
        if (item?.Action is null)
        {
            return false;
        }

        Application.UngrabMouse ();
        CloseAllMenus ();
        Application.Refresh ();
        _openedByAltKey = true;

        return Run (item?.Action);
    }

    private void CloseMenuBar ()
    {
        if (!CloseMenu (false))
        {
            return;
        }

        if (_openedByAltKey)
        {
            _openedByAltKey = false;
            LastFocused?.SetFocus ();
        }

        SetNeedsDisplay ();
    }

    private Point GetLocationOffset ()
    {
        if (MenusBorderStyle != LineStyle.None)
        {
            return new (0, 1);
        }

        return new (-2, 0);
    }

    private void MenuBar_Added (object sender, SuperViewChangedEventArgs e)
    {
        _initialCanFocus = CanFocus;
        Added -= MenuBar_Added;
    }

    private void MoveLeft ()
    {
        _selected--;

        if (_selected < 0)
        {
            _selected = Menus.Length - 1;
        }

        OpenMenu (_selected);
        SetNeedsDisplay ();
    }

    private void MoveRight ()
    {
        _selected = (_selected + 1) % Menus.Length;
        OpenMenu (_selected);
        SetNeedsDisplay ();
    }

    private void ProcessMenu (int i, MenuBarItem mi)
    {
        if (_selected < 0 && IsMenuOpen)
        {
            return;
        }

        if (mi.IsTopLevel)
        {
            Rectangle screen = ViewportToScreen (new (new (0, i), Size.Empty));
            var menu = new Menu { Host = this, X = screen.X, Y = screen.Y, BarItems = mi };
            menu.Run (mi.Action);
            menu.Dispose ();
        }
        else
        {
            Application.GrabMouse (this);
            _selected = i;
            OpenMenu (i);

            if (!SelectEnabledItem (
                                    openCurrentMenu.BarItems.Children,
                                    openCurrentMenu._currentChild,
                                    out openCurrentMenu._currentChild
                                   )
                && !CloseMenu (false))
            {
                return;
            }

            if (!openCurrentMenu.CheckSubMenu ())
            {
                return;
            }
        }

        SetNeedsDisplay ();
    }

    private void RemoveSubMenu (int index, bool ignoreUseSubMenusSingleFrame = false)
    {
        if (_openSubMenu == null
            || (UseSubMenusSingleFrame
                && !ignoreUseSubMenusSingleFrame
                && _openSubMenu.Count == 0))
        {
            return;
        }

        for (int i = _openSubMenu.Count - 1; i > index; i--)
        {
            _isMenuClosing = true;
            Menu menu;

            if (_openSubMenu.Count - 1 > 0)
            {
                menu = _openSubMenu [i - 1];
            }
            else
            {
                menu = _openMenu;
            }

            if (!menu.Visible)
            {
                menu.Visible = true;
            }

            openCurrentMenu = menu;
            openCurrentMenu.SetFocus ();

            if (_openSubMenu is { })
            {
                menu = _openSubMenu [i];
                Application.Current.Remove (menu);
                _openSubMenu.Remove (menu);

                if (Application.MouseGrabView == menu)
                {
                    Application.GrabMouse (this);
                }

                menu.Dispose ();
            }

            RemoveSubMenu (i, ignoreUseSubMenusSingleFrame);
        }

        if (_openSubMenu.Count > 0)
        {
            openCurrentMenu = _openSubMenu.Last ();
        }

        _isMenuClosing = false;
    }

    #region Keyboard handling

    private Key _key = Key.F9;

    /// <summary>
    ///     The <see cref="Key"/> used to activate or close the menu bar by keyboard. The default is <see cref="Key.F9"/>
    ///     .
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the user presses any <see cref="MenuItem.HotKey"/>s defined in the <see cref="MenuBarItem"/>s, the menu
    ///         bar will be activated and the sub-menu will be opened.
    ///     </para>
    ///     <para><see cref="Key.Esc"/> will close the menu bar and any open sub-menus.</para>
    /// </remarks>
    public Key Key
    {
        get => _key;
        set
        {
            if (_key == value)
            {
                return;
            }

            KeyBindings.Remove (_key);
            KeyBindings.Add (value, KeyBindingScope.HotKey, Command.ToggleExpandCollapse);
            _key = value;
        }
    }

    private bool _useKeysUpDownAsKeysLeftRight;

    /// <summary>Used for change the navigation key style.</summary>
    public bool UseKeysUpDownAsKeysLeftRight
    {
        get => _useKeysUpDownAsKeysLeftRight;
        set
        {
            _useKeysUpDownAsKeysLeftRight = value;

            if (value && UseSubMenusSingleFrame)
            {
                UseSubMenusSingleFrame = false;
                SetNeedsDisplay ();
            }
        }
    }

    private static Rune _shortcutDelimiter = new ('+');

    /// <summary>Sets or gets the shortcut delimiter separator. The default is "+".</summary>
    public static Rune ShortcutDelimiter
    {
        get => _shortcutDelimiter;
        set
        {
            if (_shortcutDelimiter != value)
            {
                _shortcutDelimiter = value == default (Rune) ? new ('+') : value;
            }
        }
    }

    /// <summary>The specifier character for the hot keys.</summary>
    public new static Rune HotKeySpecifier => (Rune)'_';

    // Set in OnInvokingKeyBindings. -1 means no menu item is selected for activation.
    private int _menuBarItemToActivate;

    // Set in OnInvokingKeyBindings. null means no sub-menu is selected for activation.
    private MenuItem _menuItemToSelect;
    private bool _openedByAltKey;
    private bool _openedByHotKey;

    /// <summary>
    ///     Called when a key bound to Command.Select is pressed. Either activates the menu item or runs it, depending on
    ///     whether it has a sub-menu. If the menu is open, it will close the menu bar.
    /// </summary>
    /// <returns></returns>
    private bool SelectOrRun ()
    {
        if (!IsInitialized || !Visible)
        {
            return true;
        }

        _openedByHotKey = true;

        if (_menuBarItemToActivate != -1)
        {
            Activate (_menuBarItemToActivate);
        }
        else if (_menuItemToSelect is { })
        {
            Run (_menuItemToSelect.Action);
        }
        else
        {
            if (IsMenuOpen && _openMenu is { })
            {
                CloseAllMenus ();
            }
            else
            {
                OpenMenu ();
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public override bool? OnInvokingKeyBindings (Key key)
    {
        // This is a bit of a hack. We want to handle the key bindings for menu bar but
        // InvokeKeyBindings doesn't pass any context so we can't tell which item it is for.
        // So before we call the base class we set SelectedItem appropriately.
        // TODO: Figure out if there's a way to have KeyBindings pass context instead. Maybe a KeyBindingContext property?

        if (KeyBindings.TryGet (key, out _))
        {
            _menuBarItemToActivate = -1;
            _menuItemToSelect = null;

            // Search for shortcuts first. If there's a shortcut, we don't want to activate the menu item.
            for (var i = 0; i < Menus.Length; i++)
            {
                // Recurse through the menu to find one with the shortcut.
                if (FindShortcutInChildMenu (key.KeyCode, Menus [i], out _menuItemToSelect))
                {
                    _menuBarItemToActivate = i;

                    //keyEvent.Scope = KeyBindingScope.HotKey;

                    return base.OnInvokingKeyBindings (key);
                }

                // Now see if any of the menu bar items have a hot key that matches
                // Technically this is not possible because menu bar items don't have 
                // shortcuts or Actions. But it's here for completeness. 
                KeyCode? shortcut = Menus [i]?.Shortcut;

                if (key == shortcut)
                {
                    throw new InvalidOperationException ("Menu bar items cannot have shortcuts");
                }
            }

            // Search for hot keys next.
            for (var i = 0; i < Menus.Length; i++)
            {
                if (IsMenuOpen)
                {
                    // We don't need to do anything because `Menu` will handle the key binding.
                    //break;
                }

                // No submenu item matched (or the menu is closed)

                // Check if one of the menu bar item has a hot key that matches
                var hotKey = new Key ((char)Menus [i]?.HotKey.Value);

                if (hotKey != Key.Empty)
                {
                    bool matches = key == hotKey || key == hotKey.WithAlt || key == hotKey.NoShift.WithAlt;

                    if (IsMenuOpen)
                    {
                        // If the menu is open, only match if Alt is not pressed.
                        matches = key == hotKey;
                    }

                    if (matches)
                    {
                        _menuBarItemToActivate = i;

                        //keyEvent.Scope = KeyBindingScope.HotKey;

                        break;
                    }
                }
            }
        }

        return base.OnInvokingKeyBindings (key);
    }

    // TODO: Update to use Key instead of KeyCode
    // Recurse the child menus looking for a shortcut that matches the key
    private bool FindShortcutInChildMenu (KeyCode key, MenuBarItem menuBarItem, out MenuItem menuItemToSelect)
    {
        menuItemToSelect = null;

        if (key == KeyCode.Null || menuBarItem?.Children is null)
        {
            return false;
        }

        for (var c = 0; c < menuBarItem.Children.Length; c++)
        {
            MenuItem menuItem = menuBarItem.Children [c];

            if (key == menuItem?.Shortcut)
            {
                menuItemToSelect = menuItem;

                return true;
            }

            MenuBarItem subMenu = menuBarItem.SubMenu (menuItem);

            if (subMenu is { })
            {
                if (FindShortcutInChildMenu (key, subMenu, out menuItemToSelect))
                {
                    return true;
                }
            }
        }

        return false;
    }

    #endregion Keyboard handling

    #region Mouse Handling

    /// <inheritdoc/>
    public override bool OnEnter (View view)
    {
        Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

        return base.OnEnter (view);
    }

    /// <inheritdoc/>
    public override bool OnLeave (View view)
    {
        if (((!(view is MenuBar) && !(view is Menu)) || (!(view is MenuBar) && !(view is Menu) && _openMenu is { })) && !_isCleaning && !_reopen)
        {
            CleanUp ();
        }

        return base.OnLeave (view);
    }

    /// <inheritdoc/>
    protected internal override bool OnMouseEvent (MouseEvent me)
    {
        if (!_handled && !HandleGrabView (me, this))
        {
            return false;
        }

        _handled = false;

        if (me.Flags == MouseFlags.Button1Pressed
            || me.Flags == MouseFlags.Button1DoubleClicked
            || me.Flags == MouseFlags.Button1TripleClicked
            || me.Flags == MouseFlags.Button1Clicked
            || (me.Flags == MouseFlags.ReportMousePosition && _selected > -1)
            || (me.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition) && _selected > -1))
        {
            int pos = _xOrigin;
            Point locationOffset = default;

            if (SuperView is { })
            {
                locationOffset.X += SuperView.Border.Thickness.Left;
                locationOffset.Y += SuperView.Border.Thickness.Top;
            }

            int cx = me.X - locationOffset.X;

            for (var i = 0; i < Menus.Length; i++)
            {
                if (cx >= pos && cx < pos + _leftPadding + Menus [i].TitleLength + Menus [i].Help.GetColumns () + _rightPadding)
                {
                    if (me.Flags == MouseFlags.Button1Clicked)
                    {
                        if (Menus [i].IsTopLevel)
                        {
                            Rectangle screen = ViewportToScreen (new (new (0, i), Size.Empty));
                            var menu = new Menu { Host = this, X = screen.X, Y = screen.Y, BarItems = Menus [i] };
                            menu.Run (Menus [i].Action);
                            menu.Dispose ();
                        }
                        else if (!IsMenuOpen)
                        {
                            Activate (i);
                        }
                    }
                    else if (me.Flags == MouseFlags.Button1Pressed
                             || me.Flags == MouseFlags.Button1DoubleClicked
                             || me.Flags == MouseFlags.Button1TripleClicked)
                    {
                        if (IsMenuOpen && !Menus [i].IsTopLevel)
                        {
                            CloseAllMenus ();
                        }
                        else if (!Menus [i].IsTopLevel)
                        {
                            Activate (i);
                        }
                    }
                    else if (_selected != i
                             && _selected > -1
                             && (me.Flags == MouseFlags.ReportMousePosition
                                 || (me.Flags == MouseFlags.Button1Pressed && me.Flags == MouseFlags.ReportMousePosition)))
                    {
                        if (IsMenuOpen)
                        {
                            if (!CloseMenu (true, false))
                            {
                                return me.Handled = true;
                            }

                            Activate (i);
                        }
                    }
                    else if (IsMenuOpen)
                    {
                        if (!UseSubMenusSingleFrame
                            || (UseSubMenusSingleFrame
                                && openCurrentMenu != null
                                && openCurrentMenu.BarItems.Parent != null
                                && openCurrentMenu.BarItems.Parent.Parent != Menus [i]))
                        {
                            Activate (i);
                        }
                    }

                    return me.Handled = true;
                }

                if (i == Menus.Length - 1 && me.Flags == MouseFlags.Button1Clicked)
                {
                    if (IsMenuOpen && !Menus [i].IsTopLevel)
                    {
                        CloseAllMenus ();

                        return me.Handled = true;
                    }
                }

                pos += _leftPadding + Menus [i].TitleLength + _rightPadding;
            }
        }

        return false;
    }

    internal bool _handled;
    internal bool _isContextMenuLoading;
    private MenuBarItem [] _menus;

    internal bool HandleGrabView (MouseEvent me, View current)
    {
        if (Application.MouseGrabView is { })
        {
            if (me.View is MenuBar || me.View is Menu)
            {
                MenuBar mbar = GetMouseGrabViewInstance (me.View);

                if (mbar is { })
                {
                    if (me.Flags == MouseFlags.Button1Clicked)
                    {
                        mbar.CleanUp ();
                        Application.GrabMouse (me.View);
                    }
                    else
                    {
                        _handled = false;

                        return false;
                    }
                }

                if (me.View != current)
                {
                    Application.UngrabMouse ();
                    View v = me.View;
                    Application.GrabMouse (v);
                    MouseEvent nme;

                    if (me.Y > -1)
                    {
                        Point frameLoc = v.ScreenToFrame (me.X, me.Y);

                        nme = new ()
                        {
                            X = frameLoc.X,
                            Y = frameLoc.Y,
                            Flags = me.Flags,
                            View = v
                        };
                    }
                    else
                    {
                        nme = new () { X = me.X + current.Frame.X, Y = 0, Flags = me.Flags, View = v };
                    }

                    v.NewMouseEvent (nme);

                    return false;
                }
            }
            else if (!_isContextMenuLoading
                     && !(me.View is MenuBar || me.View is Menu)
                     && me.Flags != MouseFlags.ReportMousePosition
                     && me.Flags != 0)
            {
                Application.UngrabMouse ();

                if (IsMenuOpen)
                {
                    CloseAllMenus ();
                }

                _handled = false;

                return false;
            }
            else
            {
                _handled = false;
                _isContextMenuLoading = false;

                return false;
            }
        }
        else if (!IsMenuOpen
                 && (me.Flags == MouseFlags.Button1Pressed
                     || me.Flags == MouseFlags.Button1DoubleClicked
                     || me.Flags == MouseFlags.Button1TripleClicked
                     || me.Flags.HasFlag (
                                          MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
                                         )))
        {
            Application.GrabMouse (current);
        }
        else if (IsMenuOpen && (me.View is MenuBar || me.View is Menu))
        {
            Application.GrabMouse (me.View);
        }
        else
        {
            _handled = false;

            return false;
        }

        _handled = true;

        return true;
    }

    private MenuBar GetMouseGrabViewInstance (View view)
    {
        if (view is null || Application.MouseGrabView is null)
        {
            return null;
        }

        MenuBar hostView = null;

        if (view is MenuBar)
        {
            hostView = (MenuBar)view;
        }
        else if (view is Menu)
        {
            hostView = ((Menu)view).Host;
        }

        View grabView = Application.MouseGrabView;
        MenuBar hostGrabView = null;

        if (grabView is MenuBar)
        {
            hostGrabView = (MenuBar)grabView;
        }
        else if (grabView is Menu)
        {
            hostGrabView = ((Menu)grabView).Host;
        }

        return hostView != hostGrabView ? hostGrabView : null;
    }

    #endregion Mouse Handling
}
