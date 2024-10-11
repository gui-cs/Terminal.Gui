#nullable enable
using System.ComponentModel;
using System.Diagnostics;

namespace Terminal.Gui;

public static partial class Application // Mouse handling
{
    internal static Point? _lastMousePosition;

    /// <summary>
    ///     Gets the most recent position of the mouse.
    /// </summary>
    public static Point? GetLastMousePosition () { return _lastMousePosition; }

    /// <summary>Disable or enable the mouse. The mouse is enabled by default.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    public static bool IsMouseDisabled { get; set; }

    /// <summary>The current <see cref="View"/> object that wants continuous mouse button pressed events.</summary>
    public static View? WantContinuousButtonPressedView { get; private set; }

    /// <summary>
    ///     Gets the view that grabbed the mouse (e.g. for dragging). When this is set, all mouse events will be routed to
    ///     this view until the view calls <see cref="UngrabMouse"/> or the mouse is released.
    /// </summary>
    public static View? MouseGrabView { get; private set; }

    /// <summary>Invoked when a view wants to grab the mouse; can be canceled.</summary>
    public static event EventHandler<GrabMouseEventArgs>? GrabbingMouse;

    /// <summary>Invoked when a view wants un-grab the mouse; can be canceled.</summary>
    public static event EventHandler<GrabMouseEventArgs>? UnGrabbingMouse;

    /// <summary>Invoked after a view has grabbed the mouse.</summary>
    public static event EventHandler<ViewEventArgs>? GrabbedMouse;

    /// <summary>Invoked after a view has un-grabbed the mouse.</summary>
    public static event EventHandler<ViewEventArgs>? UnGrabbedMouse;

    /// <summary>
    ///     Grabs the mouse, forcing all mouse events to be routed to the specified view until <see cref="UngrabMouse"/>
    ///     is called.
    /// </summary>
    /// <param name="view">View that will receive all mouse events until <see cref="UngrabMouse"/> is invoked.</param>
    public static void GrabMouse (View? view)
    {
        if (view is null || OnGrabbingMouse (view))
        {
            return;
        }

        OnGrabbedMouse (view);
        MouseGrabView = view;
    }

    /// <summary>Releases the mouse grab, so mouse events will be routed to the view on which the mouse is.</summary>
    public static void UngrabMouse ()
    {
        if (MouseGrabView is null)
        {
            return;
        }

#if DEBUG_IDISPOSABLE
        ObjectDisposedException.ThrowIf (MouseGrabView.WasDisposed, MouseGrabView);
#endif

        if (!OnUnGrabbingMouse (MouseGrabView))
        {
            View view = MouseGrabView;
            MouseGrabView = null;
            OnUnGrabbedMouse (view);
        }
    }

    /// <exception cref="Exception">A delegate callback throws an exception.</exception>
    private static bool OnGrabbingMouse (View? view)
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
    private static bool OnUnGrabbingMouse (View? view)
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
    private static void OnGrabbedMouse (View? view)
    {
        if (view is null)
        {
            return;
        }

        GrabbedMouse?.Invoke (view, new (view));
    }

    /// <exception cref="Exception">A delegate callback throws an exception.</exception>
    private static void OnUnGrabbedMouse (View? view)
    {
        if (view is null)
        {
            return;
        }

        UnGrabbedMouse?.Invoke (view, new (view));
    }

    /// <summary>Event fired when a mouse move or click occurs. Coordinates are screen relative.</summary>
    /// <remarks>
    ///     <para>
    ///         Use this event to receive mouse events in screen coordinates. Use <see cref="MouseEvent"/> to
    ///         receive mouse events relative to a <see cref="View.Viewport"/>.
    ///     </para>
    ///     <para>The <see cref="MouseEvent.View"/> will contain the <see cref="View"/> that contains the mouse coordinates.</para>
    /// </remarks>
    public static event EventHandler<MouseEvent>? MouseEvent;

