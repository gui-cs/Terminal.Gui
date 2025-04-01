#nullable enable

namespace Terminal.Gui;

/// <summary>
///     Helper class for support of <see cref="IPopover"/> views for <see cref="Application"/>. Held by
///     <see cref="Application.Popover"/>
/// </summary>
public sealed class ApplicationPopover : IDisposable
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ApplicationPopover"/> class.
    /// </summary>
    public ApplicationPopover () { }

    private readonly List<IPopover> _popovers = [];

    /// <summary>
    ///     Gets the list of popovers registered with the application.
    /// </summary>
    public IReadOnlyCollection<IPopover> Popovers => _popovers.AsReadOnly ();

    /// <summary>
    ///     Registers <paramref name="popover"/> with the application.
    ///     This enables the popover to receive keyboard events even when it is not active.
    /// </summary>
    /// <remarks>
    ///     When a popover is registered, the View instance lifetime is managed by the application. Call
    ///     <see cref="DeRegister"/>
    ///     to manage the lifetime of the popover directly.
    /// </remarks>
    /// <param name="popover"></param>
    /// <returns><paramref name="popover"/>, after it has been registered.</returns>
    public IPopover? Register (IPopover? popover)
    {
        if (popover is { } && !_popovers.Contains (popover))
        {
            _popovers.Add (popover);
        }

        return popover;
    }

    /// <summary>
    ///     De-registers <paramref name="popover"/> with the application. Use this to remove the popover and it's
    ///     keyboard bindings from the application.
    /// </summary>
    /// <remarks>
    ///     When a popover is registered, the View instance lifetime is managed by the application. Call
    ///     <see cref="DeRegister"/>
    ///     to manage the lifetime of the popover directly.
    /// </remarks>
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
    /// <remarks>
    ///     Note, the active pop over does not necessarily to be registered with the application.
    /// </remarks>
    /// <returns></returns>
    public IPopover? GetActivePopover () { return _activePopover; }

    /// <summary>
    ///     Shows <paramref name="popover"/>. IPopover implementations should use OnVisibleChanaged/VisibleChanged to be
    ///     notified when the user has done something to cause the popover to be hidden.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This API calls <see cref="Register"/>. To disable the popover from processing keyboard events,
    ///         either call <see cref="DeRegister"/> to
    ///         remove the popover from the application or set <see cref="View.Enabled"/> to <see langword="false"/>.
    ///     </para>
    /// </remarks>
    /// <param name="popover"></param>
    public void Show (IPopover? popover)
    {
        // If there's an existing popover, hide it.
        if (_activePopover is View popoverView)
        {
            popoverView.Visible = false;
            _activePopover = null;
        }

        if (popover is View newPopover)
        {
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
    ///     If the popover is dervied from <see cref="PopoverBaseImpl"/>, this is the same as setting
    ///     <see cref="View.Visible"/> to <see langword="false"/>.
    /// </summary>
    /// <param name="popover"></param>
    public void Hide (IPopover? popover)
    {
        // If there's an existing popover, hide it.
        if (_activePopover is View popoverView && popoverView == popover)
        {
            _activePopover = null;
            popoverView.Visible = false;
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
        var activePopover = GetActivePopover () as View;

        if (activePopover is { Visible: true })
        {
            if (activePopover.NewKeyDownEvent (key))
            {
                return true;
            }
        }

        // If the active popover didn't handle the key, try the inactive ones.
        // Inactive only get hotkeys
        bool? hotKeyHandled = null;

        foreach (IPopover popover in _popovers)
        {
            if (popover == activePopover || popover is not View popoverView)
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

    /// <inheritdoc/>
    public void Dispose ()
    {
        foreach (IPopover popover in _popovers)
        {
            if (popover is View view)
            {
                view.Dispose ();
            }
        }

        _popovers.Clear ();
    }
}
