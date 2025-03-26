#nullable enable

using System.ComponentModel;

namespace Terminal.Gui;

/// <summary>
///     A <see cref="MenuBarItemv2"/> has title, an associated help text, and an action to execute on activation.
/// </summary>
public class MenuBarItemv2 : MenuItemv2
{
    /// <summary>
    ///     Creates a new instance of <see cref="MenuBarItemv2"/>.
    /// </summary>
    public MenuBarItemv2 () : base (null, Command.NotBound, null!, null)
    {
    }

    /// <summary>
    ///     Creates a new instance of <see cref="MenuBarItemv2"/>, binding it to <paramref name="targetView"/> and
    ///     <paramref name="command"/>. The Key <paramref name="targetView"/>
    ///     has bound to <paramref name="command"/> will be used as <see cref="Key"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is a helper API that simplifies creation of multiple Shortcuts when adding them to <see cref="Bar"/>-based
    ///         objects, like <see cref="MenuBarv2"/>.
    ///     </para>
    /// </remarks>
    /// <param name="targetView">
    ///     The View that <paramref name="command"/> will be invoked on when user does something that causes the Shortcut's Accept
    ///     event to be raised.
    /// </param>
    /// <param name="command">
    ///     The Command to invoke on <paramref name="targetView"/>. The Key <paramref name="targetView"/>
    ///     has bound to <paramref name="command"/> will be used as <see cref="Key"/>
    /// </param>
    /// <param name="commandText">The text to display for the command.</param>
    /// <param name="helpText">The help text to display.</param>
    /// <param name="popoverMenu"></param>
    public MenuBarItemv2 (View? targetView, Command command, string commandText, string? helpText = null, PopoverMenu? popoverMenu = null)
        : base (
                targetView,
                command,
                commandText, null,
                null)
    {
        TargetView = targetView;
        Command = command;
        PopoverMenu = popoverMenu;
    }

    /// <summary>
    /// 
    /// </summary>
    public PopoverMenu? PopoverMenu { get; set; }

    /// <inheritdoc />
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
