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

    /// <inheritdoc/>
    public bool HandleMouseGrab (View? deepestViewUnderMouse, MouseEventArgs mouseEvent)
    {
        if (MouseGrabView is { })
        {
#if DEBUG_IDISPOSABLE
            if (View.EnableDebugIDisposableAsserts && MouseGrabView.WasDisposed)
            {
                throw new ObjectDisposedException (MouseGrabView.GetType ().FullName);
            }
#endif

            // If the mouse is grabbed, send the event to the view that grabbed it.
            // The coordinates are relative to the Bounds of the view that grabbed the mouse.
            Point frameLoc = MouseGrabView.ScreenToViewport (mouseEvent.ScreenPosition);

            var viewRelativeMouseEvent = new MouseEventArgs
            {
                Position = frameLoc,
                Flags = mouseEvent.Flags,
                ScreenPosition = mouseEvent.ScreenPosition,
                View = deepestViewUnderMouse ?? MouseGrabView
            };

            //System.Diagnostics.Debug.WriteLine ($"{nme.Flags};{nme.X};{nme.Y};{mouseGrabView}");
            if (MouseGrabView?.NewMouseEvent (viewRelativeMouseEvent) is true)
            {
                return true;
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (MouseGrabView is null && deepestViewUnderMouse is Adornment)
            {
                // The view that grabbed the mouse has been disposed
                return true;
            }
        }

        return false;
    }
}
