using System.ComponentModel;
using System.Diagnostics;

namespace Terminal.Gui;

public partial class View
{
    /// <summary>
    ///     Gets or sets whether the <see cref="View"/> will be highlighted visually while the mouse button is
    ///     pressed.
    /// </summary>
    public bool HighlightOnPress { get; set; }

    /// <summary>Gets or sets whether the <see cref="View"/> wants continuous button pressed events.</summary>
    public virtual bool WantContinuousButtonPressed { get; set; }

    /// <summary>Gets or sets whether the <see cref="View"/> wants mouse position reports.</summary>
    /// <value><see langword="true"/> if mouse position reports are wanted; otherwise, <see langword="false"/>.</value>
    public virtual bool WantMousePositionReports { get; set; }

    /// <summary>
    ///     Called when the mouse enters the View's <see cref="Viewport"/>. The view will now receive mouse events until the mouse leaves
    ///     the view. At which time, <see cref="OnMouseLeave(Gui.MouseEvent)"/> will be called.
    /// </summary>
    /// <remarks>
    /// The coordinates are relative to <see cref="Viewport"/>.
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    protected internal virtual bool OnMouseEnter (MouseEvent mouseEvent)
    {
        if (!Enabled)
        {
            return true;
        }

        if (!CanBeVisible (this))
        {
            return false;
        }

        var args = new MouseEventEventArgs (mouseEvent);
        MouseEnter?.Invoke (this, args);

        return args.Handled;
    }

    /// <summary>Event fired when the mouse moves into the View's <see cref="Bounds"/>.</summary>
    public event EventHandler<MouseEventEventArgs> MouseEnter;

    /// <summary>
    ///     Called when the mouse has moved out of the View's <see cref="Viewport"/>. The view will no longer receive mouse events (until the
    ///     mouse moves within the view again and <see cref="OnMouseEnter(Gui.MouseEvent)"/> is called).
    /// </summary>
    /// <remarks>
    /// The coordinates are relative to <see cref="Viewport"/>.
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    protected internal virtual bool OnMouseLeave (MouseEvent mouseEvent)
    {
        if (!Enabled)
        {
            return true;
        }

        if (!CanBeVisible (this))
        {
            return false;
        }

        var args = new MouseEventEventArgs (mouseEvent);
        MouseLeave?.Invoke (this, args);

        return args.Handled;
    }

    /// <summary>Event fired when the mouse leaves the View's <see cref="Bounds"/>.</summary>
    public event EventHandler<MouseEventEventArgs> MouseLeave;

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
    ///         If <see cref="HighlightOnPress"/> is <see langword="true"/>, the view will be highlighted when the mouse is
    ///         pressed.
    ///         See <see cref="EnableHighlight"/> and <see cref="DisableHighlight"/> for more information.
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

        if (OnMouseEvent (mouseEvent))
        {
            // Technically mouseEvent.Handled should already be true if implementers of OnMouseEvent
            // follow the rules. But we'll update it just in case.
            return mouseEvent.Handled = true;
        }

