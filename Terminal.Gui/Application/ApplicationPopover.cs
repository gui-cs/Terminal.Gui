#nullable enable

using System.Diagnostics;

namespace Terminal.Gui;

/// <summary>
///     Helper class for support of <see cref="IPopover"/> views for <see cref="Application"/>. Held by <see cref="Application.Popover"/>
/// </summary>
public class ApplicationPopover
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ApplicationPopover"/> class.
    /// </summary>
    public ApplicationPopover ()
    {
    }

    private readonly List<IPopover> _popovers = [];

    /// <summary></summary>
    public IReadOnlyCollection<IPopover> Popovers => _popovers.AsReadOnly ();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="popover"></param>
    public void Register (IPopover? popover)
    {
        if (popover is { } && !_popovers.Contains (popover))
        {
            _popovers.Add (popover);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="popover"></param>
    /// <returns></returns>
    public bool DeRegister (IPopover? popover)
    {
        if (popover is { } && _popovers.Contains (popover))
        {
            if (GetActivePopover () == popover)
            {
                _activePopover = null;
            }
            _popovers.Remove (popover);

            return true;
        }
        return false;
    }

    private IPopover? _activePopover;

    /// <summary>
    ///     Gets the active popover, if any.
    /// </summary>
    /// <returns></returns>
    public IPopover? GetActivePopover ()
    {
        return _activePopover;
    }

    /// <summary>
    ///     Shows <paramref name="popover"/>. IPopover implementations should use OnVisibleChnaged/VisibleChanged to be
    ///     notified when the user has done something to cause the popover to be hidden.
    /// </summary>
    /// <param name="popover"></param>
    public void ShowPopover (IPopover? popover)
    {
        // If there's an existing popover, hide it.
        if (_activePopover is View popoverView)
        {
            popoverView.Visible = false;
            _activePopover = null;
        }

        if (popover is View newPopover)
        {
            Register (popover);
            _activePopover = newPopover as IPopover;
            newPopover.Visible = true;
        }
    }

    public void HidePopover (IPopover? popover)
    {
        // If there's an existing popover, hide it.
        if (_activePopover is View popoverView)
        {
            popoverView.Visible = false;
            _activePopover = null;
        }
    }


    /// <summary>
    ///     Called when the user presses 
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    internal bool NewKeyDownEvent (Key key)
    {
        // Do active first
        if (GetActivePopover () as View is { Visible: true } visiblePopover)
        {
            if (visiblePopover.NewKeyDownEvent (key))
            {
                return true;
            }
        }

        foreach (IPopover popover in _popovers)
        {
            if (GetActivePopover () == popover)
            {
                continue;
            }

            if (popover is View popoverView)
            {
                if (popoverView.NewKeyDownEvent (key))
                {
                    return true;
                }
            }
        }

        return false;
    }

}
