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
        TabStop = TabBehavior.TabStop;
        Width = Dim.Fill ();
        Height = Dim.Fill ();
        base.Visible = false;
    }

    /// <inheritdoc/>
    protected override bool OnHandlingHotKey (CommandEventArgs args)
    {
        if (base.OnHandlingHotKey (args))
        {
            return true;
        }

        if (SuperView is TabView tabView)
        {
            tabView.SelectedTabIndex = tabView.Tabs.IndexOf (this);

            return true;
        }

        return false;
    }
}
