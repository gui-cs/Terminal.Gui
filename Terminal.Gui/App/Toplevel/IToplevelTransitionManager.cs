namespace Terminal.Gui.App;


// TODO: This whole concept is bogus and over-engineered.
// TODO: Remove it and just subscribers use the IApplication.Iteration
// TODO: If the requirement is they know if it's the first iteration, they can
// TODO: count invocations.

/// <summary>
///     Interface for class that handles bespoke behaviours that occur when application
///     top level changes.
/// </summary>
public interface IToplevelTransitionManager
{
    /// <summary>
    ///     Raises the <see cref="Toplevel.Ready"/> event on tahe current top level
    ///     if it has not been raised before now.
    /// </summary>
    /// <param name="app"></param>
    void RaiseReadyEventIfNeeded (IApplication? app);

    /// <summary>
    ///     Handles any state change needed when the application top changes e.g.
    ///     setting redraw flags
    /// </summary>
    /// <param name="app"></param>
    void HandleTopMaybeChanging (IApplication? app);
}
