namespace Terminal.Gui;

/// <summary>Specifies how a <see cref="MenuItem"/> shows selection state.</summary>
[Flags]
public enum MenuItemCheckStyle
{
    /// <summary>The menu item will be shown normally, with no check indicator. The default.</summary>
    NoCheck = 0b_0000_0000,

    /// <summary>The menu item will indicate checked/un-checked state (see <see cref="Checked"/>).</summary>
    Checked = 0b_0000_0001,

    /// <summary>The menu item is part of a menu radio group (see <see cref="Checked"/>) and will indicate selected state.</summary>
    Radio = 0b_0000_0010
}

/// <summary>
///     A <see cref="MenuItem"/> has title, an associated help text, and an action to execute on activation. MenuItems
///     can also have a checked indicator (see <see cref="Checked"/>).
/// </summary>
public class MenuItem
{
    private readonly ShortcutHelper _shortcutHelper;
    private bool _allowNullChecked;
    private MenuItemCheckStyle _checkType;

    private string _title;

    // TODO: Update to use Key instead of KeyCode
    /// <summary>Initializes a new instance of <see cref="MenuItem"/></summary>
    public MenuItem (KeyCode shortcut = KeyCode.Null) : this ("", "", null, null, null, shortcut) { }

    // TODO: Update to use Key instead of KeyCode
    /// <summary>Initializes a new instance of <see cref="MenuItem"/>.</summary>
    /// <param name="title">Title for the menu item.</param>
    /// <param name="help">Help text to display.</param>
    /// <param name="action">Action to invoke when the menu item is activated.</param>
    /// <param name="canExecute">Function to determine if the action can currently be executed.</param>
    /// <param name="parent">The <see cref="Parent"/> of this menu item.</param>
    /// <param name="shortcut">The <see cref="Shortcut"/> keystroke combination.</param>
    public MenuItem (
        string title,
        string help,
        Action action,
        Func<bool> canExecute = null,
        MenuItem parent = null,
        KeyCode shortcut = KeyCode.Null
    )
    {
        Title = title ?? "";
        Help = help ?? "";
        Action = action;
        CanExecute = canExecute;
        Parent = parent;
        _shortcutHelper = new ShortcutHelper ();

        if (shortcut != KeyCode.Null)
        {
            Shortcut = shortcut;
        }
    }

    /// <summary>Gets or sets the action to be invoked when the menu item is triggered.</summary>
    /// <value>Method to invoke.</value>
    public Action Action { get; set; }

    /// <summary>
    ///     Used only if <see cref="CheckType"/> is of <see cref="MenuItemCheckStyle.Checked"/> type. If
    ///     <see langword="true"/> allows <see cref="Checked"/> to be null, true or false. If <see langword="false"/> only
    ///     allows <see cref="Checked"/> to be true or false.
    /// </summary>
    public bool AllowNullChecked
    {
        get => _allowNullChecked;
        set
        {
            _allowNullChecked = value;
            Checked ??= false;
        }
    }

    /// <summary>
    ///     Gets or sets the action to be invoked to determine if the menu can be triggered. If <see cref="CanExecute"/>
    ///     returns <see langword="true"/> the menu item will be enabled. Otherwise, it will be disabled.
    /// </summary>
    /// <value>Function to determine if the action is can be executed or not.</value>
    public Func<bool> CanExecute { get; set; }

    /// <summary>
    ///     Sets or gets whether the <see cref="MenuItem"/> shows a check indicator or not. See
    ///     <see cref="MenuItemCheckStyle"/>.
    /// </summary>
    public bool? Checked { set; get; }

    /// <summary>
    ///     Sets or gets the <see cref="MenuItemCheckStyle"/> of a menu item where <see cref="Checked"/> is set to
    ///     <see langword="true"/>.
    /// </summary>
    public MenuItemCheckStyle CheckType
    {
        get => _checkType;
        set
        {
            _checkType = value;

            if (_checkType == MenuItemCheckStyle.Checked && !_allowNullChecked && Checked is null)
            {
                Checked = false;
            }
        }
    }

    /// <summary>Gets or sets arbitrary data for the menu item.</summary>
    /// <remarks>This property is not used internally.</remarks>
    public object Data { get; set; }

    /// <summary>Gets or sets the help text for the menu item. The help text is drawn to the right of the <see cref="Title"/>.</summary>
    /// <value>The help text.</value>
    public string Help { get; set; }

    /// <summary>Gets the parent for this <see cref="MenuItem"/>.</summary>
    /// <value>The parent.</value>
    public MenuItem Parent { get; set; }

