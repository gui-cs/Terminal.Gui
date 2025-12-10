using System.ComponentModel;
using System.Diagnostics;

namespace Terminal.Gui.ViewBase;

public partial class View // Mouse APIs
{
    /// <summary>Gets the mouse bindings for this view. By default, all mouse buttons are bound to the <see cref="Command.Activate"/> command.</summary>
    public MouseBindings MouseBindings { get; internal set; } = null!;

    private void SetupMouse ()
    {
        MouseBindings = new ();

        // TODO: Should the default really work with any button or just button1?
        MouseBindings.Add (MouseFlags.LeftButtonClicked, Command.Activate);
        MouseBindings.Add (MouseFlags.MiddleButtonClicked, Command.Activate);
        MouseBindings.Add (MouseFlags.RightButtonClicked, Command.Context);
        MouseBindings.Add (MouseFlags.Button4Clicked, Command.Activate);
        MouseBindings.Add (MouseFlags.LeftButtonClicked | MouseFlags.ButtonCtrl, Command.Context);
    }

    #region MouseEnterLeave

    /// <summary>
    ///     INTERNAL Called by <see cref="IMouse.RaiseMouseEvent"/> when the mouse moves over the View's
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

        MouseState |= MouseState.In;

        if (HighlightStates != MouseState.None)
        {
            SetNeedsDraw ();
        }

        return false;
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
    ///         See <see cref="MouseState"/> for more information.
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
    ///         See <see cref="MouseState"/> for more information.
    ///     </para>
    /// </remarks>
    public event EventHandler<CancelEventArgs>? MouseEnter;

