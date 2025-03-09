#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Handles bespoke behaviours that occur when application top level changes.
/// </summary>
public class ToplevelTransitionManager : IToplevelTransitionManager
{
    private readonly HashSet<Toplevel> _readiedTopLevels = new ();

    private View? _lastTop;

    /// <inheritdoc/>
    public void RaiseReadyEventIfNeeded ()
    {
        Toplevel? top = Application.Top;

        if (top != null && !_readiedTopLevels.Contains (top))
        {
            top.OnReady ();
            _readiedTopLevels.Add (top);
        }
    }

    /// <inheritdoc/>
    public void HandleTopMaybeChanging ()
    {
        Toplevel? newTop = Application.Top;

        if (_lastTop != null && _lastTop != newTop && newTop != null)
        {
            newTop.SetNeedsDraw ();
        }

        _lastTop = Application.Top;
    }
}
