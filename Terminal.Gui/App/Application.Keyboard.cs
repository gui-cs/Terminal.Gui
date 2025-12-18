namespace Terminal.Gui.App;

public static partial class Application // Keyboard handling
{
    /// <inheritdoc cref="IApplication.Keyboard"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static IKeyboard Keyboard
    {
        get => ApplicationImpl.Instance.Keyboard;
        set => ApplicationImpl.Instance.Keyboard = value ??
                                                           throw new ArgumentNullException(nameof(value));
    }

    /// <inheritdoc cref="IKeyboard.RaiseKeyDownEvent"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static bool RaiseKeyDownEvent (Key key) => ApplicationImpl.Instance.Keyboard.RaiseKeyDownEvent (key);

    /// <summary>
    ///     Raised when the user presses a key.
    ///     <para>
    ///         Set <see cref="Key.Handled"/> to <see langword="true"/> to indicate the key was handled and to prevent
    ///         additional processing.
    ///     </para>
    /// </summary>
    [Obsolete ("The legacy static Application object is going away.")]
    public static event EventHandler<Key>? KeyDown
    {
        add => ApplicationImpl.Instance.Keyboard.KeyDown += value;
        remove => ApplicationImpl.Instance.Keyboard.KeyDown -= value;
    }

    /// <inheritdoc cref="IKeyboard.KeyBindings"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static KeyBindings KeyBindings => ApplicationImpl.Instance.Keyboard.KeyBindings;
}