    /// <summary>
    ///     INTERNAL Called by <see cref="IMouse.RaiseMouseEvent"/> when the mouse leaves <see cref="Frame"/>, or is
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
    ///         See <see cref="MouseState"/> for more information.
    ///     </para>
    /// </remarks>
    internal void NewMouseLeaveEvent ()
    {
        // Non-cancellable event
        OnMouseLeave ();

        MouseLeave?.Invoke (this, EventArgs.Empty);

        MouseState &= ~MouseState.In;

        // TODO: Should we also MouseState &= ~MouseState.Pressed; ??

        if (HighlightStates != MouseState.None)
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
    ///         See <see cref="MouseState"/> for more information.
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
    ///         See <see cref="MouseState"/> for more information.
    ///     </para>
    /// </remarks>
    public event EventHandler? MouseLeave;

    #endregion MouseEnterLeave

    #region Low Level Mouse Events

    /// <summary>
    ///     Gets or sets whether the <see cref="View"/> wants continuous button pressed events. When set to
    ///     <see langword="true"/>,
    ///     and the user presses and holds the mouse button, <see cref="NewMouseEvent"/> will be
    ///     repeatedly called with the same <see cref="MouseFlags"/> for as long as the mouse button remains pressed.
    /// </summary>
    public bool WantContinuousButtonPressed { get; set; }

    /// <summary>Gets or sets whether the <see cref="View"/> wants mouse position reports.</summary>
    /// <value><see langword="true"/> if mouse position reports are wanted; otherwise, <see langword="false"/>.</value>
    public bool WantMousePositionReports { get; set; }

    /// <summary>
    ///     Processes a mouse event for this view. This is the main entry point for mouse input handling,
    ///     called by <see cref="IMouse.RaiseMouseEvent"/> when the mouse interacts with this view.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method orchestrates the complete mouse event handling pipeline:
    ///     </para>
    ///     <list type="number">
    ///         <item>
    ///             <description>
    ///                 Validates pre-conditions (view must be enabled and visible)
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Raises <see cref="MouseEvent"/> for low-level handling via <see cref="OnMouseEvent"/>
    ///                 and event subscribers
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Handles mouse grab scenarios when <see cref="HighlightStates"/> or
    ///                 <see cref="WantContinuousButtonPressed"/> are set (press/release/click)
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Invokes commands bound to mouse clicks via <see cref="MouseBindings"/>
    ///                 (default: <see cref="Command.Activate"/> → <see cref="Activating"/> event)
    ///             </description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         <strong>Continuous Button Press:</strong> When <see cref="WantContinuousButtonPressed"/> is
    ///         <see langword="true"/> and the user holds a mouse button down, this method is repeatedly called
    ///         with <see cref="MouseFlags.LeftButtonPressed"/> (or other button pressed) events, enabling repeating button
    ///         behavior (e.g., scroll buttons).
    ///     </para>
    ///     <para>
    ///         <strong>Mouse Grab:</strong> Views with <see cref="HighlightStates"/> or
    ///         <see cref="WantContinuousButtonPressed"/> enabled automatically grab the mouse on button press,
    ///         receiving all subsequent mouse events until the button is released, even if the mouse moves
    ///         outside the view's <see cref="Viewport"/>.
    ///     </para>
    /// </remarks>
    /// <param name="mouseEvent">
    ///     The mouse event to process. Coordinates in <see cref="MouseEventArgs.Position"/> are relative
    ///     to the view's <see cref="Viewport"/>.
    /// </param>
    /// <returns>
    ///     <see langword="true"/> if the event was handled and should not be propagated;
    ///     <see langword="false"/> if the event was not handled and should continue propagating;
    ///     <see langword="null"/> if the view declined to handle the event (e.g., disabled or not visible).
    /// </returns>
    /// <seealso cref="MouseEvent"/>
    /// <seealso cref="OnMouseEvent"/>
    /// <seealso cref="MouseBindings"/>
    /// <seealso cref="Activating"/>
    /// <seealso cref="WantContinuousButtonPressed"/>
    /// <seealso cref="HighlightStates"/>
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

        if (MouseHeldDown is null)
        {
            MouseHeldDown = new MouseHeldDown (this, App?.TimedEvents, App?.Mouse);
        }

        if (WantContinuousButtonPressed)
        {
            if (mouseEvent.IsPressed)
            {
                MouseHeldDown.MouseIsHeldDownTick += MouseHeldDownOnMouseIsHeldDownTick;
                MouseHeldDown.Start (mouseEvent);
            }
            else
            {
                MouseHeldDown.MouseIsHeldDownTick -= MouseHeldDownOnMouseIsHeldDownTick;
                MouseHeldDown.Stop ();
            }
        }

        // Cancellable event
        if (RaiseMouseEvent (mouseEvent) || mouseEvent.Handled)
        {
            return true;
        }

        // Post-Conditions

        if (HighlightStates != MouseState.None || WantContinuousButtonPressed)
        {
            if (WhenGrabbedHandlePressed (mouseEvent))
            {
                // If we raised a command on the grabbed view, and it handled it, we are done
                // regardless of whether the event was handled.
                return true;
            }

            // This will change mouseEvent.Flags to clicked if appropriate.
            WhenGrabbedHandleReleased (mouseEvent);

            if (WhenGrabbedHandleClicked (mouseEvent))
            {
                return mouseEvent.Handled;
            }
        }

        // We get here if the view did not handle the mouse event via RaiseMouseEvent, and
        // it did not handle the commands via WhenGrabbed* methods.
        if (mouseEvent.IsSingleDoubleOrTripleClicked)
        {
            return RaiseCommandsBoundToButtonClickedFlags (mouseEvent);
        }

        if (mouseEvent.IsWheel)
        {
            return RaiseCommandsBoundToWheelFlags (mouseEvent);
        }

        return false;
    }

    /// <summary>
    ///     INTERNAL: Manages continuous button press behavior for views that have <see cref="WantContinuousButtonPressed"/> set to <see langword="true"/>.
    ///     When a mouse button is held down on such a view, this instance periodically raises events to enable auto-repeat functionality
    ///     (e.g., scrollbars that continue scrolling while the button is held, or buttons that repeat their action).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property is automatically instantiated when needed in <see cref="RaiseMouseEvent"/>. It implements an accelerating timeout
    ///         pattern where the first event fires after 500ms, with subsequent events occurring every 50ms with a 0.5 acceleration factor.
    ///     </para>
    ///     <para>
    ///         When a button press is detected, the mouse is grabbed and periodic <see cref="IMouseHeldDown.MouseIsHeldDownTick"/> events
    ///         are raised until the button is released. Each tick event triggers command execution via <see cref="RaiseCommandsBoundToButtonClickedFlags"/>,
    ///         enabling continuous actions like scrolling or button repetition.
    ///     </para>
    ///     <para>
    ///         This is used for UI elements that benefit from auto-repeat behavior, such as scrollbar arrows, spin buttons, or other
    ///         controls where holding down a button should continue the action.
    ///     </para>
    /// </remarks>
    internal IMouseHeldDown? MouseHeldDown { get; set; }

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

