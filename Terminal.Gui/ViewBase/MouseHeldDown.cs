#nullable enable
using System.ComponentModel;

namespace Terminal.Gui.ViewBase;

/// <summary>
///     INTERNAL: Manages the logic for handling a "mouse held down" state on a View. It is used to
///     repeatedly trigger an action (via events) while the mouse button is held down, such as for auto-repeat in
///     scrollbars or buttons.
/// </summary>
internal class MouseHeldDown : IMouseHeldDown
{
    public MouseHeldDown (View host, ITimedEvents? timedEvents, IMouseGrabHandler? mouseGrabber)
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

    public event EventHandler<CancelEventArgs>? MouseIsHeldDownTick;

    public void Start ()
    {
        if (_isDown)
        {
            return;
        }

        _isDown = true;
        _mouseGrabber?.GrabMouse (_mouseGrabView);

        // Then periodic ticks
        _timeout = _timedEvents?.Add (_smoothTimeout);
    }

    public void Stop ()
    {
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
            Stop ();
        }
    }

    protected virtual bool OnMouseIsHeldDownTick (CancelEventArgs eventArgs) { return false; }

    private bool RaiseMouseIsHeldDownTick ()
    {
        CancelEventArgs args = new ();

        args.Cancel = OnMouseIsHeldDownTick (args) || args.Cancel;

        if (!args.Cancel && MouseIsHeldDownTick is { })
        {
            MouseIsHeldDownTick?.Invoke (this, args);
        }

        // User event cancelled the mouse held down status so
        // stop the currently running operation.
        if (args.Cancel)
        {
            Stop ();
        }

        return args.Cancel;
    }

    private bool TickWhileMouseIsHeldDown ()
    {
        Logging.Debug ("Raising TickWhileMouseIsHeldDown...");

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
