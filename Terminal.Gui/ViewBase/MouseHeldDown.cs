#nullable enable
using System.ComponentModel;

namespace Terminal.Gui.ViewBase;

internal class MouseHeldDown : IMouseHeldDown
{
    private readonly View _host;
    private bool _down;
    private object? _timeout;
    private readonly ITimedEvents? _timedEvents;
    private readonly IMouseGrabHandler? _mouseGrabber;

    private readonly SmoothAcceleratingTimeout _smoothTimeout;

    public MouseHeldDown (View host, ITimedEvents? timedEvents, IMouseGrabHandler? mouseGrabber)
    {
        _host = host;
        _timedEvents = timedEvents;
        _mouseGrabber = mouseGrabber;
        _smoothTimeout = new (TimeSpan.FromMilliseconds (500), TimeSpan.FromMilliseconds (50), 0.5, TickWhileMouseIsHeldDown);
    }

    public event EventHandler<CancelEventArgs>? MouseIsHeldDownTick;

    public bool RaiseMouseIsHeldDownTick ()
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

    protected virtual bool OnMouseIsHeldDownTick (CancelEventArgs eventArgs) { return false; }

    public void Start ()
    {
        if (_down)
        {
            return;
        }

        _down = true;
        _mouseGrabber?.GrabMouse (_host);


        // Then periodic ticks
        _timeout = _timedEvents?.Add (_smoothTimeout);
    }

    private bool TickWhileMouseIsHeldDown ()
    {
        Logging.Debug ("Raising TickWhileMouseIsHeldDown...");
        if (_down)
        {
            _smoothTimeout.AdvanceStage ();
            RaiseMouseIsHeldDownTick ();
        }
        else
        {
            _smoothTimeout.Reset ();
            Stop ();
        }

        return _down;
    }

    public void Stop ()
    {
        _smoothTimeout.Reset ();

        if (_mouseGrabber?.MouseGrabView == _host)
        {
            _mouseGrabber?.UngrabMouse ();
        }

        if (_timeout != null)
        {
            _timedEvents?.Remove (_timeout);
        }

        _down = false;
    }

    public void Dispose ()
    {
        if (_mouseGrabber?.MouseGrabView == _host)
        {
            Stop ();
        }
    }
}
