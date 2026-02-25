namespace Terminal.Gui.Input;

#pragma warning disable CS1574, CS0419 // XML comment has cref attribute that could not be resolved
/// <summary>
///     Provides context for a <see cref="Command"/> invocation.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="CommandContext"/> is immutable. Use <see cref="WithCommand"/> or <see cref="WithRouting"/>
///         to create a new context with modified values while preserving all other fields.
///     </para>
///     <para>
///         Use pattern matching to access specific binding types:
///         <code>
///         if (ctx.Binding is KeyBinding kb) { /* key input */ }
///         else if (ctx.Binding is MouseBinding mb) { /* mouse input */ }
///         else if (ctx.Binding is CommandBinding ib) { /* programmatic */ }
///         </code>
///     </para>
/// </remarks>
/// <seealso cref="View.InvokeCommand"/>
/// .
#pragma warning restore CS1574, CS0419 // XML comment has cref attribute that could not be resolved
public readonly record struct CommandContext : ICommandContext
{
    /// <summary>
    ///     Initializes a new instance with the specified <see cref="Command"/>.
    /// </summary>
    /// <param name="command">The command being invoked.</param>
    /// <param name="source">A weak reference to the view that is the source of the command invocation.</param>
    /// <param name="binding">The binding that triggered the command, if any.</param>
    public CommandContext (Command command, WeakReference<View>? source, ICommandBinding? binding)
    {
        Command = command;
        Binding = binding;
        Source = source;
    }

    /// <inheritdoc/>
    public Command Command { get; init; }

    /// <inheritdoc/>
    public WeakReference<View>? Source { get; init; }

    /// <inheritdoc/>
    public ICommandBinding? Binding { get; init; }

    /// <inheritdoc/>
    public CommandRouting Routing { get; init; }

    /// <summary>
    ///     Creates a new context with a different command, preserving all other fields.
    /// </summary>
    /// <param name="command">The new command.</param>
    /// <returns>A new <see cref="CommandContext"/> with the specified command.</returns>
    public CommandContext WithCommand (Command command) => this with { Command = command };

    /// <summary>
    ///     Creates a new context with different routing, preserving all other fields.
    /// </summary>
    /// <param name="routing">The new routing mode.</param>
    /// <returns>A new <see cref="CommandContext"/> with the specified routing.</returns>
    public CommandContext WithRouting (CommandRouting routing) => this with { Routing = routing };

    /// <inheritdoc/>
    public override string ToString () => $"{(Routing == CommandRouting.BubblingUp ? Glyphs.UpArrow : Routing == CommandRouting.DispatchingDown ? Glyphs.DownArrow : "")}{Command} ({(Source is { } ? $"Source={Source.ToIdentifyingString ()}" : "")}{(Binding is { } ? $", Binding={Binding}" : "")})";
}
