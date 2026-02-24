using Terminal.Gui.Tracing;

namespace Terminal.Gui.App;

/// <summary>
///     Helper class for support of <see cref="IPopover"/> views for <see cref="IApplication"/>. Held by
///     <see cref="IApplication.Popovers"/>
/// </summary>
public sealed class ApplicationPopover : IDisposable
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ApplicationPopover"/> class.
    /// </summary>
    public ApplicationPopover () { }

    private readonly List<IPopover> _popovers = [];

    private IPopover? _activePopover;

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
        _activePopover = null;
    }

    /// <summary>
    ///     The <see cref="IApplication"/> instance used by this instance.
    /// </summary>
    public IApplication? App { get; set; }

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
        if (popover is null || !IsRegistered (popover))
        {
            return false;
        }

        if (GetActivePopover () == popover)
        {
            _activePopover = null;
        }

        _popovers.Remove (popover);

        return true;
    }

    /// <summary>
    ///     Gets the active popover, if any.
    /// </summary>
    /// <remarks>
    ///     Note, the active pop over does not necessarily to be registered with the application.
    /// </remarks>
    /// <returns></returns>
    public IPopover? GetActivePopover () => _activePopover;

    /// <summary>
    ///     Causes the specified popover to be hidden.
    ///     If the popover is derived from <see cref="PopoverBaseImpl"/>, this is the same as setting
    ///     <see cref="View.Visible"/> to <see langword="false"/>.
    /// </summary>
    /// <param name="popover"></param>
    public void Hide (IPopover? popover)
    {
        if (_activePopover is not View popoverView || popoverView != popover)
        {
            return;
        }

        // If there's an existing popover, hide it.
        _activePopover = null;
        popoverView.Visible = false;
        popoverView.App?.TopRunnableView?.SetNeedsDraw ();
    }

    /// <summary>
    ///     Indicates whether a popover has been registered or not.
    /// </summary>
    /// <param name="popover"></param>
    /// <returns></returns>
    public bool IsRegistered (IPopover? popover) => popover is { } && _popovers.Contains (popover);

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
        if (popover is null || IsRegistered (popover))
        {
            return popover;
        }
        popover.Owner ??= App?.TopRunnableView as IRunnable;

        if (popover is View popoverView)
        {
            popoverView.App = App;

            if (!popoverView.IsInitialized)
            {
                popoverView.BeginInit ();
                popoverView.EndInit ();
            }
        }

        _popovers.Add (popover);

        return popover;
    }

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
        if (!IsRegistered (popover))
        {
            throw new InvalidOperationException (@"Popovers must be registered before being shown.");
        }

        // If there's an existing popover, hide it.
        if (_activePopover is View popoverView)
        {
            popoverView.App = App;
            popoverView.Visible = false;
            _activePopover = null;
        }

        if (popover is not View newPopover)
        {
            return;
        }

        if (!(newPopover.ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent)
              && newPopover.ViewportSettings.HasFlag (ViewportSettingsFlags.TransparentMouse)))
        {
            throw new InvalidOperationException ("Popovers must have ViewportSettings.Transparent and ViewportSettings.TransparentMouse set.");
        }

        if (newPopover.KeyBindings.GetFirstFromCommands (Command.Quit) is null)
        {
            throw new InvalidOperationException ("Popovers must have a key binding for Command.Quit.");
        }

        if (!newPopover.IsInitialized)
        {
            newPopover.BeginInit ();
            newPopover.EndInit ();
        }

        _activePopover = newPopover as IPopover;
        newPopover.Enabled = true;
        newPopover.Visible = true;
    }

    /// <summary>
    ///     Called when the user presses a key. Dispatches the key to the active popover, if any,
    ///     otherwise to the popovers in the order they were registered. Inactive popovers only get hotkeys.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    internal bool DispatchKeyDown (Key key)
    {
        Trace.Keyboard ("Popovers", key, "Entry");

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

        foreach (IPopover popover in _popovers.ToList ())
        {
            if (popover == activePopover || popover is not View popoverView || (popover.Owner is { } && popover.Owner != App?.TopRunnableView))
            {
                continue;
            }

            Trace.Keyboard ("Popovers", key, "InactiveDispatch", $"Sending to {popoverView.ToIdentifyingString ()}");

            // hotKeyHandled = popoverView.InvokeCommandsBoundToHotKey (key);
            popoverView.App ??= App;
            hotKeyHandled = popoverView.NewKeyDownEvent (key);

            Trace.Keyboard ("Popovers", key, "InactiveResult", $"{popoverView.ToIdentifyingString ()} returned {hotKeyHandled}");

            if (hotKeyHandled is true)
            {
                return true;
            }
        }

        return hotKeyHandled is true;
    }

    /// <summary>
    ///     Hides a popover view if it supports the quit command and is currently visible. It checks for the command's
    ///     support before hiding.
    /// </summary>
    /// <param name="visiblePopover">
    ///     The view that is being checked and potentially hidden based on its visibility and command
    ///     support.
    /// </param>
    internal static void HideWithQuitCommand (View visiblePopover)
    {
        if (visiblePopover.Visible
            && (!visiblePopover.GetSupportedCommands ().Contains (Command.Quit)
                || (visiblePopover.InvokeCommand (Command.Quit) is true && visiblePopover.Visible)))
        {
            visiblePopover.Visible = false;
        }
    }
}
