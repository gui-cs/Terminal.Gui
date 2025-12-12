using System.ComponentModel;

namespace Terminal.Gui.ViewBase;

/// <summary>
///     INTERNAL: Manages the logic for handling a "mouse held down" state on a View. It is used to
///     repeatedly trigger an action (via events) while the mouse button is held down, such as for auto-repeat in
///     scrollbars or buttons.
/// </summary>
/// <remarks>
///     <para>
///         This class implements an accelerating timeout pattern: the first tick occurs after 500ms, 
///         subsequent ticks occur every 50ms with a 0.5 acceleration factor.
///     </para>
///     <para>
///         When started, it automatically grabs the mouse to ensure all mouse events are directed to the host view.
///         The <see cref="MouseIsHeldDownTick"/> event is raised periodically until <see cref="Stop"/> is called
///         or the event is cancelled.
///     </para>
///     <para>
///         This is typically used by views that set <see cref="View.MouseHoldRepeat"/> to <see langword="true"/>,
///         enabling behaviors like auto-scrolling or button repeat.
///     </para>
/// </remarks>
internal sealed class MouseHoldRepeaterImpl : IMouseHoldRepeater
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MouseHoldRepeaterImpl"/> class.
    /// </summary>
    /// <param name="host">The view that will receive the mouse held down events.</param>
    /// <param name="timedEvents">The timed events service for scheduling periodic ticks. Can be null for testing.</param>
    /// <param name="mouseGrabber">The mouse grab handler for managing mouse capture. Can be null for testing.</param>
    public MouseHoldRepeaterImpl (View host, ITimedEvents? timedEvents, IMouseGrabHandler? mouseGrabber)
    {
        _mouseGrabView = host;
        _timedEvents = timedEvents;
        _mouseGrabber = mouseGrabber;
        _smoothTimeout = new (TimeSpan.FromMilliseconds (500), TimeSpan.FromMilliseconds (50), 0.5, TickWhileMouseIsHeldDown);
    }

    private readonly View _mouseGrabView;
    private readonly ITimedEvents? _timedEvents;
    private readonly IMouseGrabHandler? _mouseGrabber;

    private readonly SmoothAcceleratingTimeout _smoothTimeout;
    private bool _isDown;
    private object? _timeout;

    /// <summary>
    /// The most recent mouse event arguments associated with the mouse held down action.
    /// </summary>
    private Mouse? _mouseEvent = null;

    public void Start (Mouse mouse)
    {
        if (_isDown)
        {
            return;
        }

        _mouseEvent = new ()
        {
            Timestamp = mouse.Timestamp,
            Flags = mouse.Flags,
            Position = mouse.Position,
            ScreenPosition = mouse.ScreenPosition,
            View = mouse.View
        };
        Logging.Trace ($"host: {_mouseGrabView.Id} {_mouseEvent.View?.Id}: {_mouseEvent.Flags}");

        _isDown = true;
        _mouseGrabber?.GrabMouse (_mouseGrabView);

        // Then periodic ticks
        _timeout = _timedEvents?.Add (_smoothTimeout);
    }

    public void Stop ()
    {
        if (_mouseEvent is null)
        {
            Logging.Trace ($"host: {_mouseGrabView.Id}");

            return;
        }
        Logging.Trace ($"host: {_mouseGrabView.Id} {_mouseEvent.View?.Id}: {_mouseEvent.Flags}");

        _mouseEvent = null;
        _smoothTimeout.Reset ();

        if (_mouseGrabber?.MouseGrabView == _mouseGrabView)
        {
            _mouseGrabber?.UngrabMouse ();
        }

        if (_timeout != null)
        {
            _timedEvents?.Remove (_timeout);
        }

        _mouseGrabView.MouseState = MouseState.None;
        _isDown = false;
    }

    public void Dispose ()
    {
        if (_mouseGrabber?.MouseGrabView == _mouseGrabView)
        {
            Logging.Trace ($"host: {_mouseGrabView.Id} Disposing and ungrabbing mouse");
            Stop ();
        }
    }

    public event EventHandler<CancelEventArgs<Mouse>>? MouseIsHeldDownTick;

    private bool RaiseMouseIsHeldDownTick ()
    {
        Mouse currentMouseEventArgs = _mouseEvent ?? new Mouse ();
        Mouse newMouseEventArgs = _mouseEvent ?? new Mouse ();
        CancelEventArgs<Mouse> args = new (currentValue: ref currentMouseEventArgs, newValue: ref newMouseEventArgs);

        MouseIsHeldDownTick?.Invoke (this, args);

        // User event cancelled the mouse held down status so
        // stop the currently running operation.
        if (args.Cancel)
        {
            Logging.Trace ($"host: {_mouseGrabView.Id} MouseIsHeldDownTick cancelled, stopping");
            Stop ();
        }

        return args.Cancel;
    }

    private bool TickWhileMouseIsHeldDown ()
    {
        if (_isDown)
        {
            _smoothTimeout.AdvanceStage ();
            RaiseMouseIsHeldDownTick ();
        }
        else
        {
            _smoothTimeout.Reset ();
            Stop ();
        }

        return _isDown;
    }
}
