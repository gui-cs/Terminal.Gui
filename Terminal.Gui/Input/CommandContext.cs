#nullable enable
namespace Terminal.Gui;

#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
/// <summary>
///     Provides context for a <see cref="Command"/> that is being invoked.
/// </summary>
/// <remarks>
///     <para>
///         To define a <see cref="Command"/> that is invoked with context,
///         use <see cref="View.AddCommand(Command,Func{CommandContext,System.Nullable{bool}})"/>.
///     </para>
/// </remarks>
/// <seealso cref="Application.KeyBindings"/>
/// <seealso cref="View.KeyBindings"/>
/// <seealso cref="Command"/>
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
public record struct CommandContext
{
    /// <summary>
    ///     Initializes a new instance of <see cref="CommandContext"/> with the specified <see cref="Command"/>,
    /// </summary>
    /// <param name="command"></param>
    /// <param name="key"></param>
    /// <param name="keyBinding"></param>
    /// <param name="data"></param>
    public CommandContext (Command command, Key? key, KeyBinding? keyBinding = null, object? data = null)
    {
        Command = command;
        Key = key;
        KeyBinding = keyBinding;
        Data = data;
    }

    /// <summary>
    ///     The <see cref="Command"/> that is being invoked.
    /// </summary>
    public Command Command { get; set; }

    /// <summary>
    ///     The <see cref="Key"/> that is being invoked. This is the key that was pressed to invoke the <see cref="Command"/>.
    /// </summary>
    public Key? Key { get; set; }

    /// <summary>
    /// The KeyBinding that was used to invoke the <see cref="Command"/>, if any.
    /// </summary>
    public KeyBinding? KeyBinding { get; set; }

    /// <summary>
    ///     Arbitrary data.
    /// </summary>
    public object? Data { get; set; }
}
