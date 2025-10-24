#nullable enable
using System.ComponentModel;

namespace Terminal.Gui.App;

/// <summary>
///     INTERNAL: Implements <see cref="IMouse"/> to manage mouse event handling and state.
///     <para>
///         This class holds all mouse-related state that was previously in the static <see cref="Application"/> class,
///         enabling better testability and parallel test execution.
///     </para>
/// </summary>
internal class Mouse : IMouse
{
    private readonly IMouseGrabHandler _mouseGrabHandler;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Mouse"/> class.
    /// </summary>
    /// <param name="mouseGrabHandler">The mouse grab handler to use for this instance.</param>
    public Mouse (IMouseGrabHandler mouseGrabHandler)
    {
        _mouseGrabHandler = mouseGrabHandler ?? throw new ArgumentNullException (nameof (mouseGrabHandler));
    }

    /// <inheritdoc/>
    public Point? LastMousePosition { get; set; }

    /// <inheritdoc/>
    public Point? GetLastMousePosition () { return LastMousePosition; }

    /// <inheritdoc/>
    public bool IsMouseDisabled { get; set; }

    /// <inheritdoc/>
    public List<View?> CachedViewsUnderMouse { get; } = [];

    /// <inheritdoc/>
    public event EventHandler<MouseEventArgs>? MouseEvent;

    /// <inheritdoc/>
    public void RaiseMouseEvent (MouseEventArgs mouseEvent)
    {
        if (Application.Initialized)
        {
            // LastMousePosition is only set if the application is initialized.
            LastMousePosition = mouseEvent.ScreenPosition;
        }

        if (IsMouseDisabled)
        {
            return;
        }

        // The position of the mouse is the same as the screen position at the application level.
        //Debug.Assert (mouseEvent.Position == mouseEvent.ScreenPosition);
        mouseEvent.Position = mouseEvent.ScreenPosition;

        List<View?> currentViewsUnderMouse = View.GetViewsUnderLocation (mouseEvent.ScreenPosition, ViewportSettingsFlags.TransparentMouse);

        View? deepestViewUnderMouse = currentViewsUnderMouse.LastOrDefault ();

        if (deepestViewUnderMouse is { })
        {
#if DEBUG_IDISPOSABLE
            if (View.EnableDebugIDisposableAsserts && deepestViewUnderMouse.WasDisposed)
            {
                throw new ObjectDisposedException (deepestViewUnderMouse.GetType ().FullName);
            }
#endif
            mouseEvent.View = deepestViewUnderMouse;
        }

        MouseEvent?.Invoke (null, mouseEvent);

        if (mouseEvent.Handled)
        {
            return;
        }

        // Dismiss the Popover if the user presses mouse outside of it
        if (mouseEvent.IsPressed
            && Application.Popover?.GetActivePopover () as View is { Visible: true } visiblePopover
            && View.IsInHierarchy (visiblePopover, deepestViewUnderMouse, includeAdornments: true) is false)
        {
            ApplicationPopover.HideWithQuitCommand (visiblePopover);

            // Recurse once so the event can be handled below the popover
            RaiseMouseEvent (mouseEvent);

            return;
        }

        if (HandleMouseGrab (deepestViewUnderMouse, mouseEvent))
        {
            return;
        }

        // May be null before the prior condition or the condition may set it as null.
        // So, the checking must be outside the prior condition.
        if (deepestViewUnderMouse is null)
        {
            return;
        }

        // if the mouse is outside the Application.Top or Application.Popover hierarchy, we don't want to
        // send the mouse event to the deepest view under the mouse.
        if (!View.IsInHierarchy (Application.Top, deepestViewUnderMouse, true) && !View.IsInHierarchy (Application.Popover?.GetActivePopover () as View, deepestViewUnderMouse, true))
        {
            return;
        }

        // Create a view-relative mouse event to send to the view that is under the mouse.
        MouseEventArgs viewMouseEvent;

        if (deepestViewUnderMouse is Adornment adornment)
        {
            Point frameLoc = adornment.ScreenToFrame (mouseEvent.ScreenPosition);

            viewMouseEvent = new ()
            {
                Position = frameLoc,
                Flags = mouseEvent.Flags,
                ScreenPosition = mouseEvent.ScreenPosition,
                View = deepestViewUnderMouse
            };
        }
        else if (deepestViewUnderMouse.ViewportToScreen (Rectangle.Empty with { Size = deepestViewUnderMouse.Viewport.Size }).Contains (mouseEvent.ScreenPosition))
        {
            Point viewportLocation = deepestViewUnderMouse.ScreenToViewport (mouseEvent.ScreenPosition);

            viewMouseEvent = new ()
            {
                Position = viewportLocation,
                Flags = mouseEvent.Flags,
                ScreenPosition = mouseEvent.ScreenPosition,
                View = deepestViewUnderMouse
            };
        }
        else
        {
            // The mouse was outside any View's Viewport.
            // Debug.Fail ("This should never happen. If it does please file an Issue!!");

            return;
        }

        RaiseMouseEnterLeaveEvents (viewMouseEvent.ScreenPosition, currentViewsUnderMouse);

        while (deepestViewUnderMouse.NewMouseEvent (viewMouseEvent) is not true && _mouseGrabHandler.MouseGrabView is not { })
        {
            if (deepestViewUnderMouse is Adornment adornmentView)
            {
                deepestViewUnderMouse = adornmentView.Parent?.SuperView;
            }
            else
            {
                deepestViewUnderMouse = deepestViewUnderMouse.SuperView;
            }

            if (deepestViewUnderMouse is null)
            {
                break;
            }

            Point boundsPoint = deepestViewUnderMouse.ScreenToViewport (mouseEvent.ScreenPosition);

            viewMouseEvent = new ()
            {
                Position = boundsPoint,
                Flags = mouseEvent.Flags,
                ScreenPosition = mouseEvent.ScreenPosition,
                View = deepestViewUnderMouse
            };
        }
    }

