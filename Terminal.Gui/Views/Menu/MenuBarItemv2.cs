#nullable enable


namespace Terminal.Gui.Views;

/// <summary>
///     A <see cref="Shortcut"/>-derived object to be used as items in a <see cref="MenuBarv2"/>.
///     MenuBarItems hold a <see cref="PopoverMenu"/> instead of a <see cref="SubMenu"/>.
/// </summary>
public class MenuBarItemv2 : MenuItemv2
{
    /// <summary>
    ///     Creates a new instance of <see cref="MenuBarItemv2"/>.
    /// </summary>
    public MenuBarItemv2 () : base (null, Command.NotBound) { }

    /// <summary>
    ///     Creates a new instance of <see cref="MenuBarItemv2"/>. Each MenuBarItem typically has a <see cref="PopoverMenu"/>
    ///     that is
    ///     shown when the item is selected.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="targetView">
    ///     The View that <paramref name="command"/> will be invoked on when user does something that causes the MenuBarItems's
    ///     Accept event to be raised.
    /// </param>
    /// <param name="command">
    ///     The Command to invoke on <paramref name="targetView"/>. The Key <paramref name="targetView"/>
    ///     has bound to <paramref name="command"/> will be used as <see cref="Key"/>
    /// </param>
    /// <param name="commandText">The text to display for the command.</param>
    /// <param name="popoverMenu">The Popover Menu that will be displayed when this item is selected.</param>
    public MenuBarItemv2 (View? targetView, Command command, string? commandText, PopoverMenu? popoverMenu = null)
        : base (
                targetView,
                command,
                commandText)
    {
        TargetView = targetView;
        Command = command;
        PopoverMenu = popoverMenu;
    }

    /// <summary>
    ///     Creates a new instance of <see cref="MenuBarItemv2"/> with the specified <paramref name="popoverMenu"/>. This is a
    ///     helper for the most common MenuBar use-cases.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="commandText">The text to display for the command.</param>
    /// <param name="popoverMenu">The Popover Menu that will be displayed when this item is selected.</param>
    public MenuBarItemv2 (string commandText, PopoverMenu? popoverMenu = null)
        : this (
                null,
                Command.NotBound,
                commandText,
                popoverMenu)
    { }

    /// <summary>
    ///     Creates a new instance of <see cref="MenuBarItemv2"/> with the <paramref name="menuItems"/> automatcialy added to a
    ///     <see cref="PopoverMenu"/>.
    ///     This is a helper for the most common MenuBar use-cases.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="commandText">The text to display for the command.</param>
    /// <param name="menuItems">
    ///     The menu items that will be added to the Popover Menu that will be displayed when this item is
    ///     selected.
    /// </param>
    public MenuBarItemv2 (string commandText, IEnumerable<View> menuItems)
        : this (
                null,
                Command.NotBound,
                commandText,
                new (menuItems) { Title = $"PopoverMenu for {commandText}" })
    { }

    /// <summary>
    ///     Do not use this property. MenuBarItem does not support SubMenu. Use <see cref="PopoverMenu"/> instead.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public new Menuv2? SubMenu
    {
        get => null;
        set => throw new InvalidOperationException ("MenuBarItem does not support SubMenu. Use PopoverMenu instead.");
    }

    private PopoverMenu? _popoverMenu;

    /// <summary>
    ///     The Popover Menu that will be displayed when this item is selected.
    /// </summary>
    public PopoverMenu? PopoverMenu
    {
        get => _popoverMenu;
        set
        {
            if (_popoverMenu == value)
            {
                return;
            }

            if (_popoverMenu is { })
            {
                _popoverMenu.VisibleChanged -= OnPopoverVisibleChanged;
                _popoverMenu.Accepted -= OnPopoverMenuOnAccepted;
            }

            _popoverMenu = value;

            if (_popoverMenu is { })
            {
                PopoverMenuOpen = _popoverMenu.Visible;
                _popoverMenu.VisibleChanged += OnPopoverVisibleChanged;
                _popoverMenu.Accepted += OnPopoverMenuOnAccepted;
            }

            return;

            void OnPopoverVisibleChanged (object? sender, EventArgs args)
            {
                // Logging.Debug ($"OnPopoverVisibleChanged - {Title} - Visible = {_popoverMenu?.Visible} ");
                PopoverMenuOpen = _popoverMenu?.Visible ?? false;
            }

            void OnPopoverMenuOnAccepted (object? sender, CommandEventArgs args)
            {
                // Logging.Debug ($"OnPopoverMenuOnAccepted - {Title} - {args.Context?.Source?.Title} - {args.Context?.Command}");
                RaiseAccepted (args.Context);
            }
        }
    }

    private bool _popoverMenuOpen;

    /// <summary>
    ///     Gets or sets whether the MenuBarItem is active. This is used to determine if the MenuBarItem should be
    /// </summary>
    public bool PopoverMenuOpen
    {
        get => _popoverMenuOpen;
        set
        {
            if (_popoverMenuOpen == value)
            {
                return;
            }
            _popoverMenuOpen = value;

            RaisePopoverMenuOpenChanged();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void RaisePopoverMenuOpenChanged ()
    {
        OnPopoverMenuOpenChanged();
        PopoverMenuOpenChanged?.Invoke (this, new EventArgs<bool> (PopoverMenuOpen));
    }

    /// <summary>
    /// 
    /// </summary>
    protected virtual void OnPopoverMenuOpenChanged () {}

    /// <summary>
    /// 
    /// </summary>
    public event EventHandler<EventArgs<bool>>? PopoverMenuOpenChanged;

    /// <inheritdoc />
    protected override bool OnKeyDownNotHandled (Key key)
    {
        Logging.Trace ($"{key}");

        if (PopoverMenu is { Visible: true } && HotKeyBindings.TryGet (key, out _))
        {
            // If the user presses the hotkey for a menu item that is already open,
            // it should close the menu item (Test: MenuBarItem_HotKey_DeActivates)
            if (SuperView is MenuBarv2 { } menuBar)
            {
                menuBar.HideActiveItem ();
            }


            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    protected override void OnHasFocusChanged (bool newHasFocus, View? previousFocusedView, View? focusedView)
    {
        // Logging.Debug ($"CanFocus = {CanFocus}, HasFocus = {HasFocus}");
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            PopoverMenu?.Dispose ();
            PopoverMenu = null;
        }

        base.Dispose (disposing);
    }
}
