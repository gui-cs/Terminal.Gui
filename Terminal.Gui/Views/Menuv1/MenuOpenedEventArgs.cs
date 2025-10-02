namespace Terminal.Gui.Views;
#pragma warning disable CS0618 // Type or member is obsolete

/// <summary>Defines arguments for the <see cref="MenuBar.MenuOpened"/> event</summary>
public class MenuOpenedEventArgs : EventArgs
{
    /// <summary>Creates a new instance of the <see cref="MenuOpenedEventArgs"/> class</summary>
    /// <param name="parent"></param>
    /// <param name="menuItem"></param>
    public MenuOpenedEventArgs (MenuBarItem parent, MenuItem menuItem)
    {
        Parent = parent;
        MenuItem = menuItem;
    }

    /// <summary>Gets the <see cref="MenuItem"/> being opened.</summary>
    public MenuItem MenuItem { get; }

    /// <summary>The parent of <see cref="MenuItem"/>. Will be null if menu opening is the root.</summary>
    public MenuBarItem Parent { get; }
}