    /// <summary>Gets or sets the title of the menu item .</summary>
    /// <value>The title.</value>
    public string Title
    {
        get => _title;
        set
        {
            if (_title == value)
            {
                return;
            }

            _title = value;
            GetHotKey ();
        }
    }

    /// <summary>Gets if this <see cref="MenuItem"/> is from a sub-menu.</summary>
    internal bool IsFromSubMenu => Parent != null;

    internal int TitleLength => GetMenuBarItemLength (Title);

    // 
    // ┌─────────────────────────────┐
    // │ Quit  Quit UI Catalog  Ctrl+Q │
    // └─────────────────────────────┘
    // ┌─────────────────┐
    // │ ◌ TopLevel Alt+T │
    // └─────────────────┘
    // TODO: Replace the `2` literals with named constants 
    internal int Width => 1
                          + // space before Title
                          TitleLength
                          + 2
                          + // space after Title - BUGBUG: This should be 1 
                          (Checked == true || CheckType.HasFlag (MenuItemCheckStyle.Checked) || CheckType.HasFlag (MenuItemCheckStyle.Radio)
                               ? 2
                               : 0)
                          + // check glyph + space 
                          (Help.GetColumns () > 0 ? 2 + Help.GetColumns () : 0)
                          + // Two spaces before Help
                          (ShortcutTag.GetColumns () > 0
                               ? 2 + ShortcutTag.GetColumns ()
                               : 0); // Pad two spaces before shortcut tag (which are also aligned right)

    /// <summary>Merely a debugging aid to see the interaction with main.</summary>
    public bool GetMenuBarItem () { return IsFromSubMenu; }

    /// <summary>Merely a debugging aid to see the interaction with main.</summary>
    public MenuItem GetMenuItem () { return this; }

    /// <summary>
    ///     Returns <see langword="true"/> if the menu item is enabled. This method is a wrapper around
    ///     <see cref="CanExecute"/>.
    /// </summary>
    public bool IsEnabled () { return CanExecute?.Invoke () ?? true; }

    /// <summary>
    ///     Toggle the <see cref="Checked"/> between three states if <see cref="AllowNullChecked"/> is
    ///     <see langword="true"/> or between two states if <see cref="AllowNullChecked"/> is <see langword="false"/>.
    /// </summary>
    public void ToggleChecked ()
    {
        if (_checkType != MenuItemCheckStyle.Checked)
        {
            throw new InvalidOperationException ("This isn't a Checked MenuItemCheckStyle!");
        }

        bool? previousChecked = Checked;

        if (AllowNullChecked)
        {
            Checked = previousChecked switch
                      {
                          null => true,
                          true => false,
                          false => null
                      };
        }
        else
        {
            Checked = !Checked;
        }
    }

    private static int GetMenuBarItemLength (string title)
    {
        return title.EnumerateRunes ()
                    .Where (ch => ch != MenuBar.HotKeySpecifier)
                    .Sum (ch => Math.Max (ch.GetColumns (), 1));
    }

    #region Keyboard Handling

    // TODO: Update to use Key instead of Rune
    /// <summary>
    ///     The HotKey is used to activate a <see cref="MenuItem"/> with the keyboard. HotKeys are defined by prefixing the
    ///     <see cref="Title"/> of a MenuItem with an underscore ('_').
    ///     <para>
    ///         Pressing Alt-Hotkey for a <see cref="MenuBarItem"/> (menu items on the menu bar) works even if the menu is
    ///         not active). Once a menu has focus and is active, pressing just the HotKey will activate the MenuItem.
    ///     </para>
    ///     <para>
    ///         For example for a MenuBar with a "_File" MenuBarItem that contains a "_New" MenuItem, Alt-F will open the
    ///         File menu. Pressing the N key will then activate the New MenuItem.
    ///     </para>
    ///     <para>See also <see cref="Shortcut"/> which enable global key-bindings to menu items.</para>
    /// </summary>
    public Rune HotKey { get; set; }

    private void GetHotKey ()
    {
        var nextIsHot = false;

        foreach (char x in _title)
        {
            if (x == MenuBar.HotKeySpecifier.Value)
            {
                nextIsHot = true;
            }
            else
            {
                if (nextIsHot)
                {
                    HotKey = (Rune)char.ToUpper (x);

                    break;
                }

                nextIsHot = false;
                HotKey = default (Rune);
            }
        }
    }