    /// <inheritdoc/>
    public bool HandleMouseGrab (View? deepestViewUnderMouse, MouseEventArgs mouseEvent)
    {
        if (_mouseGrabHandler.MouseGrabView is { })
        {
#if DEBUG_IDISPOSABLE
            if (View.EnableDebugIDisposableAsserts && _mouseGrabHandler.MouseGrabView.WasDisposed)
            {
                throw new ObjectDisposedException (_mouseGrabHandler.MouseGrabView.GetType ().FullName);
            }
#endif

            // If the mouse is grabbed, send the event to the view that grabbed it.
            // The coordinates are relative to the Bounds of the view that grabbed the mouse.
            Point frameLoc = _mouseGrabHandler.MouseGrabView.ScreenToViewport (mouseEvent.ScreenPosition);

            var viewRelativeMouseEvent = new MouseEventArgs
            {
                Position = frameLoc,
                Flags = mouseEvent.Flags,
                ScreenPosition = mouseEvent.ScreenPosition,
                View = deepestViewUnderMouse ?? _mouseGrabHandler.MouseGrabView
            };

            //System.Diagnostics.Debug.WriteLine ($"{nme.Flags};{nme.X};{nme.Y};{mouseGrabView}");
            if (_mouseGrabHandler.MouseGrabView?.NewMouseEvent (viewRelativeMouseEvent) is true)
            {
                return true;
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (_mouseGrabHandler.MouseGrabView is null && deepestViewUnderMouse is Adornment)
            {
                // The view that grabbed the mouse has been disposed
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public void RaiseMouseEnterLeaveEvents (Point screenPosition, List<View?> currentViewsUnderMouse)
    {
        // Tell any views that are no longer under the mouse that the mouse has left
        List<View?> viewsToLeave = CachedViewsUnderMouse.Where (v => v is { } && !currentViewsUnderMouse.Contains (v)).ToList ();

        foreach (View? view in viewsToLeave)
        {
            if (view is null)
            {
                continue;
            }

            view.NewMouseLeaveEvent ();
            CachedViewsUnderMouse.Remove (view);
        }

        // Tell any views that are now under the mouse that the mouse has entered and add them to the list
        foreach (View? view in currentViewsUnderMouse)
        {
            if (view is null)
            {
                continue;
            }

            if (CachedViewsUnderMouse.Contains (view))
            {
                continue;
            }

            CachedViewsUnderMouse.Add (view);
            var raise = false;

            if (view is Adornment { Parent: { } } adornmentView)
            {
                Point superViewLoc = adornmentView.Parent.SuperView?.ScreenToViewport (screenPosition) ?? screenPosition;
                raise = adornmentView.Contains (superViewLoc);
            }
            else
            {
                Point superViewLoc = view.SuperView?.ScreenToViewport (screenPosition) ?? screenPosition;
                raise = view.Contains (superViewLoc);
            }

            if (!raise)
            {
                continue;
            }

            CancelEventArgs eventArgs = new System.ComponentModel.CancelEventArgs ();
            bool? cancelled = view.NewMouseEnterEvent (eventArgs);

            if (cancelled is true || eventArgs.Cancel)
            {
                break;
            }
        }
    }

    /// <inheritdoc/>
    public void ResetState ()
    {
        // Do not clear LastMousePosition; Popover's require it to stay set with last mouse pos.
        CachedViewsUnderMouse.Clear ();
        MouseEvent = null;
    }
}
