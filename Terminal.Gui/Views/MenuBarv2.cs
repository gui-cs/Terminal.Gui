using System;
using System.Reflection;

namespace Terminal.Gui;

/// <summary>
///     A menu bar is a <see cref="View"/> that snaps to the top of a <see cref="Toplevel"/> displaying set of
///     <see cref="Shortcut"/>s.
/// </summary>
public class MenuBarv2 : Bar
{
    /// <inheritdoc/>
    public MenuBarv2 () : this ([]) { }

    /// <inheritdoc/>
    public MenuBarv2 (IEnumerable<Shortcut> shortcuts) : base (shortcuts)
    {
        Y = 0;
        Width = Dim.Fill ();
        Height = Dim.Auto (DimAutoStyle.Content, 1);
        BorderStyle = LineStyle.Dashed;
        ColorScheme = Colors.ColorSchemes ["Menu"];
        Orientation = Orientation.Horizontal;

        SubViewLayout += MenuBarv2_LayoutStarted;
    }

    // MenuBarv2 arranges the items horizontally.
    // The first item has no left border, the last item has no right border.
    // The Shortcuts are configured with the command, help, and key views aligned in reverse order (EndToStart).
    private void MenuBarv2_LayoutStarted (object sender, LayoutEventArgs e)
    {
       
    }

    /// <inheritdoc/>
    protected override void OnSubViewAdded (View subView)
    {
        subView.CanFocus = false;

        if (subView is Shortcut shortcut)
        {
            // TODO: not happy about using AlignmentModes for this. Too implied.
            // TODO: instead, add a property (a style enum?) to Shortcut to control this
            //shortcut.AlignmentModes = AlignmentModes.EndToStart;

            shortcut.KeyView.Visible = false;
            shortcut.HelpView.Visible = false;
        }
    }
}
