namespace Terminal.Gui;

/// <summary>Args GrabMouse related events.</summary>
public class GrabMouseEventArgs : EventArgs
{
    /// <summary>Creates a new instance of the <see cref="GrabMouseEventArgs"/> class.</summary>
    /// <param name="view">The view that the event is about.</param>
    public GrabMouseEventArgs (View view) { View = view; }

    /// <summary>
    ///     Flag that allows the cancellation of the event. If set to <see langword="true"/> in the event handler, the
    ///     event will be canceled.
    /// </summary>
    public bool Cancel { get; set; }

    /// <summary>Gets the view that grabbed the mouse.</summary>
    public View View { get; }
}