    /// <summary>Called when a mouse event is raised by the driver.</summary>
    /// <remarks>This method can be used to simulate a mouse event, e.g. in unit tests.</remarks>
    /// <param name="mouseEvent">The mouse event with coordinates relative to the screen.</param>
    internal static void OnMouseEvent (MouseEvent mouseEvent)
    {
        _lastMousePosition = mouseEvent.ScreenPosition;

        if (IsMouseDisabled)
        {
            return;
        }

        // The position of the mouse is the same as the screen position at the application level.
        //Debug.Assert (mouseEvent.Position == mouseEvent.ScreenPosition);
        mouseEvent.Position = mouseEvent.ScreenPosition;

        List<View?> currentViewsUnderMouse = View.GetViewsUnderMouse (mouseEvent.ScreenPosition);

        View? deepestViewUnderMouse = currentViewsUnderMouse.LastOrDefault ();

        if (deepestViewUnderMouse is { })
        {
#if DEBUG_IDISPOSABLE
            if (deepestViewUnderMouse.WasDisposed)
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

        if (HandleMouseGrab (deepestViewUnderMouse, mouseEvent))
        {
            return;
        }

        // We can combine this into the switch expression to reduce cognitive complexity even more and likely
        // avoid one or two of these checks in the process, as well.

        WantContinuousButtonPressedView = deepestViewUnderMouse switch
        {
            { WantContinuousButtonPressed: true } => deepestViewUnderMouse,
            _ => null
        };


        if (Popover is { Visible: true }
            && View.IsInHierarchy (Popover, deepestViewUnderMouse, includeAdornments: true) is false
            && (mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed)
                || mouseEvent.Flags.HasFlag (MouseFlags.Button2Pressed)
                || mouseEvent.Flags.HasFlag (MouseFlags.Button3Pressed)))
        {

            Popover.Visible = false;

            // Recurse once
            OnMouseEvent (mouseEvent);

            return;
        }

        // May be null before the prior condition or the condition may set it as null.
        // So, the checking must be outside the prior condition.
        if (deepestViewUnderMouse is null)
        {
            return;
        }

        // Create a view-relative mouse event to send to the view that is under the mouse.
        MouseEvent? viewMouseEvent;

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
        else if (deepestViewUnderMouse.ViewportToScreen (Rectangle.Empty with { Size = deepestViewUnderMouse.Viewport.Size }).Contains (mouseEvent.Position))
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
            Debug.Fail ("this should not happen.");

            // Debug.Fail ("This should never happen. If it does please file an Issue!!");

            return;
        }

        RaiseMouseEnterLeaveEvents (viewMouseEvent.ScreenPosition, currentViewsUnderMouse);

        WantContinuousButtonPressedView = deepestViewUnderMouse.WantContinuousButtonPressed ? deepestViewUnderMouse : null;

        while (deepestViewUnderMouse.NewMouseEvent (viewMouseEvent) is not true && MouseGrabView is not { })
        {
            if (deepestViewUnderMouse is Adornment adornmentView)
            {
                deepestViewUnderMouse = adornmentView.Parent!.SuperView;
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

    internal static bool HandleMouseGrab (View? deepestViewUnderMouse, MouseEvent mouseEvent)
    {
        if (MouseGrabView is { })
        {
#if DEBUG_IDISPOSABLE
            if (MouseGrabView.WasDisposed)
            {
                throw new ObjectDisposedException (MouseGrabView.GetType ().FullName);
            }
#endif

            // If the mouse is grabbed, send the event to the view that grabbed it.
            // The coordinates are relative to the Bounds of the view that grabbed the mouse.
            Point frameLoc = MouseGrabView.ScreenToViewport (mouseEvent.ScreenPosition);

            var viewRelativeMouseEvent = new MouseEvent
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

    internal static readonly List<View?> _cachedViewsUnderMouse = new ();

    // TODO: Refactor MouseEnter/LeaveEvents to not take MouseEvent param.
    /// <summary>
    ///     INTERNAL: Raises the MouseEnter and MouseLeave events for the views that are under the mouse.
    /// </summary>
    /// <param name="screenPosition">The position of the mouse.</param>
    /// <param name="currentViewsUnderMouse">The most recent result from GetViewsUnderMouse().</param>
    internal static void RaiseMouseEnterLeaveEvents (Point screenPosition, List<View?> currentViewsUnderMouse)
    {
        // Tell any views that are no longer under the mouse that the mouse has left
        List<View?> viewsToLeave = _cachedViewsUnderMouse.Where (v => v is { } && !currentViewsUnderMouse.Contains (v)).ToList ();

        foreach (View? view in viewsToLeave)
        {
            if (view is null)
            {
                continue;
            }

            view.NewMouseLeaveEvent ();
            _cachedViewsUnderMouse.Remove (view);
        }

        // Tell any views that are now under the mouse that the mouse has entered and add them to the list
        foreach (View? view in currentViewsUnderMouse)
        {
            if (view is null)
            {
                continue;
            }

            if (_cachedViewsUnderMouse.Contains (view))
            {
                continue;
            }

            _cachedViewsUnderMouse.Add (view);
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

            CancelEventArgs eventArgs = new ();
            bool? cancelled = view.NewMouseEnterEvent (eventArgs);

            if (cancelled is true || eventArgs.Cancel)
            {
                break;
            }
        }
    }
}
