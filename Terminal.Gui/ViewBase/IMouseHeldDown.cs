#nullable enable
using System.ComponentModel;

namespace Terminal.Gui.ViewBase;

/// <summary>
///     <para>
///         Handler for raising periodic events while the mouse is held down.
///         Typically, mouse button only needs to be pressed down in a view
///         to begin this event after which it can be moved elsewhere.
///     </para>
///     <para>
///         Common use cases for this includes holding a button down to increase
///         a counter (e.g. in <see cref="NumericUpDown"/>).
///     </para>
/// </summary>
public interface IMouseHeldDown : IDisposable
{
    /// <summary>
    ///     Periodically raised when the mouse is pressed down inside the view <see cref="View"/>.
    /// </summary>
    public event EventHandler<CancelEventArgs> MouseIsHeldDownTick;

    /// <summary>
    ///     Call to indicate that the mouse has been pressed down and any relevant actions should
    ///     be undertaken (start timers, <see cref="IMouseGrabHandler.GrabMouse"/> etc).
    /// </summary>
    void Start ();

    /// <summary>
    ///     Call to indicate that the mouse has been released and any relevant actions should
    ///     be undertaken (stop timers, <see cref="IMouseGrabHandler.UngrabMouse"/> etc).
    /// </summary>
    void Stop ();
}
