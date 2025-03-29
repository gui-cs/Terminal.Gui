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
    public ApplicationPopover () { }

    private readonly List<IPopover> _popovers = [];

    /// <summary></summary>
    public IReadOnlyCollection<IPopover> Popovers => _popovers.AsReadOnly ();

    /// <summary>
    ///     Registers <paramref name="popover"/> with the application.
    ///     This enables the popover to receive keyboard events even when when it is not active.
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
    ///     De-registers <paramref name="popover"/> with the application. Use this to remove the popover and it's
    ///     keyboard bindings from the application.
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
    public IPopover? GetActivePopover () { return _activePopover; }

    /// <summary>
    ///     Shows <paramref name="popover"/>. IPopover implementations should use OnVisibleChnaged/VisibleChanged to be
    ///     notified when the user has done something to cause the popover to be hidden.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Note, this API calls <see cref="Register"/>. To disable the popover from processing keyboard events,
    ///         either call <see cref="DeRegister"/> to
    ///         remove the popover from the application or set <see cref="View.Enabled"/> to <see langword="false"/>.
    ///     </para>
    /// </remarks>
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

            if (!newPopover.IsInitialized)
            {
                newPopover.BeginInit ();
                newPopover.EndInit ();
            }

            _activePopover = newPopover as IPopover;
            newPopover.Enabled = true;
            newPopover.Visible = true;
        }
    }

    /// <summary>
    ///     Causes the specified popover to be hidden.
    ///     If the popover is dervied from <see cref="PopoverBaseImpl"/>, this is the same as setting <see cref="View.Visible"/> to <see langword="false"/>.
    /// </summary>
    /// <param name="popover"></param>
    public void HidePopover (IPopover? popover)
    {
        // If there's an existing popover, hide it.
        if (_activePopover is View popoverView && popoverView == popover)
        {
            popoverView.Visible = false;
            _activePopover = null;
            Application.Top?.SetNeedsDraw ();
        }
    }


    /// <summary>
    ///     Called when the user presses a key. Dispatches the key to the active popover, if any,
    ///     otherwise to the popovers in the order they were registered. Inactive popovers only get hotkeys.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    internal bool DispatchKeyDown (Key key)
    {
        // Do active first - Active gets all key down events.
        if (GetActivePopover () as View is { Visible: true } visiblePopover)
        {
            if (visiblePopover.NewKeyDownEvent (key))
            {
                return true;
            }
        }

        // If the active popover didn't handle the key, try the inactive ones.
        // Inactive only get hotkeys
        bool? hotKeyHandled = null;

        foreach (IPopover popover in _popovers)
        {
            if (GetActivePopover () == popover || popover is not View popoverView)
            {
                continue;
            }

            // hotKeyHandled = popoverView.InvokeCommandsBoundToHotKey (key);
            hotKeyHandled = popoverView.NewKeyDownEvent (key);

            if (hotKeyHandled is true)
            {
                return true;
            }
        }

        return hotKeyHandled is true;
    }
}
