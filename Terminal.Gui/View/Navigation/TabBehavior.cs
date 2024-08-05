namespace Terminal.Gui;

/// <summary>
///     Describes how <see cref="View.TabStop"/> behaves. A TabStop is a stop-point for keyboard navigation between Views.
/// </summary>
public enum TabBehavior
{
    /// <summary>
    ///     The View will not be a stop-poknt for keyboard-based navigation.
    /// </summary>
    NoStop = 0,

    /// <summary>
    ///     The View will be a stop-point for keybaord-based navigation across Views (e.g. if the user presses `Tab`).
    /// </summary>
    TabStop = 1,

    /// <summary>
    ///     The View will be a stop-point for keyboard-based navigation across groups (e.g. if the user presses <see cref="Application.NextTabGroupKey"/> (`Ctrl-PageDown`).
    /// </summary>
    TabGroup = 2,
}
