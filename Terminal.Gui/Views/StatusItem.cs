namespace Terminal.Gui;

/// <summary>
///     <see cref="StatusItem"/> objects are contained by <see cref="StatusBar"/> <see cref="View"/>s. Each
///     <see cref="StatusItem"/> has a title, a shortcut (hotkey), and an <see cref="Command"/> that will be invoked when
///     the <see cref="StatusItem.Shortcut"/> is pressed. The <see cref="StatusItem.Shortcut"/> will be a global hotkey for
///     the application in the current context of the screen. The color of the <see cref="StatusItem.Title"/> will be
///     changed after each ~. A <see cref="StatusItem.Title"/> set to `~F1~ Help` will render as *F1* using
///     <see cref="ColorScheme.HotNormal"/> and *Help* as <see cref="ColorScheme.HotNormal"/>.
/// </summary>
public class StatusItem
{
    /// <summary>Initializes a new <see cref="StatusItem"/>.</summary>
    /// <param name="shortcut">Shortcut to activate the <see cref="StatusItem"/>.</param>
    /// <param name="title">Title for the <see cref="StatusItem"/>.</param>
    /// <param name="action">Action to invoke when the <see cref="StatusItem"/> is activated.</param>
    /// <param name="canExecute">Function to determine if the action can currently be executed.</param>
    public StatusItem (Key shortcut, string title, Action action, Func<bool> canExecute = null)
    {
        Title = title ?? "";
        Shortcut = shortcut;
        Action = action;
        CanExecute = canExecute;
    }

    /// <summary>Gets or sets the action to be invoked when the <see cref="StatusItem"/> is triggered</summary>
    /// <value>Action to invoke.</value>
    public Action Action { get; set; }

    /// <summary>
    ///     Gets or sets the action to be invoked to determine if the <see cref="StatusItem"/> can be triggered. If
    ///     <see cref="CanExecute"/> returns <see langword="true"/> the status item will be enabled. Otherwise, it will be
    ///     disabled.
    /// </summary>
    /// <value>Function to determine if the action is can be executed or not.</value>
    public Func<bool> CanExecute { get; set; }

    /// <summary>Gets or sets arbitrary data for the status item.</summary>
    /// <remarks>This property is not used internally.</remarks>
    public object Data { get; set; }

    /// <summary>Gets the global shortcut to invoke the action on the menu.</summary>
    public Key Shortcut { get; set; }

    /// <summary>Gets or sets the title.</summary>
    /// <value>The title.</value>
    /// <remarks>
    ///     The colour of the <see cref="StatusItem.Title"/> will be changed after each ~. A
    ///     <see cref="StatusItem.Title"/> set to `~F1~ Help` will render as *F1* using <see cref="ColorScheme.HotNormal"/> and
    ///     *Help* as <see cref="ColorScheme.HotNormal"/>.
    /// </remarks>
    public string Title { get; set; }

    /// <summary>
    ///     Returns <see langword="true"/> if the status item is enabled. This method is a wrapper around
    ///     <see cref="CanExecute"/>.
    /// </summary>
    public bool IsEnabled () { return CanExecute?.Invoke () ?? true; }
}
