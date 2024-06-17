using System;
using System.Reflection;

namespace Terminal.Gui;

/// <summary>
/// </summary>
public class Menuv2 : Bar
{
    /// <inheritdoc/>
    public Menuv2 () : this ([]) { }

    /// <inheritdoc/>
    public Menuv2 (IEnumerable<Shortcut> shortcuts) : base (shortcuts)
    {
        Orientation = Orientation.Vertical;
        Width = Dim.Auto ();
        Height = Dim.Auto (DimAutoStyle.Content, 1);
        ColorScheme = Colors.ColorSchemes ["Menu"];

        LayoutStarted += Menuv2_LayoutStarted;
    }

    // Menuv2 arranges the items horizontally.
    // The first item has no left border, the last item has no right border.
    // The Shortcuts are configured with the command, help, and key views aligned in reverse order (EndToStart).
    private void Menuv2_LayoutStarted (object sender, LayoutEventArgs e)
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
                barItem.Border.Thickness = new Thickness (1, 0, 1, 1);
            }
            else if (index == 0)
            {
                barItem.Border.Thickness = new Thickness (1, 1, 1, 0);
            }
            else
            {
                barItem.Border.Thickness = new Thickness (1, 0, 1, 0);
            }

            if (barItem is Shortcut shortcut)
            {
                //                shortcut.Min
                // shortcut.Orientation = Orientation.Vertical;
            }

            prevBarItem = barItem;
            // HACK: This should not be needed
            barItem.SetRelativeLayout (GetContentSize ());

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

        }

        return view;
    }
}
