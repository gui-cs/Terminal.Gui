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

    private IPopover? _popover;

    /// <summary>
    ///     Gets the current popover, if any.
    /// </summary>
    /// <returns></returns>
    public IPopover? GetPopover ()
    {
        return _popover;
    }

    /// <summary>
    ///     Shows <paramref name="popover"/>. IPopover implementations should use OnVisibleChnaged/VisibleChanged to be
    ///     notified when the user has done something to cause the popover to be hidden.
    /// </summary>
    /// <param name="popover"></param>
    public void ShowPopover (IPopover? popover)
    {
        // If there's an existing popover, hide it.
        if (_popover is View popoverView)
        {
            popoverView.Visible = false;
        }

        _popover = popover;
    }

}
