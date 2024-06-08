#nullable enable
namespace Terminal.Gui;
/// <summary>
///     Provides context for a <see cref="Command"/> that is being invoked.
/// </summary
/// <remarks>
///     <para>
///         To define a <see cref="Command"/> that is invoked with context,
///         use <see cref="View.AddCommand(Command,Func{CommandContext,Nullable{bool}})"/>
///     </para>
/// </remarks>
public record struct CommandContext
{
    /// <summary>
    ///     Initializes a new instance of <see cref="CommandContext"/> with the specified <see cref="Command"/>,
    /// </summary>
    /// <param name="command"></param>
    /// <param name="key"></param>
    public CommandContext (Command command, Key? key)
    {
        Command = command;
        Key = key;
    }

    /// <summary>
    ///     The <see cref="Command"/> that is being invoked.
    /// </summary>
    public Command Command { get; set; }

    /// <summary>
    ///     The <see cref="Key"/> that is being invoked. This is the key that was pressed to invoke the <see cref="Command"/>.
    /// </summary>
    public Key? Key { get; set; }
}