    // TODO: Update to use Key instead of KeyCode
    /// <summary>
    ///     Shortcut defines a key binding to the MenuItem that will invoke the MenuItem's action globally for the
    ///     <see cref="View"/> that is the parent of the <see cref="MenuBar"/> or <see cref="ContextMenu"/> this
    ///     <see cref="MenuItem"/>.
    ///     <para>
    ///         The <see cref="KeyCode"/> will be drawn on the MenuItem to the right of the <see cref="Title"/> and
    ///         <see cref="Help"/> text. See <see cref="ShortcutTag"/>.
    ///     </para>
    /// </summary>
    public KeyCode Shortcut
    {
        get => _shortcutHelper.Shortcut;
        set
        {
            if (_shortcutHelper.Shortcut != value && (ShortcutHelper.PostShortcutValidation (value) || value == KeyCode.Null))
            {
                _shortcutHelper.Shortcut = value;
            }
        }
    }

    /// <summary>Gets the text describing the keystroke combination defined by <see cref="Shortcut"/>.</summary>
    public string ShortcutTag => _shortcutHelper.Shortcut == KeyCode.Null
                                     ? string.Empty
                                     : Key.ToString (_shortcutHelper.Shortcut, MenuBar.ShortcutDelimiter);

    #endregion Keyboard Handling
}

/// <summary>
///     An internal class used to represent a menu pop-up menu. Created and managed by <see cref="MenuBar"/> and
///     <see cref="ContextMenu"/>.
/// </summary>
internal sealed class Menu : View
{
    private readonly MenuBarItem _barItems;
    private readonly MenuBar _host;
    internal int _currentChild;
    internal View _previousSubFocused;

    internal static Rectangle MakeFrame (int x, int y, MenuItem [] items, Menu parent = null)
    {
        if (items is null || items.Length == 0)
        {
            return Rectangle.Empty;
        }

        int minX = x;
        int minY = y;
        const int borderOffset = 2; // This 2 is for the space around
        int maxW = (items.Max (z => z?.Width) ?? 0) + borderOffset;
        int maxH = items.Length + borderOffset;

        if (parent is { } && x + maxW > Driver.Cols)
        {
            minX = Math.Max (parent.Frame.Right - parent.Frame.Width - maxW, 0);
        }

        if (y + maxH > Driver.Rows)
        {
            minY = Math.Max (Driver.Rows - maxH, 0);
        }

        return new (minX, minY, maxW, maxH);
    }

    internal required MenuBar Host
    {
        get => _host;
        init
        {
            ArgumentNullException.ThrowIfNull (value);
            _host = value;
        }
    }

    internal required MenuBarItem BarItems
    {
        get => _barItems;
        init
        {
            ArgumentNullException.ThrowIfNull (value);
            _barItems = value;

            // Debugging aid so ToString() is helpful
            Text = _barItems.Title;
        }
    }

    internal Menu Parent { get; init; }

    public override void BeginInit ()
    {
        base.BeginInit ();

        Frame = MakeFrame (Frame.X, Frame.Y, _barItems?.Children, Parent);

        if (_barItems is { IsTopLevel: true })
        {
            // This is a standalone MenuItem on a MenuBar
            ColorScheme = _host.ColorScheme;
            CanFocus = true;
        }
        else
        {
            _currentChild = -1;

            for (var i = 0; i < _barItems!.Children?.Length; i++)
            {
                if (_barItems.Children [i]?.IsEnabled () == true)
                {
                    _currentChild = i;

                    break;
                }
            }

            ColorScheme = _host.ColorScheme;
            CanFocus = true;
            WantMousePositionReports = _host.WantMousePositionReports;
        }

        BorderStyle = _host.MenusBorderStyle;

        AddCommand (
                    Command.Right,
                    () =>
                    {
                        _host.NextMenu (
                                        !_barItems.IsTopLevel
                                        || (_barItems.Children != null
                                            && _barItems!.Children.Length > 0
                                            && _currentChild > -1
                                            && _currentChild < _barItems.Children.Length
                                            && _barItems.Children [_currentChild].IsFromSubMenu),
                                        _barItems!.Children != null
                                        && _barItems.Children.Length > 0
                                        && _currentChild > -1
                                        && _host.UseSubMenusSingleFrame
                                        && _barItems.SubMenu (
                                                              _barItems.Children [_currentChild]
                                                             )
                                        != null
                                       );

                        return true;
                    }
                   );

        AddKeyBindings (_barItems);
#if SUPPORT_ALT_TO_ACTIVATE_MENU
        Initialized += (s, e) =>
                       {
                           if (SuperView is { })
                           {
                               SuperView.KeyUp += SuperView_KeyUp;
                           }
                       };
#endif
    }

