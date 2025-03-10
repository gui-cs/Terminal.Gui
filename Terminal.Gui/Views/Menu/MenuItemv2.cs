#nullable enable

namespace Terminal.Gui;

/// <summary>
///     A <see cref="MenuItemv2"/> has title, an associated help text, and an action to execute on activation. MenuItems
///     can also have a checked indicator (see <see cref="Checked"/>).
/// </summary>
public class MenuItemv2 : Shortcut
{
    /// <summary>
    ///     Creates a new instance of <see cref="MenuItemv2"/>.
    /// </summary>
    public MenuItemv2 () : base (Key.Empty, null, null, null)
    {
        HighlightStyle = HighlightStyle.Hover;
    }

    /// <summary>
    ///     Creates a new instance of <see cref="Shortcut"/>, binding it to <paramref name="targetView"/> and
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
    public MenuItemv2 (View targetView, Command command, string commandText, string? helpText = null)
        : base (
                targetView?.HotKeyBindings.GetFirstFromCommands (command)!,
                commandText,
                null,
                helpText)
    {
        _targetView = targetView;
        Command = command;

        if (Command == Command.Context)
        {
            KeyView.Text = $"{Glyphs.RightArrow}";
        }
    }

    private readonly View? _targetView; // If set, _command will be invoked

    /// <summary>
    ///     Gets the target <see cref="View"/> that the <see cref="Command"/> will be invoked on.
    /// </summary>
    public View? TargetView => _targetView;

    /// <summary>
    ///     Gets the <see cref="Command"/> that will be invoked on <see cref="TargetView"/> when the Shortcut is activated.
    /// </summary>
    public Command Command { get; }

    internal override bool? DispatchCommand (ICommandContext? commandContext)
    {
        bool? ret = base.DispatchCommand (commandContext);

        if (_targetView is { })
        {
            _targetView.InvokeCommand (Command, commandContext);
        }

        return ret;
    }
}
