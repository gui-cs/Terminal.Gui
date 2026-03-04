namespace Terminal.Gui.Views;

/// <summary>
///     Defines the contract shared by <see cref="MenuBarItem"/> and <see cref="InlineMenuBarItem"/>,
///     enabling <see cref="MenuBar"/> to operate on either uniformly without knowing the
///     concrete type or underlying menu mechanism (PopoverMenu vs. SubMenu).
/// </summary>
public interface IMenuBarEntry
{
    /// <summary>
    ///     Gets or sets whether the menu dropdown is currently open and visible.
    /// </summary>
    bool IsMenuOpen { get; set; }

    /// <summary>
    ///     Raised when <see cref="IsMenuOpen"/> has changed.
    /// </summary>
    event EventHandler<ValueChangedEventArgs<bool>>? MenuOpenChanged;

    /// <summary>
    ///     Gets the root <see cref="Menu"/> for this entry's dropdown, used for item
    ///     search and hierarchy traversal.
    /// </summary>
    Menu? RootMenu { get; }
}