    public Menu ()
    {
        if (Application.Current is { })
        {
            Application.Current.DrawContentComplete += Current_DrawContentComplete;
            Application.Current.SizeChanging += Current_TerminalResized;
        }

        Application.MouseEvent += Application_RootMouseEvent;

        // Things this view knows how to do
        AddCommand (Command.LineUp, () => MoveUp ());
        AddCommand (Command.LineDown, () => MoveDown ());

        AddCommand (
                    Command.Left,
                    () =>
                    {
                        _host.PreviousMenu (true);

                        return true;
                    }
                   );

        AddCommand (
                    Command.Cancel,
                    () =>
                    {
                        CloseAllMenus ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Accept,
                    () =>
                    {
                        RunSelected ();

                        return true;
                    }
                   );
        AddCommand (Command.Select, () => _host?.SelectItem (_menuItemToSelect));
        AddCommand (Command.ToggleExpandCollapse, () => SelectOrRun ());
        AddCommand (Command.HotKey, () => _host?.SelectItem (_menuItemToSelect));

        // Default key bindings for this view
        KeyBindings.Add (Key.CursorUp, Command.LineUp);
        KeyBindings.Add (Key.CursorDown, Command.LineDown);
        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.Esc, Command.Cancel);
        KeyBindings.Add (Key.Enter, Command.Accept);
        KeyBindings.Add (Key.F9, KeyBindingScope.HotKey, Command.ToggleExpandCollapse);

        KeyBindings.Add (
                         KeyCode.CtrlMask | KeyCode.Space,
                         KeyBindingScope.HotKey,
                         Command.ToggleExpandCollapse
                        );
    }

#if SUPPORT_ALT_TO_ACTIVATE_MENU
    void SuperView_KeyUp (object sender, KeyEventArgs e)
    {
        if (SuperView is null || SuperView.CanFocus == false || SuperView.Visible == false)
        {
            return;
        }

        _host.AltKeyUpHandler (e);
    }
#endif

    private void AddKeyBindings (MenuBarItem menuBarItem)
    {
        if (menuBarItem is null || menuBarItem.Children is null)
        {
            return;
        }

        foreach (MenuItem menuItem in menuBarItem.Children.Where (m => m is { }))
        {
            KeyBindings.Add ((KeyCode)menuItem.HotKey.Value, Command.ToggleExpandCollapse);

            KeyBindings.Add (
                             (KeyCode)menuItem.HotKey.Value | KeyCode.AltMask,
                             Command.ToggleExpandCollapse
                            );

            if (menuItem.Shortcut != KeyCode.Null)
            {
                KeyBindings.Add (menuItem.Shortcut, KeyBindingScope.HotKey, Command.Select);
            }

            MenuBarItem subMenu = menuBarItem.SubMenu (menuItem);
            AddKeyBindings (subMenu);
        }
    }

    private int _menuBarItemToActivate = -1;
    private MenuItem _menuItemToSelect;

    /// <summary>Called when a key bound to Command.Select is pressed. This means a hot key was pressed.</summary>
    /// <returns></returns>
    private bool SelectOrRun ()
    {
        if (!IsInitialized || !Visible)
        {
            return true;
        }

        if (_menuBarItemToActivate != -1)
        {
            _host.Activate (1, _menuBarItemToActivate);
        }
        else if (_menuItemToSelect is { })
        {
            var m = _menuItemToSelect as MenuBarItem;

            if (m?.Children?.Length > 0)
            {
                MenuItem item = _barItems.Children [_currentChild];

                if (item is null)
                {
                    return true;
                }

                bool disabled = item is null || !item.IsEnabled ();

                if (!disabled && (_host.UseSubMenusSingleFrame || !CheckSubMenu ()))
                {
                    SetNeedsDisplay ();
                    SetParentSetNeedsDisplay ();

                    return true;
                }

                if (!disabled)
                {
                    _host.OnMenuOpened ();
                }
            }
            else
            {
                _host.SelectItem (_menuItemToSelect);
            }
        }
        else if (_host.IsMenuOpen)
        {
            _host.CloseAllMenus ();
        }
        else
        {
            _host.OpenMenu ();
        }

        //_openedByHotKey = true;
        return true;
    }

