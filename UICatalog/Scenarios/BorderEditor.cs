using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

public class BorderEditor : AdornmentEditor
{
    public BorderEditor ()
    {
        Title = "_Border";
        Initialized += BorderEditor_Initialized;
    }

    private void BorderEditor_Initialized (object sender, EventArgs e)
    {

        List<LineStyle> borderStyleEnum = Enum.GetValues (typeof (LineStyle)).Cast<LineStyle> ().ToList ();

        var rbBorderStyle = new RadioGroup
        {
            X = 0,
            Y = Pos.Bottom (Subviews [^1]),
            Width = Dim.Width (Subviews [^2]) + Dim.Width (Subviews [^1]) - 1,
            SelectedItem = (int)(((Border)AdornmentToEdit)?.LineStyle ?? LineStyle.None),
            BorderStyle = LineStyle.Single,
            Title = "Border St_yle",
            SuperViewRendersLineCanvas = true,
            Enabled = AdornmentToEdit is { },
            RadioLabels = borderStyleEnum.Select (e => e.ToString ()).ToArray ()
        };
        Add (rbBorderStyle);

        rbBorderStyle.SelectedItemChanged += OnRbBorderStyleOnSelectedItemChanged;

        var ckbTitle = new CheckBox
        {
            X = 0,
            Y = Pos.Bottom (rbBorderStyle),

            Checked = true,
            SuperViewRendersLineCanvas = true,
            Text = "Show Title",
            Enabled = AdornmentToEdit is { }
        };


        ckbTitle.Toggled += OnCkbTitleOnToggled;
        Add (ckbTitle);

        return;

        void OnRbBorderStyleOnSelectedItemChanged (object s, SelectedItemChangedArgs e)
        {
            LineStyle prevBorderStyle = AdornmentToEdit.BorderStyle;
            ((Border)AdornmentToEdit).LineStyle = (LineStyle)e.SelectedItem;

            if (((Border)AdornmentToEdit).LineStyle == LineStyle.None)
            {
                ((Border)AdornmentToEdit).Thickness = new (0);
            }
            else if (prevBorderStyle == LineStyle.None && ((Border)AdornmentToEdit).LineStyle != LineStyle.None)
            {
                ((Border)AdornmentToEdit).Thickness = new (1);
            }

            ((Border)AdornmentToEdit).SetNeedsDisplay ();
            LayoutSubviews ();
        }

        void OnCkbTitleOnToggled (object sender, StateEventArgs<bool?> args) { ((Border)AdornmentToEdit).ShowTitle = args.NewValue!.Value; }
    }
}