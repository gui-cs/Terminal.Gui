#nullable enable
using System.ComponentModel;

namespace Terminal.Gui;

public partial class View // Mouse APIs
{
    /// <summary>Gets the mouse bindings for this view.</summary>
    public MouseBindings MouseBindings { get; internal set; } = null!;

    private void SetupMouse ()
    {
        MouseBindings = new ();

        // TODO: Should the default really work with any button or just button1?
        MouseBindings.Add (MouseFlags.Button1Clicked, Command.Select);
        MouseBindings.Add (MouseFlags.Button2Clicked, Command.Select);
        MouseBindings.Add (MouseFlags.Button3Clicked, Command.Select);
        MouseBindings.Add (MouseFlags.Button4Clicked, Command.Select);
        MouseBindings.Add (MouseFlags.Button1Clicked | MouseFlags.ButtonCtrl, Command.Select);
    }

    /// <summary>
    ///     Invokes the Commands bound to the MouseFlags specified by <paramref name="mouseEventArgs"/>.
    ///     <para>See <see href="../docs/mouse.md">for an overview of Terminal.Gui mouse APIs.</see></para>
    /// </summary>
    /// <param name="mouseEventArgs">The mouse event passed.</param>
    /// <returns>
    ///     <see langword="null"/> if no command was invoked; input processing should continue.
    ///     <see langword="false"/> if at least one command was invoked and was not handled (or cancelled); input processing
    ///     should continue.
    ///     <see langword="true"/> if at least one command was invoked and handled (or cancelled); input processing should
    ///     stop.
    /// </returns>
    protected bool? InvokeCommandsBoundToMouse (MouseEventArgs mouseEventArgs)
    {
        if (!MouseBindings.TryGet (mouseEventArgs.Flags, out MouseBinding binding))
        {
            return null;
        }

        binding.MouseEventArgs = mouseEventArgs;

        return InvokeCommands (binding.Commands, binding);
    }

    #region MouseEnterLeave

    private bool _hovering;
    private Scheme? _savedNonHoverScheme;

    /// <summary>
    ///     INTERNAL Called by <see cref="Application.RaiseMouseEvent"/> when the mouse moves over the View's
    ///     <see cref="Frame"/>.
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
        if (HighlightStyle.HasFlag (HighlightStyle.Hover) /* || Diagnostics.HasFlag (ViewDiagnosticFlags.Hover)*/)
        {
            HighlightStyle copy = HighlightStyle;
            var hover = HighlightStyle.Hover;
            CancelEventArgs<HighlightStyle> args = new (ref copy, ref hover);

            if (RaiseHighlight (args) || args.Cancel)
            {
                return args.Cancel;
            }

            // BUGBUG: HighlightSty;e impl breaks Schemes - Disable it until fixed

            //if (HasScheme)
            //{
            //    Scheme? cs = GetScheme ();

            //    _savedNonHoverScheme = cs;

            //    SetScheme (GetHighlightScheme ());
            //}

            SetNeedsDraw ();
        }

