using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

public class BorderEditor : AdornmentEditor
{
    private CheckBox _ckbTitle;
    private RadioGroup _rbBorderStyle;

    public BorderEditor ()
    {
        Title = "_Border";
        Initialized += BorderEditor_Initialized;
        AdornmentChanged += BorderEditor_AdornmentChanged;

    }

    private void BorderEditor_AdornmentChanged (object sender, EventArgs e)
    {
        _ckbTitle.State = ((Border)AdornmentToEdit).ShowTitle ? CheckState.Checked : CheckState.UnChecked;
        _rbBorderStyle.SelectedItem = (int)((Border)AdornmentToEdit).LineStyle;
    }

    private void BorderEditor_Initialized (object sender, EventArgs e)
    {

        List<LineStyle> borderStyleEnum = Enum.GetValues (typeof (LineStyle)).Cast<LineStyle> ().ToList ();

        _rbBorderStyle = new RadioGroup
        {
            X = 0,
            // BUGBUG: Hack until dimauto is working properly
            Y = Pos.Bottom (Subviews [^1]),
            Width = Dim.Width (Subviews [^2]) + Dim.Width (Subviews [^1]) - 1,
            SelectedItem = (int)(((Border)AdornmentToEdit)?.LineStyle ?? LineStyle.None),
            BorderStyle = LineStyle.Single,
            Title = "Border St_yle",
            SuperViewRendersLineCanvas = true,
            Enabled = AdornmentToEdit is { },
            RadioLabels = borderStyleEnum.Select (e => e.ToString ()).ToArray ()
        };
        Add (_rbBorderStyle);

        _rbBorderStyle.SelectedItemChanged += OnRbBorderStyleOnSelectedItemChanged;

        _ckbTitle = new CheckBox
        {
            X = 0,
            Y = Pos.Bottom (_rbBorderStyle),

            State = CheckState.Checked,
            SuperViewRendersLineCanvas = true,
            Text = "Show Title",
            Enabled = AdornmentToEdit is { }
        };


        _ckbTitle.Toggle += OnCkbTitleOnToggle;
        Add (_ckbTitle);

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

        void OnCkbTitleOnToggle (object sender, CancelEventArgs<CheckState> args) { ((Border)AdornmentToEdit).ShowTitle = args.NewValue == CheckState.Checked; }
    }
}