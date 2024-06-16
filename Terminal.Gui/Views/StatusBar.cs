namespace Terminal.Gui;

/// <summary>
///     A status bar is a <see cref="View"/> that snaps to the bottom of a <see cref="Toplevel"/> displaying set of
///     <see cref="Shortcut"/>s. The <see cref="StatusBar"/> should be context sensitive. This means, if the main menu
///     and an open text editor are visible, the items probably shown will be ~F1~ Help ~F2~ Save ~F3~ Load. While a dialog
///     to ask a file to load is executed, the remaining commands will probably be ~F1~ Help. So for each context must be a
///     new instance of a status bar.
/// </summary>
public class StatusBar : Bar
{
    /// <inheritdoc />
    public StatusBar () : this ([]) { }

    /// <inheritdoc />
    public StatusBar (IEnumerable<Shortcut> shortcuts) : base (shortcuts)
    {
        Orientation = Orientation.Horizontal;
        Y = Pos.AnchorEnd ();
        Width = Dim.Fill ();
        StatusBarStyle = true;
    }

    /// <inheritdoc />
    public override View Add (View view)
    {
        view.CanFocus = false;
        if (view is Shortcut shortcut)
        {
            shortcut.KeyBindingScope = KeyBindingScope.Application;
            shortcut.AlignmentModes = AlignmentModes.EndToStart | AlignmentModes.IgnoreFirstOrLast;
        }
        return base.Add (view);
    }

}
