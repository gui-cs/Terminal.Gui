#nullable enable
namespace Terminal.Gui.Input;

/// <summary>
///     Provides a collection of <see cref="MouseBinding"/> objects bound to a combination of <see cref="MouseFlags"/>.
/// </summary>
/// <seealso cref="View.MouseBindings"/>
/// <seealso cref="Command"/>
public class MouseBindings : InputBindings<MouseFlags, MouseBinding>
{
    /// <summary>
    ///     Initializes a new instance.
    /// </summary>
    public MouseBindings () : base (
                                    (commands, flags) => new (commands, flags),
                                    EqualityComparer<MouseFlags>.Default)
    { }

    /// <inheritdoc />
    public override bool IsValid (MouseFlags eventArgs) { return eventArgs != MouseFlags.None; }
}
