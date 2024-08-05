namespace Terminal.Gui;

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
    ///     event subscriber. It's important to set this value to true specially when updating any View's layout from inside the
    ///     subscriber method.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>Indicates the view that is losing focus.</summary>
    public View Leaving { get; set; }

    /// <summary>Indicates the view that is gaining focus.</summary>
    public View Entering { get; set; }

}
