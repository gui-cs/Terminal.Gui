#nullable enable

namespace Terminal.Gui.Views;

/// <summary>
///     A <see cref="Bar"/>-derived object to be used as a vertically-oriented menu. Each subview is a <see cref="MenuItemv2"/>.
/// </summary>
public class Menuv2 : Bar
{
    /// <inheritdoc/>
    public Menuv2 () : this ([]) { }

    /// <inheritdoc/>
    public Menuv2 (IEnumerable<MenuItemv2>? menuItems) : this (menuItems?.Cast<View> ()) { }

    /// <inheritdoc/>
    public Menuv2 (IEnumerable<View>? shortcuts) : base (shortcuts)
    {
        // Do this to support debugging traces where Title gets set
        base.HotKeySpecifier = (Rune)'\xffff';

        Orientation = Orientation.Vertical;
        Width = Dim.Auto ();
        Height = Dim.Auto (DimAutoStyle.Content, 1);
        SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Menu);

        if (Border is { })
        {
            Border.Settings &= ~BorderSettings.Title;
        }

        BorderStyle = DefaultBorderStyle;

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
    public MenuItemv2? SuperMenuItem { get; set; }

    /// <inheritdoc />
    protected override void OnVisibleChanged ()
    {
        if (Visible)
        {
            SelectedMenuItem = SubViews.Where (mi => mi is MenuItemv2).ElementAtOrDefault (0) as MenuItemv2;
        }
    }

    /// <inheritdoc />
    protected override void OnSubViewAdded (View view)
    {
        base.OnSubViewAdded (view);

        switch (view)
        {
            case MenuItemv2 menuItem:
                {
                    menuItem.CanFocus = true;

                    AddCommand (menuItem.Command, (ctx) =>
                                                  {
                                                      RaiseAccepted (ctx);

                                                      return true;

                                                  });

                    menuItem.Accepted += MenuItemOnAccepted;

                    break;

                    void MenuItemOnAccepted (object? sender, CommandEventArgs e)
                    {
                        // Logging.Debug ($"MenuItemOnAccepted: Calling RaiseAccepted {e.Context?.Source?.Title}");
                        RaiseAccepted (e.Context);
                    }
                }
            case Line line:
                // Grow line so we get auto-join line
                line.X = Pos.Func (_ => -Border!.Thickness.Left);
                line.Width = Dim.Fill ()! + Dim.Func (_ => Border!.Thickness.Right);

                break;
        }
    }


    /// <inheritdoc />
    protected override bool OnAccepting (CommandEventArgs args)
    {
        // When the user accepts a menuItem, Menu.RaiseAccepting is called, and we intercept that here.

        // Logging.Debug ($"{Title} - {args.Context?.Source?.Title} Command: {args.Context?.Command}");

        // TODO: Consider having PopoverMenu subscribe to Accepting instead of us overriding OnAccepting here
        // TODO: Doing so would be better encapsulation and might allow us to remove the SuperMenuItem property.
        if (SuperView is { })
        {
            // Logging.Debug ($"{Title} - SuperView is null");
            //return false;
        }

        // Logging.Debug ($"{Title} - {args.Context}");

        if (args.Context is CommandContext<KeyBinding> { Binding.Key: { } } keyCommandContext && keyCommandContext.Binding.Key == Application.QuitKey)
        {
            // Special case QuitKey if we are Visible - This supports a MenuItem with Key = Application.QuitKey/Command = Command.Quit
            // And causes just the menu to quit.
            // Logging.Debug ($"{Title} - Returning true - Application.QuitKey/Command = Command.Quit");
            return true;
        }

        // Because we may not have a SuperView (if we are in a PopoverMenu), we need to propagate
        // Command.Accept to the SuperMenuItem if it exists.
        if (SuperView is null && SuperMenuItem is { })
        {
            // Logging.Debug ($"{Title} - Invoking Accept on SuperMenuItem: {SuperMenuItem?.Title}...");
            return SuperMenuItem?.InvokeCommand (Command.Accept, args.Context) is true;
        }
        return false;
    }

    // TODO: Consider moving Accepted to Bar?

    /// <summary>
    ///     Raises the <see cref="OnAccepted"/>/<see cref="Accepted"/> event indicating an item in this menu (or submenu)
    ///     was accepted. This is used to determine when to hide the menu.
    /// </summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    protected void RaiseAccepted (ICommandContext? ctx)
    {
        //Logging.Trace ($"RaiseAccepted: {ctx}");
        CommandEventArgs args = new () { Context = ctx };

        OnAccepted (args);
        Accepted?.Invoke (this, args);
    }

    /// <summary>
    ///     Called when the user has accepted an item in this menu (or submenu). This is used to determine when to hide the menu.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="args"></param>
    protected virtual void OnAccepted (CommandEventArgs args) { }

    /// <summary>
    ///     Raised when the user has accepted an item in this menu (or submenu). This is used to determine when to hide the menu.
    /// </summary>
    /// <remarks>
    /// <para>
    ///    See <see cref="RaiseAccepted"/> for more information.
    /// </para>
    /// </remarks>
    public event EventHandler<CommandEventArgs>? Accepted;

    /// <inheritdoc />
    protected override void OnFocusedChanged (View? previousFocused, View? focused)
    {
        base.OnFocusedChanged (previousFocused, focused);

        SelectedMenuItem = focused as MenuItemv2;
        RaiseSelectedMenuItemChanged (SelectedMenuItem);
    }

    /// <summary>
    ///     Gets or set the currently selected menu item. This is a helper that
    ///     tracks <see cref="View.Focused"/>.
    /// </summary>
    public MenuItemv2? SelectedMenuItem
    {
        get => Focused as MenuItemv2;
        set
        {
            if (value == Focused)
            {
                return;
            }

            // Note we DO NOT set focus here; This property tracks Focused
        }
    }

    internal void RaiseSelectedMenuItemChanged (MenuItemv2? selected)
    {
        // Logging.Debug ($"{Title} ({selected?.Title})");

        OnSelectedMenuItemChanged (selected);
        SelectedMenuItemChanged?.Invoke (this, selected);
    }

    /// <summary>
    ///     Called when the selected menu item has changed.
    /// </summary>
    /// <param name="selected"></param>
    protected virtual void OnSelectedMenuItemChanged (MenuItemv2? selected)
    {
    }

    /// <summary>
    ///     Raised when the selected menu item has changed.
    /// </summary>
    public event EventHandler<MenuItemv2?>? SelectedMenuItemChanged;

    /// <inheritdoc />
    protected override void Dispose (bool disposing)
    {
        base.Dispose (disposing);

        if (disposing)
        {
            ConfigurationManager.Applied -= OnConfigurationManagerApplied;
        }
    }
}