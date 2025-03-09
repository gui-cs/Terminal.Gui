#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Interface for main loop class that will process the queued input buffer contents.
///     Is responsible for <see cref="ProcessQueue"/> and translating into common Terminal.Gui
///     events and data models.
/// </summary>
public interface IInputProcessor
{
    /// <summary>Event fired when a key is pressed down. This is a precursor to <see cref="KeyUp"/>.</summary>
    event EventHandler<Key>? KeyDown;

    /// <summary>Event fired when a key is released.</summary>
    /// <remarks>
    ///     Drivers that do not support key release events will fire this event after <see cref="KeyDown"/> processing is
    ///     complete.
    /// </remarks>
    event EventHandler<Key>? KeyUp;

    /// <summary>Event fired when a terminal sequence read from input is not recognized and therefore ignored.</summary>
    public event EventHandler<string>? AnsiSequenceSwallowed;

    /// <summary>Event fired when a mouse event occurs.</summary>
    event EventHandler<MouseEventArgs>? MouseEvent;

    /// <summary>
    ///     Called when a key is pressed down. Fires the <see cref="KeyDown"/> event. This is a precursor to
    ///     <see cref="OnKeyUp"/>.
    /// </summary>
    /// <param name="key">The key event data.</param>
    void OnKeyDown (Key key);

    /// <summary>
    ///     Called when a key is released. Fires the <see cref="KeyUp"/> event.
    /// </summary>
    /// <remarks>
    ///     Drivers that do not support key release events will call this method after <see cref="OnKeyDown"/> processing
    ///     is complete.
    /// </remarks>
    /// <param name="key">The key event data.</param>
    void OnKeyUp (Key key);

    /// <summary>
    ///     Called when a mouse event occurs. Fires the <see cref="MouseEvent"/> event.
    /// </summary>
    /// <param name="mouseEventArgs">The mouse event data.</param>
    void OnMouseEvent (MouseEventArgs mouseEventArgs);

    /// <summary>
    ///     Drains the input buffer, processing all available keystrokes
    /// </summary>
    void ProcessQueue ();

    /// <summary>
    ///     Gets the response parser currently configured on this input processor.
    /// </summary>
    /// <returns></returns>
    public IAnsiResponseParser GetParser ();
}
