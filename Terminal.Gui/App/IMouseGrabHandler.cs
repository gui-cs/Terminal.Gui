#nullable enable
namespace Terminal.Gui.App;


/// <summary>
///     Interface for class that tracks which <see cref="View"/> (if any) has 'grabbed' the mouse
///     and wants priority updates about its activity e.g. where it moves to, when it is released
///     etc. Example use case is a button on a scroll bar being held down by the mouse - resulting
///     in continuous scrolling.
/// </summary>
public interface IMouseGrabHandler
{
    /// <summary>
    ///     Gets the view that grabbed the mouse (e.g. for dragging). When this is set, all mouse events will be routed to
    ///     this view until the view calls <see cref="UngrabMouse"/> or the mouse is released.
    /// </summary>
    public View? MouseGrabView { get; }

    /// <summary>Invoked when a view wants to grab the mouse; can be canceled.</summary>
    public event EventHandler<GrabMouseEventArgs>? GrabbingMouse;

    /// <summary>Invoked when a view wants un-grab the mouse; can be canceled.</summary>
    public event EventHandler<GrabMouseEventArgs>? UnGrabbingMouse;

    /// <summary>Invoked after a view has grabbed the mouse.</summary>
    public event EventHandler<ViewEventArgs>? GrabbedMouse;

    /// <summary>Invoked after a view has un-grabbed the mouse.</summary>
    public event EventHandler<ViewEventArgs>? UnGrabbedMouse;

    /// <summary>
    ///     Grabs the mouse, forcing all mouse events to be routed to the specified view until <see cref="UngrabMouse"/>
    ///     is called.
    /// </summary>
    /// <param name="view">View that will receive all mouse events until <see cref="UngrabMouse"/> is invoked.</param>
    public void GrabMouse (View? view);

    /// <summary>Releases the mouse grab, so mouse events will be routed to the view on which the mouse is.</summary>
    public void UngrabMouse ();
}
