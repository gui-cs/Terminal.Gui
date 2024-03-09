namespace Terminal.Gui;

public partial class View
{
    /// <summary>Gets or sets a value indicating whether this <see cref="View"/> want continuous button pressed event.</summary>
    public virtual bool WantContinuousButtonPressed { get; set; }

    /// <summary>Gets or sets a value indicating whether this <see cref="View"/> wants mouse position reports.</summary>
    /// <value><see langword="true"/> if want mouse position reports; otherwise, <see langword="false"/>.</value>
    public virtual bool WantMousePositionReports { get; set; }

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
            return true;
        }

        if (!CanBeVisible (this))
        {
            return false;
        }

        var args = new MouseEventEventArgs (mouseEvent);

        // Clicked support for all buttons and single and double click
        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button2Clicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button3Clicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button4Clicked))
        {
            return OnMouseClick (args);
        }

        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1DoubleClicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button2DoubleClicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button3DoubleClicked)
            || mouseEvent.Flags.HasFlag (MouseFlags.Button4DoubleClicked))
        {
            return OnMouseClick (args);
        }

        return false;
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
            return true;
        }

        MouseClick?.Invoke (this, args);
        if (args.Handled)
        {
            return true;
        }

        if (!HasFocus && CanFocus)
        {
            SetFocus();
        }

        return args.Handled;
    }
}
