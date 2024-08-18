#nullable enable

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

    private View? _focused = null;

    /// <summary>
    ///     Gets the most focused <see cref="View"/> in the application, if there is one.
    /// </summary>
    public View? GetFocused () { return _focused; }

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

        return;
    }

    /// <summary>
    ///     Raised when the most focused <see cref="View"/> in the application has changed.
    /// </summary>
    public event EventHandler<EventArgs>? FocusedChanged;


    /// <summary>
    ///     Gets whether <paramref name="view"/> is in the Subview hierarchy of <paramref name="start"/>.
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

        foreach (View subView in start.Subviews)
        {
            if (view == subView)
            {
                return true;
            }

            var found = IsInHierarchy (subView, view);
            if (found)
            {
                return found;
            }
        }

        return false;
    }


    /// <summary>
    ///     Gets the deepest focused subview of the specified <paramref name="view"/>.
    /// </summary>
    /// <param name="view"></param>
    /// <returns></returns>
    internal static View? GetDeepestFocusedSubview (View? view)
    {
        if (view is null)
        {
            return null;
        }

        foreach (View v in view.Subviews)
        {
            if (v.HasFocus)
            {
                return GetDeepestFocusedSubview (v);
            }
        }

        return view;
    }
    

    
    internal static void MovePreviousViewOrTop ()
    {
        if (ApplicationOverlapped.OverlappedTop is null)
        {
            Toplevel? top = Application.Current!.Modal ? Application.Current : Application.Top;
            top!.AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabGroup);

            if (top.Focused is null)
            {
                top.AdvanceFocus (NavigationDirection.Backward, null);
            }

            top.SetNeedsDisplay ();
            ApplicationOverlapped.BringOverlappedTopToFront ();
        }
        else
        {
            ApplicationOverlapped.OverlappedMovePrevious ();
        }
    }
}
