namespace Terminal.Gui.Input;

#pragma warning disable CS1574, CS0419 // XML comment has cref attribute that could not be resolved
/// <summary>
///     Provides context for a <see cref="Command"/> invocation.
/// </summary>
/// <remarks>
///     <para>
///         Use pattern matching to access specific binding types:
///         <code>
///         if (ctx.Binding is KeyBinding kb) { /* key input */ }
///         else if (ctx.Binding is MouseBinding mb) { /* mouse input */ }
///         else if (ctx.Binding is InputBinding ib) { /* programmatic */ }
///         </code>
///     </para>
/// </remarks>
/// <seealso cref="View.InvokeCommand"/>.
#pragma warning restore CS1574, CS0419 // XML comment has cref attribute that could not be resolved
public record struct CommandContext : ICommandContext
{
    /// <summary>
    ///     Initializes a new instance with the specified <see cref="Command"/>.
    /// </summary>
    /// <param name="command">The command being invoked.</param>
    /// <param name="source">A weak reference to the view that is the source of the command invocation.</param>
    /// <param name="binding">The binding that triggered the command, if any.</param>
    public CommandContext (Command command, WeakReference<View>? source, IInputBinding? binding)
    {
        Command = command;
        Binding = binding;
        Source = source;
    }

    /// <inheritdoc />
    public Command Command { get; set; }

    /// <inheritdoc />
    public WeakReference<View>? Source { get; set; }

    /// <inheritdoc />
    public IInputBinding? Binding { get; set; }

    /// <inheritdoc />
    public override string ToString () => $"{Command} (Source={Source.ToIdentifyingString ()}, Binding={Binding})";
}
