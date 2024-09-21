#nullable enable
using System.ComponentModel;

namespace Terminal.Gui;

public partial class View // Mouse APIs
{
    private ColorScheme? _savedHighlightColorScheme;

    /// <summary>
    ///     Fired when the view is highlighted. Set <see cref="CancelEventArgs.Cancel"/> to <see langword="true"/>
    ///     to implement a custom highlight scheme or prevent the view from being highlighted.
    /// </summary>
    public event EventHandler<CancelEventArgs<HighlightStyle>>? Highlight;

    /// <summary>
    ///     Gets or sets whether the <see cref="View"/> will be highlighted visually while the mouse button is
    ///     pressed.
    /// </summary>
    public HighlightStyle HighlightStyle { get; set; }

    /// <summary>Event fired when a mouse click occurs.</summary>
    /// <remarks>
    ///     <para>
    ///         Fired when the mouse is either clicked or double-clicked. Check
    ///         <see cref="MouseEvent.Flags"/> to see which button was clicked.
    ///     </para>
    ///     <para>
    ///         The coordinates are relative to <see cref="View.Viewport"/>.
    ///     </para>
    /// </remarks>
    public event EventHandler<MouseEventEventArgs>? MouseClick;

    /// <summary>Event fired when the mouse moves into the View's <see cref="Viewport"/>.</summary>
    public event EventHandler<MouseEventEventArgs>? MouseEnter;

    /// <summary>Event fired when a mouse event occurs.</summary>
    /// <remarks>
    ///     <para>
    ///         The coordinates are relative to <see cref="View.Viewport"/>.
    ///     </para>
    /// </remarks>
    public event EventHandler<MouseEventEventArgs>? MouseEvent;

    /// <summary>Event fired when the mouse leaves the View's <see cref="Viewport"/>.</summary>
    public event EventHandler<MouseEventEventArgs>? MouseLeave;

