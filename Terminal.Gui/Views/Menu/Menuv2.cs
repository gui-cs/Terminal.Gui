#nullable enable
using System;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Reflection;

namespace Terminal.Gui;

/// <summary>
/// </summary>
public class Menuv2 : Bar
{
    /// <inheritdoc/>
    public Menuv2 () : this ([]) { }

    /// <inheritdoc/>
    public Menuv2 (IEnumerable<Shortcut> shortcuts) : base (shortcuts)
    {
        Orientation = Orientation.Vertical;
        Width = Dim.Auto ();
        Height = Dim.Auto (DimAutoStyle.Content, 1);

        Initialized += Menuv2_Initialized;
        VisibleChanged += OnVisibleChanged;

    }

    /// <summary>
    ///     Gets or sets the menu item that opened this menu as a sub-menu.
    /// </summary>
    public MenuItemv2? SuperMenuItem { get; set; }

    private void OnVisibleChanged (object? sender, EventArgs e)
    {
        if (Visible)
        {
            SelectedMenuItem = SubViews.Where (mi => mi is MenuItemv2).ElementAtOrDefault (0) as MenuItemv2;
        }
    }

    private void Menuv2_Initialized (object? sender, EventArgs e)
    {
        if (Border is { })
        {
            Border.Thickness = new Thickness (1, 1, 1, 1);
            Border.LineStyle = LineStyle.Single;
        }

        ColorScheme = Colors.ColorSchemes ["Menu"];
    }

    /// <inheritdoc />
    protected override void OnSubViewAdded (View view)
    {
        base.OnSubViewAdded (view);

        if (view is MenuItemv2 menuItem)
        {
            menuItem.CanFocus = true;
            menuItem.Orientation = Orientation.Vertical;

            AddCommand (menuItem.Command, RaiseAccepted);

            menuItem.Selecting += MenuItemOnSelecting;
            menuItem.Accepting += MenuItemOnAccepting;
            menuItem.Accepted += MenuItemOnAccepted;

            void MenuItemOnSelecting (object? sender, CommandEventArgs e)
            {
                //Logging.Trace ($"MenuItemOnSelecting: {e.Context?.Source?.Title}");
            }

            void MenuItemOnAccepting (object? sender, CommandEventArgs e)
            {
                // Logging.Trace ($"MenuItemOnAccepting: {e.Context?.Source?.Title}");
            }

            void MenuItemOnAccepted (object? sender, CommandEventArgs e)
            {
                Logging.Trace ($"MenuItemOnAccepted: {e.Context?.Source?.Title}");
                RaiseAccepted (e.Context);
            }
        }
    }


    /// <summary>
    ///     Riases the <see cref="OnAccepted"/>/<see cref="Accepted"/> event indicating an item in this menu (or submenu)
    ///     was accepted. This is used to determine when to hide the menu.
    /// </summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    protected bool? RaiseAccepted (ICommandContext? ctx)
    {
        Logging.Trace ($"RaiseAccepted: {ctx}");
        CommandEventArgs args = new () { Context = ctx };

        OnAccepted (args);
        Accepted?.Invoke (this, args);

        return true;
    }

    /// <summary>
    ///     Called when the user has accepted an item in this menu (or submenu. This is used to determine when to hide the menu.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="args"></param>
    protected virtual void OnAccepted (CommandEventArgs args) { }

    /// <summary>
    ///     Raised when the user has accepted an item in this menu (or submenu. This is used to determine when to hide the menu.
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
    /// 
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

            //value?.SetFocus ();
        }
    }

    internal void RaiseSelectedMenuItemChanged (MenuItemv2? selected)
    {
        //Logging.Trace ($"RaiseSelectedMenuItemChanged: {selected?.Title}");

        //ShowSubMenu (selected);
        OnSelectedMenuItemChanged (selected);

        SelectedMenuItemChanged?.Invoke (this, selected);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="selected"></param>
    protected virtual void OnSelectedMenuItemChanged (MenuItemv2? selected)
    {
    }

    /// <summary>
    /// 
    /// </summary>
    public event EventHandler<MenuItemv2?>? SelectedMenuItemChanged;

}