    /// <inheritdoc/>
    public override bool? OnInvokingKeyBindings (Key keyEvent)
    {
        // This is a bit of a hack. We want to handle the key bindings for menu bar but
        // InvokeKeyBindings doesn't pass any context so we can't tell which item it is for.
        // So before we call the base class we set SelectedItem appropriately.

        KeyCode key = keyEvent.KeyCode;

        if (KeyBindings.TryGet (key, out _))
        {
            _menuBarItemToActivate = -1;
            _menuItemToSelect = null;

            MenuItem [] children = _barItems.Children;

            if (children is null)
            {
                return base.OnInvokingKeyBindings (keyEvent);
            }

            // Search for shortcuts first. If there's a shortcut, we don't want to activate the menu item.
            foreach (MenuItem c in children)
            {
                if (key == c?.Shortcut)
                {
                    _menuBarItemToActivate = -1;
                    _menuItemToSelect = c;
                    //keyEvent.Scope = KeyBindingScope.HotKey;

                    return base.OnInvokingKeyBindings (keyEvent);
                }

                MenuBarItem subMenu = _barItems.SubMenu (c);

                if (FindShortcutInChildMenu (key, subMenu))
                {
                    //keyEvent.Scope = KeyBindingScope.HotKey;

                    return base.OnInvokingKeyBindings (keyEvent);
                }
            }

            // Search for hot keys next.
            for (var c = 0; c < children.Length; c++)
            {
                int hotKeyValue = children [c]?.HotKey.Value ?? default (int);
                var hotKey = (KeyCode)hotKeyValue;

                if (hotKey == KeyCode.Null)
                {
                    continue;
                }

                bool matches = key == hotKey || key == (hotKey | KeyCode.AltMask);

                if (!_host.IsMenuOpen)
                {
                    // If the menu is open, only match if Alt is not pressed.
                    matches = key == hotKey;
                }

                if (matches)
                {
                    _menuItemToSelect = children [c];
                    _currentChild = c;

                    return base.OnInvokingKeyBindings (keyEvent);
                }
            }
        }

        bool? handled = base.OnInvokingKeyBindings (keyEvent);

        if (handled is { } && (bool)handled)
        {
            return true;
        }

        // This supports the case where the menu bar is a context menu
        return _host.OnInvokingKeyBindings (keyEvent);
    }

    private bool FindShortcutInChildMenu (KeyCode key, MenuBarItem menuBarItem)
    {
        if (menuBarItem?.Children is null)
        {
            return false;
        }

        foreach (MenuItem menuItem in menuBarItem.Children)
        {
            if (key == menuItem?.Shortcut)
            {
                _menuBarItemToActivate = -1;
                _menuItemToSelect = menuItem;

                return true;
            }

            MenuBarItem subMenu = menuBarItem.SubMenu (menuItem);
            FindShortcutInChildMenu (key, subMenu);
        }

        return false;
    }

    private void Current_TerminalResized (object sender, SizeChangedEventArgs e)
    {
        if (_host.IsMenuOpen)
        {
            _host.CloseAllMenus ();
        }
    }

    /// <inheritdoc/>
    public override void OnVisibleChanged ()
    {
        base.OnVisibleChanged ();

        if (Visible)
        {
            Application.MouseEvent += Application_RootMouseEvent;
        }
        else
        {
            Application.MouseEvent -= Application_RootMouseEvent;
        }
    }

    private void Application_RootMouseEvent (object sender, MouseEvent a)
    {
        if (a.View is { } and (MenuBar or not Menu))
        {
            return;
        }

        if (!Visible)
        {
            throw new InvalidOperationException ("This shouldn't running on a invisible menu!");
        }

        View view = a.View ?? this;

        Point boundsPoint = view.ScreenToViewport (a.X, a.Y);
        var me = new MouseEvent
        {
            X = boundsPoint.X,
            Y = boundsPoint.Y,
            Flags = a.Flags,
            ScreenPosition = new (a.X, a.Y),
            View = view
        };

        if (view.NewMouseEvent (me) == true || a.Flags == MouseFlags.Button1Pressed || a.Flags == MouseFlags.Button1Released)
        {
            a.Handled = true;
        }
    }

    internal Attribute DetermineColorSchemeFor (MenuItem item, int index)
    {
        if (item is null)
        {
            return GetNormalColor ();
        }

        if (index == _currentChild)
        {
            return ColorScheme.Focus;
        }

        return !item.IsEnabled () ? ColorScheme.Disabled : GetNormalColor ();
    }

