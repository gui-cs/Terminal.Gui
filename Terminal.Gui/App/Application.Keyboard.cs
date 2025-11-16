namespace Terminal.Gui.App;

public static partial class Application // Keyboard handling
{
    /// <inheritdoc cref="IApplication.Keyboard"/>
    public static IKeyboard Keyboard
    {
        get => ApplicationImpl.Instance.Keyboard;
        set => ApplicationImpl.Instance.Keyboard = value ??
                                                           throw new ArgumentNullException(nameof(value));
    }

    /// <inheritdoc cref="IKeyboard.RaiseKeyDownEvent"/>
    public static bool RaiseKeyDownEvent (Key key) => ApplicationImpl.Instance.Keyboard.RaiseKeyDownEvent (key);

    /// <inheritdoc cref="IKeyboard.InvokeCommandsBoundToKey"/>
    public static bool? InvokeCommandsBoundToKey (Key key) => ApplicationImpl.Instance.Keyboard.InvokeCommandsBoundToKey (key);

    /// <inheritdoc cref="IKeyboard.InvokeCommand"/>
    public static bool? InvokeCommand (Command command, Key key, KeyBinding binding) => ApplicationImpl.Instance.Keyboard.InvokeCommand (command, key, binding);

    /// <summary>
    ///     Raised when the user presses a key.
    ///     <para>
    ///         Set <see cref="Key.Handled"/> to <see langword="true"/> to indicate the key was handled and to prevent
    ///         additional processing.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     All drivers support firing the <see cref="KeyDown"/> event. Some drivers (Unix) do not support firing the
    ///     <see cref="KeyDown"/> and <see cref="KeyUp"/> events.
    ///     <para>Fired after <see cref="KeyDown"/> and before <see cref="KeyUp"/>.</para>
    /// </remarks>
    public static event EventHandler<Key>? KeyDown
    {
        add => ApplicationImpl.Instance.Keyboard.KeyDown += value;
        remove => ApplicationImpl.Instance.Keyboard.KeyDown -= value;
    }

    /// <inheritdoc cref="IKeyboard.RaiseKeyUpEvent"/>
    public static bool RaiseKeyUpEvent (Key key) => ApplicationImpl.Instance.Keyboard.RaiseKeyUpEvent (key);

    /// <inheritdoc cref="IKeyboard.KeyBindings"/>
    public static KeyBindings KeyBindings => ApplicationImpl.Instance.Keyboard.KeyBindings;
}
