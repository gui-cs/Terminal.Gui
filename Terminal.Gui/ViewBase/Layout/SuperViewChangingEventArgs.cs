using Terminal.Gui.App;

namespace Terminal.Gui.ViewBase;

/// <summary>
///     Args for events where the <see cref="View.SuperView"/> of a <see cref="View"/> is about to be changed (e.g.
///     before <see cref="View.Remove(View?)"/>).
/// </summary>
/// <remarks>
///     <para>
///         This event is raised before the <see cref="View.SuperView"/> property is changed, allowing access to the
///         current SuperView and its associated resources (such as <see cref="View.App"/>) for cleanup purposes.
///     </para>
///     <para>
///         This event follows the Cancellable Work Pattern (CWP) and can be cancelled by setting <see cref="System.ComponentModel.CancelEventArgs.Cancel"/> to <see langword="true"/>.
///     </para>
/// </remarks>
public class SuperViewChangingEventArgs : CancelEventArgs<View?>
{
    /// <summary>Creates a new instance of the <see cref="SuperViewChangingEventArgs"/> class.</summary>
    /// <param name="currentSuperView">The current SuperView before the change.</param>
    /// <param name="newSuperView">The new SuperView that will be set, or <see langword="null"/> if being removed.</param>
    public SuperViewChangingEventArgs (View? currentSuperView, View? newSuperView) 
        : base (currentSuperView, newSuperView)
    {
    }
}
