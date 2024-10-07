#nullable enable

using System.Diagnostics;

namespace Terminal.Gui;

/// <summary>
///     Helper class for <see cref="Application"/> navigation. Held by <see cref="Application.Navigation"/>
/// </summary>
public class ApplicationNavigation
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ApplicationNavigation"/> class.
    /// </summary>
    public ApplicationNavigation ()
    {
        // TODO: Move navigation key bindings here from AddApplicationKeyBindings
    }

    private View? _focused;

    /// <summary>
    ///     Raised when the most focused <see cref="View"/> in the application has changed.
    /// </summary>
    public event EventHandler<EventArgs>? FocusedChanged;

    /// <summary>
    ///     Gets the most focused <see cref="View"/> in the application, if there is one.
    /// </summary>
    public View? GetFocused ()
    {
        return _focused;
    }

    /// <summary>
    ///     INTERNAL method to record the most focused <see cref="View"/> in the application.
    /// </summary>
    /// <remarks>
    ///     Raises <see cref="FocusedChanged"/>.
    /// </remarks>
    internal void SetFocused (View? value)
    {
        if (value is null)
        {

        }
        if (_focused == value)
        {
            return;
        }

        _focused = value;

        FocusedChanged?.Invoke (null, EventArgs.Empty);
    }

    /// <summary>
    ///     Advances the focus to the next or previous view in the focus chain, based on
    ///     <paramref name="direction"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If there is no next/previous view, the focus remains on the current view.
    ///     </para>
    /// </remarks>
    /// <param name="direction">The direction to advance.</param>
    /// <param name="behavior">The tab behavior.</param>
    /// <returns>
    ///     <see langword="true"/> if focus was changed to another subview (or stayed on this one), <see langword="false"/>
    ///     otherwise.
    /// </returns>
    public bool AdvanceFocus (NavigationDirection direction, TabBehavior? behavior)
    {
        if (Application.Popover is { Visible: true })
        {
            return Application.Popover.AdvanceFocus (direction, behavior);
        }
        return Application.Top is { } && Application.Top.AdvanceFocus (direction, behavior);
    }
}
