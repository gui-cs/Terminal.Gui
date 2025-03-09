#nullable enable
namespace Terminal.Gui;

#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
/// <summary>
///     Provides context for a <see cref="Command"/> invocation.
/// </summary>
/// <seealso cref="View.InvokeCommand"/>.
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
public record struct CommandContext<TBinding> : ICommandContext
{
    /// <summary>
    ///     Initializes a new instance with the specified <see cref="Command"/>,
    /// </summary>
    /// <param name="command"></param>
    /// <param name="binding"></param>
    public CommandContext (Command command, TBinding? binding)
    {
        Command = command;
        Binding = binding;
    }

    /// <inheritdoc />
    public Command Command { get; set; }

    /// <summary>
    /// The keyboard or mouse minding that was used to invoke the <see cref="Command"/>, if any.
    /// </summary>
    public TBinding? Binding { get; set; }
}