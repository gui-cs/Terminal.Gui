namespace Terminal.Gui.Input;

/// <summary>
///     A generic input binding used for programmatic command invocations
///     or when a specific binding type is not needed.
/// </summary>
/// <remarks>
///     <para>
///         Use <see cref="InputBinding"/> when invoking commands programmatically via
///         <see cref="View.InvokeCommand(Command)"/> or when a binding is needed
///         but is not associated with a specific key or mouse event.
///     </para>
///     <para>
///         Pattern match on binding types to discriminate between input sources:
///         <code>
///         if (ctx.Binding is KeyBinding kb) { /* key input */ }
///         else if (ctx.Binding is MouseBinding mb) { /* mouse input */ }
///         else if (ctx.Binding is InputBinding ib) { /* programmatic */ }
///         </code>
///     </para>
/// </remarks>
/// <seealso cref="KeyBinding"/>
/// <seealso cref="MouseBinding"/>
/// <seealso cref="IInputBinding"/>
public record struct InputBinding : IInputBinding
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="commands">The commands this input binding will invoke.</param>
    /// <param name="source">The view that is the origin of this binding.</param>
    /// <param name="data">Arbitrary context data that can be associated with this binding.</param>
    public InputBinding (Command [] commands, View? source = null, object? data = null)
    {
        Commands = commands;
        Source = source;
        Data = data;
    }

    /// <summary>The commands this binding will invoke.</summary>
    public Command [] Commands { get; set; }

    /// <inheritdoc/>
    public object? Data { get; set; }

    /// <inheritdoc/>
    public View? Source { get; set; }
}
