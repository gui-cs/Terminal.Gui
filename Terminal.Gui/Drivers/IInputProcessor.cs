namespace Terminal.Gui.Drivers;

/// <summary>
///     Processes queued input on the main loop thread, translating driver-specific input
///     into Terminal.Gui events and data models.
/// </summary>
public interface IInputProcessor
{
    #region Configuration and Core Processing

    /// <summary>
    ///     Gets the name of the driver associated with this input processor.
    /// </summary>
    string? DriverName { get; init; }

    /// <summary>
    ///     Drains the input queue, processing all available input. Must be called on the main loop thread.
    /// </summary>
    void ProcessQueue ();

    /// <summary>
    ///     Gets the ANSI response parser for handling escape sequences.
    /// </summary>
    /// <returns>The configured ANSI response parser instance.</returns>
    IAnsiResponseParser GetParser ();

    /// <summary>
    ///     Validates and processes Unicode surrogate pairs in the input stream.
    /// </summary>
    /// <param name="key">The key to validate.</param>
    /// <param name="result">The validated key or completed surrogate pair.</param>
    /// <returns><see langword="true"/> if the result is valid; <see langword="false"/> if more input is needed.</returns>
    bool IsValidInput (Key key, out Key result);

    #endregion

    #region Keyboard Events

    /// <summary>
    ///     Raises the <see cref="KeyDown"/> event after a key down event is dequeued.
    /// </summary>
    /// <param name="key">The key event data.</param>
    void RaiseKeyDownEvent (Key key);

    /// <summary>
    ///     Event raised when a key down event is dequeued. Precursor to <see cref="KeyUp"/>.
    /// </summary>
    event EventHandler<Key>? KeyDown;

    /// <summary>
    ///     Enqueues a key down event. For unit tests.
    /// </summary>
    /// <param name="key">The key to enqueue.</param>
    void EnqueueKeyDownEvent (Key key);

    /// <summary>
    ///     Raises the <see cref="KeyUp"/> event after a key up event is dequeued.
    /// </summary>
    /// <remarks>
    ///     Drivers that don't support key release will call this immediately after <see cref="RaiseKeyDownEvent"/>.
    /// </remarks>
    /// <param name="key">The key event data.</param>
    void RaiseKeyUpEvent (Key key);

    /// <summary>
    ///     Event raised when a key up event is dequeued.
    /// </summary>
    /// <remarks>
    ///     Drivers that don't support key release fire this immediately after <see cref="KeyDown"/>.
    /// </remarks>
    event EventHandler<Key>? KeyUp;

    /// <summary>
    ///     Enqueues a key up event. For unit tests.
    /// </summary>
    /// <param name="key">The key to enqueue.</param>
    void EnqueueKeyUpEvent (Key key);

    #endregion

    #region Mouse Events

    /// <summary>
    ///     Raises the <see cref="MouseEventParsed"/> event after a mouse event is parsed from the driver.
    /// </summary>
    /// <param name="mouseEvent">The parsed mouse event data.</param>
    void RaiseMouseEventParsed (Mouse mouseEvent);

    /// <summary>
    ///     Event raised when a mouse event is parsed from the driver. For debugging and unit tests.
    /// </summary>
    event EventHandler<Mouse>? MouseEventParsed;

    /// <summary>
    ///     Raises the <see cref="SyntheticMouseEvent"/> event for generated click/double-click/triple-click events.
    /// </summary>
    /// <remarks>
    ///     Called by <see cref="ProcessQueue"/> after processing raw mouse input through <see cref="MouseInterpreter"/>
    ///     to generate higher-level click events based on timing and position.
    /// </remarks>
    /// <param name="mouseEvent">The synthetic mouse event data.</param>
    void RaiseSyntheticMouseEvent (Mouse mouseEvent);

    /// <summary>
    ///     Event raised when synthetic mouse events (clicks, double-clicks, triple-clicks) are generated.
    /// </summary>
    event EventHandler<Mouse>? SyntheticMouseEvent;

    /// <summary>
    ///     Enqueues a mouse event. For unit tests.
    /// </summary>
    /// <param name="app">
    ///     Application instance for cross-thread marshalling. When called from non-main thread,
    ///     uses <see cref="IApplication.Invoke(Action)"/> to raise events on the main thread.
    /// </param>
    /// <param name="mouseEvent">The mouse event to enqueue.</param>
    void EnqueueMouseEvent (IApplication? app, Mouse mouseEvent);

    #endregion

    #region ANSI Sequence Handling

    /// <summary>
    ///     Event raised when an unrecognized ANSI escape sequence is ignored.
    /// </summary>
    event EventHandler<string>? AnsiSequenceSwallowed;

    #endregion
}
