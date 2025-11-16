#nullable enable
namespace Terminal.Gui.Input;

/// <summary>
///     Describes an input binding. Used to bind a set of <see cref="Command"/> objects to a specific input event.
/// </summary>
public interface IInputBinding
{
    /// <summary>
    ///     Gets or sets the commands this input binding will invoke.
    /// </summary>
    Command [] Commands { get; set; }

    /// <summary>
    ///     Arbitrary context that can be associated with this input binding.
    /// </summary>
    public object? Data { get; set; }

}
