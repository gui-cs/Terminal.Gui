namespace Terminal.Gui.Input;

/// <summary>
///     Describes an input binding. Used to bind a set of <see cref="Command"/> objects to a specific input event and
///     passed as part of command invocations (see <see cref="CommandContext"/>).
/// </summary>
public interface IInputBinding
{
    /// <summary>
    ///     Gets or sets the commands this input binding will invoke.
    /// </summary>
    Command [] Commands { get; set; }

    /// <summary>
    ///     Arbitrary context that can be associated with this input binding.
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    ///     Gets or sets the <see cref="View"/> that registered the binding.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         For key bindings, this is the view that registered the binding.
    ///     </para>
    ///     <para>
    ///         For mouse bindings, this is the view that received the mouse event.
    ///     </para>
    ///     <para>
    ///         For programmatic invocations, this is the view that called <see cref="View.InvokeCommand(Command)"/>.
    ///     </para>
    /// </remarks>
    public View? Source { get; init; }
}
