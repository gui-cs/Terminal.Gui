#nullable enable

namespace Terminal.Gui.App;

public static partial class Application // Keyboard handling
{
    /// <summary>
    /// Static reference to the current <see cref="IApplication"/> <see cref="IKeyboard"/>.
    /// </summary>
    public static IKeyboard Keyboard
    {
        get => ApplicationImpl.Instance.Keyboard;
        set => ApplicationImpl.Instance.Keyboard = value ??
                                                           throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    ///     Called when the user presses a key (by the <see cref="IConsoleDriver"/>). Raises the cancelable
    ///     <see cref="KeyDown"/> event, then calls <see cref="View.NewKeyDownEvent"/> on all top level views, and finally
    ///     if the key was not handled, invokes any Application-scoped <see cref="KeyBindings"/>.
    /// </summary>
    /// <remarks>Can be used to simulate key press events.</remarks>
    /// <param name="key"></param>
    /// <returns><see langword="true"/> if the key was handled.</returns>
    public static bool RaiseKeyDownEvent (Key key) => Keyboard.RaiseKeyDownEvent (key);

    /// <summary>
    ///     Invokes any commands bound at the Application-level to <paramref name="key"/>.
    /// </summary>
    /// <param name="key"></param>
    /// <returns>
    ///     <see langword="null"/> if no command was found; input processing should continue.
    ///     <see langword="false"/> if the command was invoked and was not handled (or cancelled); input processing should continue.
    ///     <see langword="true"/> if the command was invoked the command was handled (or cancelled); input processing should stop.
    /// </returns>
    public static bool? InvokeCommandsBoundToKey (Key key) => Keyboard.InvokeCommandsBoundToKey (key);

    /// <summary>
    ///     Invokes an Application-bound command.
    /// </summary>
    /// <param name="command">The Command to invoke</param>
    /// <param name="key">The Application-bound Key that was pressed.</param>
    /// <param name="binding">Describes the binding.</param>
    /// <returns>
    ///     <see langword="null"/> if no command was found; input processing should continue.
    ///     <see langword="false"/> if the command was invoked and was not handled (or cancelled); input processing should continue.
    ///     <see langword="true"/> if the command was invoked the command was handled (or cancelled); input processing should stop.
    /// </returns>
    /// <exception cref="NotSupportedException"></exception>
    public static bool? InvokeCommand (Command command, Key key, KeyBinding binding) => Keyboard.InvokeCommand (command, key, binding);

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
        add => Keyboard.KeyDown += value;
        remove => Keyboard.KeyDown -= value;
    }

    /// <summary>
    ///     Called when the user releases a key (by the <see cref="IConsoleDriver"/>). Raises the cancelable
    ///     <see cref="KeyUp"/>
    ///     event
    ///     then calls <see cref="View.NewKeyUpEvent"/> on all top level views. Called after <see cref="RaiseKeyDownEvent"/>.
    /// </summary>
    /// <remarks>Can be used to simulate key release events.</remarks>
    /// <param name="key"></param>
    /// <returns><see langword="true"/> if the key was handled.</returns>
    public static bool RaiseKeyUpEvent (Key key) => Keyboard.RaiseKeyUpEvent (key);

    /// <summary>Gets the Application-scoped key bindings.</summary>
    public static KeyBindings KeyBindings => Keyboard.KeyBindings;

    internal static void AddKeyBindings ()
    {
        if (Keyboard is Keyboard keyboard)
        {
            keyboard.AddKeyBindings ();
        }
    }
}
