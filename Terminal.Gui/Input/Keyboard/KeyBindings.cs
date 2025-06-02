#nullable enable

namespace Terminal.Gui.Input;

/// <summary>
///     Provides a collection of <see cref="Key"/>s bound to <see cref="Command"/>s.
/// </summary>
/// <seealso cref="KeyBindings"/>
/// <seealso cref="KeyBindings"/>
/// <seealso cref="Command"/>
public class KeyBindings : InputBindings<Key, KeyBinding>
{
    /// <summary>Initializes a new instance bound to <paramref name="target"/>.</summary>
    public KeyBindings (View? target) : base ((commands, key) => new (commands), new KeyEqualityComparer ())
    {
        Target = target;
    }

    /// <inheritdoc />
    public override bool IsValid (Key eventArgs) { return eventArgs.IsValid; }

    /// <summary>
    ///     <para>
    ///         Adds a new key combination that will trigger the commands in <paramref name="commands"/> on the View
    ///         specified by <paramref name="target"/>.
    ///     </para>
    ///     <para>
    ///         If the key is already bound to a different array of <see cref="Command"/>s it will be rebound
    ///         <paramref name="commands"/>.
    ///     </para>
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="key">The key to check.</param>
    /// <param name="target">
    ///     The View the commands will be invoked on. If <see langword="null"/>, the key will be bound to
    ///     <see cref="Application"/>.
    /// </param>
    /// <param name="commands">
    ///     The command to invoked on the <see paramref="target"/> when <paramref name="key"/> is pressed. When
    ///     multiple commands are provided,they will be applied in sequence. The bound <paramref name="key"/> strike will be
    ///     consumed if any took effect.
    /// </param>
    public void Add (Key key, View? target, params Command [] commands)
    {
        KeyBinding binding = new (commands, target);
        Add (key, binding);
    }

    /// <summary>
    ///     The view that the <see cref="KeyBindings"/> are bound to.
    /// </summary>
    /// <remarks>
    ///     If <see langword="null"/> the KeyBindings object is being used for Application.KeyBindings.
    /// </remarks>
    public View? Target { get; init; }
}
