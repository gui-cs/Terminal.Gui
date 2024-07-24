#nullable enable
namespace Terminal.Gui;
public static partial class Application // Mouse handling
{
    #region Mouse handling

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
        if (view is null)
        {
            return;
        }

        if (!OnGrabbingMouse (view))
        {
            OnGrabbedMouse (view);
            MouseGrabView = view;
        }
    }

    /// <summary>Releases the mouse grab, so mouse events will be routed to the view on which the mouse is.</summary>
    public static void UngrabMouse ()
    {
        if (MouseGrabView is null)
        {
            return;
        }

        if (!OnUnGrabbingMouse (MouseGrabView))
        {
            View view = MouseGrabView;
            MouseGrabView = null;
            OnUnGrabbedMouse (view);
        }
    }

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

    private static void OnGrabbedMouse (View? view)
    {
        if (view is null)
        {
            return;
        }

        GrabbedMouse?.Invoke (view, new (view));
    }

    private static void OnUnGrabbedMouse (View? view)
    {
        if (view is null)
        {
            return;
        }

        UnGrabbedMouse?.Invoke (view, new (view));
    }

#nullable enable

    // Used by OnMouseEvent to track the last view that was clicked on.
    internal static View? MouseEnteredView { get; set; }

    /// <summary>Event fired when a mouse move or click occurs. Coordinates are screen relative.</summary>
    /// <remarks>
    ///     <para>
    ///         Use this event to receive mouse events in screen coordinates. Use <see cref="MouseEvent"/> to
    ///         receive mouse events relative to a <see cref="View.Viewport"/>.
    ///     </para>
    ///     <para>The <see cref="MouseEvent.View"/> will contain the <see cref="View"/> that contains the mouse coordinates.</para>
    /// </remarks>
    public static event EventHandler<MouseEvent>? MouseEvent;

    /// <summary>Called when a mouse event occurs. Raises the <see cref="MouseEvent"/> event.</summary>
    /// <remarks>This method can be used to simulate a mouse event, e.g. in unit tests.</remarks>
    /// <param name="mouseEvent">The mouse event with coordinates relative to the screen.</param>
    internal static void OnMouseEvent (MouseEvent mouseEvent)
    {
        if (IsMouseDisabled)
        {
            return;
        }

        var view = View.FindDeepestView (Current, mouseEvent.Position);

        if (view is { })
        {
            mouseEvent.View = view;
        }

        MouseEvent?.Invoke (null, mouseEvent);

        if (mouseEvent.Handled)
        {
            return;
        }

        if (MouseGrabView is { })
        {
            // If the mouse is grabbed, send the event to the view that grabbed it.
            // The coordinates are relative to the Bounds of the view that grabbed the mouse.
            Point frameLoc = MouseGrabView.ScreenToViewport (mouseEvent.Position);

            var viewRelativeMouseEvent = new MouseEvent
            {
                Position = frameLoc,
                Flags = mouseEvent.Flags,
                ScreenPosition = mouseEvent.Position,
                View = MouseGrabView
            };

            if ((MouseGrabView.Viewport with { Location = Point.Empty }).Contains (viewRelativeMouseEvent.Position) is false)
            {
                // The mouse has moved outside the bounds of the view that grabbed the mouse
                MouseEnteredView?.NewMouseLeaveEvent (mouseEvent);
            }

            //System.Diagnostics.Debug.WriteLine ($"{nme.Flags};{nme.X};{nme.Y};{mouseGrabView}");
            if (MouseGrabView?.NewMouseEvent (viewRelativeMouseEvent) == true)
            {
                return;
            }
        }

        if (view is { WantContinuousButtonPressed: true })
        {
            WantContinuousButtonPressedView = view;
        }
        else
        {
            WantContinuousButtonPressedView = null;
        }

        if (view is not Adornment)
        {
            if ((view is null || view == ApplicationOverlapped.OverlappedTop)
                && Current is { Modal: false }
                && ApplicationOverlapped.OverlappedTop != null
                && mouseEvent.Flags != MouseFlags.ReportMousePosition
                && mouseEvent.Flags != 0)
            {
                // This occurs when there are multiple overlapped "tops"
                // E.g. "Mdi" - in the Background Worker Scenario
                View? top = ApplicationOverlapped.FindDeepestTop (Top!, mouseEvent.Position);
                view = View.FindDeepestView (top, mouseEvent.Position);

                if (view is { } && view != ApplicationOverlapped.OverlappedTop && top != Current && top is { })
                {
                    ApplicationOverlapped.MoveCurrent ((Toplevel)top);
                }
            }
        }

        if (view is null)
        {
            return;
        }

        MouseEvent? me = null;

        if (view is Adornment adornment)
        {
            Point frameLoc = adornment.ScreenToFrame (mouseEvent.Position);

            me = new ()
            {
                Position = frameLoc,
                Flags = mouseEvent.Flags,
                ScreenPosition = mouseEvent.Position,
                View = view
            };
        }
        else if (view.ViewportToScreen (Rectangle.Empty with { Size = view.Viewport.Size }).Contains (mouseEvent.Position))
        {
            Point viewportLocation = view.ScreenToViewport (mouseEvent.Position);

            me = new ()
            {
                Position = viewportLocation,
                Flags = mouseEvent.Flags,
                ScreenPosition = mouseEvent.Position,
                View = view
            };
        }

        if (me is null)
        {
            return;
        }

        if (MouseEnteredView is null)
        {
            MouseEnteredView = view;
            view.NewMouseEnterEvent (me);
        }
        else if (MouseEnteredView != view)
        {
            MouseEnteredView.NewMouseLeaveEvent (me);
            view.NewMouseEnterEvent (me);
            MouseEnteredView = view;
        }

        if (!view.WantMousePositionReports && mouseEvent.Flags == MouseFlags.ReportMousePosition)
        {
            return;
        }

        WantContinuousButtonPressedView = view.WantContinuousButtonPressed ? view : null;

        //Debug.WriteLine ($"OnMouseEvent: ({a.MouseEvent.X},{a.MouseEvent.Y}) - {a.MouseEvent.Flags}");

        while (view.NewMouseEvent (me) != true)
        {
            if (MouseGrabView is { })
            {
                break;
            }

            if (view is Adornment adornmentView)
            {
                view = adornmentView.Parent!.SuperView;
            }
            else
            {
                view = view.SuperView;
            }

            if (view is null)
            {
                break;
            }

            Point boundsPoint = view.ScreenToViewport (mouseEvent.Position);

            me = new ()
            {
                Position = boundsPoint,
                Flags = mouseEvent.Flags,
                ScreenPosition = mouseEvent.Position,
                View = view
            };
        }

        ApplicationOverlapped.BringOverlappedTopToFront ();
    }

    #endregion Mouse handling
}