    public override void OnDrawContent (Rectangle viewport)
    {
        if (_barItems.Children is null)
        {
            return;
        }

        Rectangle savedClip = Driver.Clip;
        Driver.Clip = new (0, 0, Driver.Cols, Driver.Rows);
        Driver.SetAttribute (GetNormalColor ());

        OnDrawAdornments ();
        OnRenderLineCanvas ();

        for (int i = Viewport.Y; i < _barItems.Children.Length; i++)
        {
            if (i < 0)
            {
                continue;
            }

            if (ViewportToScreen (Viewport).Y + i >= Driver.Rows)
            {
                break;
            }

            MenuItem item = _barItems.Children [i];

            Driver.SetAttribute (
                                 item is null ? GetNormalColor () :
                                 i == _currentChild ? ColorScheme.Focus : GetNormalColor ()
                                );

            if (item is null && BorderStyle != LineStyle.None)
            {
                var s = ViewportToScreen (new (-1, i, 0, 0));
                Driver.Move (s.X, s.Y);
                Driver.AddRune (Glyphs.LeftTee);
            }
            else if (Frame.X < Driver.Cols)
            {
                Move (0, i);
            }

            Driver.SetAttribute (DetermineColorSchemeFor (item, i));

            for (int p = Viewport.X; p < Frame.Width - 2; p++)
            {
                // This - 2 is for the border
                if (p < 0)
                {
                    continue;
                }

                if (ViewportToScreen (Viewport).X + p >= Driver.Cols)
                {
                    break;
                }

                if (item is null)
                {
                    Driver.AddRune (Glyphs.HLine);
                }
                else if (i == 0 && p == 0 && _host.UseSubMenusSingleFrame && item.Parent.Parent is { })
                {
                    Driver.AddRune (Glyphs.LeftArrow);
                }

                // This `- 3` is left border + right border + one row in from right
                else if (p == Frame.Width - 3 && _barItems.SubMenu (_barItems.Children [i]) is { })
                {
                    Driver.AddRune (Glyphs.RightArrow);
                }
                else
                {
                    Driver.AddRune ((Rune)' ');
                }
            }

            if (item is null)
            {
                if (BorderStyle != LineStyle.None && SuperView?.Frame.Right - Frame.X > Frame.Width)
                {
                    var s = ViewportToScreen (new (Frame.Width - 2, i, 0, 0));
                    Driver.Move (s.X, s.Y);
                    Driver.AddRune (Glyphs.RightTee);
                }

                continue;
            }

            string textToDraw = null;
            Rune nullCheckedChar = Glyphs.NullChecked;
            Rune checkChar = Glyphs.Selected;
            Rune uncheckedChar = Glyphs.UnSelected;

            if (item.CheckType.HasFlag (MenuItemCheckStyle.Checked))
            {
                checkChar = Glyphs.Checked;
                uncheckedChar = Glyphs.UnChecked;
            }

            // Support Checked even though CheckType wasn't set
            if (item.CheckType == MenuItemCheckStyle.Checked && item.Checked is null)
            {
                textToDraw = $"{nullCheckedChar} {item.Title}";
            }
            else if (item.Checked == true)
            {
                textToDraw = $"{checkChar} {item.Title}";
            }
            else if (item.CheckType.HasFlag (MenuItemCheckStyle.Checked) || item.CheckType.HasFlag (MenuItemCheckStyle.Radio))
            {
                textToDraw = $"{uncheckedChar} {item.Title}";
            }
            else
            {
                textToDraw = item.Title;
            }

            Rectangle screen = ViewportToScreen (new (new (0 , i), Size.Empty));
            if (screen.X < Driver.Cols)
            {
                Driver.Move (screen.X + 1, screen.Y);

                if (!item.IsEnabled ())
                {
                    DrawHotString (textToDraw, ColorScheme.Disabled, ColorScheme.Disabled);
                }
                else if (i == 0 && _host.UseSubMenusSingleFrame && item.Parent.Parent is { })
                {
                    var tf = new TextFormatter
                    {
                        Alignment = TextAlignment.Centered, HotKeySpecifier = MenuBar.HotKeySpecifier, Text = textToDraw
                    };

                    // The -3 is left/right border + one space (not sure what for)
                    tf.Draw (
                             ViewportToScreen (new (1, i, Frame.Width - 3, 1)),
                             i == _currentChild ? ColorScheme.Focus : GetNormalColor (),
                             i == _currentChild ? ColorScheme.HotFocus : ColorScheme.HotNormal,
                             SuperView?.ViewportToScreen (SuperView.Viewport) ?? Rectangle.Empty
                            );
                }
                else
                {
                    DrawHotString (
                                   textToDraw,
                                   i == _currentChild ? ColorScheme.HotFocus : ColorScheme.HotNormal,
                                   i == _currentChild ? ColorScheme.Focus : GetNormalColor ()
                                  );
                }

                // The help string
                int l = item.ShortcutTag.GetColumns () == 0
                            ? item.Help.GetColumns ()
                            : item.Help.GetColumns () + item.ShortcutTag.GetColumns () + 2;
                int col = Frame.Width - l - 3;
                screen = ViewportToScreen (new (new (col, i), Size.Empty));

                if (screen.X < Driver.Cols)
                {
                    Driver.Move (screen.X, screen.Y);
                    Driver.AddStr (item.Help);

                    // The shortcut tag string
                    if (!string.IsNullOrEmpty (item.ShortcutTag))
                    {
                        Driver.Move (screen.X + l - item.ShortcutTag.GetColumns (), screen.Y);
                        Driver.AddStr (item.ShortcutTag);
                    }
                }
            }
        }

        Driver.Clip = savedClip;

        PositionCursor ();
    }

