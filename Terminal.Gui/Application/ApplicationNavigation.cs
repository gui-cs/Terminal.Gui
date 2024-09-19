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
    public View? GetFocused () { return _focused; }

    /// <summary>
    ///     Gets whether <paramref name="view"/> is in the Subview hierarchy of <paramref name="start"/>.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="view"></param>
    /// <param name="includeAdornments">Will search the subview hierarchy of the adornments if true.</param>
    /// <returns></returns>
    public static bool IsInHierarchy (View? start, View? view, bool includeAdornments = false)
    {
        if (view is null)
        {
            return false;
        }

        if (view == start || start is null)
        {
            return true;
        }

        foreach (View subView in start.Subviews)
        {
            if (view == subView)
            {
                return true;
            }

            bool found = IsInHierarchy (subView, view, includeAdornments);

            if (found)
            {
                return found;
            }
        }

        if (includeAdornments)
        {
            bool found = IsInHierarchy (start.Padding, view, includeAdornments);

            if (found)
            {
                return found;
            }

            found = IsInHierarchy (start.Border, view, includeAdornments);

            if (found)
            {
                return found;
            }

            found = IsInHierarchy (start.Margin, view, includeAdornments);

            if (found)
            {
                return found;
            }
        }

        return false;
    }

    /// <summary>
    ///     INTERNAL method to record the most focused <see cref="View"/> in the application.
    /// </summary>
    /// <remarks>
    ///     Raises <see cref="FocusedChanged"/>.
    /// </remarks>
    internal void SetFocused (View? value)
    {
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
            Application.Popover.AdvanceFocus (direction, behavior);
        }
        return Application.Top is { } && Application.Top.AdvanceFocus (direction, behavior);
    }
}
