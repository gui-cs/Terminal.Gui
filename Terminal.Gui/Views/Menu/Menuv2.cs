#nullable enable
using System.ComponentModel;
using System.Diagnostics;

namespace Terminal.Gui;

/// <summary>
///     A <see cref="Bar"/>-derived object to be used as a vertically-oriented menu. Each subview is a
///     <see cref="MenuItemv2"/>.
/// </summary>
public class Menuv2 : Bar, IDesignable
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
        base.ColorScheme = Colors.ColorSchemes ["Menu"];

        if (Border is { })
        {
            Border.Settings &= ~BorderSettings.Title;
        }

        BorderStyle = DefaultBorderStyle;

        Arrangement = ViewArrangement.Overlapped;

        Applied += OnConfigurationManagerApplied;

        KeyBindings.ReplaceCommands (Application.QuitKey, Command.Quit);
        AddCommand (Command.Quit, Quit);

        return;

        bool? Quit (ICommandContext? ctx)
        {
            Logging.Debug ($"{Title} Command.Quit - {ctx?.Source?.Title}");

            if (!Visible)
            {
                // If we're not visible, the command is not for us
                return false;
            }

            Visible = false;

            return true;
        }
    }

    private void OnConfigurationManagerApplied (object? sender, ConfigurationManagerEventArgs e)
    {
        if (SuperView is { })
        {
            BorderStyle = DefaultBorderStyle;
        }
    }

    /// <summary>
    ///     Gets or sets the default Border Style for Menus.
    /// </summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static LineStyle DefaultBorderStyle { get; set; } = LineStyle.Rounded;

    private MenuItemv2? _superMenuItem;

    /// <summary>
    ///     Gets or sets the <see cref="MenuItemv2"/> that is the parent of this menu.
    ///     If this menu is at the root of the menu hierarchy, this property will be <see langword="null"/> and the parent will be <see cref="View.SuperView"/>.
    ///     If this menu is not at the root of the menu hierarchy, this property will be the <see cref="MenuItemv2"/> that has it as a sub-menu.
    /// </summary>
    public MenuItemv2? SuperMenuItem
    {
        get => _superMenuItem;
        set
        {
            if (value is { SuperView: { } })
            {
                //throw new ArgumentException ($"A Menu with a SuperView can not also have a SuperMenuItem.");
            }
            _superMenuItem = value;
        }
    }

    /// <inheritdoc/>
    protected override bool OnVisibleChanging ()
    {
        Logging.Debug ($"{Title} - Visible: {Visible}");
        // If we have a SuperView, we are either the root of the menu hierarchy or activated. Act just like a normal View.
        if (SuperView is { })
        {
            Logging.Debug ($"{Title} - SuperView: {SuperView?.Title} - Calling base.OnVisibleChanged");
            return base.OnVisibleChanging ();
        }

        // If we don't have a SuperView, we need to be added to one in order to be visible.
        // Our SuperMenuItem will be the one that adds us to a SuperView, or it will pass our request up
        // the menu hierarchy.
        bool ret = RaiseShowingSubMenu ();

        if (ret)
        {
            // If handled...
        }
        else
        {
            // If not handled, bubble up


        }

        return ret;
    }

    protected bool RaiseShowingSubMenu ()
    {
        Logging.Debug ($"{Title} - SuperView: {SuperView?.Title}; SuperMenuItem: {SuperMenuItem?.Title}");
        HandledEventArgs eventArgs = new HandledEventArgs ();

        if (OnShowingSubMenu (eventArgs) || eventArgs.Handled)
        {
            return true;
        }

        ShowingSubMenu?.Invoke (this, eventArgs);

        return eventArgs.Handled;

    }

    protected virtual bool OnShowingSubMenu (HandledEventArgs cancelEventArgs) { return false; }

    public event EventHandler<HandledEventArgs>? ShowingSubMenu;

    /// <inheritdoc/>
    protected override void OnVisibleChanged ()
    {
        if (Visible)
        {
            // Whenever we're made visible, make the first menuitem be selected
            SelectedMenuItem = SubViews.OfType<MenuItemv2> ().ElementAtOrDefault (0);
        }
    }


    ///// <summary>
    /////     If a menu does not have a SuperView, it needs to be given one to be made visible. This is done by
    /////     raising an event on the Menu's SuperMenuItem, which is a proxy for the SuperView. The SuperMenuItem
    /////     will then do what's needed to add the Menu to a SuperView (which may be a Popover if the Menu is a
    /////     being used as a context menu or part of a MenuBar, or may just be a normal View).
    ///// </summary>
    ///// <returns></returns>
    //public bool Activate ()
    //{
    //    if (SuperView is { Visible: false })
    //    {
    //        Visible = true;

    //        return true;
    //    }

    //    if (SuperMenuItem is { })
    //    {
    //        // If we have a SuperMenuItem, we need let our SuperMenuItem know we are being activated.
    //        // This is used to add us to a SuperView, which may be a Popover.
    //        RaiseActivating ();

    //        return true;
    //    }
    //    return false;
    //}

    //public bool Deactivate ()
    //{
    //    return true;
    //}

    /// <inheritdoc/>
    protected override void OnSubViewAdded (View view)
    {
        base.OnSubViewAdded (view);

        switch (view)
        {
            case MenuItemv2 menuItem:
                {
                    menuItem.CanFocus = true;

                    AddCommand (
                                menuItem.Command,
                                ctx =>
                                {
                                    RaiseAccepted (ctx);

                                    return true;
                                });

                    menuItem.Accepted += MenuItemOnAccepted;

                    break;

                    void MenuItemOnAccepted (object? sender, CommandEventArgs e)
                    {
                        Logging.Debug ($"MenuItemOnAccepted: Calling RaiseAccepted {e.Context?.Source?.Title}");
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

    /// <inheritdoc />
    protected override bool OnAccepting (CommandEventArgs args)
    {
        // When the user accepts a menuItem, Menu.RaiseAccepting is called, and we intercept that here.

        Logging.Debug ($"{Title} - {args.Context?.Source?.Title} Command: {args.Context?.Command}");

        // TODO: Consider having PopoverMenu subscribe to Accepting instead of us overriding OnAccepting here
        // TODO: Doing so would be better encapsulation and might allow us to remove the SuperMenuItem property.

        Debug.Assert (SuperView is { });

        if (args.Context is CommandContext<KeyBinding> { Binding.Key: { } } keyCommandContext)
        {
            if (keyCommandContext is { Command: Command.HotKey, Source.HotKey: { } hotkey } && hotkey == keyCommandContext.Binding.Key)
            {
                Logging.Debug ($"{Title} - Returning true - Accepting came from HotKey of menuitem.");

                //MenuItemv2? source = keyCommandContext.Source as MenuItemv2;

                //if (source is { SubMenu.Visible: true })
                //{
                //    return false;
                //}
                return true;
            }

            // Special case QuitKey if we are Visible - This supports a MenuItem with Key = Application.QuitKey/Command = Command.Quit
            // And causes just the menu to quit.
            //Logging.Debug ($"{Title} - Returning true - Application.QuitKey/Command = Command.Quit");
            //return true;
        }

        // We need to propagate Command.Accept to the SuperMenuItem if it exists.
        var ret = false;

        if (args.Context is CommandContext<KeyBinding> { Binding.Key: { } } keyCommandContext && keyCommandContext.Binding.Key == Application.QuitKey)
        {
            Logging.Debug ($"{Title} - Invoking Accept on SuperMenuItem: {SuperMenuItem?.Title}...");
            ret = SuperMenuItem?.InvokeCommand (Command.Accept, args.Context) is true;
        }

        return ret;
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
    ///     Called when the user has accepted an item in this menu (or submenu). This is used to determine when to hide the
    ///     menu.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="args"></param>
    protected virtual void OnAccepted (CommandEventArgs args) { }

    /// <summary>
    ///     Raised when the user has accepted an item in this menu (or submenu). This is used to determine when to hide the
    ///     menu.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         See <see cref="RaiseAccepted"/> for more information.
    ///     </para>
    /// </remarks>
    public event EventHandler<CommandEventArgs>? Accepted;

    /// <inheritdoc/>
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
            { }

            // Note we DO NOT set focus here; This property tracks Focused
        }
    }

    internal void RaiseSelectedMenuItemChanged (MenuItemv2? selected)
    {
        Logging.Debug ($"{Title} ({selected?.Title})");

        if (RaiseSelecting (new CommandContext<object> ()
        {
            Source = selected
        }) is true)
        {
            if (selected is { SubMenu: { Visible: false } subMenu })
            {
                Debug.Assert (subMenu?.SuperView is { });

                Point idealLocation = ScreenToViewport (
                                                        new (
                                                             selected.FrameToScreen ().Right - selected.SubMenu.GetAdornmentsThickness ().Left,
                                                             selected.FrameToScreen ().Top - selected.SubMenu.GetAdornmentsThickness ().Top));

                Point pos = GetMostVisibleLocationForSubMenu (selected.SubMenu, idealLocation);
                selected.SubMenu.X = pos.X;
                selected.SubMenu.Y = pos.Y;

                selected.SubMenu.Visible = true;
                selected.SubMenu.Layout ();
            }

            return;
        }

        OnSelectedMenuItemChanged (selected);
        SelectedMenuItemChanged?.Invoke (this, selected);
    }

    /// <summary>
    ///     Gets the most visible screen-relative location for <paramref name="menu"/>.
    /// </summary>
    /// <param name="menu">The menu to locate.</param>
    /// <param name="idealLocation">Ideal screen-relative location.</param>
    /// <returns></returns>
    internal Point GetMostVisibleLocationForSubMenu (Menuv2 menu, Point idealLocation)
    {
        var pos = Point.Empty;

        // Calculate the initial position to the right of the menu item
        GetLocationEnsuringFullVisibility (
                                           menu,
                                           idealLocation.X,
                                           idealLocation.Y,
                                           out int nx,
                                           out int ny);

        return new (nx, ny);
    }

    /// <summary>
    ///     Called when the selected menu item has changed.
    /// </summary>
    /// <param name="selected"></param>
    protected virtual void OnSelectedMenuItemChanged (MenuItemv2? selected)
    {
        Logging.Debug ($"{Title} ({selected?.Title})");

        if (selected?.SubMenu is { })
        {
            selected.SubMenu.Visible = true;

            //Point idealLocation = ScreenToViewport (
            //                                        new (
            //                                             selected.FrameToScreen ().Right - selected.SubMenu.GetAdornmentsThickness ().Left,
            //                                             selected.FrameToScreen ().Top - selected.SubMenu.GetAdornmentsThickness ().Top));

            //Point pos = GetMostVisibleLocationForSubMenu (selected.SubMenu, idealLocation);
            //selected.SubMenu.X = pos.X;
            //selected.SubMenu.Y = pos.Y;

            //selected.SubMenu.Visible = true;
            //selected.SubMenu.Layout ();
        }
    }

    /// <summary>
    ///     Raised when the selected menu item has changed.
    /// </summary>
    public event EventHandler<MenuItemv2?>? SelectedMenuItemChanged;

    /// <inheritdoc/>
    public bool EnableForDesign<TContext> (ref TContext context) where TContext : notnull
    {
        // Note: This menu is used by unit tests. If you modify it, you'll likely have to update
        // unit tests.

        Add (
             new MenuItemv2 (context as View, Command.Cut),
             new MenuItemv2 (context as View, Command.Copy),
             new MenuItemv2 (context as View, Command.Paste),
             new Line (),
             new MenuItemv2 (context as View, Command.SelectAll),
             new Line (),
             new MenuItemv2
             {
                 Title = "_Details",
                 SubMenu = new (ConfigureDetailsSubMenu ())
             },
             new Line (),
             new MenuItemv2 (context as View, Command.Quit));

        return true;

        MenuItemv2 [] ConfigureDetailsSubMenu ()
        {
            var detail = new MenuItemv2
            {
                Title = "_Detail 1",
                Text = "Some detail #1"
            };

            var nestedSubMenu = new MenuItemv2
            {
                Title = "_Moar Details",
                SubMenu = new (ConfigureMoreDetailsSubMenu ())
                {
                    Title = "MoreDetailsSubMenu"
                }
            };

            var editMode = new MenuItemv2
            {
                Text = "Command = Edit; TargetView = null",
                Id = "EditMode",
                Command = Command.Edit,
                CommandView = new CheckBox
                {
                    Title = "_Edit Mode"
                }
            };

            return [detail, nestedSubMenu, null!, editMode];

            View [] ConfigureMoreDetailsSubMenu ()
            {
                var deeperDetail = new MenuItemv2
                {
                    Title = "_Deeper Detail",
                    Text = "Deeper Detail",
                    Action = () => { MessageBox.Query ("Deeper Detail", "Lots of details", "_Ok"); }
                };

                var belowLineDetail = new MenuItemv2
                {
                    Title = "_Even more detail",
                    Text = "Below the line"
                };

                // This ensures the checkbox state toggles when the hotkey of Title is pressed.
                //shortcut4.Accepting += (sender, args) => args.Cancel = true;

                return [deeperDetail, new Line (), belowLineDetail];
            }
        }
    }

    /// <inheritdoc />
    protected override void Dispose (bool disposing)
    {
        base.Dispose (disposing);

        if (disposing)
        {
            Applied -= OnConfigurationManagerApplied;
        }
    }
}
