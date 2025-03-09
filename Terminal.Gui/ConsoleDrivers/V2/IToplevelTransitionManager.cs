namespace Terminal.Gui;

/// <summary>
///     Interface for class that handles bespoke behaviours that occur when application
///     top level changes.
/// </summary>
public interface IToplevelTransitionManager
{
    /// <summary>
    ///     Raises the <see cref="Toplevel.Ready"/> event on the current top level
    ///     if it has not been raised before now.
    /// </summary>
    void RaiseReadyEventIfNeeded ();

    /// <summary>
    ///     Handles any state change needed when the application top changes e.g.
    ///     setting redraw flags
    /// </summary>
    void HandleTopMaybeChanging ();
}
