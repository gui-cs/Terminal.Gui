namespace Terminal.Gui.Views;

#pragma warning disable CS0618 // Type or member is obsolete

/// <summary>An <see cref="EventArgs"/> which allows passing a cancelable menu closing event.</summary>
public class MenuClosingEventArgs : EventArgs
{
    /// <summary>Initializes a new instance of <see cref="MenuClosingEventArgs"/>.</summary>
    /// <param name="currentMenu">The current <see cref="MenuBarItem"/> parent.</param>
    /// <param name="reopen">Whether the current menu will reopen.</param>
    /// <param name="isSubMenu">Indicates whether it is a sub-menu.</param>
    public MenuClosingEventArgs (MenuBarItem currentMenu, bool reopen, bool isSubMenu)
    {
        CurrentMenu = currentMenu;
        Reopen = reopen;
        IsSubMenu = isSubMenu;
    }

    /// <summary>
    ///     Flag that allows the cancellation of the event. If set to <see langword="true"/> in the event handler, the
    ///     event will be canceled.
    /// </summary>
    public bool Cancel { get; set; }

    /// <summary>The current <see cref="MenuBarItem"/> parent.</summary>
    public MenuBarItem CurrentMenu { get; }

    /// <summary>Indicates whether the current menu is a sub-menu.</summary>
    public bool IsSubMenu { get; }

    /// <summary>Indicates whether the current menu will reopen.</summary>
    public bool Reopen { get; }
}