#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace UICatalog.Scenarios;

public class BorderEditor : AdornmentEditor
{
    private CheckBox? _ckbTitle;
    private OptionSelector<LineStyle>? _osBorderStyle;
    private CheckBox? _ckbGradient;

    public BorderEditor ()
    {
        Title = "_Border";
        Initialized += BorderEditor_Initialized;
        AdornmentChanged += BorderEditor_AdornmentChanged;
    }

    private void BorderEditor_AdornmentChanged (object? sender, EventArgs e)
    {
        _ckbTitle!.CheckedState = ((Border)AdornmentToEdit!).Settings.FastHasFlags (BorderSettings.Title) ? CheckState.Checked : CheckState.UnChecked;
        _osBorderStyle!.SelectedItem = (int)((Border)AdornmentToEdit).LineStyle;
        _ckbGradient!.CheckedState = ((Border)AdornmentToEdit).Settings.FastHasFlags (BorderSettings.Gradient) ? CheckState.Checked : CheckState.UnChecked;
    }

    private void BorderEditor_Initialized (object? sender, EventArgs e)
    {
        _osBorderStyle = new ()
        {
            X = 0,

            Y = Pos.Bottom (SubViews.ToArray() [^1]),
            Width = Dim.Fill (),
            SelectedItem = (int)(((Border)AdornmentToEdit!)?.LineStyle ?? LineStyle.None),
            BorderStyle = LineStyle.Single,
            Title = "Border St_yle",
            SuperViewRendersLineCanvas = true,
        };
        Add (_osBorderStyle);

        _osBorderStyle.SelectedItemChanged += OnRbBorderStyleOnSelectedItemChanged;

        _ckbTitle = new ()
        {
            X = 0,
            Y = Pos.Bottom (_osBorderStyle),

            CheckedState = CheckState.Checked,
            SuperViewRendersLineCanvas = true,
            Text = "Title",
        };

        _ckbTitle.CheckedStateChanging += OnCkbTitleOnToggle;
        Add (_ckbTitle);

        _ckbGradient = new ()
        {
            X = 0,
            Y = Pos.Bottom (_ckbTitle),

            CheckedState = CheckState.Checked,
            SuperViewRendersLineCanvas = true,
            Text = "Gradient",
        };

        _ckbGradient.CheckedStateChanging += OnCkbGradientOnToggle;
        Add (_ckbGradient);

        return;

        void OnRbBorderStyleOnSelectedItemChanged (object? s, SelectedItemChangedArgs args)
        {
            LineStyle prevBorderStyle = AdornmentToEdit!.BorderStyle;
            ((Border)AdornmentToEdit).LineStyle = (LineStyle)args.SelectedItem!;

            if (((Border)AdornmentToEdit).LineStyle == LineStyle.None)
            {
                ((Border)AdornmentToEdit).Thickness = new (0);
            }
            else if (prevBorderStyle == LineStyle.None && ((Border)AdornmentToEdit).LineStyle != LineStyle.None)
            {
                ((Border)AdornmentToEdit).Thickness = new (1);
            }

            ((Border)AdornmentToEdit).SetNeedsDraw ();
            SetNeedsLayout ();
        }

        void OnCkbTitleOnToggle (object? _, ResultEventArgs<CheckState> args)
        {
            if (args.Result == CheckState.Checked)

            {
                ((Border)AdornmentToEdit!).Settings |= BorderSettings.Title;
            }
            else

            {
                ((Border)AdornmentToEdit!).Settings &= ~BorderSettings.Title;
            }
        }

        void OnCkbGradientOnToggle (object? _, ResultEventArgs<CheckState> args)
        {
            if (args.Result == CheckState.Checked)

            {
                ((Border)AdornmentToEdit!).Settings |= BorderSettings.Gradient;
            }
            else

            {
                ((Border)AdornmentToEdit!).Settings &= ~BorderSettings.Gradient;
            }
        }
    }
}