    private void Current_DrawContentComplete (object sender, DrawEventArgs e)
    {
        if (Visible)
        {
            OnDrawContent (Viewport);
        }
    }

    public override Point? PositionCursor ()
    {
        if (_host?.IsMenuOpen != false)
        {
            if (_barItems.IsTopLevel)
            {
                return _host?.PositionCursor ();
            }
            else
            {
                Move (2, 1 + _currentChild);
                return new (2, 1 + _currentChild);
            }
        }

        return _host?.PositionCursor ();
    }

    public void Run (Action action)
    {
        if (action is null || _host is null)
        {
            return;
        }

        Application.UngrabMouse ();
        _host.CloseAllMenus ();
        Application.Refresh ();

        _host.Run (action);
    }

    public override bool OnLeave (View view) { return _host.OnLeave (view); }

    private void RunSelected ()
    {
        if (_barItems.IsTopLevel)
        {
            Run (_barItems.Action);
        }
        else
        {
            switch (_currentChild)
            {
                case > -1 when _barItems.Children [_currentChild].Action != null:
                    Run (_barItems.Children [_currentChild].Action);

                    break;
                case 0 when _host.UseSubMenusSingleFrame && _barItems.Children [_currentChild].Parent.Parent != null:
                    _host.PreviousMenu (_barItems.Children [_currentChild].Parent.IsFromSubMenu, true);

                    break;
                case > -1 when _barItems.SubMenu (_barItems.Children [_currentChild]) != null:
                    CheckSubMenu ();

                    break;
            }
        }
    }

    private void CloseAllMenus ()
    {
        Application.UngrabMouse ();
        _host.CloseAllMenus ();
    }

    private bool MoveDown ()
    {
        if (_barItems.IsTopLevel)
        {
            return true;
        }

        bool disabled;

        do
        {
            _currentChild++;

            if (_currentChild >= _barItems.Children.Length)
            {
                _currentChild = 0;
            }

            if (this != _host.openCurrentMenu && _barItems.Children [_currentChild]?.IsFromSubMenu == true && _host._selectedSub > -1)
            {
                _host.PreviousMenu (true);
                _host.SelectEnabledItem (_barItems.Children, _currentChild, out _currentChild);
                _host.openCurrentMenu = this;
            }

            MenuItem item = _barItems.Children [_currentChild];

            if (item?.IsEnabled () != true)
            {
                disabled = true;
            }
            else
            {
                disabled = false;
            }

            if (!_host.UseSubMenusSingleFrame
                && _host.UseKeysUpDownAsKeysLeftRight
                && _barItems.SubMenu (_barItems.Children [_currentChild]) != null
                && !disabled
                && _host.IsMenuOpen)
            {
                if (!CheckSubMenu ())
                {
                    return false;
                }

                break;
            }

            if (!_host.IsMenuOpen)
            {
                _host.OpenMenu (_host._selected);
            }
        }
        while (_barItems.Children [_currentChild] is null || disabled);

        SetNeedsDisplay ();
        SetParentSetNeedsDisplay ();

        if (!_host.UseSubMenusSingleFrame)
        {
            _host.OnMenuOpened ();
        }

        return true;
    }

    private bool MoveUp ()
    {
        if (_barItems.IsTopLevel || _currentChild == -1)
        {
            return true;
        }

        bool disabled;

        do
        {
            _currentChild--;

            if (_host.UseKeysUpDownAsKeysLeftRight && !_host.UseSubMenusSingleFrame)
            {
                if ((_currentChild == -1 || this != _host.openCurrentMenu)
                    && _barItems.Children [_currentChild + 1].IsFromSubMenu
                    && _host._selectedSub > -1)
                {
                    _currentChild++;
                    _host.PreviousMenu (true);

                    if (_currentChild > 0)
                    {
                        _currentChild--;
                        _host.openCurrentMenu = this;
                    }

                    break;
                }
            }

            if (_currentChild < 0)
            {
                _currentChild = _barItems.Children.Length - 1;
            }

            if (!_host.SelectEnabledItem (_barItems.Children, _currentChild, out _currentChild, false))
            {
                _currentChild = 0;

                if (!_host.SelectEnabledItem (_barItems.Children, _currentChild, out _currentChild) && !_host.CloseMenu (false))
                {
                    return false;
                }

                break;
            }

            MenuItem item = _barItems.Children [_currentChild];
            disabled = item?.IsEnabled () != true;

            if (_host.UseSubMenusSingleFrame
                || !_host.UseKeysUpDownAsKeysLeftRight
                || _barItems.SubMenu (_barItems.Children [_currentChild]) == null
                || disabled
                || !_host.IsMenuOpen)
            {
                continue;
            }

            if (!CheckSubMenu ())
            {
                return false;
            }

            break;
        }
        while (_barItems.Children [_currentChild] is null || disabled);

        SetNeedsDisplay ();
        SetParentSetNeedsDisplay ();

        if (!_host.UseSubMenusSingleFrame)
        {
            _host.OnMenuOpened ();
        }

        return true;
    }