    /// <summary>
    ///     Processes a <see cref="MouseEvent"/>. This method is called by <see cref="Application.OnMouseEvent"/> when a mouse
    ///     event occurs.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A view must be both enabled and visible to receive mouse events.
    ///     </para>
    ///     <para>
    ///         This method calls <see cref="OnMouseEvent"/> to process the event. If the event is not handled, and one of the
    ///         mouse buttons was clicked, it calls <see cref="OnMouseClick"/> to process the click.
    ///     </para>
    ///     <para>
    ///         See <see cref="SetHighlight"/> for more information.
    ///     </para>
    ///     <para>
    ///         If <see cref="WantContinuousButtonPressed"/> is <see langword="true"/>, the <see cref="OnMouseClick"/> event
    ///         will be invoked repeatedly while the button is pressed.
    ///     </para>
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/> if the event was handled, <see langword="false"/> otherwise.</returns>
    public bool? NewMouseEvent (MouseEvent mouseEvent)
    {
        if (!Enabled)
        {
            // A disabled view should not eat mouse events
            return false;
        }

        if (!CanBeVisible (this))
        {
            return false;
        }

        if (!WantMousePositionReports && mouseEvent.Flags == MouseFlags.ReportMousePosition)
        {
            return false;
        }

        if (OnMouseEvent (mouseEvent))
        {
            // Technically mouseEvent.Handled should already be true if implementers of OnMouseEvent
            // follow the rules. But we'll update it just in case.
            return mouseEvent.Handled = true;
        }

        if (HighlightStyle != HighlightStyle.None || (WantContinuousButtonPressed && WantMousePositionReports))
        {
            if (HandlePressed (mouseEvent))
            {
                return mouseEvent.Handled;
            }

            if (HandleReleased (mouseEvent))
            {
                return mouseEvent.Handled;
            }

            if (HandleClicked (mouseEvent))
            {
                return mouseEvent.Handled;
            }
        }

        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button2Clicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button3Clicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button4Clicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button1DoubleClicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button2DoubleClicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button3DoubleClicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button4DoubleClicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button1TripleClicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button2TripleClicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button3TripleClicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button4TripleClicked)
           )
        {
            // If it's a click, and we didn't handle it, then we'll call OnMouseClick
            // We get here if the view did not handle the mouse event via OnMouseEvent/MouseEvent and
            // it did not handle the press/release/clicked events via HandlePress/HandleRelease/HandleClicked
            return OnMouseClick (new (mouseEvent));
        }

        return false;
    }

    /// <summary>Gets or sets whether the <see cref="View"/> wants continuous button pressed events.</summary>
    public virtual bool WantContinuousButtonPressed { get; set; }

    /// <summary>Gets or sets whether the <see cref="View"/> wants mouse position reports.</summary>
    /// <value><see langword="true"/> if mouse position reports are wanted; otherwise, <see langword="false"/>.</value>
    public virtual bool WantMousePositionReports { get; set; }

    /// <summary>
    ///     Called by <see cref="NewMouseEvent"/> when the mouse enters <see cref="Viewport"/>. The view will
    ///     then receive mouse events until <see cref="OnMouseLeave"/> is called indicating the mouse has left
    ///     the view.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Override this method or subscribe to <see cref="MouseEnter"/> to change the default enter behavior.
    ///     </para>
    ///     <para>
    ///         The coordinates are relative to <see cref="View.Viewport"/>.
    ///     </para>
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    protected internal virtual bool? OnMouseEnter (MouseEvent mouseEvent)
    {
        return false;
    }

    /// <summary>Called when a mouse event occurs within the view's <see cref="Viewport"/>.</summary>
    /// <remarks>
    ///     <para>
    ///         The coordinates are relative to <see cref="View.Viewport"/>.
    ///     </para>
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    protected internal virtual bool OnMouseEvent (MouseEvent mouseEvent)
    {
        var args = new MouseEventEventArgs (mouseEvent);

        MouseEvent?.Invoke (this, args);

        return args.Handled;
    }

    /// <summary>
    ///     Called by <see cref="NewMouseEvent"/> when a mouse leaves <see cref="Viewport"/>. The view will
    ///     no longer receive mouse events.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Override this method or subscribe to <see cref="MouseEnter"/> to change the default leave behavior.
    ///     </para>
    ///     <para>
    ///         The coordinates are relative to <see cref="View.Viewport"/>.
    ///     </para>
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    protected internal virtual bool OnMouseLeave (MouseEvent mouseEvent)
    {
        return false;
    }

    /// <summary>
    ///     Called when the view is to be highlighted.
    /// </summary>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    protected virtual bool? OnHighlight (CancelEventArgs<HighlightStyle> args)
    {
        Highlight?.Invoke (this, args);

        if (args.Cancel)
        {
            return true;
        }

        Margin?.Highlight?.Invoke (this, args);

        //args = new (highlight);
        //Border?.Highlight?.Invoke (this, args);

        //args = new (highlight);
        //Padding?.Highlight?.Invoke (this, args);

        return args.Cancel;
    }

    /// <summary>Invokes the MouseClick event.</summary>
    /// <remarks>
    ///     <para>
    ///         Called when the mouse is either clicked or double-clicked. Check
    ///         <see cref="MouseEvent.Flags"/> to see which button was clicked.
    ///     </para>
    /// </remarks>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    protected bool OnMouseClick (MouseEventEventArgs args)
    {
        if (!Enabled)
        {
            // QUESTION: Is this right? Should a disabled view eat mouse clicks?
            return args.Handled = false;
        }

        MouseClick?.Invoke (this, args);

        if (args.Handled)
        {
            return true;
        }

        if (!HasFocus && CanFocus)
        {
            args.Handled = true;
            SetFocus ();
        }

        return args.Handled;
    }

    /// <summary>
    ///     For cases where the view is grabbed and the mouse is clicked, this method handles the click event (typically
    ///     when <see cref="WantContinuousButtonPressed"/> or <see cref="HighlightStyle"/> are set).
    /// </summary>
    /// <remarks>
    ///     Marked internal just to support unit tests
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    internal bool HandleClicked (MouseEvent mouseEvent)
    {
        if (Application.MouseGrabView == this
            && (mouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked)
                || mouseEvent.Flags.HasFlag (MouseFlags.Button2Clicked)
                || mouseEvent.Flags.HasFlag (MouseFlags.Button3Clicked)
                || mouseEvent.Flags.HasFlag (MouseFlags.Button4Clicked)))
        {
            // We're grabbed. Clicked event comes after the last Release. This is our signal to ungrab
            Application.UngrabMouse ();

            if (SetHighlight (HighlightStyle.None))
            {
                return true;
            }

            // If mouse is still in bounds, click
            if (!WantContinuousButtonPressed && Viewport.Contains (mouseEvent.Position))
            {
                return OnMouseClick (new (mouseEvent));
            }

            return mouseEvent.Handled = true;
        }

        return false;
    }

    /// <summary>
    ///     For cases where the view is grabbed and the mouse is clicked, this method handles the released event (typically
    ///     when <see cref="WantContinuousButtonPressed"/> or <see cref="HighlightStyle"/> are set).
    /// </summary>
    /// <remarks>
    ///     Marked internal just to support unit tests
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    internal bool HandleReleased (MouseEvent mouseEvent)
    {
        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Released)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button2Released)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button3Released)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button4Released))
        {
            if (Application.MouseGrabView == this)
            {
                SetHighlight (HighlightStyle.None);
            }

            return mouseEvent.Handled = true;
        }

        return false;
    }

    /// <summary>
    ///     INTERNAL Called by <see cref="Application.OnMouseEvent"/> when the mouse moves over the View's <see cref="Frame"/>. <see cref="MouseLeave"/> will
    ///     be raised when the mouse is no longer over the <see cref="Frame"/>. If another View occludes this View, the
    ///     that View will also receive MouseEnter/Leave events.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A view must be visible to receive Enter events (Leave events are always received).
    ///     </para>
    ///     <para>
    ///         This method calls <see cref="OnMouseEnter"/> to raise the <see cref="MouseEnter"/> event.
    ///     </para>
    ///     <para>
    ///         Adornments receive MouseEnter/Leave events when the mouse is over the Adornment's <see cref="Thickness"/>.
    ///     </para>
    ///     <para>
    ///         See <see cref="SetHighlight"/> for more information.
    ///     </para>
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/> if the event was handled, <see langword="false"/> otherwise. Handling the event
    /// prevents Views higher in the visible hierarchy from receiving Enter/Leave events.</returns>
    internal bool? NewMouseEnterEvent (MouseEvent mouseEvent)
    {
        if (!Enabled)
        {
            return false;
        }

        if (!CanBeVisible (this))
        {
            return false;
        }

        if (OnMouseEnter (mouseEvent) == true)
        {
            return true;
        }

        var args = new MouseEventEventArgs (mouseEvent);
        MouseEnter?.Invoke (this, args);

#if HOVER
        if (HighlightStyle.HasFlag(HighlightStyle.Hover))
        {
            if (SetHighlight (HighlightStyle.Hover))
            {
                return true;
            }
        }
#endif

        return args.Handled;
    }

    /// <summary>
    ///     INTERNAL Called by <see cref="Application.OnMouseEvent"/> when the mouse leaves <see cref="Frame"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A view must be visible to receive Enter events (Leave events are always received).
    ///     </para>
    ///     <para>
    ///         This method calls <see cref="OnMouseLeave"/> to raise the <see cref="MouseLeave"/> event.
    ///     </para>
    ///     <para>
    ///         Adornments receive MouseEnter/Leave events when the mouse is over the Adornment's <see cref="Thickness"/>.
    ///     </para>
    ///     <para>
    ///         See <see cref="SetHighlight"/> for more information.
    ///     </para>
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/> if the event was handled, <see langword="false"/> otherwise. </returns>
    internal bool? NewMouseLeaveEvent (MouseEvent mouseEvent)
    {
        if (OnMouseLeave (mouseEvent))
        {
            return true;
        }

        var args = new MouseEventEventArgs (mouseEvent);
        MouseLeave?.Invoke (this, args);

#if HOVER
        if (HighlightStyle.HasFlag (HighlightStyle.Hover))
        {
            SetHighlight (HighlightStyle.None);
        }
#endif

        return args.Handled;
    }

    /// <summary>
    ///     Enables the highlight for the view when the mouse is pressed. Called from OnMouseEvent.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Set <see cref="HighlightStyle"/> to have the view highlighted based on the mouse.
    ///     </para>
    ///     <para>
    ///         Calls <see cref="OnHighlight"/> which fires the <see cref="Highlight"/> event.
    ///     </para>
    ///     <para>
    ///         Marked internal just to support unit tests
    ///     </para>
    /// </remarks>
    /// <returns><see langword="true"/>, if the Highlight event was handled, <see langword="false"/> otherwise.</returns>
    internal bool SetHighlight (HighlightStyle newHighlightStyle)
    {
        // TODO: Make the highlight colors configurable
        if (!CanFocus)
        {
            return false;
        }

        // Enable override via virtual method and/or event
        HighlightStyle copy = HighlightStyle;
        var args = new CancelEventArgs<HighlightStyle> (ref copy, ref newHighlightStyle);

        if (OnHighlight (args) == true)
        {
            return true;
        }
#if HOVER
        if (style.HasFlag (HighlightStyle.Hover))
        {
            if (_savedHighlightColorScheme is null && ColorScheme is { })
            {
                _savedHighlightColorScheme ??= ColorScheme;

                var cs = new ColorScheme (ColorScheme)
                {
                    Normal = GetFocusColor (),
                    HotNormal = ColorScheme.HotFocus
                };
                ColorScheme = cs;
            }

            return true;
        }
#endif
        if (args.NewValue.HasFlag (HighlightStyle.Pressed) || args.NewValue.HasFlag (HighlightStyle.PressedOutside))
        {
            if (_savedHighlightColorScheme is null && ColorScheme is { })
            {
                _savedHighlightColorScheme ??= ColorScheme;

                if (CanFocus)
                {
                    var cs = new ColorScheme (ColorScheme)
                    {
                        // Highlight the foreground focus color
                        Focus = new (ColorScheme.Focus.Foreground.GetHighlightColor (), ColorScheme.Focus.Background.GetHighlightColor ())
                    };
                    ColorScheme = cs;
                }
                else
                {
                    var cs = new ColorScheme (ColorScheme)
                    {
                        // Invert Focus color foreground/background. We can do this because we know the view is not going to be focused.
                        Normal = new (ColorScheme.Focus.Background, ColorScheme.Normal.Foreground)
                    };
                    ColorScheme = cs;
                }
            }

            // Return false since we don't want to eat the event
            return false;
        }

        if (args.NewValue == HighlightStyle.None)
        {
            // Unhighlight
            if (_savedHighlightColorScheme is { })
            {
                ColorScheme = _savedHighlightColorScheme;
                _savedHighlightColorScheme = null;
            }
        }

        return false;
    }

    /// <summary>
    ///     For cases where the view is grabbed and the mouse is clicked, this method handles the released event (typically
    ///     when <see cref="WantContinuousButtonPressed"/> or <see cref="HighlightStyle"/> are set).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Marked internal just to support unit tests
    ///     </para>
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    private bool HandlePressed (MouseEvent mouseEvent)
    {
        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button2Pressed)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button3Pressed)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button4Pressed))
        {
            // The first time we get pressed event, grab the mouse and set focus
            if (Application.MouseGrabView != this)
            {
                Application.GrabMouse (this);

                if (!HasFocus && CanFocus)
                {
                    // Set the focus, but don't invoke Accept
                    SetFocus ();
                }

                mouseEvent.Handled = true;
            }

            if (Viewport.Contains (mouseEvent.Position))
            {
                if (this is not Adornment
                    && SetHighlight (HighlightStyle.HasFlag (HighlightStyle.Pressed) ? HighlightStyle.Pressed : HighlightStyle.None))
                {
                    return true;
                }
            }
            else
            {
                if (this is not Adornment
                    && SetHighlight (HighlightStyle.HasFlag (HighlightStyle.PressedOutside) ? HighlightStyle.PressedOutside : HighlightStyle.None))

                {
                    return true;
                }
            }

            if (WantContinuousButtonPressed && Application.MouseGrabView == this)
            {
                // If this is not the first pressed event, click
                return OnMouseClick (new (mouseEvent));
            }

            return mouseEvent.Handled = true;
        }

        return false;
    }

    /// <summary>
    ///    INTERNAL: Gets the Views that are under the mouse at <paramref name="location"/>, including Adornments.
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    internal static List<View?> GetViewsUnderMouse (in Point location)
    {
        List<View?> viewsUnderMouse = new ();

        View? start = Application.Current ?? Application.Top;

        Point currentLocation = location;

        while (start is { Visible: true } && start.Contains (currentLocation))
        {
            viewsUnderMouse.Add (start);

            Adornment? found = null;

            if (start.Margin.Contains (currentLocation))
            {
                found = start.Margin;
            }
            else if (start.Border.Contains (currentLocation))
            {
                found = start.Border;
            }
            else if (start.Padding.Contains (currentLocation))
            {
                found = start.Padding;
            }

            Point viewportOffset = start.GetViewportOffsetFromFrame ();

            if (found is { })
            {
                start = found;
                viewsUnderMouse.Add (start);
                viewportOffset = found.Parent?.Frame.Location ?? Point.Empty;
            }

            int startOffsetX = currentLocation.X - (start.Frame.X + viewportOffset.X);
            int startOffsetY = currentLocation.Y - (start.Frame.Y + viewportOffset.Y);

            View? subview = null;

            for (int i = start.InternalSubviews.Count - 1; i >= 0; i--)
            {
                if (start.InternalSubviews [i].Visible
                    && start.InternalSubviews [i].Contains (new (startOffsetX + start.Viewport.X, startOffsetY + start.Viewport.Y)))
                {
                    subview = start.InternalSubviews [i];
                    currentLocation.X = startOffsetX + start.Viewport.X;
                    currentLocation.Y = startOffsetY + start.Viewport.Y;

                    // start is the deepest subview under the mouse; stop searching the subviews
                    break;
                }
            }

            if (subview is null)
            {
                // No subview was found that's under the mouse, so we're done
                return viewsUnderMouse;
            }

            // We found a subview of start that's under the mouse, continue...
            start = subview;
        }

        return viewsUnderMouse;
    }

}
