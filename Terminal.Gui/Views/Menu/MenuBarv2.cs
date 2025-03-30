#nullable enable
namespace Terminal.Gui;

/// <summary>
///     A horizontal list of <see cref="MenuBarItemv2"/>s. Each <see cref="MenuBarItemv2"/> can have a
///     <see cref="PopoverMenu"/> that is shown when the <see cref="MenuBarItemv2"/> is selected.
/// </summary>
/// <remarks>
///     MenuBars may be hosted by any View and will, by default, be positioned the full width across the top of the View's
///     Viewport.
/// </remarks>
public class MenuBarv2 : Menuv2, IDesignable
{
    /// <inheritdoc/>
    public MenuBarv2 () : this ([]) { }

    /// <inheritdoc/>
    public MenuBarv2 (IEnumerable<MenuBarItemv2> menuBarItems) : base (menuBarItems)
    {
        TabStop = TabBehavior.TabGroup;
        Y = 0;
        Width = Dim.Fill ();
        Orientation = Orientation.Horizontal;

        AddCommand (Command.Right, MoveRight);
        KeyBindings.Add (Key.CursorRight, Command.Right);

        AddCommand (Command.Left, MoveLeft);
        KeyBindings.Add (Key.CursorLeft, Command.Left);

        return;

        bool? MoveLeft (ICommandContext? ctx) { return AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabStop); }

        bool? MoveRight (ICommandContext? ctx) { return AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop); }
    }

    /// <inheritdoc/>
    protected override void OnSelectedMenuItemChanged (MenuItemv2? selected)
    {
        if (selected is MenuBarItemv2 { } selectedMenuBarItem)
        {
            ShowPopover (selectedMenuBarItem);
        }
    }

    /// <inheritdoc/>
    public override void EndInit ()
    {
        base.EndInit ();

        if (Border is { })
        {
            Border.Thickness = new (0);
            Border.LineStyle = LineStyle.None;
        }

        // TODO: This needs to be done whenever a menuitem in any MenuBarItem changes
        foreach (MenuBarItemv2? mbi in SubViews.Select(s => s as MenuBarItemv2))
        {
            Application.Popover?.Register (mbi?.PopoverMenu);
        }
    }

    /// <inheritdoc/>
    protected override bool OnAccepting (CommandEventArgs args)
    {
        if (args.Context?.Source is MenuBarItemv2 { PopoverMenu: { } } menuBarItem)
        {
            ShowPopover (menuBarItem);
        }

        return base.OnAccepting (args);
    }

    private void ShowPopover (MenuBarItemv2? menuBarItem)
    {
        if (menuBarItem?.PopoverMenu is { IsInitialized: false })
        {
            menuBarItem.PopoverMenu.BeginInit ();
            menuBarItem.PopoverMenu.EndInit ();
        }

        // If the active popover is a PopoverMenu and part of this MenuBar...
        if (menuBarItem?.PopoverMenu is null
            && Application.Popover?.GetActivePopover () is PopoverMenu popoverMenu
            && popoverMenu?.Root?.SuperMenuItem?.SuperView == this)
        {
            Application.Popover?.Hide (popoverMenu);
        }

        menuBarItem?.PopoverMenu?.MakeVisible (new Point (menuBarItem.FrameToScreen ().X, menuBarItem.FrameToScreen ().Bottom));

        if (menuBarItem?.PopoverMenu?.Root is { })
        {
            menuBarItem.PopoverMenu.Root.SuperMenuItem = menuBarItem;
        }
    }

    /// <inheritdoc/>
    public bool EnableForDesign<TContext> (ref readonly TContext context) where TContext : notnull
    {
        Add (
             new MenuBarItemv2 (
                                "_File",
                                [
                                    new MenuItemv2 (this, Command.New),
                                    new MenuItemv2 (this, Command.Open),
                                    new MenuItemv2 (this, Command.Save),
                                    new MenuItemv2 (this, Command.SaveAs),
                                    new Line (),
                                    new MenuItemv2
                                    {
                                        Title = "_Preferences",
                                        SubMenu = new (
                                                       [
                                                           new MenuItemv2
                                                           {
                                                               CommandView = new CheckBox ()
                                                               {
                                                                   Title = "O_ption",
                                                               },
                                                               HelpText = "Toggle option"
                                                           },
                                                           new MenuItemv2
                                                           {
                                                               Title = "_Settings...",
                                                               HelpText = "More settings",
                                                               Action = () =>  MessageBox.Query ("Settings", "This is the Settings Dialog\n", ["_Ok", "_Cancel"])
                                                           }
                                                       ]
                                                      )
                                    },
                                    new Line (),
                                    new MenuItemv2 (this, Command.Quit)
                                ]
                               )
            );

        Add (
             new MenuBarItemv2 (
                                "_Edit",
                                [
                                    new MenuItemv2 (this, Command.Cut),
                                    new MenuItemv2 (this, Command.Copy),
                                    new MenuItemv2 (this, Command.Paste),
                                    new Line (),
                                    new MenuItemv2 (this, Command.SelectAll)
                                ]
                               )
            );

        Add (
             new MenuBarItemv2 (
                                "_Help",
                                [
                                    new MenuItemv2
                                    {
                                        Title = "_Online Help...",
                                        Action = () => MessageBox.Query ("Online Help", "https://gui-cs.github.io/Terminal.GuiV2Docs", "Ok")
                                    },
                                    new MenuItemv2
                                    {
                                        Title = "About...",
                                        Action = () => MessageBox.Query ("About", "Something About Mary.", "Ok")
                                    }
                                ]
                               )
            );
        return true;
    }
}
