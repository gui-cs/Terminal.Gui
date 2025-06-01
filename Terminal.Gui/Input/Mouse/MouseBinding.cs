#nullable enable

namespace Terminal.Gui.Input;

/// <summary>
///     Provides a collection of <see cref="MouseFlags"/> bound to <see cref="Command"/>s.
/// </summary>
/// <seealso cref="MouseBindings"/>
/// <seealso cref="Command"/>
public record struct MouseBinding : IInputBinding
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="commands">The commands this mouse binding will invoke.</param>
    /// <param name="mouseFlags">The mouse flags that trigger this binding.</param>
    public MouseBinding (Command [] commands, MouseFlags mouseFlags)
    {
        Commands = commands;

        MouseEventArgs = new MouseEventArgs()
        {
            Flags = mouseFlags
        };
    }

    /// <summary>The commands this binding will invoke.</summary>
    public Command [] Commands { get; set; }

    /// <inheritdoc />
    public object? Data { get; set; }

    /// <summary>
    ///     The mouse event arguments.
    /// </summary>
    public MouseEventArgs? MouseEventArgs { get; set; }
}
