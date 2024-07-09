namespace Terminal.Gui;

/// <summary>Args for events that relate to specific <see cref="View"/></summary>
public class ViewEventArgs : EventArgs
{
    /// <summary>Creates a new instance of the <see cref="Terminal.Gui.ViewEventArgs"/> class.</summary>
    /// <param name="view">The view that the event is about.</param>
    public ViewEventArgs (View view) { View = view; }

    /// <summary>The view that the event is about.</summary>
    /// <remarks>
    ///     Can be different from the sender of the <see cref="EventHandler"/> for example if event describes the adding a
    ///     child then sender may be the parent while <see cref="View"/> is the child being added.
    /// </remarks>
    public View View { get; }
}

/// <summary>Event arguments for the <see cref="View.LayoutComplete"/> event.</summary>
public class LayoutEventArgs : EventArgs
{
    /// <summary>Creates a new instance of the <see cref="Terminal.Gui.LayoutEventArgs"/> class.</summary>
    /// <param name="oldContentSize">The view that the event is about.</param>
    public LayoutEventArgs (Size oldContentSize) { OldContentSize = oldContentSize; }

    /// <summary>The viewport of the <see cref="View"/> before it was laid out.</summary>
    public Size OldContentSize { get; set; }
}

/// <summary>Event args for draw events</summary>
public class DrawEventArgs : EventArgs
{
    /// <summary>Creates a new instance of the <see cref="DrawEventArgs"/> class.</summary>
    /// <param name="newViewport">
    ///     The Content-relative rectangle describing the new visible viewport into the
    ///     <see cref="View"/>.
    /// </param>
    /// <param name="oldViewport">
    ///     The Content-relative rectangle describing the old visible viewport into the
    ///     <see cref="View"/>.
    /// </param>
    public DrawEventArgs (Rectangle newViewport, Rectangle oldViewport)
    {
        NewViewport = newViewport;
        OldViewport = oldViewport;
    }

    /// <summary>If set to true, the draw operation will be canceled, if applicable.</summary>
    public bool Cancel { get; set; }

    /// <summary>Gets the Content-relative rectangle describing the old visible viewport into the <see cref="View"/>.</summary>
    public Rectangle OldViewport { get; }

    /// <summary>Gets the Content-relative rectangle describing the currently visible viewport into the <see cref="View"/>.</summary>
    public Rectangle NewViewport { get; }
}

/// <summary>Defines the event arguments for <see cref="View.SetFocus()"/></summary>
public class FocusEventArgs : EventArgs
{
    /// <summary>Constructs.</summary>
    /// <param name="leaving">The view that is losing focus.</param>
    /// <param name="entering">The view that is gaining focus.</param>
    public FocusEventArgs (View leaving, View entering) {
        Leaving = leaving;
        Entering = entering;
    }

    /// <summary>
    ///     Indicates if the current focus event has already been processed and the driver should stop notifying any other
    ///     event subscriber. Its important to set this value to true specially when updating any View's layout from inside the
    ///     subscriber method.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>Indicates the view that is losing focus.</summary>
    public View Leaving { get; set; }

    /// <summary>Indicates the view that is gaining focus.</summary>
    public View Entering { get; set; }

}