        if ((HighlightOnPress || WantContinuousButtonPressed) && Highlight (mouseEvent))
        {
            Debug.Assert (mouseEvent.Handled);

            return mouseEvent.Handled;
        }

        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button2Clicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button3Clicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button4Clicked))
        {
            return OnMouseClick (new (mouseEvent));
        }

        return false;
    }

    /// <summary>
    ///     Highlight the view when the mouse is pressed.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Set <see cref="HighlightOnPress"/> to <see langword="true"/> to have the view highlighted when the mouse is
    ///         pressed.
    ///     </para>
    ///     <para>
    ///         Calls <see cref="OnEnablingHighlight"/> which fires the <see cref="EnablingHighlight"/> event.
    ///     </para>
    ///     <para>
    ///         Calls <see cref="OnDisablingHighlight"/> which fires the <see cref="DisablingHighlight"/> event.
    ///     </para>
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    private bool Highlight (MouseEvent mouseEvent)
    {
        if (Application.MouseGrabView == this
            && (mouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked)
                || mouseEvent.Flags.HasFlag (MouseFlags.Button2Clicked)
                || mouseEvent.Flags.HasFlag (MouseFlags.Button3Clicked)
                || mouseEvent.Flags.HasFlag (MouseFlags.Button4Clicked)))
        {
            // We're grabbed. Clicked event comes after the last Release. This is our signal to ungrab
            Application.UngrabMouse ();
            DisableHighlight ();

            // If mouse is still in bounds, click
            if (!WantContinuousButtonPressed && Viewport.Contains (mouseEvent.X, mouseEvent.Y))
            {
                return OnMouseClick (mouseEvent);
            }

            return mouseEvent.Handled = true;
        }

        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button2Pressed)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button3Pressed)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button4Pressed))
        {
            // The first time we get pressed event, grab the mouse and set focus
            if (Application.MouseGrabView != this)
            {
                Application.GrabMouse (this);

                if (CanFocus)
                {
                    // Set the focus, but don't invoke Accept
                    SetFocus ();
                }

                args.Handled = true;
            }

            if (Viewport.Contains (mouseEvent.X, mouseEvent.Y))
            {
                EnableHighlight ();
            }
            else
            {
                DisableHighlight ();
            }

            if (WantContinuousButtonPressed && Application.MouseGrabView == this)
            {
                // If this is not the first pressed event, click
                return OnMouseClick (new (mouseEvent));
            }

            return mouseEvent.Handled = true;
        }

        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Released)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button2Released)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button3Released)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button4Released))
        {
            if (Application.MouseGrabView == this)
            {
                DisableHighlight ();
            }

            return mouseEvent.Handled = true;
        }

        return mouseEvent.Handled;
    }

    [CanBeNull]
    private ColorScheme _savedHighlightColorScheme;

    /// <summary>
    ///     Enables the highlight for the view. Called from OnMouseEvent.
    /// </summary>
    public void EnableHighlight ()
    {
        if (OnEnablingHighlight () == true)
        {
            return;
        }

        if (_savedHighlightColorScheme is null && ColorScheme is { })
        {
            _savedHighlightColorScheme ??= ColorScheme;

            if (CanFocus)
            {
                // TODO: Make the inverted color configurable
                var cs = new ColorScheme (ColorScheme)
                {
                    // For Buttons etc...
                    Focus = new (ColorScheme.Normal.Foreground, ColorScheme.Focus.Background),

                    // For Adornments
                    Normal = new (ColorScheme.Focus.Foreground, ColorScheme.Normal.Background)
                };
                ColorScheme = cs;
            }
            else
            {
                var cs = new ColorScheme (ColorScheme)
                {
                    // For Buttons etc... that can't focus (like up/down).
                    Normal = new (ColorScheme.Focus.Background, ColorScheme.Normal.Foreground)
                };
                ColorScheme = cs;
            }
        }
    }

    /// <summary>
    ///     Fired when the view is highlighted. Set <see cref="CancelEventArgs.Cancel"/> to <see langword="true"/>
    ///     to implement a custom highlight scheme or prevent the view from being highlighted.
    /// </summary>
    public event EventHandler<CancelEventArgs> EnablingHighlight;

    /// <summary>
    ///     Called when the view is to be highlighted.
    /// </summary>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    protected virtual bool? OnEnablingHighlight ()
    {
        CancelEventArgs args = new ();
        EnablingHighlight?.Invoke (this, args);

        return args.Cancel;
    }

    /// <summary>
    ///     Disables the highlight for the view. Called from OnMouseEvent.
    /// </summary>
    public void DisableHighlight ()
    {
        if (OnDisablingHighlight () == true)
        {
            return;
        }

        // Unhighlight
        if (_savedHighlightColorScheme is { })
        {
            ColorScheme = _savedHighlightColorScheme;
            _savedHighlightColorScheme = null;
        }
    }

    /// <summary>
    ///     Fired when the view is no longer to be highlighted. Set <see cref="CancelEventArgs.Cancel"/> to
    ///     <see langword="true"/>
    ///     to implement a custom highlight scheme or prevent the view from being highlighted.
    /// </summary>
    public event EventHandler<CancelEventArgs> DisablingHighlight;

    /// <summary>
    ///     Called when the view is no longer to be highlighted.
    /// </summary>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    protected virtual bool? OnDisablingHighlight ()
    {
        CancelEventArgs args = new ();
        DisablingHighlight?.Invoke (this, args);

        return args.Cancel;
    }

    /// <summary>Called when a mouse event occurs within the view's <see cref="Bounds"/>.</summary>
    /// <remarks>
    ///     <para>
    ///         The coordinates are relative to <see cref="View.Bounds"/>.
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

    /// <summary>Event fired when a mouse event occurs.</summary>
    /// <remarks>
    ///     <para>
    ///         The coordinates are relative to <see cref="View.Bounds"/>.
    ///     </para>
    /// </remarks>
    public event EventHandler<MouseEventEventArgs> MouseEvent;

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
            return args.Handled = true;
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

    /// <summary>Event fired when a mouse click occurs.</summary>
    /// <remarks>
    ///     <para>
    ///         Fired when the mouse is either clicked or double-clicked. Check
    ///         <see cref="MouseEvent.Flags"/> to see which button was clicked.
    ///     </para>
    ///     <para>
    ///         The coordinates are relative to <see cref="View.Bounds"/>.
    ///     </para>
    /// </remarks>
    public event EventHandler<MouseEventEventArgs> MouseClick;
}
