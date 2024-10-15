#nullable enable
using System.ComponentModel;

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
        if (view is null || RaiseGrabbingMouseEvent (view))
        {
            return;
        }

        RaiseGrabbedMouseEvent (view);
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

        if (!RaiseUnGrabbingMouseEvent (MouseGrabView))
        {
            View view = MouseGrabView;
            MouseGrabView = null;
            RaiseUnGrabbedMouseEvent (view);
        }
    }

    /// <exception cref="Exception">A delegate callback throws an exception.</exception>
    private static bool RaiseGrabbingMouseEvent (View? view)
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
    private static bool RaiseUnGrabbingMouseEvent (View? view)
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
    private static void RaiseGrabbedMouseEvent (View? view)
    {
        if (view is null)
        {
            return;
        }

        GrabbedMouse?.Invoke (view, new (view));
    }

    /// <exception cref="Exception">A delegate callback throws an exception.</exception>
    private static void RaiseUnGrabbedMouseEvent (View? view)
    {
        if (view is null)
        {
            return;
        }

        UnGrabbedMouse?.Invoke (view, new (view));
    }


    /// <summary>
    ///     INTERNAL API: Called when a mouse event is raised by the driver. Determines the view under the mouse and
    ///     calls the appropriate View mouse event handlers.
    /// </summary>
    /// <remarks>This method can be used to simulate a mouse event, e.g. in unit tests.</remarks>
    /// <param name="mouseEvent">The mouse event with coordinates relative to the screen.</param>
    internal static void RaiseMouseEvent (MouseEventArgs mouseEvent)
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

        WantContinuousButtonPressedView = deepestViewUnderMouse switch
        {
            { WantContinuousButtonPressed: true } => deepestViewUnderMouse,
            _ => null
        };

        // May be null before the prior condition or the condition may set it as null.
        // So, the checking must be outside the prior condition.
        if (deepestViewUnderMouse is null)
        {
            return;
        }

        // Create a view-relative mouse event to send to the view that is under the mouse.
        MouseEventArgs? viewMouseEvent;

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


#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
    /// <summary>
    /// Raised when a mouse event occurs. Can be cancelled by setting <see cref="MouseEventArgs.Handled"/> to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="MouseEventArgs.ScreenPosition"/> coordinates are screen-relative.
    ///     </para>
    ///     <para>
    ///         <see cref="MouseEventArgs.View"/> will be the deepest view under the under the mouse.
    ///     </para>
    ///     <para>
    ///         <see cref="MouseEventArgs.Position"/> coordinates are view-relative. Only valid if <see cref="MouseEventArgs.View"/> is set.
    ///     </para>
    ///     <para>
    ///         Use this evento to handle mouse events at the application level, before View-specific handling.
    ///     </para>
    /// </remarks>
    public static event EventHandler<MouseEventArgs>? MouseEvent;
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved

    internal static bool HandleMouseGrab (View? deepestViewUnderMouse, MouseEventArgs mouseEvent)
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

    internal static readonly List<View?> _cachedViewsUnderMouse = new ();

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
