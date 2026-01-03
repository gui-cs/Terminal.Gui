
using System.Diagnostics;

namespace Terminal.Gui.App;

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
        // TODO: Move navigation key bindings here from KeyboardImpl
        
        // Subscribe to focus changes to trigger cursor updates
        FocusedChanged += (sender, args) => _cursorNeedsUpdate = true;
    }

    /// <summary>
    ///     The <see cref="IApplication"/> instance used by this instance.
    /// </summary>
    public IApplication? App { get; set; }

    private View? _focused;

    // Cursor caching fields
    private bool _cursorNeedsUpdate = true;
    private Point? _lastCursorPosition;
    private CursorVisibility _lastCursorVisibility;

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
    ///     Signals that the cursor position needs to be updated without requiring a full redraw.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method is called by <see cref="View.SetCursorNeedsUpdate"/> when a view's cursor position
    ///         changes but the view content does not need to be redrawn.
    ///     </para>
    /// </remarks>
    public void SetCursorNeedsUpdate ()
    {
        _cursorNeedsUpdate = true;
    }

    /// <summary>
    ///     Updates the terminal cursor based on the currently focused view.
    /// </summary>
    /// <param name="output">The output driver to use for cursor positioning.</param>
    /// <remarks>
    ///     This method is called once per main loop iteration by <see cref="IApplicationMainLoop{T}"/>.
    /// </remarks>
    public void UpdateCursor (IOutput output)
    {
        View? mostFocused = _focused;

        // Check if we need to update based on cached state
        if (!_cursorNeedsUpdate && mostFocused == null && _lastCursorVisibility == CursorVisibility.Invisible)
        {
            // No focused view and cursor already invisible - no update needed
            return;
        }

        if (mostFocused == null)
        {
            // Only update if visibility changed
            if (_lastCursorVisibility != CursorVisibility.Invisible)
            {
                output.SetCursorVisibility (CursorVisibility.Invisible);
                _lastCursorVisibility = CursorVisibility.Invisible;
            }

            _lastCursorPosition = null;
            _cursorNeedsUpdate = false;

            return;
        }

        Point? to = mostFocused.PositionCursor ();

        // Check if cursor position or visibility changed
        if (to == _lastCursorPosition
            && mostFocused.CursorVisibility == _lastCursorVisibility
            && !_cursorNeedsUpdate)
        {
            return; // No changes
        }

        if (to.HasValue)
        {
            // Translate to screen coordinates
            Point screenPos = mostFocused.ViewportToScreen (to.Value);

            output.SetCursorPosition (screenPos.X, screenPos.Y);
            output.SetCursorVisibility (mostFocused.CursorVisibility);

            _lastCursorPosition = to;
            _lastCursorVisibility = mostFocused.CursorVisibility;
        }
        else
        {
            output.SetCursorVisibility (CursorVisibility.Invisible);
            _lastCursorPosition = null;
            _lastCursorVisibility = CursorVisibility.Invisible;
        }

        _cursorNeedsUpdate = false;
    }

    // BUGBUG: This only gets Subviews and ignores Adornments. Should it use View.IsInHierarchy?
    /// <summary>
    ///     Gets whether <paramref name="view"/> is in the SubView hierarchy of <paramref name="start"/>.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="view"></param>
    /// <returns></returns>
    public static bool IsInHierarchy (View? start, View? view)
    {
        if (view is null)
        {
            return false;
        }

        if (view == start || start is null)
        {
            return true;
        }

        foreach (View subView in start.SubViews)
        {
            if (view == subView)
            {
                return true;
            }

            bool found = IsInHierarchy (subView, view);

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
        Debug.Assert (value is null or { CanFocus: true, HasFocus: true });

        _focused = value;

        FocusedChanged?.Invoke (this, EventArgs.Empty);
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
        if (App?.Popover?.GetActivePopover () as View is { Visible: true } visiblePopover)
        {
            return visiblePopover.AdvanceFocus (direction, behavior);
        }
        return App?.TopRunnableView is { } && App.TopRunnableView.AdvanceFocus (direction, behavior);
    }
}
