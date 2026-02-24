using Terminal.Gui.Tracing;

namespace Terminal.Gui.Views;

/// <summary>
///     A <see cref="Bar"/>-derived object to be used as a vertically-oriented menu. Each subview is a
///     <see cref="MenuItem"/>.
/// </summary>
public class Menu : Bar
{
    /// <summary>
    ///     Gets or sets the default Border Style for Menus. The default is <see cref="LineStyle.None"/>.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static LineStyle DefaultBorderStyle { get; set; } = LineStyle.None;

    /// <inheritdoc/>
    public Menu () : this ([]) { }

    /// <inheritdoc/>
    public Menu (IEnumerable<MenuItem>? menuItems) : this (menuItems?.Cast<View> ()) { }

    /// <inheritdoc/>
    public Menu (IEnumerable<View>? shortcuts) : base (shortcuts)
    {
        // Do this to support debugging traces where Title gets set
        base.HotKeySpecifier = (Rune)'\xffff';

        Orientation = Orientation.Vertical;
        Width = Dim.Auto ();
        Height = Dim.Auto (DimAutoStyle.Content, 1);
        SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Menu);

        Border?.Settings &= ~BorderSettings.Title;

        BorderStyle = DefaultBorderStyle;

        CommandsToBubbleUp = [Command.Accept, Command.Activate];

        KeyBindings.Clear ();
        MouseBindings.Clear ();

