#nullable enable
using System.ComponentModel;

namespace Terminal.Gui;

public partial class View // Mouse APIs
{
    #region MouseEnterLeave

    private bool _hovering;
    private ColorScheme? _savedNonHoverColorScheme;

    /// <summary>
    ///     INTERNAL Called by <see cref="Application.OnMouseEvent"/> when the mouse moves over the View's <see cref="Frame"/>.
    ///     <see cref="MouseLeave"/> will
    ///     be raised when the mouse is no longer over the <see cref="Frame"/>. If another View occludes this View, the
    ///     that View will also receive MouseEnter/Leave events.
    /// </summary>
    /// <param name="eventArgs"></param>
    /// <returns>
    ///     <see langword="true"/> if the event was canceled, <see langword="false"/> if not, <see langword="null"/> if the
    ///     view is not visible. Cancelling the event
    ///     prevents Views higher in the visible hierarchy from receiving Enter/Leave events.
    /// </returns>
    internal bool? NewMouseEnterEvent (CancelEventArgs eventArgs)
    {
        // Pre-conditions
        if (!CanBeVisible (this))
        {
            return null;
        }

        // Cancellable event
        if (OnMouseEnter (eventArgs))
        {
            return true;
        }

        MouseEnter?.Invoke (this, eventArgs);

        _hovering = !eventArgs.Cancel;

        if (eventArgs.Cancel)
        {
            return true;
        }

        // Post-conditions
        if (HighlightStyle.HasFlag (HighlightStyle.Hover) || Diagnostics.HasFlag (ViewDiagnosticFlags.Hover))
        {
            HighlightStyle copy = HighlightStyle;
            var hover = HighlightStyle.Hover;
            CancelEventArgs<HighlightStyle> args = new (ref copy, ref hover);

            if (RaiseHighlight (args) || args.Cancel)
            {
                return args.Cancel;
            }

            ColorScheme cs = ColorScheme;

            if (cs is null)
            {
                cs = new ();
            }

            _savedNonHoverColorScheme = cs;

            ColorScheme = ColorScheme.GetHighlightColorScheme ();
        }

        return false;
    }

    /// <summary>
    ///     Called when the mouse moves over the View's <see cref="Frame"/> and no other non-Subview occludes it.
    ///     <see cref="MouseLeave"/> will
    ///     be raised when the mouse is no longer over the <see cref="Frame"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A view must be visible to receive Enter events (Leave events are always received).
    ///     </para>
    ///     <para>
    ///         If the event is cancelled, the mouse event will not be propagated to other views and <see cref="MouseEnter"/>
    ///         will not be raised.
    ///     </para>
    ///     <para>
    ///         Adornments receive MouseEnter/Leave events when the mouse is over the Adornment's <see cref="Thickness"/>.
    ///     </para>
    ///     <para>
    ///         See <see cref="SetPressedHighlight"/> for more information.
    ///     </para>
    /// </remarks>
    /// <param name="eventArgs"></param>
    /// <returns>
    ///     <see langword="true"/> if the event was canceled, <see langword="false"/> if not. Cancelling the event
    ///     prevents Views higher in the visible hierarchy from receiving Enter/Leave events.
    /// </returns>
    protected virtual bool OnMouseEnter (CancelEventArgs eventArgs) { return false; }

    /// <summary>
    ///     Raised when the mouse moves over the View's <see cref="Frame"/>. <see cref="MouseLeave"/> will
    ///     be raised when the mouse is no longer over the <see cref="Frame"/>. If another View occludes this View, the
    ///     that View will also receive MouseEnter/Leave events.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A view must be visible to receive Enter events (Leave events are always received).
    ///     </para>
    ///     <para>
    ///         If the event is cancelled, the mouse event will not be propagated to other views.
    ///     </para>
    ///     <para>
    ///         Adornments receive MouseEnter/Leave events when the mouse is over the Adornment's <see cref="Thickness"/>.
    ///     </para>
    ///     <para>
    ///         Set <see cref="CancelEventArgs.Cancel"/> to <see langword="true"/> if the event was canceled,
    ///         <see langword="false"/> if not. Cancelling the event
    ///         prevents Views higher in the visible hierarchy from receiving Enter/Leave events.
    ///     </para>
    ///     <para>
    ///         See <see cref="SetPressedHighlight"/> for more information.
    ///     </para>
    /// </remarks>
    public event EventHandler<CancelEventArgs>? MouseEnter;

