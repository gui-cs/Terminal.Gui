using System.ComponentModel;

namespace Terminal.Gui.App;

/// <summary>
///     INTERNAL: Implements <see cref="IMouse"/> to manage mouse event handling and state.
///     <para>
///         This class holds all mouse-related state that was previously in the static <see cref="App"/> class,
///         enabling better testability and parallel test execution.
///     </para>
/// </summary>
internal class MouseImpl : IMouse, IDisposable
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MouseImpl"/> class and subscribes to Application configuration
    ///     property events.
    /// </summary>
    public MouseImpl ()
    {
        // Subscribe to Application static property change events
        Application.IsMouseDisabledChanged += OnIsMouseDisabledChanged;
    }

    /// <inheritdoc/>
    public IApplication? App { get; set; }

    /// <inheritdoc/>
    public Point? LastMousePosition { get; set; }

    /// <inheritdoc/>
    public bool IsMouseDisabled { get; set; }

    // Event handler for Application static property changes
    private void OnIsMouseDisabledChanged (object? sender, ValueChangedEventArgs<bool> e) { IsMouseDisabled = e.NewValue; }

    /// <inheritdoc/>
    public List<View?> CachedViewsUnderMouse { get; } = [];

    /// <inheritdoc/>
    public event EventHandler<Mouse>? MouseEvent;

    /// <inheritdoc/>
    public void RaiseMouseEvent (Mouse mouseEvent)
    {
        //Debug.Assert (App.Application.MainThreadId == Thread.CurrentThread.ManagedThreadId);
        if (App?.Initialized is true)
        {
            // LastMousePosition is only set if the application is initialized.
            LastMousePosition = mouseEvent.ScreenPosition;
        }

        if (IsMouseDisabled)
        {
            return;
        }

        // The position of the mouse is the same as the screen position at the application level.
        //Debug.Assert (mouse.Position == mouse.ScreenPosition);
        mouseEvent.Position = mouseEvent.ScreenPosition;

        List<View?>? currentViewsUnderMouse = App?.TopRunnableView?.GetViewsUnderLocation (mouseEvent.ScreenPosition, ViewportSettingsFlags.TransparentMouse);

        View? deepestViewUnderMouse = currentViewsUnderMouse?.LastOrDefault ();

        if (deepestViewUnderMouse is { })
        {
#if DEBUG_IDISPOSABLE
            if (View.EnableDebugIDisposableAsserts && deepestViewUnderMouse.WasDisposed)
            {
                throw new ObjectDisposedException (deepestViewUnderMouse.ToDebugString ());
            }
#endif
            mouseEvent.View = deepestViewUnderMouse;
        }

        MouseEvent?.Invoke (this, mouseEvent);

        if (mouseEvent.Handled)
        {
            return;
        }

        // Dismiss the Popover if the user presses mouse outside of it
        if (mouseEvent.IsPressed
            && App?.Popover?.GetActivePopover () as View is { Visible: true } visiblePopover
            && View.IsInHierarchy (visiblePopover, deepestViewUnderMouse, true) is false)
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

        // if the mouse is outside the Application.TopRunnable or Popover hierarchy, we don't want to
        // send the mouse event to the deepest view under the mouse.
        if (!View.IsInHierarchy (App?.TopRunnableView, deepestViewUnderMouse, true)
            && !View.IsInHierarchy (App?.Popover?.GetActivePopover () as View, deepestViewUnderMouse, true))
        {
            return;
        }

        // Create a view-relative mouse event to send to the view that is under the mouse.
        Mouse viewMouseEvent;

        if (deepestViewUnderMouse is Adornment adornment)
        {
            Point frameLoc = adornment.ScreenToFrame (mouseEvent.ScreenPosition);

            viewMouseEvent = new ()
            {
                Timestamp = mouseEvent.Timestamp,
                Position = frameLoc,
                Flags = mouseEvent.Flags,
                ScreenPosition = mouseEvent.ScreenPosition,
                View = deepestViewUnderMouse
            };
        }
        else if (deepestViewUnderMouse.ViewportToScreen (Rectangle.Empty with { Size = deepestViewUnderMouse.Viewport.Size })
                                      .Contains (mouseEvent.ScreenPosition))
        {
            Point viewportLocation = deepestViewUnderMouse.ScreenToViewport (mouseEvent.ScreenPosition);

            viewMouseEvent = new ()
            {
                Timestamp = mouseEvent.Timestamp,
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

        if (currentViewsUnderMouse is { })
        {
            RaiseMouseEnterLeaveEvents (viewMouseEvent.ScreenPosition, currentViewsUnderMouse);
        }

        while (deepestViewUnderMouse.NewMouseEvent (viewMouseEvent) is not true && _mouseGrabViewRef is null)
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
                Timestamp = mouseEvent.Timestamp,
                Position = boundsPoint,
                Flags = mouseEvent.Flags,
                ScreenPosition = mouseEvent.ScreenPosition,
                View = deepestViewUnderMouse
            };
        }
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

            // If the mouse is grabbed by another view, don't send MouseEnter events to other views.
            // This prevents views from highlighting when the user drags the mouse over them while holding
            // a button down on a different view.
            if (_mouseGrabViewRef is not null && !IsGrabbed (view))
            {
                continue;
            }

            CachedViewsUnderMouse.Add (view);
            bool raise;

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

            var eventArgs = new CancelEventArgs ();
            bool? cancelled = view.NewMouseEnterEvent (eventArgs);

            if (cancelled is true || eventArgs.Cancel)
            {
                break;
            }
        }
    }

    #region IMouseGrabHandler Implementation

    private WeakReference<View>? _mouseGrabViewRef;

    /// <inheritdoc/>
    public bool IsGrabbed (View? view)
    {
        if (view is null || _mouseGrabViewRef is null)
        {
            return false;
        }

        return _mouseGrabViewRef.TryGetTarget (out View? grabbed) && ReferenceEquals (grabbed, view);
    }

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
        if (RaiseGrabbingMouseEvent (view))
        {
            return;
        }

        if (view is null)
        {
            UngrabMouse ();

            return;
        }

        RaiseGrabbedMouseEvent (view);

        // _mouseGrabViewRef is only set if the application is initialized.
        _mouseGrabViewRef = new (view);
    }

    /// <inheritdoc/>
    public void UngrabMouse ()
    {
        if (_mouseGrabViewRef is null || !_mouseGrabViewRef.TryGetTarget (out View? grabbedView))
        {
            return;
        }

        if (!RaiseUnGrabbingMouseEvent (grabbedView))
        {
            _mouseGrabViewRef = null;
            RaiseUnGrabbedMouseEvent (grabbedView);

            // After ungrabbing, immediately update enter/leave state for views under the current mouse position
            // This ensures that if the mouse was released over a different view, that view receives MouseEnter
            if (App?.Initialized is true && LastMousePosition is { } position)
            {
                List<View?>? currentViewsUnderMouse = App.TopRunnableView?.GetViewsUnderLocation (position, ViewportSettingsFlags.TransparentMouse);

                if (currentViewsUnderMouse is { })
                {
                    RaiseMouseEnterLeaveEvents (position, currentViewsUnderMouse);
                }
            }
        }
    }

    /// <exception cref="Exception">A delegate callback throws an exception.</exception>
    private bool RaiseGrabbingMouseEvent (View? view)
    {
        if (view is null)
        {
            return false;
        }

        GrabMouseEventArgs evArgs = new (view);
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

        GrabMouseEventArgs evArgs = new (view);
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

    /// <summary>
    ///     Handles mouse grab logic for a mouse event.
    /// </summary>
    /// <param name="deepestViewUnderMouse">The deepest view under the mouse.</param>
    /// <param name="mouse">The mouse event to handle.</param>
    /// <returns><see langword="true"/> if the event was handled by the grab handler; otherwise <see langword="false"/>.</returns>
    public bool HandleMouseGrab (View? deepestViewUnderMouse, Mouse mouse)
    {
        if (_mouseGrabViewRef?.TryGetTarget (out View? grabbed) is true)
        {
#if DEBUG_IDISPOSABLE
            if (View.EnableDebugIDisposableAsserts && grabbed.WasDisposed)
            {
                throw new ObjectDisposedException (grabbed.ToDebugString ());
            }
#endif

            // If the mouse is grabbed, send the event to the view that grabbed it.
            // The coordinates are relative to the Bounds of the view that grabbed the mouse.
            Point frameLoc = grabbed.ScreenToViewport (mouse.ScreenPosition);

            Mouse viewRelativeMouseEvent = new ()
            {
                Timestamp = mouse.Timestamp,
                Position = frameLoc,
                Flags = mouse.Flags,
                ScreenPosition = mouse.ScreenPosition,
                View = grabbed // Always set to the grab view. See Issue #4370
            };

            //System.Diagnostics.Debug.WriteLine ($"{nme.Flags};{nme.X};{nme.Y};{mouseGrabView}");
            grabbed.NewMouseEvent (viewRelativeMouseEvent);

            // When the mouse is grabbed, always return true to prevent the event from propagating
            // to other views, regardless of whether the grabbed view handled it or not.
            // This ensures that during a drag operation starting on one view, other views don't
            // receive Enter/Leave events or process the mouse event.
            return true;
        }

        return false;
    }

    #endregion IMouseGrabHandler Implementation

    /// <inheritdoc/>
    public void ResetState ()
    {
        // Do not clear LastMousePosition; Popovers require it to stay set with last mouse pos.
        CachedViewsUnderMouse.Clear ();
        MouseEvent = null;
        _mouseGrabViewRef = null;
    }

    /// <inheritdoc/>
    public void Dispose ()
    {
        ResetState ();

        // Unsubscribe from Application static property change events
        Application.IsMouseDisabledChanged -= OnIsMouseDisabledChanged;
    }
}
