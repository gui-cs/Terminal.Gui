#nullable enable

namespace Terminal.Gui;

/// <summary>
///     A <see cref="Shortcut"/>-derived object to be used as items in a <see cref="MenuBarv2"/>.
///     MenuBarItems have a title, a hotkey, and an action to execute on activation.
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
                new (menuItems))
    { }

    // TODO: Hide base.SubMenu?

    /// <summary>
    ///     The Popover Menu that will be displayed when this item is selected.
    /// </summary>
    public PopoverMenu? PopoverMenu { get; set; }

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
