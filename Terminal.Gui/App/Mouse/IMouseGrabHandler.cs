#nullable enable
namespace Terminal.Gui.App;

/// <summary>
///     Defines a contract for tracking which <see cref="View"/> (if any) has 'grabbed' the mouse,
///     giving it exclusive priority for mouse events such as movement, button presses, and release.
///     <para>
///         This is typically used for scenarios like dragging, scrolling, or any interaction where a view
///         needs to receive all mouse events until the operation completes (e.g., a scrollbar thumb being dragged).
///     </para>
///     <para>
///         Usage pattern:
///         <list type="number">
///             <item>
///                 <description>Call <see cref="GrabMouse"/> to route all mouse events to a specific view.</description>
///             </item>
///             <item>
///                 <description>Call <see cref="UngrabMouse"/> to release the grab and restore normal mouse routing.</description>
///             </item>
///             <item>
///                 <description>
///                     Listen to <see cref="GrabbingMouse"/>, <see cref="GrabbedMouse"/>, <see cref="UnGrabbingMouse"/>,
///                     and <see cref="UnGrabbedMouse"/> for grab lifecycle events.
///                 </description>
///             </item>
///         </list>
///     </para>
/// </summary>
public interface IMouseGrabHandler
{
    /// <summary>
    ///     Occurs after a view has grabbed the mouse.
    ///     <para>
    ///         This event is raised after the mouse grab operation is complete and the specified view will receive all mouse
    ///         events.
    ///     </para>
    /// </summary>
    public event EventHandler<ViewEventArgs>? GrabbedMouse;

    /// <summary>
    ///     Occurs when a view requests to grab the mouse; can be canceled.
    ///     <para>
    ///         Handlers can set <c>e.Cancel</c> to <see langword="true"/> to prevent the grab.
    ///     </para>
    /// </summary>
    public event EventHandler<GrabMouseEventArgs>? GrabbingMouse;

    /// <summary>
    ///     Grabs the mouse, forcing all mouse events to be routed to the specified view until <see cref="UngrabMouse"/> is
    ///     called.
    /// </summary>
    /// <param name="view">
    ///     The <see cref="View"/> that will receive all mouse events until <see cref="UngrabMouse"/> is invoked.
    ///     If <see langword="null"/>, the grab is released.
    /// </param>
    public void GrabMouse (View? view);

    /// <summary>
    ///     Gets the view that currently has grabbed the mouse (e.g., for dragging).
    ///     <para>
    ///         When this property is not <see langword="null"/>, all mouse events are routed to this view until
    ///         <see cref="UngrabMouse"/> is called or the mouse is released.
    ///     </para>
    /// </summary>
    public View? MouseGrabView { get; }

    /// <summary>
    ///     Occurs after a view has released the mouse grab.
    ///     <para>
    ///         This event is raised after the mouse grab has been released and normal mouse routing resumes.
    ///     </para>
    /// </summary>
    public event EventHandler<ViewEventArgs>? UnGrabbedMouse;

    /// <summary>
    ///     Occurs when a view requests to release the mouse grab; can be canceled.
    ///     <para>
    ///         Handlers can set <c>e.Cancel</c> to <see langword="true"/> to prevent the ungrab.
    ///     </para>
    /// </summary>
    public event EventHandler<GrabMouseEventArgs>? UnGrabbingMouse;

    /// <summary>
    ///     Releases the mouse grab, so mouse events will be routed to the view under the mouse pointer.
    /// </summary>
    public void UngrabMouse ();
}
