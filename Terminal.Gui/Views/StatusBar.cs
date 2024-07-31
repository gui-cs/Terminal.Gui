using System;
using System.Reflection;

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
    /// <inheritdoc/>
    public StatusBar () : this ([]) { }

    /// <inheritdoc/>
    public StatusBar (IEnumerable<Shortcut> shortcuts) : base (shortcuts)
    {
        TabStop = TabBehavior.NoStop;
        Orientation = Orientation.Horizontal;
        Y = Pos.AnchorEnd ();
        Width = Dim.Fill ();
        Height = Dim.Auto (DimAutoStyle.Content, 1);
        BorderStyle = LineStyle.Dashed;
        ColorScheme = Colors.ColorSchemes ["Menu"];

        LayoutStarted += StatusBar_LayoutStarted;
    }

    // StatusBar arranges the items horizontally.
    // The first item has no left border, the last item has no right border.
    // The Shortcuts are configured with the command, help, and key views aligned in reverse order (EndToStart).
    private void StatusBar_LayoutStarted (object sender, LayoutEventArgs e)
    {
        for (int index = 0; index < Subviews.Count; index++)
        {
            View barItem = Subviews [index];

            barItem.BorderStyle = BorderStyle;

            if (index == Subviews.Count - 1)
            {
                barItem.Border.Thickness = new Thickness (0, 0, 0, 0);
            }
            else
            {
                barItem.Border.Thickness = new Thickness (0, 0, 1, 0);
            }

            if (barItem is Shortcut shortcut)
            {
                shortcut.Orientation = Orientation.Horizontal;
            }
        }
    }

    /// <inheritdoc/>
    public override View Add (View view)
    {
        // Call base first, because otherwise it resets CanFocus to true
        base.Add (view);

        view.CanFocus = false;

        if (view is Shortcut shortcut)
        {
            // TODO: not happy about using AlignmentModes for this. Too implied.
            // TODO: instead, add a property (a style enum?) to Shortcut to control this
            shortcut.AlignmentModes = AlignmentModes.EndToStart;
        }

        return view;
    }
}