    /// <summary>
    ///     INTERNAL Called by <see cref="Application.OnMouseEvent"/> when the mouse leaves <see cref="Frame"/>, or is occluded
    ///     by another non-SubView.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method calls <see cref="OnMouseLeave"/> and raises the <see cref="MouseLeave"/> event.
    ///     </para>
    ///     <para>
    ///         Adornments receive MouseEnter/Leave events when the mouse is over the Adornment's <see cref="Thickness"/>.
    ///     </para>
    ///     <para>
    ///         See <see cref="SetPressedHighlight"/> for more information.
    ///     </para>
    /// </remarks>
    internal void NewMouseLeaveEvent ()
    {
        // Pre-conditions

        // Non-cancellable event
        OnMouseLeave ();

        MouseLeave?.Invoke (this, EventArgs.Empty);

        // Post-conditions
        _hovering = false;

        if (HighlightStyle.HasFlag (HighlightStyle.Hover) || Diagnostics.HasFlag (ViewDiagnosticFlags.Hover))
        {
            HighlightStyle copy = HighlightStyle;
            var hover = HighlightStyle.None;
            RaiseHighlight (new (ref copy, ref hover));

            if (_savedNonHoverColorScheme is { })
            {
                ColorScheme = _savedNonHoverColorScheme;
                _savedNonHoverColorScheme = null;
            }
        }
    }

    /// <summary>
    ///     Called when the mouse moves outside View's <see cref="Frame"/>, or is occluded by another non-SubView.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Adornments receive MouseEnter/Leave events when the mouse is over the Adornment's <see cref="Thickness"/>.
    ///     </para>
    ///     <para>
    ///         See <see cref="SetPressedHighlight"/> for more information.
    ///     </para>
    /// </remarks>
    protected virtual void OnMouseLeave () { }

    /// <summary>
    ///     Raised when the mouse moves outside View's <see cref="Frame"/>, or is occluded by another non-SubView.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Adornments receive MouseEnter/Leave events when the mouse is over the Adornment's <see cref="Thickness"/>.
    ///     </para>
    ///     <para>
    ///         See <see cref="SetPressedHighlight"/> for more information.
    ///     </para>
    /// </remarks>
    public event EventHandler? MouseLeave;

    #endregion MouseEnterLeave

    #region Low Level Mouse Events

    /// <summary>Event fired when a mouse event occurs.</summary>
    /// <remarks>
    ///     <para>
    ///         The coordinates are relative to <see cref="View.Viewport"/>.
    ///     </para>
    /// </remarks>
    public event EventHandler<MouseEventEventArgs>? MouseEvent;

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
    ///         See <see cref="SetPressedHighlight"/> for more information.
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
        // Pre-conditions
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

        // Cancellable event
        if (OnMouseEvent (mouseEvent))
        {
            // Technically mouseEvent.Handled should already be true if implementers of OnMouseEvent
            // follow the rules. But we'll update it just in case.
            return mouseEvent.Handled = true;
        }

        // BUGBUG: MouseEvent should be fired from here. Fix this in https://github.com/gui-cs/Terminal.Gui/issues/3029

