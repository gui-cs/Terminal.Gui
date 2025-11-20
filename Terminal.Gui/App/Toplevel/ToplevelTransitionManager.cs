namespace Terminal.Gui.App;

/// <summary>
///     Handles bespoke behaviours that occur when application top level changes.
/// </summary>
public class ToplevelTransitionManager : IToplevelTransitionManager
{
    private readonly HashSet<Toplevel> _readiedTopLevels = new ();

    private View? _lastTop;

    /// <param name="app"></param>
    /// <inheritdoc/>
    public void RaiseReadyEventIfNeeded (IApplication? app)
    {
        Toplevel? top = app?.TopRunnable;

        if (top != null && !_readiedTopLevels.Contains (top))
        {
            top.OnReady ();
            _readiedTopLevels.Add (top);

            // Views can be closed and opened and run again multiple times, see End_Does_Not_Dispose
            top.Closed += (s, e) => _readiedTopLevels.Remove (top);
        }
    }

    /// <param name="app"></param>
    /// <inheritdoc/>
    public void HandleTopMaybeChanging (IApplication? app)
    {
        Toplevel? newTop = app?.TopRunnable;

        if (_lastTop != null && _lastTop != newTop && newTop != null)
        {
            newTop.SetNeedsDraw ();
        }

        _lastTop = app?.TopRunnable;
    }
}