        return false;
    }

    /// <summary>
    ///     Gets the <see cref="Scheme"/> to use when the view is highlighted. The highlight colorscheme
    ///     is based on the current <see cref="Scheme"/>, using <see cref="Color.GetHighlightColor()"/>.
    /// </summary>
    /// <remarks>The highlight scheme.</remarks>
    public Scheme GetHighlightScheme ()
    {
        Scheme cs = GetScheme ();

        return cs with
        {
            Normal = new (
                          GetAttributeForRole (VisualRole.Normal).Foreground.GetHighlightColor (),
                          GetAttributeForRole (VisualRole.Normal).Background,
                          GetAttributeForRole (VisualRole.Normal).Style),
            HotNormal = new (
                             GetAttributeForRole (VisualRole.HotNormal).Foreground.GetHighlightColor (),
                             GetAttributeForRole (VisualRole.HotNormal).Background,
                             GetAttributeForRole (VisualRole.HotNormal).Style),
            Focus = new (
                         GetAttributeForRole (VisualRole.Focus).Foreground.GetHighlightColor (),
                         GetAttributeForRole (VisualRole.Focus).Background,
                         GetAttributeForRole (VisualRole.Focus).Style),
            HotFocus = new (
                            GetAttributeForRole (VisualRole.HotFocus).Foreground.GetHighlightColor (),
                            GetAttributeForRole (VisualRole.HotFocus).Background,
                            GetAttributeForRole (VisualRole.HotFocus).Style)
        };
    }

    /// <summary>
    ///     Called when the mouse moves over the View's <see cref="Frame"/> and no other non-SubView occludes it.
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
    ///     INTERNAL Called by <see cref="Application.RaiseMouseEvent"/> when the mouse leaves <see cref="Frame"/>, or is
    ///     occluded
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

        if (HighlightStyle.HasFlag (HighlightStyle.Hover) /* || Diagnostics.HasFlag (ViewDiagnosticFlags.Hover)*/)
        {
            HighlightStyle copy = HighlightStyle;
            var hover = HighlightStyle.None;
            RaiseHighlight (new (ref copy, ref hover));

            if (HasScheme && _savedNonHoverScheme is { })
            {
                SetScheme (_savedNonHoverScheme);
                _savedNonHoverScheme = null;
                SetNeedsDraw ();
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

    /// <summary>Gets or sets whether the <see cref="View"/> wants continuous button pressed events.</summary>
    public virtual bool WantContinuousButtonPressed { get; set; }

    /// <summary>Gets or sets whether the <see cref="View"/> wants mouse position reports.</summary>
    /// <value><see langword="true"/> if mouse position reports are wanted; otherwise, <see langword="false"/>.</value>
    public bool WantMousePositionReports { get; set; }

    /// <summary>
    ///     Processes a new <see cref="MouseEvent"/>. This method is called by <see cref="Application.RaiseMouseEvent"/> when a
    ///     mouse
    ///     event occurs.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A view must be both enabled and visible to receive mouse events.
    ///     </para>
    ///     <para>
    ///         This method raises <see cref="RaiseMouseEvent"/>/<see cref="MouseEvent"/>; if not handled, and one of the
    ///         mouse buttons was clicked, the <see cref="RaiseMouseClickEvent"/>/<see cref="MouseClick"/> event will be raised
    ///     </para>
    ///     <para>
    ///         See <see cref="SetPressedHighlight"/> for more information.
    ///     </para>
    ///     <para>
    ///         If <see cref="WantContinuousButtonPressed"/> is <see langword="true"/>, the <see cref="RaiseMouseEvent"/>/
    ///         <see cref="MouseEvent"/> event
    ///         will be raised on any new mouse event where <see cref="Terminal.Gui.MouseEventArgs.Flags"/> indicates a button
    ///         is pressed.
    ///     </para>
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/> if the event was handled, <see langword="false"/> otherwise.</returns>
    public bool? NewMouseEvent (MouseEventArgs mouseEvent)
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
        if (RaiseMouseEvent (mouseEvent) || mouseEvent.Handled)
        {
            return true;
        }

        // Post-Conditions
        if (HighlightStyle != HighlightStyle.None || WantContinuousButtonPressed)
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

        // We get here if the view did not handle the mouse event via OnMouseEvent/MouseEvent and
        // it did not handle the press/release/clicked events via HandlePress/HandleRelease/HandleClicked
        if (mouseEvent.IsSingleDoubleOrTripleClicked)
        {
            return RaiseMouseClickEvent (mouseEvent);
        }

        if (mouseEvent.IsWheel)
        {
            return RaiseMouseWheelEvent (mouseEvent);
        }

        return false;
    }

    /// <summary>
    ///     Raises the <see cref="RaiseMouseEvent"/>/<see cref="MouseEvent"/> event.
    /// </summary>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    public bool RaiseMouseEvent (MouseEventArgs mouseEvent)
    {
        if (OnMouseEvent (mouseEvent) || mouseEvent.Handled)
        {
            return true;
        }

        MouseEvent?.Invoke (this, mouseEvent);

        return mouseEvent.Handled;
    }

    /// <summary>Called when a mouse event occurs within the view's <see cref="Viewport"/>.</summary>
    /// <remarks>
    ///     <para>
    ///         The coordinates are relative to <see cref="View.Viewport"/>.
    ///     </para>
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    protected virtual bool OnMouseEvent (MouseEventArgs mouseEvent) { return false; }

    /// <summary>Raised when a mouse event occurs.</summary>
    /// <remarks>
    ///     <para>
    ///         The coordinates are relative to <see cref="View.Viewport"/>.
    ///     </para>
    /// </remarks>
    public event EventHandler<MouseEventArgs>? MouseEvent;

    #endregion Low Level Mouse Events

    #region Mouse Pressed Events

    /// <summary>
    ///     INTERNAL For cases where the view is grabbed and the mouse is clicked, this method handles the released event
    ///     (typically
    ///     when <see cref="WantContinuousButtonPressed"/> or <see cref="HighlightStyle"/> are set).
    /// </summary>
    /// <remarks>
    ///     Marked internal just to support unit tests
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    internal bool WhenGrabbedHandleReleased (MouseEventArgs mouseEvent)
    {
        mouseEvent.Handled = false;

        if (mouseEvent.IsReleased)
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
    ///     INTERNAL For cases where the view is grabbed and the mouse is clicked, this method handles the released event
    ///     (typically
    ///     when <see cref="WantContinuousButtonPressed"/> or <see cref="HighlightStyle"/> are set).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Marked internal just to support unit tests
    ///     </para>
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    private bool WhenGrabbedHandlePressed (MouseEventArgs mouseEvent)
    {
        mouseEvent.Handled = false;

        if (mouseEvent.IsPressed)
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
                return RaiseMouseClickEvent (mouseEvent);
            }

            return mouseEvent.Handled = true;
        }

        return false;
    }

    #endregion Mouse Pressed Events

    #region Mouse Click Events

    /// <summary>Raises the <see cref="OnMouseClick"/>/<see cref="MouseClick"/> event.</summary>
    /// <remarks>
    ///     <para>
    ///         Called when the mouse is either clicked or double-clicked.
    ///     </para>
    ///     <para>
    ///         If <see cref="WantContinuousButtonPressed"/> is <see langword="true"/>, will be invoked on every mouse event
    ///         where
    ///         the mouse button is pressed.
    ///     </para>
    /// </remarks>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    protected bool RaiseMouseClickEvent (MouseEventArgs args)
    {
        // Pre-conditions
        if (!Enabled)
        {
            // QUESTION: Is this right? Should a disabled view eat mouse clicks?
            return args.Handled = false;
        }

        // Cancellable event

        if (OnMouseClick (args) || args.Handled)
        {
            return args.Handled;
        }

        MouseClick?.Invoke (this, args);

        if (args.Handled)
        {
            return true;
        }

        // Post-conditions

        // By default, this will raise Selecting/OnSelecting - Subclasses can override this via AddCommand (Command.Select ...).
        args.Handled = InvokeCommandsBoundToMouse (args) == true;

        return args.Handled;
    }

    /// <summary>
    ///     Called when a mouse click occurs. Check <see cref="MouseEventArgs.Flags"/> to see which button was clicked.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Called when the mouse is either clicked or double-clicked.
    ///     </para>
    ///     <para>
    ///         If <see cref="WantContinuousButtonPressed"/> is <see langword="true"/>, will be called on every mouse event
    ///         where
    ///         the mouse button is pressed.
    ///     </para>
    /// </remarks>
    /// <param name="args"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    protected virtual bool OnMouseClick (MouseEventArgs args) { return false; }

    /// <summary>Raised when a mouse click occurs.</summary>
    /// <remarks>
    ///     <para>
    ///         Raised when the mouse is either clicked or double-clicked.
    ///     </para>
    ///     <para>
    ///         If <see cref="WantContinuousButtonPressed"/> is <see langword="true"/>, will be raised on every mouse event
    ///         where
    ///         the mouse button is pressed.
    ///     </para>
    /// </remarks>
    public event EventHandler<MouseEventArgs>? MouseClick;

    /// <summary>
    ///     INTERNAL For cases where the view is grabbed and the mouse is clicked, this method handles the click event
    ///     (typically
    ///     when <see cref="WantContinuousButtonPressed"/> or <see cref="HighlightStyle"/> are set).
    /// </summary>
    /// <remarks>
    ///     Marked internal just to support unit tests
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    internal bool WhenGrabbedHandleClicked (MouseEventArgs mouseEvent)
    {
        mouseEvent.Handled = false;

        if (Application.MouseGrabView == this && mouseEvent.IsSingleClicked)
        {
            // We're grabbed. Clicked event comes after the last Release. This is our signal to ungrab
            Application.UngrabMouse ();

            if (SetPressedHighlight (HighlightStyle.None))
            {
                return true;
            }

            // If mouse is still in bounds, generate a click
            if (!WantMousePositionReports && Viewport.Contains (mouseEvent.Position))
            {
                return RaiseMouseClickEvent (mouseEvent);
            }

            return mouseEvent.Handled = true;
        }

        return false;
    }

    #endregion Mouse Clicked Events

    #region Mouse Wheel Events

    /// <summary>Raises the <see cref="OnMouseWheel"/>/<see cref="MouseWheel"/> event.</summary>
    /// <remarks>
    /// </remarks>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    protected bool RaiseMouseWheelEvent (MouseEventArgs args)
    {
        // Pre-conditions
        if (!Enabled)
        {
            // QUESTION: Is this right? Should a disabled view eat mouse?
            return args.Handled = false;
        }

        // Cancellable event

        if (OnMouseWheel (args) || args.Handled)
        {
            return args.Handled;
        }

        MouseWheel?.Invoke (this, args);

        if (args.Handled)
        {
            return true;
        }

        args.Handled = InvokeCommandsBoundToMouse (args) == true;

        return args.Handled;
    }

    /// <summary>
    ///     Called when a mouse wheel event occurs. Check <see cref="MouseEventArgs.Flags"/> to see which wheel was moved was
    ///     clicked.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="args"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    protected virtual bool OnMouseWheel (MouseEventArgs args) { return false; }

    /// <summary>Raised when a mouse wheel event occurs.</summary>
    /// <remarks>
    /// </remarks>
    public event EventHandler<MouseEventArgs>? MouseWheel;

    #endregion Mouse Wheel Events

    #region Highlight Handling

    // Used for Pressed highlighting
    private Scheme? _savedHighlightScheme;

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

        //if (args.Cancel)
        //{
        //    return true;
        //}

        //args.Cancel = InvokeCommandsBoundToMouse (args) == true;

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
            if (_savedHighlightScheme is null && HasScheme)
            {
                _savedHighlightScheme = GetScheme ();

                if (GetScheme () is null)
                {
                    return false;
                }

                if (CanFocus)
                {
                    var cs = new Scheme (GetScheme ())
                    {
                        // Highlight the foreground focus color
                        Focus = new (
                                     GetScheme ().Focus.Foreground.GetHighlightColor (),
                                     GetScheme ().Focus.Background.GetHighlightColor (),
                                     GetScheme ().Focus.Style)
                    };
                    SetScheme (cs);
                }
                else
                {
                    var cs = new Scheme (GetScheme ())
                    {
                        // Invert Focus color foreground/background. We can do this because we know the view is not going to be focused.
                        Normal = new (
                                      GetScheme ().Focus.Background,
                                      GetScheme ().Normal.Foreground,
                                      GetScheme ().Focus.Style)
                    };
                    SetScheme (cs);
                }
            }

            // Return false since we don't want to eat the event
            return false;
        }

        if (HasScheme && args.NewValue == HighlightStyle.None)
        {
            // Unhighlight
            SetScheme (_savedHighlightScheme);
            _savedHighlightScheme = null;
            SetNeedsDraw ();
        }

        return false;
    }

    #endregion Highlight Handling

    /// <summary>
    ///     INTERNAL: Gets the Views that are under the mouse at <paramref name="location"/>, including Adornments.
    /// </summary>
    /// <param name="location">Screen-relative location.</param>
    /// <param name="ignoreTransparent">If <see langword="true"/> any transparent views will be ignored.</param>
    /// <returns></returns>
    internal static List<View?> GetViewsUnderMouse (in Point location, bool ignoreTransparent = false)
    {
        // PopoverHost - If visible, start with it instead of Top
        if (Application.Popover?.GetActivePopover () is View { Visible: true } visiblePopover && !ignoreTransparent)
        {
            List<View?> result = GetViewsUnderMouseForRoot (visiblePopover, location, ignoreTransparent);

            if (result.Count > 0)
            {
                result.Add (Application.Top);
                return result;
            }
        }

        var checkedTop = false;

        // Traverse all visible toplevels, topmost first (reverse stack order)
        if (Application.TopLevels.Count > 0)
        {
            foreach (Toplevel toplevel in Application.TopLevels)
            {
                if (toplevel.Visible && toplevel.Contains (location))
                {
                    List<View?> result = GetViewsUnderMouseForRoot (toplevel, location, ignoreTransparent);

                    // Only return if the result is not empty AND the result contains the toplevel itself or a non-transparent child.
                    if (result.Count > 0)
                    {
                        // If the result contains only the toplevel, but the margin is TransparentMouse, skip this toplevel.
                        // If the result contains the margin, but the margin is TransparentMouse, skip this toplevel.
                        // If the result contains the toplevel and/or margin and neither is TransparentMouse, return as before.
                        // If the result contains only subviews, return as before.

                        // If the result contains the toplevel, but the point is in a TransparentMouse margin, skip.
                        Margin? margin = toplevel.Margin as Margin;
                        bool isTransparentMargin =
                            margin is { } &&
                            margin.Contains (location) &&
                            margin.ViewportSettings.HasFlag (ViewportSettings.TransparentMouse);

                        // If the result contains only the toplevel, and the margin is transparent, skip.
                        if (isTransparentMargin &&
                            result.All (v => v == toplevel))
                        {
                            continue; // skip this toplevel, try next
                        }

                        // If the result contains only the toplevel and the margin, and the margin is transparent, skip.
                        if (isTransparentMargin &&
                            result.All (v => v == toplevel || v == margin))
                        {
                            continue; // skip this toplevel, try next
                        }

                        return result;
                    }
                }


                if (toplevel == Application.Top)
                {
                    checkedTop = true;
                }
            }
        }

        // Fallback: If TopLevels is empty or Top is not in TopLevels, check Top directly (for test compatibility)
        if (!checkedTop && Application.Top is { Visible: true } top)
        {
            // For root toplevels, allow hit-testing even if location is outside bounds (for drag/move)
            List<View?> result = GetViewsUnderMouseForRoot (top, location, ignoreTransparent);

            if (result.Count > 0)
            {
                return result;
            }
        }

        return new ();
    }

    /// <summary>
    /// INTERNAL: Helper that contains the original GetViewsUnderMouse logic, but starts from a given root view
    /// </summary>
    /// <param name="start"></param>
    /// <param name="location"></param>
    /// <param name="ignoreTransparent"></param>
    /// <returns></returns>
    internal static List<View?> GetViewsUnderMouseForRoot (View start, in Point location, bool ignoreTransparent)
    {
        List<View?> viewsUnderMouse = new ();
        Point currentLocation = location;

        // Normal logic: only traverse if the point is inside the view
        while (start is { Visible: true } && start.Contains (currentLocation))
        {
            if (!start.ViewportSettings.HasFlag (ViewportSettings.TransparentMouse))
            {
                viewsUnderMouse.Add (start);
            }

            Adornment? found = null;

            if (start is not Adornment)
            {
                if (start.Margin is { } && start.Margin.Contains (currentLocation))
                {
                    found = start.Margin;
                }
                else if (start.Border is { } && start.Border.Contains (currentLocation))
                {
                    found = start.Border;
                }
                else if (start.Padding is { } && start.Padding.Contains (currentLocation))
                {
                    found = start.Padding;
                }
            }

            Point viewportOffset = start.GetViewportOffsetFromFrame ();

            if (found is { })
            {
                // If the adornment is transparent to mouse, skip adding it, but continue traversal as if it was found.
                if (!found.ViewportSettings.HasFlag (ViewportSettings.TransparentMouse))
                {
                    viewsUnderMouse.Add (found);
                }
                start = found;
                viewportOffset = found.Parent?.Frame.Location ?? Point.Empty;
            }

            int startOffsetX = currentLocation.X - (start.Frame.X + viewportOffset.X);
            int startOffsetY = currentLocation.Y - (start.Frame.Y + viewportOffset.Y);

            View? subview = null;

            for (int i = start.InternalSubViews.Count - 1; i >= 0; i--)
            {
                if (start.InternalSubViews [i].Visible
                    && start.InternalSubViews [i].Contains (new (startOffsetX + start.Viewport.X, startOffsetY + start.Viewport.Y))
                    && (!ignoreTransparent || !start.InternalSubViews [i].ViewportSettings.HasFlag (ViewportSettings.TransparentMouse)))
                {
                    subview = start.InternalSubViews [i];
                    currentLocation.X = startOffsetX + start.Viewport.X;
                    currentLocation.Y = startOffsetY + start.Viewport.Y;

                    break;
                }
            }

            if (subview is null)
            {
                if (!ignoreTransparent && start.ViewportSettings.HasFlag (ViewportSettings.TransparentMouse))
                {
                    viewsUnderMouse.AddRange (GetViewsUnderMouseForRoot (start, location, true));

                    // De-dupe
                    HashSet<View?> hashSet = [.. viewsUnderMouse];
                    viewsUnderMouse = [.. hashSet];
                }

                return viewsUnderMouse;
            }

            start = subview;
        }

        return viewsUnderMouse;
    }


    private void DisposeMouse () { }
}
