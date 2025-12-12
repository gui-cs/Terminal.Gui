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

        // TODO: Should the default really work with any button or just LeftButton?
        MouseBindings.Add (MouseFlags.LeftButtonPressed, Command.Activate);
        MouseBindings.Add (MouseFlags.MiddleButtonPressed, Command.Activate);
        MouseBindings.Add (MouseFlags.Button4Pressed, Command.Activate);
        MouseBindings.Add (MouseFlags.RightButtonPressed, Command.Context);
        MouseBindings.Add (MouseFlags.LeftButtonPressed | MouseFlags.Ctrl, Command.Context);

        MouseBindings.Add (MouseFlags.LeftButtonClicked, Command.Accept);
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

        if (MouseHighlightStates != MouseState.None)
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

        if (MouseHighlightStates != MouseState.None)
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
    public bool MouseHoldRepeat { get; set; }

    /// <summary>Gets or sets whether the <see cref="View"/> wants mouse position reports.</summary>
    /// <value><see langword="true"/> if mouse position reports are wanted; otherwise, <see langword="false"/>.</value>
    public bool MousePositionTracking  { get; set; }

    /// <summary>
    ///     Gets whether auto-grab should be enabled for this view based on <see cref="MouseHighlightStates"/> 
    ///     or <see cref="MouseHoldRepeat"/> being set.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When <see langword="true"/>, the view will automatically grab the mouse on button press and 
    ///         ungrab on button release (clicked event), capturing all mouse events during the press-release cycle.
    ///     </para>
    ///     <para>
    ///         Auto-grab is enabled when either <see cref="MouseHighlightStates"/> is not <see cref="MouseState.None"/>
    ///         (the view wants visual feedback) or <see cref="MouseHoldRepeat"/> is <see langword="true"/>
    ///         (the view wants continuous press events).
    ///     </para>
    /// </remarks>
    private bool ShouldAutoGrab => MouseHighlightStates != MouseState.None || MouseHoldRepeat;

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
    ///                 Handles mouse grab scenarios when <see cref="MouseHighlightStates"/> or
    ///                 <see cref="MouseHoldRepeat"/> are set (press/release/click)
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
    ///         <strong>Continuous Button Press:</strong> When <see cref="MouseHoldRepeat"/> is
    ///         <see langword="true"/> and the user holds a mouse button down, this method is repeatedly called
    ///         with <see cref="MouseFlags.LeftButtonPressed"/> (or other button pressed) events, enabling repeating button
    ///         behavior (e.g., scroll buttons).
    ///     </para>
    ///     <para>
    ///         <strong>Mouse Grab:</strong> Views with <see cref="MouseHighlightStates"/> or
    ///         <see cref="MouseHoldRepeat"/> enabled automatically grab the mouse on button press,
    ///         receiving all subsequent mouse events until the button is released, even if the mouse moves
    ///         outside the view's <see cref="Viewport"/>.
    ///     </para>
    /// </remarks>
    /// <param name="mouse">
    ///     The mouse event to process. Coordinates in <see cref="Mouse.Position"/> are relative
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
    /// <seealso cref="MouseHoldRepeat"/>
    /// <seealso cref="MouseHighlightStates"/>
    public bool? NewMouseEvent (Mouse mouse)
    {
        // 1. Pre-conditions
        if (mouse.Position is null)
        {
            // Support unit tests that don't set Position
            mouse.Position = mouse.ScreenPosition;
        }

        if (!Enabled)
        {
            // A disabled view should not eat mouse events
            return false;
        }

        if (!CanBeVisible (this))
        {
            return false;
        }

        if (!MousePositionTracking && mouse.Flags == MouseFlags.PositionReport)
        {
            return false;
        }

        // 2. Setup MouseHoldRepeater if needed
        if (MouseHoldRepeater is null)
        {
            MouseHoldRepeater = new MouseHoldRepeaterImpl (this, App?.TimedEvents, App?.Mouse);
        }

        // 3. MouseHoldRepeat timer management
        if (MouseHoldRepeat)
        {
            if (mouse.IsPressed)
            {
                MouseHoldRepeater.MouseIsHeldDownTick += MouseHoldRepeaterOnMouseIsHeldDownTick;
                MouseHoldRepeater.Start (mouse);
            }
            else
            {
                MouseHoldRepeater.MouseIsHeldDownTick -= MouseHoldRepeaterOnMouseIsHeldDownTick;
                MouseHoldRepeater.Stop ();
            }
        }

        // 4. Low-level MouseEvent (cancellable)
        if (RaiseMouseEvent (mouse) || mouse.Handled)
        {
            return true;
        }

        // 5. Auto-grab lifecycle
        if (ShouldAutoGrab)
        {
            if (mouse.IsPressed)
            {
                if (HandleAutoGrabPress (mouse))
                {
                    return true;
                }
            }
            else if (mouse.IsReleased)
            {
                HandleAutoGrabRelease (mouse);
            }
            else if (mouse.IsSingleClicked)
            {
                if (HandleAutoGrabClicked (mouse))
                {
                    return mouse.Handled;
                }
            }
        }

        // 6. Command invocation
        if (mouse.IsSingleDoubleOrTripleClicked)
        {
            return RaiseCommandsBoundToButtonClickedFlags (mouse);
        }

        if (mouse.IsWheel)
        {
            return RaiseCommandsBoundToWheelFlags (mouse);
        }

        return false;
    }

    /// <summary>
    ///     INTERNAL: Manages continuous button press behavior for views that have <see cref="MouseHoldRepeat"/> set to <see langword="true"/>.
    ///     When a mouse button is held down on such a view, this instance periodically raises events to enable auto-repeat functionality
    ///     (e.g., scrollbars that continue scrolling while the button is held, or buttons that repeat their action).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property is automatically instantiated when needed in <see cref="RaiseMouseEvent"/>. It implements an accelerating timeout
    ///         pattern where the first event fires after 500ms, with subsequent events occurring every 50ms with a 0.5 acceleration factor.
    ///     </para>
    ///     <para>
    ///         When a button press is detected, the mouse is grabbed and periodic <see cref="IMouseHoldRepeater.MouseIsHeldDownTick"/> events
    ///         are raised until the button is released. Each tick event triggers command execution via <see cref="RaiseCommandsBoundToButtonClickedFlags"/>,
    ///         enabling continuous actions like scrolling or button repetition.
    ///     </para>
    ///     <para>
    ///         This is used for UI elements that benefit from auto-repeat behavior, such as scrollbar arrows, spin buttons, or other
    ///         controls where holding down a button should continue the action.
    ///     </para>
    /// </remarks>
    internal IMouseHoldRepeater? MouseHoldRepeater { get; set; }

    /// <summary>
    ///     Raises the <see cref="RaiseMouseEvent"/>/<see cref="MouseEvent"/> event.
    /// </summary>
    /// <param name="mouse"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    public bool RaiseMouseEvent (Mouse mouse)
    {
        if (OnMouseEvent (mouse) || mouse.Handled)
        {
            return true;
        }

        MouseEvent?.Invoke (this, mouse);

        return mouse.Handled;
    }

    // LEGACY - Can be rewritten
    private void MouseHoldRepeaterOnMouseIsHeldDownTick (object? sender, CancelEventArgs<Mouse> e)
    {
        Logging.Trace ($"MouseHoldRepeater tick - raising commands bound {e.NewValue.Flags}");
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
    /// <param name="mouse"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    protected virtual bool OnMouseEvent (Mouse mouse) { return false; }

    /// <summary>Raised when a mouse event occurs.</summary>
    /// <remarks>
    ///     <para>
    ///         The coordinates are relative to <see cref="View.Viewport"/>.
    ///     </para>
    /// </remarks>
    public event EventHandler<Mouse>? MouseEvent;

    #endregion Low Level Mouse Events

    #region Auto-Grab Lifecycle Helpers

    /// <summary>
    ///     Handles the pressed event when auto-grab is enabled. Grabs the mouse, sets focus if needed, 
    ///     and updates <see cref="MouseState"/>.
    /// </summary>
    /// <param name="mouse">The mouse event.</param>
    /// <returns><see langword="true"/> if processing should stop; <see langword="false"/> otherwise.</returns>
    private bool HandleAutoGrabPress (Mouse mouse)
    {
        if (!mouse.IsPressed)
        {
            return false;
        }

        // If the user has just pressed the mouse, grab the mouse and set focus
        if (App is null || App.Mouse.MouseGrabView != this)
        {
            App?.Mouse.GrabMouse (this);

            if (!HasFocus && CanFocus)
            {
                // Set the focus, but don't invoke Accept
                SetFocus ();
            }

            // This prevents raising commands the first time the mouse is pressed
            mouse.Handled = true;
        }

        // Update MouseState based on position
        UpdateMouseStateOnPress (mouse.Position);

        return mouse.Handled;
    }

    /// <summary>
    ///     Handles the released event when auto-grab is enabled. Updates <see cref="MouseState"/> and converts
    ///     released to clicked if appropriate.
    /// </summary>
    /// <param name="mouse">The mouse event.</param>
    private void HandleAutoGrabRelease (Mouse mouse)
    {
        if (!mouse.IsReleased)
        {
            return;
        }

        if (App is null || App.Mouse.MouseGrabView != this)
        {
            return;
        }

        // Update MouseState
        UpdateMouseStateOnRelease ();

        // Convert Released to Clicked for command invocation if mouse is still inside
        if (!MouseHoldRepeat && MouseState.HasFlag (MouseState.In))
        {
            ConvertReleasedToClicked (mouse);
        }
    }

    /// <summary>
    ///     Handles the clicked event when auto-grab is enabled. Ungrabs the mouse.
    /// </summary>
    /// <param name="mouse">The mouse event.</param>
    /// <returns>
    ///     <see langword="true"/> if the click was outside the viewport (should stop processing); 
    ///     <see langword="false"/> if the click was inside (should continue to invoke commands).
    /// </returns>
    private bool HandleAutoGrabClicked (Mouse mouse)
    {
        if (!mouse.IsSingleClicked)
        {
            return false;
        }

        if (App is null || App.Mouse.MouseGrabView != this)
        {
            return false;
        }

        // We're grabbed. Clicked event comes after the last Release. This is our signal to ungrab
        App.Mouse.UngrabMouse ();

        // If mouse is still in bounds, return false to indicate commands should be raised
        return !Viewport.Contains (mouse.Position!.Value);
    }

    /// <summary>
    ///     Updates <see cref="MouseState"/> when a button is pressed, setting <see cref="MouseState.Pressed"/>
    ///     or <see cref="MouseState.PressedOutside"/> as appropriate.
    /// </summary>
    /// <param name="position">The mouse position relative to the view's viewport.</param>
    private void UpdateMouseStateOnPress (Point? position)
    {
        if (position is { } pos && Viewport.Contains (pos))
        {
            // The mouse is inside the viewport
            if (MouseHighlightStates.HasFlag (MouseState.Pressed))
            {
                MouseState |= MouseState.Pressed;
            }

            // Always clear PressedOutside when the mouse is pressed inside the Viewport
            MouseState &= ~MouseState.PressedOutside;
        }
        else
        {
            // The mouse is outside the viewport
            // When MouseHoldRepeat is set we want to keep the mouse state as pressed (e.g., a repeating button).
            // This shows the user that the button is doing something, even if the mouse is outside the Viewport.
            if (MouseHighlightStates.HasFlag (MouseState.PressedOutside) && !MouseHoldRepeat)
            {
                MouseState |= MouseState.PressedOutside;
            }
        }
    }

    /// <summary>
    ///     Updates <see cref="MouseState"/> when a button is released, clearing <see cref="MouseState.Pressed"/>
    ///     and <see cref="MouseState.PressedOutside"/> flags.
    /// </summary>
    private void UpdateMouseStateOnRelease ()
    {
        MouseState &= ~MouseState.Pressed;
        MouseState &= ~MouseState.PressedOutside;
    }

    #endregion Auto-Grab Lifecycle Helpers

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
    protected bool RaiseCommandsBoundToButtonClickedFlags (Mouse args)
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


    private static void ConvertPressedToClicked (Mouse args)
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

    private static void ConvertReleasedToClicked (Mouse args)
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
    protected bool RaiseCommandsBoundToWheelFlags (Mouse args)
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
    protected bool? InvokeCommandsBoundToMouse (Mouse mouseEventArgs)
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
    ///         inside it and then moved outside of it, unless <see cref="MouseHoldRepeat"/> is set to
    ///         <see langword="true"/>, in which case the flag has no effect.
    ///     </para>
    /// </remarks>
    public MouseState MouseHighlightStates { get; set; }

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
        if (MouseHoldRepeater is { })
        {
            MouseHoldRepeater.MouseIsHeldDownTick -= MouseHoldRepeaterOnMouseIsHeldDownTick;
            MouseHoldRepeater.Dispose ();
        }

        if (App?.Mouse.MouseGrabView == this)
        {
            App.Mouse.UngrabMouse ();
        }
    }
}