        ConfigurationManager.Applied += OnConfigurationManagerApplied;
    }

    private void OnConfigurationManagerApplied (object? sender, ConfigurationManagerEventArgs e)
    {
        if (SuperView is { })
        {
            BorderStyle = DefaultBorderStyle;
        }
    }

    /// <summary>
    ///     Gets or sets the menu item that opened this menu as a sub-menu.
    /// </summary>
    public MenuItem? SuperMenuItem { get; set; }

    /// <inheritdoc/>
    protected override bool OnActivating (CommandEventArgs args)
    {
        Trace.Command (this, args.Context, "Entry", $"Routing={args.Context?.Routing} Cmd={args.Context?.Command}");

        if (base.OnActivating (args) || args.Handled)
        {
            return true;
        }

        // When a MenuItem's activation bubbles up, don't re-dispatch — let normal bubbling proceed.
        if (args.Context?.Routing == CommandRouting.BubblingUp)
        {
            return false;
        }

        // Dispatch Activate to the focused MenuItem. This enables callers to invoke
        // menu.InvokeCommand(Activate) and have it reach the selected MenuItem and its CommandView.
        if (Focused is not MenuItem menuItem)
        {
            return false;
        }
        KeyBinding binding = new ([Command.Activate]);
        WeakReference<View> source = new (this);
        CommandContext ctx = new (Command.Activate, source, binding);
        menuItem.InvokeCommand (Command.Activate, ctx);

        return true;
    }

    /// <inheritdoc/>
    protected override void OnSubViewAdded (View view)
    {
        base.OnSubViewAdded (view);

        switch (view)
        {
            case MenuItem menuItem:
            {
                menuItem.CanFocus = true;

                // Accept propagation is handled by CommandsToBubbleUp=[Accept] (line 36).
                // An explicit Accepting subscription here caused double-fire of Accepted.

                break;
            }

            case Line line:
                // Grow line so we get auto-join line
                line.X = Pos.Func (_ => -Border!.Thickness.Left);
                line.Width = Dim.Fill () + Dim.Func (_ => Border!.Thickness.Right);

                break;
        }
    }

    /// <inheritdoc/>
    protected override void OnFocusedChanged (View? previousFocused, View? focused)
    {
        base.OnFocusedChanged (previousFocused, focused);

        RaiseSelectedMenuItemChanged (SelectedMenuItem);
    }

    /// <summary>
    ///     Gets the currently selected menu item. This is a helper that
    ///     tracks <see cref="View.Focused"/>.
    /// </summary>
    public MenuItem? SelectedMenuItem => Focused as MenuItem;

    internal void RaiseSelectedMenuItemChanged (MenuItem? selected)
    {
        // Logging.Debug ($"{this.ToIdentifyingString ()} ({selected?.Title})");
        Trace.Command (this, "Handler", $"{selected?.ToIdentifyingString ()}");

        OnSelectedMenuItemChanged (selected);
        SelectedMenuItemChanged?.Invoke (this, selected);
    }

    /// <summary>
    ///     Called when the selected menu item has changed. Handles hiding peer SubMenus
    ///     and showing the selected item's SubMenu.
    /// </summary>
    /// <param name="selected">The newly selected <see cref="MenuItem"/>, or <see langword="null"/> if none.</param>
    protected virtual void OnSelectedMenuItemChanged (MenuItem? selected)
    {
        // Hide any visible peer SubMenus
        foreach (MenuItem mi in SubViews.OfType<MenuItem> ().Where (mi => mi != selected && mi.SubMenu is { Visible: true }))
        {
            mi.SubMenu!.HideMenu ();
        }

        if (selected?.SubMenu is not { Visible: false })
        {
            return;
        }

        // If SubMenu has no SuperView yet, add it to our SuperView
        if (selected.SubMenu.SuperView is null && SuperView is { })
        {
            SuperView.Add (selected.SubMenu);
        }

        selected.SubMenu.ShowMenu ();

        // Generic positioning: right of this Menu, at the MenuItem's Y
        if (selected.SubMenu.SuperView is null)
        {
            return;
        }
        Point screenPos = new (selected.FrameToScreen ().Right, selected.FrameToScreen ().Top);
        Point localPos = selected.SubMenu.SuperView.ScreenToViewport (screenPos);
        selected.SubMenu.X = localPos.X;
        selected.SubMenu.Y = localPos.Y;
    }

    /// <summary>
    ///     Raised when the selected menu item has changed.
    /// </summary>
    public event EventHandler<MenuItem?>? SelectedMenuItemChanged;

    /// <summary>
    ///     Gets all the submenus in this menu's hierarchy, including this menu.
    /// </summary>
    /// <returns>An enumerable collection of all <see cref="Menu"/> instances in the hierarchy.</returns>
    /// <remarks>
    ///     This method performs a depth-first traversal of the menu tree starting from <see langword="this"/>.
    /// </remarks>
    public IEnumerable<Menu> GetAllSubMenus ()
    {
        List<Menu> result = [];
        Stack<Menu> stack = new ();
        stack.Push (this);

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
    ///     Gets menu items in this menu's hierarchy, optionally filtered by <paramref name="predicate"/>.
    /// </summary>
    /// <param name="predicate">
    ///     If provided, only <see cref="MenuItem"/>s matching the predicate are returned.
    ///     If <see langword="null"/>, all menu items are returned.
    /// </param>
    /// <returns>The matching <see cref="MenuItem"/> instances across all menus in the hierarchy.</returns>
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

    #region ShowMenu / HideMenu

    /// <summary>
    ///     Shows this menu by setting <see cref="View.Visible"/> and <see cref="View.Enabled"/> to <see langword="true"/>.
    ///     If the menu has not been initialized, initialization is performed first.
    /// </summary>
    internal void ShowMenu ()
    {
        if (Visible)
        {
            return;
        }

        if (!IsInitialized)
        {
            BeginInit ();
            EndInit ();
        }

        ClearFocus ();

        // IMPORTANT: This must be done after adding the menu to the SuperView or Add will try
        // to set focus to it.
        Visible = true;
        Enabled = true;
    }

    private bool _isHiding;

    /// <summary>
    ///     Hides this menu and any visible SubMenus by setting <see cref="View.Visible"/> and
    ///     <see cref="View.Enabled"/> to <see langword="false"/>.
    /// </summary>
    internal void HideMenu ()
    {
        if (!Visible || _isHiding)
        {
            return;
        }

        _isHiding = true;

        try
        {
            // If there's a visible SubMenu, hide it first (deepest first)
            if (SubViews.FirstOrDefault (v => v is MenuItem { SubMenu.Visible: true }) is MenuItem visiblePeer)
            {
                _isHiding = false;
                visiblePeer.SubMenu!.HideMenu ();
                _isHiding = true;
            }

            Visible = false;
            Enabled = false;

            ClearFocus ();
        }
        finally
        {
            _isHiding = false;
        }
    }

    #endregion ShowMenu / HideMenu

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        base.Dispose (disposing);

        if (disposing)
        {
            ConfigurationManager.Applied -= OnConfigurationManagerApplied;
        }
    }
}
