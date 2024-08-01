namespace Terminal.Gui;

/// <summary>Defines the event arguments for <see cref="View.HasFocus"/></summary>
public class FocusEventArgs : EventArgs
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="leaving">The view that is losing focus.</param>
    /// <param name="entering">The view that is gaining focus.</param>
    public FocusEventArgs (View leaving, View entering) {
        Leaving = leaving;
        Entering = entering;
    }

    /// <summary>
    ///    Gets or sets whether the event should be canceled. Set to <see langword="true"/> to prevent the focus change.
    /// </summary>
    public bool Cancel { get; set; }

    /// <summary>Gets or sets the view that is losing focus.</summary>
    public View Leaving { get; set; }

    /// <summary>Gets or sets the view that is gaining focus.</summary>
    public View Entering { get; set; }

}
