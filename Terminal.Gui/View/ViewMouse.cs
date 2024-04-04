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
    ///     Called when the mouse enters the View's <see cref="Bounds"/>. The view will now receive mouse events until the
    ///     mouse leaves
    ///     the view. At which time, <see cref="OnMouseLeave(Gui.MouseEvent)"/> will be called.
    /// </summary>
    /// <remarks>
    ///     The coordinates are relative to <see cref="View.Bounds"/>.
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
    ///     Called when the mouse has moved out of the View's <see cref="Bounds"/>. The view will no longer receive mouse
    ///     events (until the
    ///     mouse moves within the view again and <see cref="OnMouseEnter(Gui.MouseEvent)"/> is called).
    /// </summary>
    /// <remarks>
    ///     The coordinates are relative to <see cref="View.Bounds"/>.
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

    [CanBeNull]
    private ColorScheme _savedColorScheme;

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
        if (!Enabled)
        {
            // A disabled view should not eat mouse events
            return false;
        }

        if (!CanBeVisible (this))
        {
            return false;
        }

        var args = new MouseEventEventArgs (mouseEvent);

        // Default behavior is to invoke Accept (via HotKey) on clicked.
        if (
             !HighlightOnPress
            && Application.MouseGrabView is null
            && (mouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked)
                || mouseEvent.Flags.HasFlag (MouseFlags.Button2Clicked)
                || mouseEvent.Flags.HasFlag (MouseFlags.Button3Clicked)
                || mouseEvent.Flags.HasFlag (MouseFlags.Button4Clicked)))
        { 
            return OnMouseClick (args);
        }

        if (!HighlightOnPress)
        {
            return false;
        }

        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button2Pressed)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button3Pressed)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button4Pressed))
        {
            // If WantContinuousButtonPressed is true, and this is not the first pressed event,
            // invoke Accept (via HotKey)
            if (WantContinuousButtonPressed && Application.MouseGrabView == this)
            {
                return OnMouseClick (args);
            }

            // The first time we get pressed event, grab the mouse and invert the colors
            if (Application.MouseGrabView != this)
            {
                Application.GrabMouse (this);

                if (HighlightOnPress && ColorScheme is { })
                {
                    _savedColorScheme = ColorScheme;
                    if (CanFocus)
                    {
                        // TODO: Make the inverted color configurable
                        var cs = new ColorScheme (ColorScheme)
                        {
                            Focus = new (ColorScheme.Normal.Foreground, ColorScheme.Focus.Background)
                        };
                        ColorScheme = cs;
                    }
                    else
                    {
                        var cs = new ColorScheme (ColorScheme)
                        {
                            Normal = new (ColorScheme.Focus.Background, ColorScheme.Normal.Foreground)
                        };
                        ColorScheme = cs;
                    }
                }

                if (CanFocus)
                {
                    // Set the focus, but don't invoke Accept
                    SetFocus ();
                }
            }
            args.Handled = true;
        }

        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Released)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button2Released)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button3Released)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button4Released))
        {

            if (Application.MouseGrabView == this)
            {
                // When the mouse is released, if WantContinuousButtonPressed is set, invoke Accept one last time.
                //if (WantContinuousButtonPressed)
                {
                    OnMouseClick (args);
                }

                Application.UngrabMouse ();

                if (HighlightOnPress && _savedColorScheme is { })
                {
                    ColorScheme = _savedColorScheme;
                    _savedColorScheme = null;
                }
            }
            args.Handled = true;
        }

        if (args.Handled != true)
        {
            MouseEvent?.Invoke (this, args);
        }

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
            args.Handled = true;

            return true;
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
