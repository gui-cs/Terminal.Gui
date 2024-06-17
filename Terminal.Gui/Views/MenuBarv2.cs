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

        LayoutStarted += MenuBarv2_LayoutStarted;
    }

    // MenuBarv2 arranges the items horizontally.
    // The first item has no left border, the last item has no right border.
    // The Shortcuts are configured with the command, help, and key views aligned in reverse order (EndToStart).
    private void MenuBarv2_LayoutStarted (object sender, LayoutEventArgs e)
    {
        var minKeyWidth = 0;

        List<Shortcut> shortcuts = Subviews.Where (s => s is Shortcut && s.Visible).Cast<Shortcut> ().ToList ();

        foreach (Shortcut shortcut in shortcuts)
        {
            // Let AutoSize do its thing to get the minimum width of each CommandView and HelpView
            //shortcut.CommandView.SetRelativeLayout (new Size (int.MaxValue, int.MaxValue));
            minKeyWidth = int.Max (minKeyWidth, shortcut.KeyView.Text.GetColumns ());
        }

        View prevBarItem = null;
        var maxBarItemWidth = 0;

        for (int index = 0; index < Subviews.Count; index++)
        {
            View barItem = Subviews [index];

            if (!barItem.Visible)
            {
                continue;
            }

            if (barItem is Shortcut scBarItem)
            {
                scBarItem.MinimumKeyViewSize = minKeyWidth;
            }

            if (index == Subviews.Count - 1)
            {
                barItem.Border.Thickness = new Thickness (0, 0, 0, 0);
            }
            else
            {
                barItem.Border.Thickness = new Thickness (0, 0, 0, 0);
            }

            if (barItem is Shortcut shortcut)
            {
                //                shortcut.Min
                // shortcut.Orientation = Orientation.Vertical;
            }

            maxBarItemWidth = Math.Max (maxBarItemWidth, barItem.Frame.Width);

        }

        foreach (Shortcut shortcut in shortcuts)
        {
            shortcut.Width = maxBarItemWidth;
        }
    }

    /// <inheritdoc/>
    public override View Add (View view)
    {
        // Call base first, because otherwise it resets CanFocus to true
        base.Add (view);

        view.CanFocus = true;

        if (view is Shortcut shortcut)
        {
            shortcut.KeyBindingScope = KeyBindingScope.Application;

            // TODO: not happy about using AlignmentModes for this. Too implied.
            // TODO: instead, add a property (a style enum?) to Shortcut to control this
            //shortcut.AlignmentModes = AlignmentModes.EndToStart;

            shortcut.KeyView.Visible = false;
            shortcut.HelpView.Visible = false;
        }

        return view;
    }
}
