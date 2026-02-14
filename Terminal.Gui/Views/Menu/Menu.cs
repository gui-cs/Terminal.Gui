namespace Terminal.Gui.Views;

/// <summary>
///     A <see cref="Bar"/>-derived object to be used as a vertically-oriented menu. Each subview is a
///     <see cref="MenuItem"/>.
/// </summary>
public class Menu : Bar
{
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

        // When a MenuItem's Activate bubbles up to this Menu, run the full handler
        // (so Activating/Activated events fire on Menu) but return false so the originating
        // MenuItem can complete its own activation (RaiseActivated → Action?.Invoke()).
        AddCommand (Command.Activate, ctx =>
                                      {
                                          if (ctx?.IsBubblingUp == true)
                                          {
                                              DefaultActivateHandler (ctx);

                                              return false;
                                          }

                                          return DefaultActivateHandler (ctx);
                                      });

        // Same for Accept: run the handler (fires Accepting/Accepted events) but return false
        // so the originating MenuItem completes its own Accept processing.
        AddCommand (Command.Accept, ctx =>
                                    {
                                        if (ctx?.IsBubblingUp == true)
                                        {
                                            DefaultAcceptHandler (ctx);

                                            return false;
                                        }

                                        return DefaultAcceptHandler (ctx);
                                    });

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
    ///     Gets or sets the default Border Style for Menus. The default is <see cref="LineStyle.None"/>.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static LineStyle DefaultBorderStyle { get; set; } = LineStyle.None;

    /// <summary>
    ///     Gets or sets the menu item that opened this menu as a sub-menu.
    /// </summary>
    public MenuItem? SuperMenuItem { get; set; }

    /// <inheritdoc/>
    protected override void OnSubViewAdded (View view)
    {
        base.OnSubViewAdded (view);

        switch (view)
        {
            case MenuItem menuItem:
            {
                menuItem.CanFocus = true;

                menuItem.Accepting += (_, e) => RaiseAccepted (e.Context);

                // When a MenuItem is activated (e.g., via HotKey → Activate flow),
                // raise Accepted on this Menu so PopoverMenu can close.
                menuItem.Activated += (_, e) => RaiseAccepted (e.Value);

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