        // Post-Conditions
        if (HighlightStyle != HighlightStyle.None || (WantContinuousButtonPressed && WantMousePositionReports))
        {
            if (WhenGrabbedHandlePressed (mouseEvent))
            {
                return mouseEvent.Handled;
            }

            if (WhenGrabbedHandleReleased (mouseEvent))
            {
                return mouseEvent.Handled;
            }

            if (WhenGrabbedHandleClicked (mouseEvent))
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
            // If it's a click, and we didn't handle it, then we need to generate a click event
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

    #endregion Low Level Mouse Events

    #region Mouse Click Events

    /// <summary>Event fired when a mouse click occurs.</summary>
    /// 
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
        // BUGBUG: This should be named NewMouseClickEvent. Fix this in https://github.com/gui-cs/Terminal.Gui/issues/3029

        // Pre-conditions
        if (!Enabled)
        {
            // QUESTION: Is this right? Should a disabled view eat mouse clicks?
            return args.Handled = false;
        }

        // Cancellable event

        // BUGBUG: There should be a call to a protected virtual OnMouseClick here. Fix this in https://github.com/gui-cs/Terminal.Gui/issues/3029

        MouseClick?.Invoke (this, args);

        if (args.Handled)
        {
            return true;
        }

        // Post-conditions

        // Always invoke Select command on MouseClick
        // By default, this will raise Selected/OnSelected - Subclasses can override this via AddCommand (Command.Select ...).
        args.Handled = InvokeCommand (Command.Select, null, new KeyBinding ([Command.Select], scope: KeyBindingScope.Focused, boundView: this, context: args.MouseEvent)) == true;

        return args.Handled;
    }

    /// <summary>
    ///     INTERNAL For cases where the view is grabbed and the mouse is clicked, this method handles the click event (typically
    ///     when <see cref="WantContinuousButtonPressed"/> or <see cref="HighlightStyle"/> are set).
    /// </summary>
    /// <remarks>
    ///     Marked internal just to support unit tests
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    internal bool WhenGrabbedHandleClicked (MouseEvent mouseEvent)
    {
        if (Application.MouseGrabView == this
            && (mouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked)
                || mouseEvent.Flags.HasFlag (MouseFlags.Button2Clicked)
                || mouseEvent.Flags.HasFlag (MouseFlags.Button3Clicked)
                || mouseEvent.Flags.HasFlag (MouseFlags.Button4Clicked)))
        {
            // We're grabbed. Clicked event comes after the last Release. This is our signal to ungrab
            Application.UngrabMouse ();

            if (SetPressedHighlight (HighlightStyle.None))
            {
                // BUGBUG: If we return true here we never generate a mouse click!
                return true;
            }

            // If mouse is still in bounds, generate a click
            if (!WantContinuousButtonPressed && Viewport.Contains (mouseEvent.Position))
            {
                var meea = new MouseEventEventArgs (mouseEvent);

                // We can ignore the return value of OnMouseClick; if the click is handled
                // meea.Handled and meea.MouseEvent.Handled will be true
                OnMouseClick (meea);
            }

            return mouseEvent.Handled = true;
        }

