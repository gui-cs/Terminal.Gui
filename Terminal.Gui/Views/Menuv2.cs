using System;
using System.ComponentModel;
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
        Initialized += Menuv2_Initialized;
        VisibleChanged += OnVisibleChanged;
    }

    private void OnVisibleChanged (object sender, EventArgs e)
    {
        if (Visible)
        {
            //Application.GrabMouse(this);
        }
        else
        {
            if (Application.MouseGrabView == this)
            {
                //Application.UngrabMouse ();
            }
        }
    }

    private void Menuv2_Initialized (object sender, EventArgs e)
    {
        Border.Thickness = new Thickness (1, 1, 1, 1);
        Border.LineStyle = LineStyle.Single;
        ColorScheme = Colors.ColorSchemes ["Menu"];
    }

    // Menuv2 arranges the items horizontally.
    // The first item has no left border, the last item has no right border.
    // The Shortcuts are configured with the command, help, and key views aligned in reverse order (EndToStart).
    /// <inheritdoc />
    protected override void OnSubViewLayout (LayoutEventArgs args)
    {
        for (int index = 0; index < SubViews.Count; index++)
        {
            View barItem = SubViews.ElementAt (index);

            if (!barItem.Visible)
            {
                continue;
            }

        }
        base.OnSubViewLayout (args);
    }

    /// <inheritdoc/>
    /// 
    protected override void OnSubViewAdded (View subView)
    {
        if (subView is Shortcut shortcut)
        {
            shortcut.CanFocus = true;
            shortcut.Orientation = Orientation.Vertical;
            shortcut.HighlightStyle |= HighlightStyle.Hover;

            // TODO: not happy about using AlignmentModes for this. Too implied.
            // TODO: instead, add a property (a style enum?) to Shortcut to control this
            //shortcut.AlignmentModes = AlignmentModes.EndToStart;

            shortcut.Accepting += ShortcutOnAccept;

            void ShortcutOnAccept (object sender, CommandEventArgs e)
            {
                if (Arrangement.HasFlag (ViewArrangement.Overlapped) && Visible)
                {
                    Visible = false;
                    e.Cancel = true;

                    return;
                }

                //if (!e.Handled)
                //{
                //    RaiseAcceptEvent ();
                //}
            }
        }
    }
}