    private void MouseHeldDownOnMouseIsHeldDownTick (object? sender, CancelEventArgs<MouseEventArgs> e)
    {
        Logging.Trace ($"MouseHeldDown tick - raising commands bound {e.NewValue.Flags}");
        e.NewValue.ScreenPosition = App?.Mouse.LastMousePosition ?? e.NewValue.ScreenPosition;
        /*e.Cancel = */
        RaiseCommandsBoundToButtonClickedFlags (e.NewValue);
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

    #region WhenGrabbed Handlers

    /// <summary>
    ///     INTERNAL: For cases where the view is grabbed and the mouse is pressed, this method handles the pressed events from
    ///     the driver.
    ///     When  <see cref="WantContinuousButtonPressed"/> is set, this method will raise the Activate event
    ///     via <see cref="Command.Activate"/> each time it is called (after the first time the mouse is pressed).
    /// </summary>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/>, if processing should stop, <see langword="false"/> otherwise.</returns>
    private bool WhenGrabbedHandlePressed (MouseEventArgs mouseEvent)
    {
        if (!mouseEvent.IsPressed)
        {
            return false;
        }

        Debug.Assert (!mouseEvent.Handled);
        mouseEvent.Handled = false;

        // If the user has just pressed the mouse, grab the mouse and set focus
        if (App is null || App.Mouse.MouseGrabView != this)
        {
            App?.Mouse.GrabMouse (this);

            if (!HasFocus && CanFocus)
            {
                // Set the focus, but don't invoke Accept
                SetFocus ();
            }

            // This prevents raising Activate the first time the mouse is pressed.
            mouseEvent.Handled = true;
        }

        if (Viewport.Contains (mouseEvent.Position))
        {
            // The mouse is inside.
            if (HighlightStates.HasFlag (MouseState.Pressed))
            {
                MouseState |= MouseState.Pressed;
            }

            // Always clear PressedOutside when the mouse is pressed inside the Viewport
            MouseState &= ~MouseState.PressedOutside;
        }
        else
        {
            // The mouse is outside.
            // When WantContinuousButtonPressed is set we want to keep the mouse state as pressed (e.g. a repeating button).
            // This shows the user that the button is doing something, even if the mouse is outside the Viewport.
            if (HighlightStates.HasFlag (MouseState.PressedOutside) && !WantContinuousButtonPressed)
            {
                MouseState |= MouseState.PressedOutside;
            }
        }

        if (!mouseEvent.Handled && WantContinuousButtonPressed && App?.Mouse.MouseGrabView == this)
        {
            // Ignore the return value here, because the semantics of WhenGrabbedHandlePressed is the return
            // value indicates whether processing should stop or not.
            //RaiseCommandsBoundToButtonClickedFlags (mouseEvent);

            return true;
        }

        return mouseEvent.Handled = true;
    }

    /// <summary>
    ///     INTERNAL: For cases where the view is grabbed, this method handles the released events from the driver
    ///     (when <see cref="WantContinuousButtonPressed"/> or <see cref="HighlightStates"/> are set). If <see cref="MouseState"/>
    ///     is <see cref="MouseState.In"/>, this method modifies the <see cref="MouseEventArgs.Flags"/> to be the corresponding
    ///     clicked flag (e.g., <see cref="MouseFlags.LeftButtonClicked"/>).
    /// </summary>
    /// <param name="mouseEvent"></param>
    internal void WhenGrabbedHandleReleased (MouseEventArgs mouseEvent)
    {
        if (App is null || App.Mouse.MouseGrabView != this)
        {
            return;
        }

        MouseState &= ~MouseState.Pressed;
        MouseState &= ~MouseState.PressedOutside;

        if (!WantContinuousButtonPressed && MouseState.HasFlag (MouseState.In))
        {
            ConvertReleasedToClicked(mouseEvent);
        }
    }

    /// <summary>
    ///     INTERNAL: For cases where the view is grabbed, this method handles the click events from the driver
    ///     (typically
    ///     when <see cref="WantContinuousButtonPressed"/> or <see cref="HighlightStates"/> are set).
    /// </summary>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/>, if processing should stop; <see langword="false"/> otherwise.</returns>
    internal bool WhenGrabbedHandleClicked (MouseEventArgs mouseEvent)
    {
        if (App is null || App.Mouse.MouseGrabView != this || !mouseEvent.IsSingleClicked)
        {
            return false;
        }

        // We're grabbed. Clicked event comes after the last Release. This is our signal to ungrab
        App?.Mouse.UngrabMouse ();

        // TODO: Prove we need to unset MouseState.Pressed and MouseState.PressedOutside here
        // TODO: There may be perf gains if we don't unset these flags here
        MouseState &= ~MouseState.Pressed;
        MouseState &= ~MouseState.PressedOutside;

        // If mouse is still in bounds, return false to indicate a click should be raised.
        return !Viewport.Contains (mouseEvent.Position);
    }

    #endregion WhenGrabbed Handlers

    #region Command Invocation

    /// <summary>
    ///     INTERNAL API: Converts mouse click events into <see cref="Command"/>s by invoking the commands bound
    ///     to the mouse buttons via <see cref="MouseBindings"/>. By default, all mouse clicks are bound to
    ///     <see cref="Command.Activate"/> which raises the <see cref="Activating"/> event.
    /// </summary>
    /// <param name="args">The mouse event arguments containing the mouse flags and position information.</param>
    /// <returns>
    ///     <see langword="true"/> if a command was invoked and handled; <see langword="false"/> if no command was invoked
    ///     or the command was not handled. Also sets <see cref="HandledEventArgs.Handled"/> on the input <paramref name="args"/>
    ///     .
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         The converted click event is then passed to <see cref="InvokeCommandsBoundToMouse"/> to execute
    ///         any commands bound to the mouse flags via <see cref="MouseBindings"/>. By default, all mouse clicks
    ///         are bound to <see cref="Command.Activate"/>, which raises the <see cref="Activating"/> event.
    ///     </para>
    /// </remarks>
    protected bool RaiseCommandsBoundToButtonClickedFlags (MouseEventArgs args)
    {
        // Pre-conditions
        if (!Enabled)
        {
            // QUESTION: Is this right? Should a disabled view eat mouse clicks?
            return args.Handled = false;
        }

        // The MouseBindings system binds commands to clicked events (like LeftButtonClicked),
        // but the actual mouse events coming from the driver are often pressed events (LeftButtonPressed).
        // This switch expression bridges that gap by converting pressed events to clicked
        // events so they can be matched against the command bindings.
        ConvertPressedToClicked (args);

        //Logging.Trace ($"Invoking commands bound to mouse: {args.Flags}");
        // By default, this will raise Activating/OnActivating - Subclasses can override this via
        // ReplaceCommand (Command.Activate ...).
        args.Handled = InvokeCommandsBoundToMouse (args) == true;

        return args.Handled;
    }


    private static void ConvertPressedToClicked (MouseEventArgs args)
    {
        if (!args.IsPressed)
        {
            return;
        }

        args.Flags = args.Flags switch
        {
            MouseFlags.LeftButtonPressed => MouseFlags.LeftButtonClicked,
            MouseFlags.MiddleButtonPressed => MouseFlags.MiddleButtonClicked,
            MouseFlags.RightButtonPressed => MouseFlags.RightButtonClicked,
            MouseFlags.Button4Pressed => MouseFlags.Button4Clicked,
            _ => args.Flags
        };
    }

    private static void ConvertReleasedToClicked (MouseEventArgs args)
    {
        if (!args.IsReleased)
        {
            return;
        }

        args.Flags = args.Flags switch
        {
            MouseFlags.LeftButtonReleased => MouseFlags.LeftButtonClicked,
            MouseFlags.MiddleButtonReleased => MouseFlags.MiddleButtonClicked,
            MouseFlags.RightButtonReleased => MouseFlags.RightButtonClicked,
            MouseFlags.Button4Released => MouseFlags.Button4Clicked,
            _ => args.Flags
        };
    }

    /// <summary>
    ///     INTERNAL API: Converts mouse wheel events into <see cref="Command"/>s by invoking the commands bound
    ///     to the mouse wheel via <see cref="MouseBindings"/>. By default, all mouse wheel events are not bound.
    /// </summary>
    /// <param name="args">The mouse event arguments containing the mouse flags and position information.</param>
    /// <returns>
    ///     <see langword="true"/> if a command was invoked and handled; <see langword="false"/> if no command was invoked
    ///     or the command was not handled.
    ///     .
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         The converted wheel event is then passed to <see cref="InvokeCommandsBoundToMouse"/> to execute
    ///         any commands bound to the mouse flags via <see cref="MouseBindings"/>.
    ///     </para>
    /// </remarks>
    protected bool RaiseCommandsBoundToWheelFlags (MouseEventArgs args)
    {
        // Pre-conditions
        if (!Enabled)
        {
            // QUESTION: Is this right? Should a disabled view eat mouse wheel?
            return args.Handled = false;
        }

        args.Handled = InvokeCommandsBoundToMouse (args) == true;

        return args.Handled;
    }


    /// <summary>
    ///     INTERNAL API: Invokes the Commands bound to the MouseFlags specified by <paramref name="mouseEventArgs"/>.
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

    #endregion Command Invocation

    #region MouseState Handling

    private MouseState _mouseState;

    /// <summary>
    ///     Gets the state of the mouse relative to the View. When changed, the <see cref="MouseStateChanged"/>/
    ///     <see cref="OnMouseStateChanged"/>
    ///     event will be raised.
    /// </summary>
    public MouseState MouseState
    {
        get => _mouseState;
        internal set
        {
            if (_mouseState == value)
            {
                return;
            }

            EventArgs<MouseState> args = new (value);

            RaiseMouseStateChanged (args);

            _mouseState = value;
        }
    }

    /// <summary>
    ///     Gets or sets which <see cref="MouseState"/> changes should cause the View to change its appearance.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="MouseState.In"/> is set by default, which means the View will be highlighted when the
    ///         mouse is over it. The default behavior of <see cref="SetAttributeForRole"/>
    ///         is to use the <see cref="Drawing.VisualRole.Highlight"/> role for the highlight Attribute.
    ///     </para>
    ///     <para>
    ///         <see cref="MouseState.Pressed"/> means the View will be highlighted when the mouse is pressed over it.
    ///         <see cref="Border"/>'s default behavior is to use
    ///         the <see cref="VisualRole.Highlight"/> role when the Border is pressed for Arrangement.
    ///         <see cref="Margin"/>'s default behavior, when shadows are enabled, is to move the shadow providing
    ///         a pressed effect.
    ///     </para>
    ///     <para>
    ///         <see cref="MouseState.PressedOutside"/> means the View will be highlighted when the mouse was pressed
    ///         inside it and then moved outside of it, unless <see cref="WantContinuousButtonPressed"/> is set to
    ///         <see langword="true"/>, in which case the flag has no effect.
    ///     </para>
    /// </remarks>
    public MouseState HighlightStates { get; set; }

    /// <summary>
    ///     INTERNAL Raises the <see cref="MouseStateChanged"/> event.
    /// </summary>
    /// <param name="args"></param>
    private void RaiseMouseStateChanged (EventArgs<MouseState> args)
    {
        //Logging.Debug ($"{Id} - {args.Value} -> {args.Value}");

        OnMouseStateChanged (args);

        MouseStateChanged?.Invoke (this, args);
    }

    /// <summary>
    ///     Called when <see cref="MouseState"/> has changed, indicating the View should be highlighted or not. The
    ///     <see cref="MouseState"/> passed in the event
    ///     indicates the highlight style that will be applied.
    /// </summary>
    protected virtual void OnMouseStateChanged (EventArgs<MouseState> args) { }

    /// <summary>
    ///     Raised when <see cref="MouseState"/> has changed, indicating the View should be highlighted or not. The
    ///     <see cref="MouseState"/> passed in the event
    ///     indicates the highlight style that will be applied.
    /// </summary>
    public event EventHandler<EventArgs<MouseState>>? MouseStateChanged;

    #endregion MouseState Handling

    private void DisposeMouse ()
    {
        if (MouseHeldDown is { })
        {
            MouseHeldDown.MouseIsHeldDownTick -= MouseHeldDownOnMouseIsHeldDownTick;
            MouseHeldDown.Dispose ();
        }

        if (App?.Mouse.MouseGrabView == this)
        {
            App.Mouse.UngrabMouse ();
        }
    }
}
