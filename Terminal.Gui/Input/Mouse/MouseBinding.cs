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
    /// <param name="mouseFlags">The mouse flags that triggered this binding.</param>
    public MouseBinding (Command [] commands, MouseFlags mouseFlags)
    {
        Commands = commands;

        MouseEvent = new Mouse { Timestamp = DateTime.Now, Flags = mouseFlags };
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="commands">The commands this mouse binding will invoke.</param>
    /// <param name="args">The mouse event that triggered this binding.</param>
    public MouseBinding (Command [] commands, Mouse args)
    {
        Commands = commands;
        MouseEvent = args;
    }

    /// <summary>The commands this binding will invoke.</summary>
    public Command [] Commands { get; set; }

    /// <inheritdoc/>
    public object? Data { get; set; }

    /// <summary>
    ///     The mouse event data associated with this binding.
    /// </summary>
    public Mouse? MouseEvent { get; set; }

    /// <inheritdoc/>
    public View? Source { get; set; }

    /// <inheritdoc />
    public override string ToString ()
    {
        return $"[{string.Join (", ", Commands)}], MouseEvent={MouseEvent}, Source={Source}, Data={Data}";
    }
}
