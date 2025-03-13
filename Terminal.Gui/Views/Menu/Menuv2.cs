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

    private void OnVisibleChanged (object? sender, EventArgs e)
    {
        if (Visible)
        {
            SelectedMenuItem = SubViews.Where (mi => mi is MenuItemv2).ElementAtOrDefault (0) as MenuItemv2;

            //Application.GrabMouse(this);
        }
        else
        {
            if (Application.MouseGrabView == this)
            {
                //Application.UngrabMouse ();
            }
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

            AddCommand (menuItem.Command, RaiseMenuItemCommandInvoked);

            menuItem.Accepting += MenuItemtOnAccepting;

            //menuItem.ActivateSubMenu += MenuItemOnActivateSubMenu;
            //void MenuItemOnActivateSubMenu (object? sender, EventArgs<Menuv2> e)
            //{
            //    Logging.Trace ($"MenuItemOnActivateSubMenu: {e}");

            //    if (e.CurrentValue is { })
            //    {
            //        SuperView.Add (e.CurrentValue);
            //        e.CurrentValue.X = Frame.X + Frame.Width;
            //        e.CurrentValue.Y = Frame.Y + menuItem.Frame.Y;
            //        e.CurrentValue.Visible = true;
            //    }
            //}

            void MenuItemtOnAccepting (object? sender, CommandEventArgs e)
            {
                //Logging.Trace($"MenuItemtOnAccepting: {e.Context}");
            }
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    protected bool? RaiseMenuItemCommandInvoked (ICommandContext? ctx)
    {
        Logging.Trace ($"RaiseMenuItemCommandInvoked: {ctx}");
        CommandEventArgs args = new () { Context = ctx };

        // Best practice is to invoke the virtual method first.
        // This allows derived classes to handle the event and potentially cancel it.
        args.Cancel = OnMenuItemCommandInvoked (args) || args.Cancel;

        if (!args.Cancel)
        {
            // If the event is not canceled by the virtual method, raise the event to notify any external subscribers.
            MenuItemCommandInvoked?.Invoke (this, args);
        }

        return MenuItemCommandInvoked is null ? null : args.Cancel;
    }

    /// <summary>
    ///     Called when the user is accepting the state of the View and the <see cref="Command.Accept"/> has been invoked. Set CommandEventArgs.Cancel to
    ///     <see langword="true"/> and return <see langword="true"/> to stop processing.
    /// </summary>
    /// <remarks>
    /// <para>
    ///    See <see cref="MenuItemCommandInvoked"/> for more information.
    /// </para>
    /// </remarks>
    /// <param name="args"></param>
    /// <returns><see langword="true"/> to stop processing.</returns>
    protected virtual bool OnMenuItemCommandInvoked (CommandEventArgs args) { return false; }

    /// <summary>
    ///     Cancelable event raised when the user is accepting the state of the View and the <see cref="Command.Accept"/> has been invoked. Set
    ///     CommandEventArgs.Cancel to cancel the event.
    /// </summary>
    /// <remarks>
    /// <para>
    ///    See <see cref="RaiseMenuItemCommandInvoked"/> for more information.
    /// </para>
    /// </remarks>
    public event EventHandler<CommandEventArgs>? MenuItemCommandInvoked;

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

        ShowSubMenu (selected);
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

    public void ShowSubMenu (MenuItemv2? menuItem)
    {
        // Hide any other submenus that might be visible
        foreach (MenuItemv2 mi in SubViews.Where (v => v is MenuItemv2 { SubMenu.Visible: true }).Cast<MenuItemv2> ())
        {
            mi.ForceFocusColors = false;
            mi.SubMenu!.Visible = false;
            SuperView?.Remove (mi.SubMenu);
        }

        if (menuItem is { SubMenu: {} })
        {
            SuperView?.Add (menuItem.SubMenu);
            menuItem.SubMenu.X = Frame.X + Frame.Width;
            menuItem.SubMenu.Y = Frame.Y + menuItem.Frame.Y;
            menuItem.SubMenu.Visible = true;
            menuItem.ForceFocusColors = true;
        }
    }
}