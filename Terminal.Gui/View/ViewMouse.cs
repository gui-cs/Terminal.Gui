namespace Terminal.Gui;

public partial class View
{
    /// <summary>
    /// Gets or sets whether the <see cref="View"/> will highlight the view visually when the mouse button is pressed/released.
    /// </summary>
    public bool HighlightOnPress { get; set; }

    /// <summary>Gets or sets a value indicating whether this <see cref="View"/> want continuous button pressed event.</summary>
    public virtual bool WantContinuousButtonPressed { get; set; }

    /// <summary>Gets or sets a value indicating whether this <see cref="View"/> wants mouse position reports.</summary>
    /// <value><see langword="true"/> if want mouse position reports; otherwise, <see langword="false"/>.</value>
    public virtual bool WantMousePositionReports { get; set; }

    /// <summary>Event fired when a mouse event occurs.</summary>
    /// <remarks>
    /// <para>
    /// The coordinates are relative to <see cref="View.Bounds"/>.
    /// </para>
    /// </remarks>
    public event EventHandler<MouseEventEventArgs> MouseEvent;

    /// <summary>Event fired when a mouse click occurs.</summary>
    /// <remarks>
    /// <para>
    /// Fired when the mouse is either clicked or double-clicked. Check
    /// <see cref="MouseEvent.Flags"/> to see which button was clicked.
    /// </para>
    /// <para>
    /// The coordinates are relative to <see cref="View.Bounds"/>.
    /// </para>
    /// </remarks>
    public event EventHandler<MouseEventEventArgs> MouseClick;

    /// <summary>Event fired when the mouse moves into the View's <see cref="Bounds"/>.</summary>
    public event EventHandler<MouseEventEventArgs> MouseEnter;

    /// <summary>Event fired when the mouse leaves the View's <see cref="Bounds"/>.</summary>
    public event EventHandler<MouseEventEventArgs> MouseLeave;

    // TODO: OnMouseEnter should not be public virtual, but protected.
    /// <summary>
    ///     Called when the mouse enters the View's <see cref="Bounds"/>. The view will now receive mouse events until the mouse leaves
    ///     the view. At which time, <see cref="OnMouseLeave(Gui.MouseEvent)"/> will be called.
    /// </summary>
    /// <remarks>
    /// The coordinates are relative to <see cref="View.Bounds"/>.
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

    // TODO: OnMouseLeave should not be public virtual, but protected.
    /// <summary>
    ///     Called when the mouse has moved out of the View's <see cref="Bounds"/>. The view will no longer receive mouse events (until the
    ///     mouse moves within the view again and <see cref="OnMouseEnter(Gui.MouseEvent)"/> is called).
    /// </summary>
    /// <remarks>
    /// The coordinates are relative to <see cref="View.Bounds"/>.
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

    [CanBeNull]
    private ColorScheme _savedColorScheme;

    // TODO: OnMouseEvent should not be public virtual, but protected.
    /// <summary>Called when a mouse event occurs within the view's <see cref="Bounds"/>.</summary>
    /// <remarks>
    /// <para>
    /// The coordinates are relative to <see cref="View.Bounds"/>.
    /// </para>
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
        if ((!WantContinuousButtonPressed &&
             Application.MouseGrabView != this &&
             mouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked))
            || mouseEvent.Flags.HasFlag (MouseFlags.Button2Clicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button3Clicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button4Clicked))
        {
            return OnMouseClick (args);
        }

        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed))
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
                _savedColorScheme = ColorScheme;

                if (HighlightOnPress && ColorScheme is { })
                {
                    if (CanFocus)
                    {
                        // TODO: Make the inverted color configurable
                        var cs = new ColorScheme (ColorScheme)
                        {
                            Focus = new Attribute (ColorScheme.Normal.Foreground, ColorScheme.Focus.Background)
                        };
                        ColorScheme = cs;
                    }
                    else
                    {
                        var cs = new ColorScheme (ColorScheme)
                        {
                            Normal = new Attribute (ColorScheme.Focus.Background, ColorScheme.Normal.Foreground)
                        };
                        ColorScheme = cs;
                    }
                }

                if (CanFocus){
                // Set the focus, but don't invoke Accept
                SetFocus ();
                }
            }
        }

        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Released))
        {
            // When the mouse is released, if WantContinuousButtonPressed is set, invoke Accept one last time.
            if (WantContinuousButtonPressed)
            {
                OnMouseClick (args);
            }

            if (Application.MouseGrabView == this)
            {
                Application.UngrabMouse ();
                if (HighlightOnPress && _savedColorScheme is { })
                {
                    ColorScheme = _savedColorScheme;
                    _savedColorScheme = null;
                }
            }
        }

        //// Clicked support for all buttons and single and double click
        //if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked)
        //    || mouseEvent.Flags.HasFlag (MouseFlags.Button2Clicked)
        //    || mouseEvent.Flags.HasFlag (MouseFlags.Button3Clicked)
        //    || mouseEvent.Flags.HasFlag (MouseFlags.Button4Clicked))
        //{
        //    OnMouseClick (args);
        //}

        //if (mouseEvent.Flags.HasFlag (MouseFlags.Button1DoubleClicked)
        //    || mouseEvent.Flags.HasFlag (MouseFlags.Button2DoubleClicked)
        //    || mouseEvent.Flags.HasFlag (MouseFlags.Button3DoubleClicked)
        //    || mouseEvent.Flags.HasFlag (MouseFlags.Button4DoubleClicked))
        //{
        //    OnMouseClick (args);
        //}

        if (args.Handled != true)
        {
            MouseEvent?.Invoke (this, args);
        }

        return args.Handled == true;
    }

    /// <summary>Invokes the MouseClick event.</summary>
    /// <remarks>
    /// <para>
    /// Called when the mouse is either clicked or double-clicked. Check
    /// <see cref="MouseEvent.Flags"/> to see which button was clicked.
    /// </para>
    /// </remarks>
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
}
