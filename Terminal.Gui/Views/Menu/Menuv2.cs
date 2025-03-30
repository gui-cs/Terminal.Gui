#nullable enable
namespace Terminal.Gui;

/// <summary>
///     A <see cref="Bar"/>-derived object to be used as a vertically-oriented menu. Each subview is a <see cref="MenuItemv2"/>.
/// </summary>
public class Menuv2 : Bar
{
    /// <inheritdoc/>
    public Menuv2 () : this ([]) { }

    /// <inheritdoc/>
    public Menuv2 (IEnumerable<MenuItemv2>? shortcuts) : this (shortcuts?.Cast<View>()) { }

    /// <inheritdoc/>
    public Menuv2 (IEnumerable<View>? shortcuts) : base (shortcuts)
    {
        Orientation = Orientation.Vertical;
        Width = Dim.Auto ();
        Height = Dim.Auto (DimAutoStyle.Content, 1);

        Border!.Thickness = new Thickness (1, 1, 1, 1);
        Border.LineStyle = LineStyle.Single;
    }

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
    public override void EndInit ()
    {
        base.EndInit ();

        if (Border is { })
        {
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

                AddCommand (menuItem.Command, RaiseAccepted);

                menuItem.Accepted += MenuItemOnAccepted;

                break;

                void MenuItemOnAccepted (object? sender, CommandEventArgs e)
                {
                    //Logging.Trace ($"Accepted: {e.Context?.Source?.Title}");
                    RaiseAccepted (e.Context);
                }
            }
            case Line line:
                // Grow line so we get auto-join line
                line.X = Pos.Func (() => -Border!.Thickness.Left);
                line.Width = Dim.Fill ()! + Dim.Func (() => Border!.Thickness.Right);

                break;
        }
    }

    // TODO: Consider moving Accepted to Bar?

    /// <summary>
    ///     Raises the <see cref="OnAccepted"/>/<see cref="Accepted"/> event indicating an item in this menu (or submenu)
    ///     was accepted. This is used to determine when to hide the menu.
    /// </summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    protected bool? RaiseAccepted (ICommandContext? ctx)
    {
        //Logging.Trace ($"RaiseAccepted: {ctx}");
        CommandEventArgs args = new () { Context = ctx };

        OnAccepted (args);
        Accepted?.Invoke (this, args);

        return true;
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
        //Logging.Trace ($"RaiseSelectedMenuItemChanged: {selected?.Title}");

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

}