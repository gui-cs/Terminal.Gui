#nullable disable
﻿#nullable enable

namespace Terminal.Gui.Drivers;

/// <summary>
///     Interface for main loop class that will process the queued input.
///     Is responsible for <see cref="ProcessQueue"/> and translating into common Terminal.Gui
///     events and data models.
/// </summary>
public interface IInputProcessor
{
    /// <summary>Event raised when a terminal sequence read from input is not recognized and therefore ignored.</summary>
    public event EventHandler<string>? AnsiSequenceSwallowed;

    /// <summary>
    /// Gets the name of the driver associated with this input processor.
    /// </summary>
    string? DriverName { get; init; }

    /// <summary>
    ///     Drains the input queue, processing all available keystrokes. To be called on the main loop thread.
    /// </summary>
    void ProcessQueue ();

    /// <summary>
    ///     Gets the response parser currently configured on this input processor.
    /// </summary>
    /// <returns></returns>
    public IAnsiResponseParser GetParser ();

    /// <summary>
    ///     Handles surrogate pairs in the input stream.
    /// </summary>
    /// <param name="key">The key from input.</param>
    /// <param name="result">Get the surrogate pair or the key.</param>
    /// <returns>
    ///     <see langword="true"/> if the result is a valid surrogate pair or a valid key, otherwise
    ///     <see langword="false"/>.
    /// </returns>
    bool IsValidInput (Key key, out Key result);

    /// <summary>
    ///     Called when a key down event has been dequeued. Raises the <see cref="KeyDown"/> event. This is a precursor to
    ///     <see cref="RaiseKeyUpEvent"/>.
    /// </summary>
    /// <param name="key">The key event data.</param>
    void RaiseKeyDownEvent (Key key);

    /// <summary>Event raised when a key down event has been dequeued. This is a precursor to <see cref="KeyUp"/>.</summary>
    event EventHandler<Key>? KeyDown;

    /// <summary>
    ///     Adds a key up event to the input queue. For unit tests.
    /// </summary>
    /// <param name="key"></param>
    void EnqueueKeyDownEvent (Key key);

    /// <summary>
    ///     Called when a key up event has been dequeued. Raises the <see cref="KeyUp"/> event.
    /// </summary>
    /// <remarks>
    ///     Drivers that do not support key release events will call this method after <see cref="RaiseKeyDownEvent"/> processing
    ///     is complete.
    /// </remarks>
    /// <param name="key">The key event data.</param>
    void RaiseKeyUpEvent (Key key);

    /// <summary>Event raised when a key up event has been dequeued.</summary>
    /// <remarks>
    ///     Drivers that do not support key release events will fire this event after <see cref="KeyDown"/> processing is
    ///     complete.
    /// </remarks>
    event EventHandler<Key>? KeyUp;

    /// <summary>
    ///     Adds a key up event to the input queue. For unit tests.
    /// </summary>
    /// <param name="key"></param>
    void EnqueueKeyUpEvent (Key key);

    /// <summary>
    ///     Called when a mouse event has been dequeued. Raises the <see cref="MouseEvent"/> event.
    /// </summary>
    /// <param name="mouseEventArgs">The mouse event data.</param>
    void RaiseMouseEvent (MouseEventArgs mouseEventArgs);

    /// <summary>Event raised when a mouse event has been dequeued.</summary>
    event EventHandler<MouseEventArgs>? MouseEvent;

    /// <summary>
    ///     Adds a mouse input event to the input queue. For unit tests.
    /// </summary>
    /// <param name="mouseEvent"></param>
    void EnqueueMouseEvent (MouseEventArgs mouseEvent);

}
