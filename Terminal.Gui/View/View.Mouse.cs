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

    /// <summary>
    ///     Gets whether the mouse is currently hovering over the View's <see cref="Viewport"/>. Is <see langword="true"/> after
    ///     <see cref="MouseEnter"/> has been raised, and before <see cref="MouseLeave"/> is raised.
    /// </summary>
    public bool MouseHovering { get; internal set; }

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

        if (eventArgs.Cancel)
        {
            return true;
        }

        MouseHovering = true;

        if (HighlightStyle != HighlightStyle.None)
        {
            SetNeedsDraw ();
        }

        return false;
    }

    /// <summary>
    ///     Gets the <see cref="Scheme"/> to use when the view is highlighted. The highlight colorscheme
    ///     is based on the current <see cref="Scheme"/>, using <see cref="Color.GetBrighterColor"/>.
    /// </summary>
    /// <remarks>The highlight scheme.</remarks>
    public Scheme GetHighlightScheme ()
    {
        Scheme cs = GetScheme ();

        return cs with
        {
            Normal = new (
                          GetAttributeForRole (VisualRole.Normal).Foreground.GetBrighterColor (),
                          GetAttributeForRole (VisualRole.Normal).Background,
                          GetAttributeForRole (VisualRole.Normal).Style),
            HotNormal = new (
                             GetAttributeForRole (VisualRole.HotNormal).Foreground.GetBrighterColor (),
                             GetAttributeForRole (VisualRole.HotNormal).Background,
                             GetAttributeForRole (VisualRole.HotNormal).Style),
            Focus = new (
                         GetAttributeForRole (VisualRole.Focus).Foreground.GetBrighterColor (),
                         GetAttributeForRole (VisualRole.Focus).Background,
                         GetAttributeForRole (VisualRole.Focus).Style),
            HotFocus = new (
                            GetAttributeForRole (VisualRole.HotFocus).Foreground.GetBrighterColor (),
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

        MouseHovering = false;

        if (HighlightStyle != HighlightStyle.None)
        {
            SetNeedsDraw ();
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
        return args.Cancel;
    }

    #endregion Highlight Handling

    private void DisposeMouse () { }
}