        return false;
    }

    /// <summary>
    ///     INTERNAL For cases where the view is grabbed and the mouse is clicked, this method handles the released event (typically
    ///     when <see cref="WantContinuousButtonPressed"/> or <see cref="HighlightStyle"/> are set).
    /// </summary>
    /// <remarks>
    ///     Marked internal just to support unit tests
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    internal bool WhenGrabbedHandleReleased (MouseEvent mouseEvent)
    {
        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Released)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button2Released)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button3Released)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button4Released))
        {
            if (Application.MouseGrabView == this)
            {
                SetPressedHighlight (HighlightStyle.None);
            }

            return mouseEvent.Handled = true;
        }

        return false;
    }

    /// <summary>
    ///     INTERNAL For cases where the view is grabbed and the mouse is clicked, this method handles the released event (typically
    ///     when <see cref="WantContinuousButtonPressed"/> or <see cref="HighlightStyle"/> are set).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Marked internal just to support unit tests
    ///     </para>
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    private bool WhenGrabbedHandlePressed (MouseEvent mouseEvent)
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
                    && SetPressedHighlight (HighlightStyle.HasFlag (HighlightStyle.Pressed) ? HighlightStyle.Pressed : HighlightStyle.None))
                {
                    return true;
                }
            }
            else
            {
                if (this is not Adornment
                    && SetPressedHighlight (HighlightStyle.HasFlag (HighlightStyle.PressedOutside) ? HighlightStyle.PressedOutside : HighlightStyle.None))

                {
                    return true;
                }
            }

            if (WantContinuousButtonPressed && Application.MouseGrabView == this)
            {
                // If this is not the first pressed event, generate a click
                return OnMouseClick (new (mouseEvent));
            }

            return mouseEvent.Handled = true;
        }

        return false;
    }

    #endregion Mouse Click Events

    #region Highlight Handling

    // Used for Pressed highlighting
    private ColorScheme? _savedHighlightColorScheme;

    /// <summary>
    ///     Gets or sets whether the <see cref="View"/> will be highlighted visually by mouse interaction.
    /// </summary>
    public HighlightStyle HighlightStyle { get; set; }

    /// <summary>
    ///     INTERNAL Raises the <see cref="Highlight"/> event. Returns <see langword="true"/> if the event was handled,
    ///     <see langword="false"/> otherwise.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    private bool RaiseHighlight (CancelEventArgs<HighlightStyle> args)
    {
        if (OnHighlight (args))
        {
            return true;
        }

        Highlight?.Invoke (this, args);

        return args.Cancel;
    }

    /// <summary>
    ///     Called when the view is to be highlighted. The <see cref="HighlightStyle"/> passed in the event indicates the
    ///     highlight style that will be applied. The view can modify the highlight style by setting the
    ///     <see cref="CancelEventArgs{T}.NewValue"/> property.
    /// </summary>
    /// <param name="args">
    ///     Set the <see cref="CancelEventArgs{T}.NewValue"/> property to <see langword="true"/>, to cancel, indicating custom
    ///     highlighting.
    /// </param>
    /// <returns><see langword="true"/>, to cancel, indicating custom highlighting.</returns>
    protected virtual bool OnHighlight (CancelEventArgs<HighlightStyle> args) { return false; }

    /// <summary>
    ///     Raised when the view is to be highlighted. The <see cref="HighlightStyle"/> passed in the event indicates the
    ///     highlight style that will be applied. The view can modify the highlight style by setting the
    ///     <see cref="CancelEventArgs{T}.NewValue"/> property.
    ///     Set to <see langword="true"/>, to cancel, indicating custom highlighting.
    /// </summary>
    public event EventHandler<CancelEventArgs<HighlightStyle>>? Highlight;

    /// <summary>
    ///     INTERNAL Enables the highlight for the view when the mouse is pressed. Called from OnMouseEvent.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Set <see cref="HighlightStyle"/> to <see cref="HighlightStyle.Pressed"/> and/or
    ///         <see cref="HighlightStyle.PressedOutside"/> to enable.
    ///     </para>
    ///     <para>
    ///         Calls <see cref="OnHighlight"/> and raises the <see cref="Highlight"/> event.
    ///     </para>
    ///     <para>
    ///         Marked internal just to support unit tests
    ///     </para>
    /// </remarks>
    /// <returns><see langword="true"/>, if the Highlight event was handled, <see langword="false"/> otherwise.</returns>
    internal bool SetPressedHighlight (HighlightStyle newHighlightStyle)
    {
        // TODO: Make the highlight colors configurable
        if (!CanFocus)
        {
            return false;
        }

        HighlightStyle copy = HighlightStyle;
        CancelEventArgs<HighlightStyle> args = new (ref copy, ref newHighlightStyle);

        if (RaiseHighlight (args) || args.Cancel)
        {
            return true;
        }

        // For 3D Pressed Style - Note we don't care about canceling the event here
        Margin?.RaiseHighlight (args);
        args.Cancel = false; // Just in case

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

    #endregion Highlight Handling

    /// <summary>
    ///     INTERNAL: Gets the Views that are under the mouse at <paramref name="location"/>, including Adornments.
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    internal static List<View?> GetViewsUnderMouse (in Point location)
    {
        List<View?> viewsUnderMouse = new ();

        View? start = Application.Top;

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
