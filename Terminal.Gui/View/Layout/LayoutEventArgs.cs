namespace Terminal.Gui;

/// <summary>Event arguments for the <see cref="View.LayoutComplete"/> event.</summary>
public class LayoutEventArgs : EventArgs
{
    /// <summary>Creates a new instance of the <see cref="Terminal.Gui.LayoutEventArgs"/> class.</summary>
    /// <param name="oldContentSize">The view that the event is about.</param>
    public LayoutEventArgs (Size oldContentSize) { OldContentSize = oldContentSize; }

    /// <summary>The viewport of the <see cref="View"/> before it was laid out.</summary>
    public Size OldContentSize { get; set; }
}
