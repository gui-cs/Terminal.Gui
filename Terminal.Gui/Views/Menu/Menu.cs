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

    /// <inheritdoc />
    protected override bool OnActivating (CommandEventArgs args)
    {
        Tracing.Trace.Command (this, args.Context, "Entry", $"Routing={args.Context?.Routing} Cmd={args.Context?.Command}");

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
        Tracing.Trace.Command (this, "Handler", $"{selected?.ToIdentifyingString ()}");

        OnSelectedMenuItemChanged (selected);
        SelectedMenuItemChanged?.Invoke (this, selected);
    }

    /// <summary>
    ///     Called when the selected menu item has changed.
    /// </summary>
    /// <param name="selected"></param>
    protected virtual void OnSelectedMenuItemChanged (MenuItem? selected) { }

    /// <summary>
    ///     Raised when the selected menu item has changed.
    /// </summary>
    public event EventHandler<MenuItem?>? SelectedMenuItemChanged;

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
