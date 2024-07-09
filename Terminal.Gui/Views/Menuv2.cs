﻿using System;
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
        Initialized += Menuv2_Initialized;
    }

    private void Menuv2_Initialized (object sender, EventArgs e)
    {
        Border.Thickness = new Thickness (1, 1, 1, 1);
    }

    // Menuv2 arranges the items horizontally.
    // The first item has no left border, the last item has no right border.
    // The Shortcuts are configured with the command, help, and key views aligned in reverse order (EndToStart).
    internal override void OnLayoutStarted (LayoutEventArgs args)
    {
        for (int index = 0; index < Subviews.Count; index++)
        {
            View barItem = Subviews [index];

            if (!barItem.Visible)
            {
                continue;
            }

        }
        base.OnLayoutStarted (args);
    }

    /// <inheritdoc/>
    public override View Add (View view)
    {
        base.Add (view);

        if (view is Shortcut shortcut)
        {
            shortcut.CanFocus = true;
            shortcut.KeyBindingScope = KeyBindingScope.Application;
            shortcut.Orientation = Orientation.Vertical;

            // TODO: not happy about using AlignmentModes for this. Too implied.
            // TODO: instead, add a property (a style enum?) to Shortcut to control this
            //shortcut.AlignmentModes = AlignmentModes.EndToStart;
        }

        return view;
    }
}