    private void SetParentSetNeedsDisplay ()
    {
        if (_host._openSubMenu is { })
        {
            foreach (Menu menu in _host._openSubMenu)
            {
                menu.SetNeedsDisplay ();
            }
        }

        _host?._openMenu?.SetNeedsDisplay ();
        _host?.SetNeedsDisplay ();
    }

    protected internal override bool OnMouseEvent  (MouseEvent me)
    {
        if (!_host._handled && !_host.HandleGrabView (me, this))
        {
            return false;
        }

        _host._handled = false;
        bool disabled;

        if (me.Flags == MouseFlags.Button1Clicked)
        {
            disabled = false;

            if (me.Y < 0)
            {
                return me.Handled = true;
            }

            if (me.Y >= _barItems.Children.Length)
            {
                return me.Handled = true;
            }

            MenuItem item = _barItems.Children [me.Y];

            if (item is null || !item.IsEnabled ())
            {
                disabled = true;
            }

            if (disabled)
            {
                return me.Handled = true;
            }

            _currentChild = me.Y;
            RunSelected ();

            return me.Handled = true;
        }

        if (me.Flags != MouseFlags.Button1Pressed
            && me.Flags != MouseFlags.Button1DoubleClicked
            && me.Flags != MouseFlags.Button1TripleClicked
            && me.Flags != MouseFlags.ReportMousePosition
            && !me.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition))
        {
            return false;
        }

        {
            disabled = false;

            if (me.Y < 0 || me.Y >= _barItems.Children.Length)
            {
                return me.Handled = true;
            }

            MenuItem item = _barItems.Children [me.Y];

            if (item is null)
            {
                return me.Handled = true;
            }

            if (item?.IsEnabled () != true)
            {
                disabled = true;
            }

            if (!disabled)
            {
                _currentChild = me.Y;
            }

            if (_host.UseSubMenusSingleFrame || !CheckSubMenu ())
            {
                SetNeedsDisplay ();
                SetParentSetNeedsDisplay ();

                return me.Handled = true;
            }

            _host.OnMenuOpened ();

            return me.Handled = true;
        }
    }

    internal bool CheckSubMenu ()
    {
        if (_currentChild == -1 || _barItems.Children [_currentChild] is null)
        {
            return true;
        }

        MenuBarItem subMenu = _barItems.SubMenu (_barItems.Children [_currentChild]);

        if (subMenu is { })
        {
            int pos = -1;

            if (_host._openSubMenu is { })
            {
                pos = _host._openSubMenu.FindIndex (o => o?._barItems == subMenu);
            }

            if (pos == -1
                && this != _host.openCurrentMenu
                && subMenu.Children != _host.openCurrentMenu._barItems.Children
                && !_host.CloseMenu (false, true))
            {
                return false;
            }

            _host.Activate (_host._selected, pos, subMenu);
        }
        else if (_host._openSubMenu?.Count == 0 || _host._openSubMenu?.Last ()._barItems.IsSubMenuOf (_barItems.Children [_currentChild]) == false)
        {
            return _host.CloseMenu (false, true);
        }
        else
        {
            SetNeedsDisplay ();
            SetParentSetNeedsDisplay ();
        }

        return true;
    }

    private int GetSubMenuIndex (MenuBarItem subMenu)
    {
        int pos = -1;

        if (Subviews.Count == 0)
        {
            return pos;
        }

        Menu v = null;

        foreach (View menu in Subviews)
        {
            if (((Menu)menu)._barItems == subMenu)
            {
                v = (Menu)menu;
            }
        }

        if (v is { })
        {
            pos = Subviews.IndexOf (v);
        }

        return pos;
    }

    /// <inheritdoc/>
    public override bool OnEnter (View view)
    {
        Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

        return base.OnEnter (view);
    }

    protected override void Dispose (bool disposing)
    {
        if (Application.Current is { })
        {
            Application.Current.DrawContentComplete -= Current_DrawContentComplete;
            Application.Current.SizeChanging -= Current_TerminalResized;
        }

        Application.MouseEvent -= Application_RootMouseEvent;
        base.Dispose (disposing);
    }
}
