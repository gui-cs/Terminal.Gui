#nullable enable
using System.ComponentModel;

namespace Terminal.Gui.App;

public static partial class Application // Mouse handling
{
    /// <summary>
    /// INTERNAL API: Holds the last mouse position.
    /// </summary>
    internal static Point? LastMousePosition { get; set; }

    /// <summary>
    ///     Gets the most recent position of the mouse.
    /// </summary>
    public static Point? GetLastMousePosition () { return LastMousePosition; }

    /// <summary>Disable or enable the mouse. The mouse is enabled by default.</summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static bool IsMouseDisabled { get; set; }

    /// <summary>
    /// Static reference to the current <see cref="IApplication"/> <see cref="IMouseGrabHandler"/>.
    /// </summary>
    public static IMouseGrabHandler MouseGrabHandler
    {
        get => ApplicationImpl.Instance.MouseGrabHandler;
        set => ApplicationImpl.Instance.MouseGrabHandler = value ??
                                                           throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    ///     INTERNAL API: Called when a mouse event is raised by the driver. Determines the view under the mouse and
    ///     calls the appropriate View mouse event handlers.
    /// </summary>
    /// <remarks>This method can be used to simulate a mouse event, e.g. in unit tests.</remarks>
    /// <param name="mouseEvent">The mouse event with coordinates relative to the screen.</param>
    internal static void RaiseMouseEvent (MouseEventArgs mouseEvent)
    {
        if (Initialized)
        {
            // LastMousePosition is a static; only set if the application is initialized.
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
            && Popover?.GetActivePopover () as View is { Visible: true } visiblePopover
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
        if (!View.IsInHierarchy (Application.Top, deepestViewUnderMouse, true) && !View.IsInHierarchy (Popover?.GetActivePopover () as View, deepestViewUnderMouse, true))
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

        while (deepestViewUnderMouse.NewMouseEvent (viewMouseEvent) is not true && MouseGrabHandler.MouseGrabView is not { })
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


#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
    /// <summary>
    /// Raised when a mouse event occurs. Can be cancelled by setting <see cref="HandledEventArgs.Handled"/> to <see langword="true"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="MouseEventArgs.ScreenPosition"/> coordinates are screen-relative.
    ///     </para>
    ///     <para>
    ///         <see cref="MouseEventArgs.View"/> will be the deepest view under the mouse.
    ///     </para>
    ///     <para>
    ///         <see cref="MouseEventArgs.Position"/> coordinates are view-relative. Only valid if <see cref="MouseEventArgs.View"/> is set.
    ///     </para>
    ///     <para>
    ///         Use this even to handle mouse events at the application level, before View-specific handling.
    ///     </para>
    /// </remarks>
    public static event EventHandler<MouseEventArgs>? MouseEvent;
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved

    internal static bool HandleMouseGrab (View? deepestViewUnderMouse, MouseEventArgs mouseEvent)
    {
        if (MouseGrabHandler.MouseGrabView is { })
        {
#if DEBUG_IDISPOSABLE
            if (View.EnableDebugIDisposableAsserts && MouseGrabHandler.MouseGrabView.WasDisposed)
            {
                throw new ObjectDisposedException (MouseGrabHandler.MouseGrabView.GetType ().FullName);
            }
#endif

            // If the mouse is grabbed, send the event to the view that grabbed it.
            // The coordinates are relative to the Bounds of the view that grabbed the mouse.
            Point frameLoc = MouseGrabHandler.MouseGrabView.ScreenToViewport (mouseEvent.ScreenPosition);

            var viewRelativeMouseEvent = new MouseEventArgs
            {
                Position = frameLoc,
                Flags = mouseEvent.Flags,
                ScreenPosition = mouseEvent.ScreenPosition,
                View = deepestViewUnderMouse ?? MouseGrabHandler.MouseGrabView
            };

            //System.Diagnostics.Debug.WriteLine ($"{nme.Flags};{nme.X};{nme.Y};{mouseGrabView}");
            if (MouseGrabHandler.MouseGrabView?.NewMouseEvent (viewRelativeMouseEvent) is true)
            {
                return true;
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (MouseGrabHandler.MouseGrabView is null && deepestViewUnderMouse is Adornment)
            {
                // The view that grabbed the mouse has been disposed
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     INTERNAL: Holds the non-<see cref="ViewportSettingsFlags.TransparentMouse"/> views that are currently under the mouse.
    /// </summary>
    internal static List<View?> CachedViewsUnderMouse { get; } = [];

    /// <summary>
    ///     INTERNAL: Raises the MouseEnter and MouseLeave events for the views that are under the mouse.
    /// </summary>
    /// <param name="screenPosition">The position of the mouse.</param>
    /// <param name="currentViewsUnderMouse">The most recent result from GetViewsUnderLocation().</param>
    internal static void RaiseMouseEnterLeaveEvents (Point screenPosition, List<View?> currentViewsUnderMouse)
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

            CancelEventArgs eventArgs = new ();
            bool? cancelled = view.NewMouseEnterEvent (eventArgs);

            if (cancelled is true || eventArgs.Cancel)
            {
                break;
            }
        }
    }
}
