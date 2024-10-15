#nullable enable

using static System.Formats.Asn1.AsnWriter;

namespace Terminal.Gui;

/// <summary>
///     An internal class used to represent a menu pop-up menu. Created and managed by <see cref="MenuBar"/> and
///     <see cref="ContextMenu"/>.
/// </summary>
internal sealed class Menu : View
{
    private readonly MenuBarItem? _barItems;
    private readonly MenuBar _host;
    internal int _currentChild;
    internal View? _previousSubFocused;

    internal static Rectangle MakeFrame (int x, int y, MenuItem? []? items, Menu? parent = null)
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

    internal required MenuBarItem? BarItems
    {
        get => _barItems!;
        init
        {
            ArgumentNullException.ThrowIfNull (value);
            _barItems = value;

            // Debugging aid so ToString() is helpful
            Text = _barItems.Title;
        }
    }

    internal Menu? Parent { get; init; }

    public override void BeginInit ()
    {
        base.BeginInit ();

        Frame = MakeFrame (Frame.X, Frame.Y, _barItems!.Children!, Parent);

        if (_barItems.Children is { })
        {
            foreach (MenuItem? menuItem in _barItems.Children)
            {
                if (menuItem is { })
                {
                    menuItem._menuBar = Host;

                    if (menuItem.ShortcutKey != Key.Empty)
                    {
                        KeyBinding keyBinding = new ([Command.Select], KeyBindingScope.HotKey, menuItem);
                        // Remove an existent ShortcutKey
                        menuItem._menuBar.KeyBindings.Remove (menuItem.ShortcutKey!);
                        menuItem._menuBar.KeyBindings.Add (menuItem.ShortcutKey!, keyBinding);
                    }
                }
            }
        }

        if (_barItems is { IsTopLevel: true })
        {
            // This is a standalone MenuItem on a MenuBar
            ColorScheme = _host.ColorScheme;
            CanFocus = true;
        }
        else
        {
            _currentChild = -1;

            for (var i = 0; i < _barItems.Children?.Length; i++)
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
                                        || (_barItems.Children is { Length: > 0 }
                                            && _currentChild > -1
                                            && _currentChild < _barItems.Children.Length
                                            && _barItems.Children [_currentChild]!.IsFromSubMenu),
                                        _barItems.Children is { Length: > 0 }
                                        && _currentChild > -1
                                        && _host.UseSubMenusSingleFrame
                                        && _barItems.SubMenu (
                                                              _barItems.Children [_currentChild]!
                                                             )
                                        != null!
                                       );

                        return true;
                    }
                   );

        AddKeyBindingsHotKey (_barItems);
    }

    public Menu ()
    {
        if (Application.Top is { })
        {
            Application.Top.DrawContentComplete += Current_DrawContentComplete;
            Application.Top.SizeChanging += Current_TerminalResized;
        }

        Application.MouseEvent += Application_RootMouseEvent;

        // Things this view knows how to do
        AddCommand (Command.Up, () => MoveUp ());
        AddCommand (Command.Down, () => MoveDown ());

        AddCommand (
                    Command.Left,
                    () =>
                    {
                        _host!.PreviousMenu (true);

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
        AddCommand (Command.Select, ctx => _host?.SelectItem ((ctx.KeyBinding?.Context as MenuItem)!));
        AddCommand (Command.Toggle, ctx => ExpandCollapse ((ctx.KeyBinding?.Context as MenuItem)!));
        AddCommand (Command.HotKey, ctx => _host?.SelectItem ((ctx.KeyBinding?.Context as MenuItem)!));

        // Default key bindings for this view
        KeyBindings.Add (Key.CursorUp, Command.Up);
        KeyBindings.Add (Key.CursorDown, Command.Down);
        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.Esc, Command.Cancel);
    }

    private void AddKeyBindingsHotKey (MenuBarItem? menuBarItem)
    {
        if (menuBarItem is null || menuBarItem.Children is null)
        {
            return;
        }

        IEnumerable<MenuItem> menuItems = menuBarItem.Children.Where (m => m is { })!;

        foreach (MenuItem menuItem in menuItems)
        {
            KeyBinding keyBinding = new ([Command.Toggle], KeyBindingScope.HotKey, menuItem);

            if (menuItem.HotKey != Key.Empty)
            {
                KeyBindings.Remove (menuItem.HotKey!);
                KeyBindings.Add (menuItem.HotKey!, keyBinding);
                KeyBindings.Remove (menuItem.HotKey!.WithAlt);
                KeyBindings.Add (menuItem.HotKey.WithAlt, keyBinding);
            }
        }
    }

    private void RemoveKeyBindingsHotKey (MenuBarItem? menuBarItem)
    {
        if (menuBarItem is null || menuBarItem.Children is null)
        {
            return;
        }

        IEnumerable<MenuItem> menuItems = menuBarItem.Children.Where (m => m is { })!;

        foreach (MenuItem menuItem in menuItems)
        {
            if (menuItem.HotKey != Key.Empty)
            {
                KeyBindings.Remove (menuItem.HotKey!);
                KeyBindings.Remove (menuItem.HotKey!.WithAlt);
            }
        }
    }

    /// <summary>Called when a key bound to Command.ToggleExpandCollapse is pressed. This means a hot key was pressed.</summary>
    /// <returns></returns>
    private bool ExpandCollapse (MenuItem? menuItem)
    {
        if (!IsInitialized || !Visible)
        {
            return true;
        }


        for (var c = 0; c < _barItems!.Children!.Length; c++)
        {
            if (_barItems.Children [c] == menuItem)
            {
                _currentChild = c;

                break;
            }
        }

        if (menuItem is { })
        {
            var m = menuItem as MenuBarItem;

            if (m?.Children?.Length > 0)
            {
                MenuItem? item = _barItems.Children [_currentChild];

                if (item is null)
                {
                    return true;
                }

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
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
                _host.SelectItem (menuItem);
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

        return true;
    }

    /// <inheritdoc />
    protected override bool OnKeyDownNotHandled (Key keyEvent)
    {
        // We didn't handle the key, pass it on to host
        return _host.InvokeCommandsBoundToKey (keyEvent) == true;
    }

    private void Current_TerminalResized (object? sender, SizeChangedEventArgs e)
    {
        if (_host.IsMenuOpen)
        {
            _host.CloseAllMenus ();
        }
    }

    /// <inheritdoc/>
    protected override void OnVisibleChanged ()
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

    private void Application_RootMouseEvent (object? sender, MouseEventArgs a)
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

        Point boundsPoint = view.ScreenToViewport (new (a.Position.X, a.Position.Y));

        var me = new MouseEventArgs
        {
            Position = boundsPoint,
            Flags = a.Flags,
            ScreenPosition = a.Position,
            View = view
        };

        if (view.NewMouseEvent (me) == true || a.Flags == MouseFlags.Button1Pressed || a.Flags == MouseFlags.Button1Released)
        {
            a.Handled = true;
        }
    }

    internal Attribute DetermineColorSchemeFor (MenuItem? item, int index)
    {
        if (item is null)
        {
            return GetNormalColor ();
        }

        if (index == _currentChild)
        {
            return GetFocusColor ();
        }

        return !item.IsEnabled () ? ColorScheme!.Disabled : GetNormalColor ();
    }

    public override void OnDrawContent (Rectangle viewport)
    {
        if (_barItems!.Children is null)
        {
            return;
        }

        Rectangle savedClip = Driver.Clip;
        Driver.Clip = new (0, 0, Driver.Cols, Driver.Rows);
        Driver.SetAttribute (GetNormalColor ());

        OnDrawAdornments ();
        OnRenderLineCanvas ();

        for (int i = Viewport.Y; i < _barItems!.Children.Length; i++)
        {
            if (i < 0)
            {
                continue;
            }

            if (ViewportToScreen (Viewport).Y + i >= Driver.Rows)
            {
                break;
            }

            MenuItem? item = _barItems.Children [i];

            Driver.SetAttribute (
                                 // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                                 item is null ? GetNormalColor () :
                                 i == _currentChild ? GetFocusColor () : GetNormalColor ()
                                );

            if (item is null && BorderStyle != LineStyle.None)
            {
                Point s = ViewportToScreen (new Point (-1, i));
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
                else if (i == 0 && p == 0 && _host.UseSubMenusSingleFrame && item.Parent!.Parent is { })
                {
                    Driver.AddRune (Glyphs.LeftArrow);
                }

                // This `- 3` is left border + right border + one row in from right
                else if (p == Frame.Width - 3 && _barItems?.SubMenu (_barItems.Children [i]!) is { })
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
                    Point s = ViewportToScreen (new Point (Frame.Width - 2, i));
                    Driver.Move (s.X, s.Y);
                    Driver.AddRune (Glyphs.RightTee);
                }

                continue;
            }

            string? textToDraw;
            Rune nullCheckedChar = Glyphs.CheckStateNone;
            Rune checkChar = Glyphs.Selected;
            Rune uncheckedChar = Glyphs.UnSelected;

            if (item.CheckType.HasFlag (MenuItemCheckStyle.Checked))
            {
                checkChar = Glyphs.CheckStateChecked;
                uncheckedChar = Glyphs.CheckStateUnChecked;
            }

            // Support Checked even though CheckType wasn't set
            if (item is { CheckType: MenuItemCheckStyle.Checked, Checked: null })
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

            Point screen = ViewportToScreen (new Point (0, i));

            if (screen.X < Driver.Cols)
            {
                Driver.Move (screen.X + 1, screen.Y);

                if (!item.IsEnabled ())
                {
                    DrawHotString (textToDraw, ColorScheme!.Disabled, ColorScheme.Disabled);
                }
                else if (i == 0 && _host.UseSubMenusSingleFrame && item.Parent!.Parent is { })
                {
                    var tf = new TextFormatter
                    {
                        ConstrainToWidth = Frame.Width - 3,
                        ConstrainToHeight = 1,
                        Alignment = Alignment.Center, HotKeySpecifier = MenuBar.HotKeySpecifier, Text = textToDraw
                    };

                    // The -3 is left/right border + one space (not sure what for)
                    tf.Draw (
                             ViewportToScreen (new Rectangle (1, i, Frame.Width - 3, 1)),
                             i == _currentChild ? GetFocusColor () : GetNormalColor (),
                             i == _currentChild ? ColorScheme!.HotFocus : ColorScheme!.HotNormal,
                             SuperView?.ViewportToScreen (SuperView.Viewport) ?? Rectangle.Empty
                            );
                }
                else
                {
                    DrawHotString (
                                   textToDraw,
                                   i == _currentChild ? ColorScheme!.HotFocus : ColorScheme!.HotNormal,
                                   i == _currentChild ? GetFocusColor () : GetNormalColor ()
                                  );
                }

                // The help string
                int l = item.ShortcutTag.GetColumns () == 0
                            ? item.Help.GetColumns ()
                            : item.Help.GetColumns () + item.ShortcutTag.GetColumns () + 2;
                int col = Frame.Width - l - 3;
                screen = ViewportToScreen (new Point (col, i));

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

        // PositionCursor ();
    }

    private void Current_DrawContentComplete (object? sender, DrawEventArgs e)
    {
        if (Visible)
        {
            OnDrawContent (Viewport);
        }
    }

    public override Point? PositionCursor ()
    {
        if (_host.IsMenuOpen)
        {
            if (_barItems!.IsTopLevel)
            {
                return _host.PositionCursor ();
            }

            Move (2, 1 + _currentChild);

            return null; // Don't show the cursor
        }

        return _host.PositionCursor ();
    }

    public void Run (Action? action)
    {
        if (action is null)
        {
            return;
        }

        Application.UngrabMouse ();
        _host.CloseAllMenus ();
        Application.Driver!.ClearContents ();
        Application.Refresh ();

        _host.Run (action);
    }

    protected override void OnHasFocusChanged (bool newHasFocus, View? previousFocusedView, View? view)
    {
        if (!newHasFocus)
        {
            _host.LostFocus (previousFocusedView!);
        }
    }

    private void RunSelected ()
    {
        if (_barItems!.IsTopLevel)
        {
            Run (_barItems.Action);
        }
        else
        {
            switch (_currentChild)
            {
                case > -1 when _barItems.Children! [_currentChild]!.Action != null!:
                    Run (_barItems.Children [_currentChild]?.Action);

                    break;
                case 0 when _host.UseSubMenusSingleFrame && _barItems.Children [_currentChild]?.Parent!.Parent != null:
                    _host.PreviousMenu (_barItems.Children [_currentChild]!.Parent!.IsFromSubMenu, true);

                    break;
                case > -1 when _barItems.SubMenu (_barItems.Children [_currentChild]) != null!:
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
        if (_barItems!.IsTopLevel)
        {
            return true;
        }

        bool disabled;

        do
        {
            _currentChild++;

            if (_currentChild >= _barItems?.Children?.Length)
            {
                _currentChild = 0;
            }

            if (this != _host.OpenCurrentMenu && _barItems?.Children? [_currentChild]?.IsFromSubMenu == true && _host._selectedSub > -1)
            {
                _host.PreviousMenu (true);
                _host.SelectEnabledItem (_barItems.Children!, _currentChild, out _currentChild);
                _host.OpenCurrentMenu = this;
            }

            MenuItem? item = _barItems?.Children? [_currentChild];

            if (item?.IsEnabled () != true)
            {
                disabled = true;
            }
            else
            {
                disabled = false;
            }

            if (_host is { UseSubMenusSingleFrame: false, UseKeysUpDownAsKeysLeftRight: true }
                && _barItems?.SubMenu (_barItems?.Children? [_currentChild]!) != null
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
        while (_barItems?.Children? [_currentChild] is null || disabled);

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
        if (_barItems!.IsTopLevel || _currentChild == -1)
        {
            return true;
        }

        bool disabled;

        do
        {
            _currentChild--;

            if (_host.UseKeysUpDownAsKeysLeftRight && !_host.UseSubMenusSingleFrame)
            {
                if ((_currentChild == -1 || this != _host.OpenCurrentMenu)
                    && _barItems.Children! [_currentChild + 1]!.IsFromSubMenu
                    && _host._selectedSub > -1)
                {
                    _currentChild++;
                    _host.PreviousMenu (true);

                    if (_currentChild > 0)
                    {
                        _currentChild--;
                        _host.OpenCurrentMenu = this;
                    }

                    break;
                }
            }

            if (_currentChild < 0)
            {
                _currentChild = _barItems.Children!.Length - 1;
            }

            if (!_host.SelectEnabledItem (_barItems.Children!, _currentChild, out _currentChild, false))
            {
                _currentChild = 0;

                if (!_host.SelectEnabledItem (_barItems.Children!, _currentChild, out _currentChild) && !_host.CloseMenu ())
                {
                    return false;
                }

                break;
            }

            MenuItem item = _barItems.Children! [_currentChild]!;
            disabled = item.IsEnabled () != true;

            if (_host.UseSubMenusSingleFrame
                || !_host.UseKeysUpDownAsKeysLeftRight
                || _barItems.SubMenu (_barItems.Children [_currentChild]!) == null!
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

        _host._openMenu?.SetNeedsDisplay ();
        _host.SetNeedsDisplay ();
    }

    protected override bool OnMouseEvent (MouseEventArgs me)
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

            if (me.Position.Y < 0)
            {
                return me.Handled = true;
            }

            if (me.Position.Y >= _barItems!.Children!.Length)
            {
                return me.Handled = true;
            }

            MenuItem item = _barItems.Children [me.Position.Y]!;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (item is null || !item.IsEnabled ())
            {
                disabled = true;
            }

            if (disabled)
            {
                return me.Handled = true;
            }

            _currentChild = me.Position.Y;
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

            if (me.Position.Y < 0 || me.Position.Y >= _barItems!.Children!.Length)
            {
                return me.Handled = true;
            }

            MenuItem item = _barItems.Children [me.Position.Y]!;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (item is null)
            {
                return me.Handled = true;
            }

            if (item.IsEnabled () != true)
            {
                disabled = true;
            }

            if (!disabled)
            {
                _currentChild = me.Position.Y;
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
        if (_currentChild == -1 || _barItems?.Children? [_currentChild] is null)
        {
            return true;
        }

        MenuBarItem? subMenu = _barItems.SubMenu (_barItems.Children [_currentChild]!);

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (subMenu is { })
        {
            int pos = -1;

            if (_host._openSubMenu is { })
            {
                pos = _host._openSubMenu.FindIndex (o => o._barItems == subMenu);
            }

            if (pos == -1
                && this != _host.OpenCurrentMenu
                && subMenu.Children != _host.OpenCurrentMenu!._barItems!.Children
                && !_host.CloseMenu (false, true))
            {
                return false;
            }

            _host.Activate (_host._selected, pos, subMenu);
        }
        else if (_host._openSubMenu?.Count == 0 || _host._openSubMenu?.Last ()._barItems!.IsSubMenuOf (_barItems.Children [_currentChild]!) == false)
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

    protected override void Dispose (bool disposing)
    {
        RemoveKeyBindingsHotKey (_barItems);

        if (Application.Top is { })
        {
            Application.Top.DrawContentComplete -= Current_DrawContentComplete;
            Application.Top.SizeChanging -= Current_TerminalResized;
        }

        Application.MouseEvent -= Application_RootMouseEvent;
        base.Dispose (disposing);
    }
}
