#nullable enable

namespace Terminal.Gui.Input;

#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
/// <summary>
///     Describes the context in which a <see cref="Command"/> is being invoked. <see cref="CommandContext{TBindingType}"/> inherits from this interface.
///     When a <see cref="Command"/> is invoked,
///     a context object is passed to Command handlers as an <see cref="ICommandContext"/> reference.
/// </summary>
/// <seealso cref="View.AddCommand(Command, View.CommandImplementation)"/>.
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
public interface ICommandContext
{
    /// <summary>
    ///     The <see cref="Command"/> that is being invoked.
    /// </summary>
    public Command Command { get; set; }

    /// <summary>
    ///     The View that was the source of the command invocation, if any.
    ///     (e.g. the view the user clicked on or the view that had focus when a key was pressed).
    /// </summary>
    public View? Source { get; set; }
}
