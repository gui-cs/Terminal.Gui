#nullable enable
namespace Terminal.Gui.App;

/// <summary>
///     INTERNAL: Implements <see cref="IMouseGrabHandler"/> to manage which <see cref="View"/> (if any) has 'grabbed' the mouse,
///     giving it exclusive priority for mouse events such as movement, button presses, and release.
///     <para>
///         Used for scenarios like dragging, scrolling, or any interaction where a view needs to receive all mouse events
///         until the operation completes (e.g., a scrollbar thumb being dragged).
///     </para>
///     <para>
///         See <see cref="IMouseGrabHandler"/> for usage details.
///     </para>
/// </summary>
internal class MouseGrabHandler : IMouseGrabHandler
{
    /// <inheritdoc/>
    public View? MouseGrabView { get; private set; }

    /// <inheritdoc/>
    public event EventHandler<GrabMouseEventArgs>? GrabbingMouse;

    /// <inheritdoc/>
    public event EventHandler<GrabMouseEventArgs>? UnGrabbingMouse;

    /// <inheritdoc/>
    public event EventHandler<ViewEventArgs>? GrabbedMouse;

    /// <inheritdoc/>
    public event EventHandler<ViewEventArgs>? UnGrabbedMouse;

    /// <inheritdoc/>
    public void GrabMouse (View? view)
    {
        if (view is null || RaiseGrabbingMouseEvent (view))
        {
            return;
        }

        RaiseGrabbedMouseEvent (view);

        // MouseGrabView is a static; only set if the application is initialized.
        MouseGrabView = view;
    }

    /// <inheritdoc/>
    public void UngrabMouse ()
    {
        if (MouseGrabView is null)
        {
            return;
        }

#if DEBUG_IDISPOSABLE
        if (View.EnableDebugIDisposableAsserts)
        {
            ObjectDisposedException.ThrowIf (MouseGrabView.WasDisposed, MouseGrabView);
        }
#endif

        if (!RaiseUnGrabbingMouseEvent (MouseGrabView))
        {
            View view = MouseGrabView;
            MouseGrabView = null;
            RaiseUnGrabbedMouseEvent (view);
        }
    }

    /// <exception cref="Exception">A delegate callback throws an exception.</exception>
    private bool RaiseGrabbingMouseEvent (View? view)
    {
        if (view is null)
        {
            return false;
        }

        var evArgs = new GrabMouseEventArgs (view);
        GrabbingMouse?.Invoke (view, evArgs);

        return evArgs.Cancel;
    }

    /// <exception cref="Exception">A delegate callback throws an exception.</exception>
    private bool RaiseUnGrabbingMouseEvent (View? view)
    {
        if (view is null)
        {
            return false;
        }

        var evArgs = new GrabMouseEventArgs (view);
        UnGrabbingMouse?.Invoke (view, evArgs);

        return evArgs.Cancel;
    }

    /// <exception cref="Exception">A delegate callback throws an exception.</exception>
    private void RaiseGrabbedMouseEvent (View? view)
    {
        if (view is null)
        {
            return;
        }

        GrabbedMouse?.Invoke (view, new (view));
    }

    /// <exception cref="Exception">A delegate callback throws an exception.</exception>
    private void RaiseUnGrabbedMouseEvent (View? view)
    {
        if (view is null)
        {
            return;
        }

        UnGrabbedMouse?.Invoke (view, new (view));
    }
}
