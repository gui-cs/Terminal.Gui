namespace Terminal.Gui.App;

/// <summary>
///     Defines a contract for managing keyboard input and key bindings at the Application level.
///     <para>
///         This interface decouples keyboard handling state from the static <see cref="App"/> class,
///         enabling parallelizable unit tests and better testability.
///     </para>
/// </summary>
public interface IKeyboard
{
    /// <summary>
    /// Sets the application instance that this keyboard handler is associated with.
    /// This provides access to application state without coupling to static Application class.
    /// </summary>
    IApplication? App { get; set; }

    /// <summary>
    ///     Called when the user presses a key (by the <see cref="IDriver"/>). Raises the cancelable
    ///     <see cref="KeyDown"/> event, then calls <see cref="View.NewKeyDownEvent"/> on all top level views, and finally
    ///     if the key was not handled, invokes any Application-scoped <see cref="KeyBindings"/>.
    /// </summary>
    /// <remarks>Can be used to simulate key press events.</remarks>
    /// <param name="key"></param>
    /// <returns><see langword="true"/> if the key was handled.</returns>
    bool RaiseKeyDownEvent (Key key);

    /// <summary>
    ///     Called when the user releases a key (by the <see cref="IDriver"/>). Raises the cancelable
    ///     <see cref="KeyUp"/>
    ///     event
    ///     then calls <see cref="View.NewKeyUpEvent"/> on all top level views. Called after <see cref="RaiseKeyDownEvent"/>.
    /// </summary>
    /// <remarks>Can be used to simulate key release events.</remarks>
    /// <param name="key"></param>
    /// <returns><see langword="true"/> if the key was handled.</returns>
    bool RaiseKeyUpEvent (Key key);

    /// <summary>
    ///     Invokes any commands bound at the Application-level to <paramref name="key"/>.
    /// </summary>
    /// <param name="key"></param>
    /// <returns>
    ///     <see langword="null"/> if no command was found; input processing should continue.
    ///     <see langword="false"/> if the command was invoked and was not handled (or cancelled); input processing should continue.
    ///     <see langword="true"/> if the command was invoked the command was handled (or cancelled); input processing should stop.
    /// </returns>
    bool? InvokeCommandsBoundToKey (Key key);

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
    bool? InvokeCommand (Command command, Key key, KeyBinding binding);

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
    event EventHandler<Key>? KeyDown;

    /// <summary>
    ///     Raised when the user releases a key.
    ///     <para>
    ///         Set <see cref="Key.Handled"/> to <see langword="true"/> to indicate the key was handled and to prevent
    ///         additional processing.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     All drivers support firing the <see cref="KeyDown"/> event. Some drivers (Unix) do not support firing the
    ///     <see cref="KeyDown"/> and <see cref="KeyUp"/> events.
    ///     <para>Fired after <see cref="KeyDown"/>.</para>
    /// </remarks>
    event EventHandler<Key>? KeyUp;

    /// <summary>Gets the Application-scoped key bindings.</summary>
    KeyBindings KeyBindings { get; }

    /// <summary>Gets or sets the key to quit the application.</summary>
    Key QuitKey { get; set; }

    /// <summary>Gets or sets the key to activate arranging views using the keyboard.</summary>
    Key ArrangeKey { get; set; }

    /// <summary>Alternative key to navigate forwards through views. Ctrl+Tab is the primary key.</summary>
    Key NextTabGroupKey { get; set; }

    /// <summary>Alternative key to navigate forwards through views. Tab is the primary key.</summary>
    Key NextTabKey { get; set; }

    /// <summary>Alternative key to navigate backwards through views. Shift+Ctrl+Tab is the primary key.</summary>
    Key PrevTabGroupKey { get; set; }

    /// <summary>Alternative key to navigate backwards through views. Shift+Tab is the primary key.</summary>
    Key PrevTabKey { get; set; }
}
