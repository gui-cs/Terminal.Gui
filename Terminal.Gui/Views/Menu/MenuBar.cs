#nullable enable

namespace Terminal.Gui;

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
public class MenuBar : View, IDesignable
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

    internal Menu? _openMenu;
    internal List<Menu>? _openSubMenu;
    internal int _selected;
    internal int _selectedSub;

    private bool _initialCanFocus;
    private bool _isCleaning;
    private View? _lastFocused;
    private Menu? _ocm;
    private View? _previousFocused;
    private bool _reopen;
    private bool _useSubMenusSingleFrame;

    /// <summary>Initializes a new instance of the <see cref="MenuBar"/>.</summary>
    public MenuBar ()
    {
        TabStop = TabBehavior.NoStop;
        X = 0;
        Y = 0;
        Width = Dim.Fill ();
        Height = 1; // BUGBUG: Views should avoid setting Height as doing so implies Frame.Size == GetContentSize ().
        Menus = new MenuBarItem [] { };

        //CanFocus = true;
        _selected = -1;
        _selectedSub = -1;
        // ReSharper disable once VirtualMemberCallInConstructor
        ColorScheme = Colors.ColorSchemes ["Menu"];
        // ReSharper disable once VirtualMemberCallInConstructor
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
                    (ctx) =>
                    {
                        if (Menus.Length > 0)
                        {
                            ProcessMenu (_selected, Menus [_selected]);
                        }

                        return RaiseAccepting (ctx);
                    }
                   );
        AddCommand (Command.Toggle, ctx =>
                                                  {
                                                      CloseOtherOpenedMenuBar ();

                                                      return Select (Menus.IndexOf (ctx.KeyBinding?.Context));
                                                  });
        AddCommand (Command.Select, ctx =>
                                    {
                                        if (ctx.Data is MouseEventArgs)
                                        {
                                            // HACK: Work around the fact that View.MouseClick always invokes Select
                                            return false;
                                        }
                                        var res = Run ((ctx.KeyBinding?.Context as MenuItem)?.Action!);
                                        CloseAllMenus ();

                                        return res;
                                    });

        // Default key bindings for this view
        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.Esc, Command.Cancel);
        KeyBindings.Add (Key.CursorDown, Command.Accept);

        KeyBinding keyBinding = new ([Command.Toggle], KeyBindingScope.HotKey, -1); // -1 indicates Key was used
        KeyBindings.Add (Key, keyBinding);

        // TODO: Why do we have two keybindings for opening the menu? Ctrl-Space and Key?
        KeyBindings.Add (Key.Space.WithCtrl, keyBinding);
        // This is needed for macOS because Key.Space.WithCtrl doesn't work
        KeyBindings.Add (Key.Space.WithAlt, keyBinding);

        // TODO: Figure out how to make Alt work (on Windows)
        //KeyBindings.Add (Key.WithAlt, keyBinding);
    }

    /// <summary><see langword="true"/> if the menu is open; otherwise <see langword="true"/>.</summary>
    public bool IsMenuOpen { get; protected set; }

    /// <summary>Gets the view that was last focused before opening the menu.</summary>
    public View? LastFocused { get; private set; }

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

            if (Menus is [])
            {
                return;
            }

            // TODO: Hotkeys should not work for sub-menus if they are not visible!
            for (var i = 0; i < Menus.Length; i++)
            {
                MenuBarItem menuBarItem = Menus [i];

                if (menuBarItem.HotKey != Key.Empty)
                {
                    KeyBindings.Remove (menuBarItem.HotKey!);
                    KeyBinding keyBinding = new ([Command.Toggle], KeyBindingScope.Focused, menuBarItem);
                    KeyBindings.Add (menuBarItem.HotKey!, keyBinding);
                    KeyBindings.Remove (menuBarItem.HotKey!.WithAlt);
                    keyBinding = new ([Command.Toggle], KeyBindingScope.HotKey, menuBarItem);
                    KeyBindings.Add (menuBarItem.HotKey.WithAlt, keyBinding);
                }

                if (menuBarItem.ShortcutKey != Key.Empty)
                {
                    // Technically this will never run because MenuBarItems don't have shortcuts
                    // unless the IsTopLevel is true
                    KeyBindings.Remove (menuBarItem.ShortcutKey!);
                    KeyBinding keyBinding = new ([Command.Select], KeyBindingScope.HotKey, menuBarItem);
                    KeyBindings.Add (menuBarItem.ShortcutKey!, keyBinding);
                }

                menuBarItem.AddShortcutKeyBindings (this);
            }
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
    ///         By default, any sub-sub-menus (sub-menus of the main <see cref="MenuItem"/>s) are displayed in a cascading
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

    internal Menu? OpenCurrentMenu
    {
        get => _ocm;
        set
        {
            if (_ocm != value)
            {
                _ocm = value!;

                if (_ocm is { _currentChild: > -1 })
                {
                    OnMenuOpened ();
                }
            }
        }
    }

    /// <summary>Closes the Menu programmatically if open and not canceled (as though F9 were pressed).</summary>
    public bool CloseMenu (bool ignoreUseSubMenusSingleFrame = false) { return CloseMenu (false, false, ignoreUseSubMenusSingleFrame); }

    /// <summary>Raised when all the menu is closed.</summary>
    public event EventHandler? MenuAllClosed;

    /// <summary>Raised when a menu is closing passing <see cref="MenuClosingEventArgs"/>.</summary>
    public event EventHandler<MenuClosingEventArgs>? MenuClosing;

    /// <summary>Raised when a menu is opened.</summary>
    public event EventHandler<MenuOpenedEventArgs>? MenuOpened;

    /// <summary>Raised as a menu is opening.</summary>
    public event EventHandler<MenuOpeningEventArgs>? MenuOpening;

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
                hotColor = i == _selected ? ColorScheme!.HotFocus : GetHotNormalColor ();
                normalColor = i == _selected ? GetFocusColor () : GetNormalColor ();
            }
            else
            {
                hotColor = GetHotNormalColor ();
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

        //PositionCursor ();
    }

    /// <summary>Virtual method that will invoke the <see cref="MenuAllClosed"/>.</summary>
    public virtual void OnMenuAllClosed () { MenuAllClosed?.Invoke (this, EventArgs.Empty); }

    /// <summary>Virtual method that will invoke the <see cref="MenuClosing"/>.</summary>
    /// <param name="currentMenu">The current menu to be closed.</param>
    /// <param name="reopen">Whether the current menu will be reopened.</param>
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
        MenuItem? mi = null;
        MenuBarItem? parent;

        if (OpenCurrentMenu?.BarItems?.Children is { Length: > 0 }
            && OpenCurrentMenu?._currentChild > -1)
        {
            parent = OpenCurrentMenu.BarItems;
            mi = parent.Children [OpenCurrentMenu._currentChild];
        }
        else if (OpenCurrentMenu!.BarItems!.IsTopLevel)
        {
            parent = null;
            mi = OpenCurrentMenu.BarItems;
        }
        else
        {
            parent = _openMenu?.BarItems;

            if (OpenCurrentMenu?._currentChild > -1)
            {
                mi = parent?.Children?.Length > 0 ? parent.Children [_openMenu!._currentChild] : null;
            }
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
        MenuBar? mbar = GetMouseGrabViewInstance (this);

        mbar?.CleanUp ();

        CloseOtherOpenedMenuBar ();

        if (!Enabled || _openMenu is { })
        {
            return;
        }

        _selected = 0;
        SetNeedsDisplay ();

        _previousFocused = (SuperView is null ? Application.Top?.Focused : SuperView.Focused)!;
        OpenMenu (_selected);

        if (!SelectEnabledItem (
                                OpenCurrentMenu?.BarItems?.Children,
                                OpenCurrentMenu!._currentChild,
                                out OpenCurrentMenu._currentChild
                               )
            && !CloseMenu ())
        {
            return;
        }

        if (!OpenCurrentMenu.CheckSubMenu ())
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

                return null; // Don't show the cursor
            }

            pos += _leftPadding
                   + Menus [i].TitleLength
                   + (Menus [i].Help.GetColumns () > 0
                          ? Menus [i].Help.GetColumns () + _parensAroundHelp
                          : 0)
                   + _rightPadding;
        }

        return null; // Don't show the cursor
    }

    // Activates the menu, handles either first focus, or activating an entry when it was already active
    // For mouse events.
    internal void Activate (int idx, int sIdx = -1, MenuBarItem? subMenu = null!)
    {
        _selected = idx;
        _selectedSub = sIdx;

        if (_openMenu is null)
        {
            _previousFocused = (SuperView is null ? Application.Top?.Focused ?? null : SuperView.Focused)!;
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
        IsMenuOpen = false;
        _selected = -1;
        CanFocus = _initialCanFocus;

        if (_lastFocused is { })
        {
            _lastFocused.SetFocus ();
        }

        SetNeedsDisplay ();

        if (Application.MouseGrabView is { } && Application.MouseGrabView is MenuBar && Application.MouseGrabView != this)
        {
            var menuBar = Application.MouseGrabView as MenuBar;

            if (menuBar!.IsMenuOpen)
            {
                menuBar.CleanUp ();
            }
        }
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

            if (!CloseMenu ())
            {
                return;
            }

            if (LastFocused is { } && LastFocused != this)
            {
                _selected = -1;
            }

            Application.UngrabMouse ();
        }

        if (OpenCurrentMenu is { })
        {
            OpenCurrentMenu = null;
        }

        IsMenuOpen = false;
        _openedByAltKey = false;
        OnMenuAllClosed ();

        CloseOtherOpenedMenuBar ();
    }

    private void CloseOtherOpenedMenuBar ()
    {
        if (Application.Top is { })
        {
            // Close others menu bar opened
            Menu? menu = Application.Top.Subviews.FirstOrDefault (v => v is Menu m && m.Host != this && m.Host.IsMenuOpen) as Menu;
            menu?.Host.CleanUp ();
        }
    }

    internal bool CloseMenu (bool reopen, bool isSubMenu, bool ignoreUseSubMenusSingleFrame = false)
    {
        MenuBarItem? mbi = isSubMenu ? OpenCurrentMenu!.BarItems : _openMenu?.BarItems;

        if (UseSubMenusSingleFrame && mbi is { } && !ignoreUseSubMenusSingleFrame && mbi.Parent is { })
        {
            return false;
        }

        _isMenuClosing = true;
        _reopen = reopen;
        MenuClosingEventArgs args = OnMenuClosing (mbi!, reopen, isSubMenu);

        if (args.Cancel)
        {
            _isMenuClosing = false;

            if (args.CurrentMenu.Parent is { } && _openMenu is { })
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
                    Application.Top?.Remove (_openMenu);
                }

                SetNeedsDisplay ();

                if (_previousFocused is Menu && _openMenu is { } && _previousFocused.ToString () != OpenCurrentMenu!.ToString ())
                {
                    _previousFocused.SetFocus ();
                }

                if (Application.MouseGrabView == _openMenu)
                {
                    Application.UngrabMouse();
                }
                _openMenu?.Dispose ();
                _openMenu = null;

                if (_lastFocused is Menu or MenuBar)
                {
                    _lastFocused = null;
                }

                LastFocused = _lastFocused;
                _lastFocused = null;

                if (LastFocused is { CanFocus: true })
                {
                    if (!reopen)
                    {
                        _selected = -1;
                    }

                    if (_openSubMenu is { })
                    {
                        _openSubMenu = null;
                    }

                    if (OpenCurrentMenu is { })
                    {
                        Application.Top?.Remove (OpenCurrentMenu);
                        if (Application.MouseGrabView == OpenCurrentMenu)
                        {
                            Application.UngrabMouse ();
                        }
                        OpenCurrentMenu.Dispose ();
                        OpenCurrentMenu = null;
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
                }

                IsMenuOpen = false;

                break;

            case true:
                _selectedSub = -1;
                SetNeedsDisplay ();
                RemoveAllOpensSubMenus ();
                OpenCurrentMenu!._previousSubFocused!.SetFocus ();
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
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (Driver is null)
        {
            return Point.Empty;
        }

        Rectangle superViewFrame = SuperView?.Frame ?? Application.Screen;
        View? sv = SuperView ?? Application.Top;

        if (sv is null)
        {
            // Support Unit Tests
            return Point.Empty;
        }

        Point viewportOffset = sv.GetViewportOffsetFromFrame ();

        return new (
                    superViewFrame.X - sv.Frame.X - viewportOffset.X,
                    superViewFrame.Y - sv.Frame.Y - viewportOffset.Y
                   );
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
                                   OpenCurrentMenu?.BarItems?.Children,
                                   OpenCurrentMenu!._currentChild,
                                   out OpenCurrentMenu._currentChild
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
                    MenuBarItem? subMenu = OpenCurrentMenu!._currentChild > -1 && OpenCurrentMenu.BarItems?.Children!.Length > 0
                                              ? OpenCurrentMenu.BarItems.SubMenu (
                                                                                  OpenCurrentMenu.BarItems.Children? [OpenCurrentMenu._currentChild]!
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
                             || (OpenCurrentMenu._currentChild > -1
                                 && !OpenCurrentMenu.BarItems!
                                                    .Children! [OpenCurrentMenu._currentChild]!
                                                    .IsFromSubMenu))
                    {
                        _selectedSub++;
                        OpenCurrentMenu.CheckSubMenu ();
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
                        OpenCurrentMenu.CheckSubMenu ();
                    }
                }

                break;
        }
    }

    internal void OpenMenu (int index, int sIndex = -1, MenuBarItem? subMenu = null!)
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
                _lastFocused ??= SuperView is null ? Application.Top?.MostFocused : SuperView.MostFocused;

                if (_openSubMenu is { } && !CloseMenu (false, true))
                {
                    return;
                }

                if (_openMenu is { })
                {
                    Application.Top?.Remove (_openMenu);
                    if (Application.MouseGrabView == _openMenu)
                    {
                        Application.UngrabMouse ();
                    }
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

                if (SuperView is { } && SuperView != Application.Top)
                {
                    locationOffset.X += SuperView.Border.Thickness.Left;
                    locationOffset.Y += SuperView.Border.Thickness.Top;
                }

                _openMenu = new ()
                {
                    Host = this,
                    X = Frame.X + pos + locationOffset.X,
                    Y = Frame.Y + 1 + locationOffset.Y,
                    BarItems = Menus [index],
                    Parent = null
                };
                OpenCurrentMenu = _openMenu;
                OpenCurrentMenu._previousSubFocused = _openMenu;

                if (Application.Top is { })
                {
                    Application.Top.Add (_openMenu);
                }
                else
                {
                    _openMenu.BeginInit ();
                    _openMenu.EndInit ();
                }

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
                    Menu? last = _openSubMenu.Count > 0 ? _openSubMenu.Last () : _openMenu;

                    if (!UseSubMenusSingleFrame)
                    {
                        locationOffset = GetLocationOffset ();

                        OpenCurrentMenu = new ()
                        {
                            Host = this,
                            X = last!.Frame.Left + last.Frame.Width + locationOffset.X,
                            Y = last.Frame.Top + locationOffset.Y + last._currentChild,
                            BarItems = subMenu,
                            Parent = last
                        };
                    }
                    else
                    {
                        Menu? first = _openSubMenu.Count > 0 ? _openSubMenu.First () : _openMenu;

                        // 2 is for the parent and the separator
                        MenuItem? [] mbi = new MenuItem [2 + subMenu.Children!.Length];
                        mbi [0] = new () { Title = subMenu.Title, Parent = subMenu };
                        mbi [1] = null;

                        for (var j = 0; j < subMenu.Children.Length; j++)
                        {
                            mbi [j + 2] = subMenu.Children [j];
                        }

                        var newSubMenu = new MenuBarItem (mbi!) { Parent = subMenu };

                        OpenCurrentMenu = new ()
                        {
                            Host = this, X = first!.Frame.Left, Y = first.Frame.Top, BarItems = newSubMenu
                        };
                        last!.Visible = false;
                        Application.GrabMouse (OpenCurrentMenu);
                    }

                    OpenCurrentMenu._previousSubFocused = last._previousSubFocused;
                    _openSubMenu.Add (OpenCurrentMenu);
                    Application.Top?.Add (OpenCurrentMenu);

                    if (!OpenCurrentMenu.IsInitialized)
                    {
                        // Supports unit tests
                        OpenCurrentMenu.BeginInit ();
                        OpenCurrentMenu.EndInit ();
                    }
                }

                _selectedSub = _openSubMenu.Count - 1;

                if (_selectedSub > -1
                    && SelectEnabledItem (
                                          OpenCurrentMenu!.BarItems!.Children,
                                          OpenCurrentMenu._currentChild,
                                          out OpenCurrentMenu._currentChild
                                         ))
                {
                    OpenCurrentMenu.SetFocus ();
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
                                        OpenCurrentMenu?.BarItems?.Children,
                                        OpenCurrentMenu!._currentChild,
                                        out OpenCurrentMenu._currentChild,
                                        false
                                       ))
                {
                    OpenCurrentMenu._currentChild = 0;
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
                Application.Top!.Remove (item);
                if (Application.MouseGrabView == item)
                {
                    Application.UngrabMouse ();
                }
                item.Dispose ();
            }
        }
    }

    internal bool Run (Action? action)
    {
        if (action is null)
        {
            return false;
        }

        Application.MainLoop!.AddIdle (
                                       () =>
                                       {
                                           action ();

                                           return false;
                                       }
                                      );

        return true;
    }

    internal bool SelectEnabledItem (
        MenuItem? []? children,
        int current,
        out int newCurrent,
        bool forward = true
    )
    {
        if (children is null)
        {
            newCurrent = -1;

            return true;
        }

        IEnumerable<MenuItem?> childMenuItems = forward ? children : children.Reverse ();

        int count;

        IEnumerable<MenuItem?> menuItems = childMenuItems as MenuItem [] ?? childMenuItems.ToArray ();

        if (forward)
        {
            count = -1;
        }
        else
        {
            count = menuItems.Count ();
        }

        foreach (MenuItem? child in menuItems)
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

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
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
    internal bool SelectItem (MenuItem? item)
    {
        if (item?.Action is null)
        {
            return false;
        }

        Application.UngrabMouse ();
        CloseAllMenus ();
        Application.Refresh ();
        _openedByAltKey = true;

        return Run (item.Action);
    }

    private void CloseMenuBar ()
    {
        if (!CloseMenu ())
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

    private void MenuBar_Added (object? sender, SuperViewChangedEventArgs e)
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

    private bool ProcessMenu (int i, MenuBarItem mi)
    {
        if (_selected < 0 && IsMenuOpen)
        {
            return false;
        }

        if (mi.IsTopLevel)
        {
            Point screen = ViewportToScreen (new Point (0, i));
            var menu = new Menu { Host = this, X = screen.X, Y = screen.Y, BarItems = mi };
            menu.Run (mi.Action);
            if (Application.MouseGrabView == menu)
            {
                Application.UngrabMouse ();
            }
            menu.Dispose ();
        }
        else
        {
            Application.GrabMouse (this);
            _selected = i;
            OpenMenu (i);

            if (!SelectEnabledItem (
                                    OpenCurrentMenu?.BarItems?.Children,
                                    OpenCurrentMenu!._currentChild,
                                    out OpenCurrentMenu._currentChild
                                   )
                && !CloseMenu ())
            {
                return true;
            }

            if (!OpenCurrentMenu.CheckSubMenu ())
            {
                return true;
            }
        }

        SetNeedsDisplay ();

        return true;
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
            Menu? menu;

            if (_openSubMenu!.Count - 1 > 0)
            {
                menu = _openSubMenu [i - 1];
            }
            else
            {
                menu = _openMenu;
            }

            if (!menu!.Visible)
            {
                menu.Visible = true;
            }

            OpenCurrentMenu = menu;
            OpenCurrentMenu.SetFocus ();

            if (_openSubMenu is { })
            {
                menu = _openSubMenu [i];
                Application.Top!.Remove (menu);
                _openSubMenu.Remove (menu);

                if (Application.MouseGrabView == menu)
                {
                    Application.GrabMouse (this);
                }

                menu.Dispose ();
            }

            RemoveSubMenu (i, ignoreUseSubMenusSingleFrame);
        }

        if (_openSubMenu!.Count > 0)
        {
            OpenCurrentMenu = _openSubMenu.Last ();
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
            KeyBinding keyBinding = new ([Command.Toggle], KeyBindingScope.HotKey, -1); // -1 indicates Key was used
            KeyBindings.Add (value, keyBinding);
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

    /// <summary>The specifier character for the hot keys.</summary>
    public new static Rune HotKeySpecifier => (Rune)'_';

    // TODO: This doesn't actually work. Figure out why.
    private bool _openedByAltKey;

    /// <summary>
    ///     Called when a key bound to Command.Select is pressed. Either activates the menu item or runs it, depending on
    ///     whether it has a sub-menu. If the menu is open, it will close the menu bar.
    /// </summary>
    /// <param name="index">The index of the menu bar item to select. -1 if the selection was via <see cref="Key"/>.</param>
    /// <returns></returns>
    private bool Select (int index)
    {
        if (!IsInitialized || !Visible)
        {
            return true;
        }

        // If the menubar is open and the menu that's open is 'index' then close it. Otherwise activate it.
        if (IsMenuOpen)
        {
            if (index == -1)
            {
                CloseAllMenus ();

                return true;
            }

            // Find the index of the open submenu and close the menu if it matches
            for (var i = 0; i < Menus.Length; i++)
            {
                MenuBarItem open = Menus [i];

                if (open == OpenCurrentMenu!.BarItems && i == index)
                {
                    CloseAllMenus ();
                    return true;
                }
            }
        }

        if (index == -1)
        {
            OpenMenu ();
        }
        else if (Menus [index].IsTopLevel)
        {
            Run (Menus [index].Action);
        }
        else
        {
            Activate (index);
        }

        return true;
    }

    #endregion Keyboard handling

    #region Mouse Handling

    internal void LostFocus (View view)
    {
        if (view is not MenuBar && view is not Menu && !_isCleaning && !_reopen)
        {
            CleanUp ();
        }
    }

    /// <inheritdoc/>
    protected override bool OnMouseEvent (MouseEventArgs me)
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

            int cx = me.Position.X - locationOffset.X;

            for (var i = 0; i < Menus.Length; i++)
            {
                if (cx >= pos && cx < pos + _leftPadding + Menus [i].TitleLength + Menus [i].Help.GetColumns () + _rightPadding)
                {
                    if (me.Flags == MouseFlags.Button1Clicked)
                    {
                        if (Menus [i].IsTopLevel)
                        {
                            Point screen = ViewportToScreen (new Point (0, i));
                            var menu = new Menu { Host = this, X = screen.X, Y = screen.Y, BarItems = Menus [i] };
                            menu.Run (Menus [i].Action);
                            if (Application.MouseGrabView == menu)
                            {
                                Application.UngrabMouse ();
                            }

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
                                 || (me.Flags is MouseFlags.Button1Pressed && me.Flags == MouseFlags.ReportMousePosition)))
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
                                && OpenCurrentMenu is { BarItems.Parent: { } }
                                && OpenCurrentMenu.BarItems.Parent.Parent != Menus [i]))
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
    private MenuBarItem [] _menus = [];

    internal bool HandleGrabView (MouseEventArgs me, View current)
    {
        if (Application.MouseGrabView is { })
        {
            if (me.View is MenuBar or Menu)
            {
                MenuBar? mbar = GetMouseGrabViewInstance (me.View);

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
                    MouseEventArgs nme;

                    if (me.Position.Y > -1)
                    {
                        Point frameLoc = v.ScreenToFrame (me.Position);

                        nme = new ()
                        {
                            Position = frameLoc,
                            Flags = me.Flags,
                            View = v
                        };
                    }
                    else
                    {
                        nme = new ()
                        {
                            Position = new (me.Position.X + current.Frame.X, me.Position.Y + current.Frame.Y),
                            Flags = me.Flags, View = v
                        };
                    }

                    v.NewMouseEvent (nme);

                    return false;
                }
            }
            else if (!(me.View is MenuBar || me.View is Menu)
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

    private MenuBar? GetMouseGrabViewInstance (View? view)
    {
        if (view is null || Application.MouseGrabView is null)
        {
            return null;
        }

        MenuBar? hostView = null;

        if (view is MenuBar)
        {
            hostView = (MenuBar)view;
        }
        else if (view is Menu)
        {
            hostView = ((Menu)view).Host;
        }

        View grabView = Application.MouseGrabView;
        MenuBar? hostGrabView = null;

        if (grabView is MenuBar bar)
        {
            hostGrabView = bar;
        }
        else if (grabView is Menu menu)
        {
            hostGrabView = menu.Host;
        }

        return hostView != hostGrabView ? hostGrabView : null;
    }

    #endregion Mouse Handling


    /// <inheritdoc />
    public bool EnableForDesign<TContext> (ref readonly TContext context) where TContext : notnull
    {
        if (context is not Func<string, bool> actionFn)
        {
            actionFn = (_) => true;
        }

        Menus =
        [
            new MenuBarItem (
                             "_File",
                             new MenuItem []
                             {
                                 new (
                                      "_New",
                                      "",
                                      () => actionFn ("New"),
                                      null,
                                      null,
                                      KeyCode.CtrlMask | KeyCode.N
                                     ),
                                 new (
                                      "_Open",
                                      "",
                                      () => actionFn ("Open"),
                                      null,
                                      null,
                                      KeyCode.CtrlMask | KeyCode.O
                                     ),
                                 new (
                                      "_Save",
                                      "",
                                      () => actionFn ("Save"),
                                      null,
                                      null,
                                      KeyCode.CtrlMask | KeyCode.S
                                     ),
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                                 null,
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

                                 // Don't use Application.Quit so we can disambiguate between quitting and closing the toplevel
                                 new (
                                      "_Quit",
                                      "",
                                      () => actionFn ("Quit"),
                                      null,
                                      null,
                                      KeyCode.CtrlMask | KeyCode.Q
                                     )
                             }
                            ),
            new MenuBarItem (
                             "_Edit",
                             new MenuItem []
                             {
                                 new (
                                      "_Copy",
                                      "",
                                      () => actionFn ("Copy"),
                                      null,
                                      null,
                                      KeyCode.CtrlMask | KeyCode.C
                                     ),
                                 new (
                                      "C_ut",
                                      "",
                                      () => actionFn ("Cut"),
                                      null,
                                      null,
                                      KeyCode.CtrlMask | KeyCode.X
                                     ),
                                 new (
                                      "_Paste",
                                      "",
                                      () => actionFn ("Paste"),
                                      null,
                                      null,
                                      KeyCode.CtrlMask | KeyCode.V
                                     ),
                                 new MenuBarItem (
                                                  "_Find and Replace",
                                                  new MenuItem []
                                                  {
                                                      new (
                                                           "F_ind",
                                                           "",
                                                           () => actionFn ("Find"),
                                                           null,
                                                           null,
                                                           KeyCode.CtrlMask | KeyCode.F
                                                          ),
                                                      new (
                                                           "_Replace",
                                                           "",
                                                           () => actionFn ("Replace"),
                                                           null,
                                                           null,
                                                           KeyCode.CtrlMask | KeyCode.H
                                                          ),
                                                      new MenuBarItem (
                                                                       "_3rd Level",
                                                                       new MenuItem []
                                                                       {
                                                                           new (
                                                                                "_1st",
                                                                                "",
                                                                                () => actionFn (
                                                                                                "1"
                                                                                               ),
                                                                                null,
                                                                                null,
                                                                                KeyCode.F1
                                                                               ),
                                                                           new (
                                                                                "_2nd",
                                                                                "",
                                                                                () => actionFn (
                                                                                                "2"
                                                                                               ),
                                                                                null,
                                                                                null,
                                                                                KeyCode.F2
                                                                               )
                                                                       }
                                                                      ),
                                                      new MenuBarItem (
                                                                       "_4th Level",
                                                                       new MenuItem []
                                                                       {
                                                                           new (
                                                                                "_5th",
                                                                                "",
                                                                                () => actionFn (
                                                                                                "5"
                                                                                               ),
                                                                                null,
                                                                                null,
                                                                                KeyCode.CtrlMask
                                                                                | KeyCode.D5
                                                                               ),
                                                                           new (
                                                                                "_6th",
                                                                                "",
                                                                                () => actionFn (
                                                                                                "6"
                                                                                               ),
                                                                                null,
                                                                                null,
                                                                                KeyCode.CtrlMask
                                                                                | KeyCode.D6
                                                                               )
                                                                       }
                                                                      )
                                                  }
                                                 ),
                                 new (
                                      "_Select All",
                                      "",
                                      () => actionFn ("Select All"),
                                      null,
                                      null,
                                      KeyCode.CtrlMask
                                      | KeyCode.ShiftMask
                                      | KeyCode.S
                                     )
                             }
                            ),
            new MenuBarItem ("_About", "Top-Level", () => actionFn ("About"))
        ];
        return true;
    }
}
