namespace Terminal.Gui;

/// <summary>
///     Describes a TabStop; a stop-point for keyboard navigation between Views.
/// </summary>
/// <remarks>
///     <para>
///         TabStop does not impact whether a view is focusable or not. <see cref="View.CanFocus"/> determines this independently of TabStop.
///     </para>
/// </remarks>
[Flags]
public enum TabStop
{
    /// <summary>
    ///     The View will not be a stop-poknt for keyboard-based navigation.
    /// </summary>
    None = 0,

    /// <summary>
    ///     The View will be a stop-point for keybaord-based navigation across Views (e.g. if the user presses `Tab`).
    /// </summary>
    TabStop = 1,

    /// <summary>
    ///     The View will be a stop-point for keyboard-based navigation across TabGroups (e.g. if the user preesses <see cref="Application.NextTabGroupKey"/> (`Ctrl-PageDown`).
    /// </summary>
    TabGroup = 2,
}
