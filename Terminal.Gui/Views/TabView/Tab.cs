namespace Terminal.Gui.Views;

/// <summary>
///     Represents a single tab in a <see cref="TabView"/>. The <see cref="View.Title"/> property provides
///     the tab header text (with underscore hotkey convention). Developers add content SubViews
///     directly to the Tab, just like any other View.
/// </summary>
public class Tab : View
{
    /// <summary>Creates a new tab. Hidden by default until selected.</summary>
    public Tab ()
    {
        CanFocus = true;
        Width = Dim.Fill ();
        Height = Dim.Fill ();
        Visible = false;
    }
}
