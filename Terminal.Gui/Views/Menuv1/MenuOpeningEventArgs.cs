namespace Terminal.Gui.Views;

#pragma warning disable CS0618 // Type or member is obsolete

/// <summary>
///     An <see cref="EventArgs"/> which allows passing a cancelable menu opening event or replacing with a new
///     <see cref="MenuBarItem"/>.
/// </summary>
public class MenuOpeningEventArgs : EventArgs
{
    /// <summary>Initializes a new instance of <see cref="MenuOpeningEventArgs"/>.</summary>
    /// <param name="currentMenu">The current <see cref="MenuBarItem"/> parent.</param>
    public MenuOpeningEventArgs (MenuBarItem currentMenu) { CurrentMenu = currentMenu; }

    /// <summary>
    ///     Flag that allows the cancellation of the event. If set to <see langword="true"/> in the event handler, the
    ///     event will be canceled.
    /// </summary>
    public bool Cancel { get; set; }

    /// <summary>The current <see cref="MenuBarItem"/> parent.</summary>
    public MenuBarItem CurrentMenu { get; }

    /// <summary>The new <see cref="MenuBarItem"/> to be replaced.</summary>
    public MenuBarItem NewMenuBarItem { get; set; }
}