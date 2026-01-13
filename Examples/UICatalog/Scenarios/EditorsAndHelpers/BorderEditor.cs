#nullable enable
using System.Reflection;
using Terminal.Gui.ViewBase;

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
        _osBorderStyle!.Value = ((Border)AdornmentToEdit).LineStyle;
        _ckbGradient!.CheckedState = ((Border)AdornmentToEdit).Settings.FastHasFlags (BorderSettings.Gradient) ? CheckState.Checked : CheckState.UnChecked;
    }

    private void BorderEditor_Initialized (object? sender, EventArgs e)
    {
        _osBorderStyle = new ()
        {
            X = 0,

            Y = Pos.Bottom (SubViews.ToArray () [^1]),
            Width = Dim.Fill (),
            Value = (AdornmentToEdit as Border)?.LineStyle ?? LineStyle.None,
            BorderStyle = LineStyle.Single,
            Title = "Border St_yle",
            SuperViewRendersLineCanvas = true
        };
        Add (_osBorderStyle);

        _osBorderStyle.ValueChanged += OnRbBorderStyleOnValueChanged;

        _ckbTitle = new ()
        {
            X = 0,
            Y = Pos.Bottom (_osBorderStyle),

            CheckedState = CheckState.Checked,
            SuperViewRendersLineCanvas = true,
            Text = "Title"
        };

        _ckbTitle.CheckedStateChanging += OnCkbTitleOnToggle;
        Add (_ckbTitle);

        _ckbGradient = new ()
        {
            X = 0,
            Y = Pos.Bottom (_ckbTitle),

            CheckedState = CheckState.Checked,
            SuperViewRendersLineCanvas = true,
            Text = "Gradient"
        };

        _ckbGradient.CheckedStateChanging += OnCkbGradientOnToggle;
        Add (_ckbGradient);

        return;

        void OnRbBorderStyleOnValueChanged (object? s, EventArgs<LineStyle?> args)
        {
            if (AdornmentToEdit is not Border border)
            {
                return;
            }

            if (args.Value is not null)
            {
                border.LineStyle = (LineStyle)args.Value;
            }

            border.SetNeedsDraw ();
            SetNeedsLayout ();
        }

        void OnCkbTitleOnToggle (object? _, ResultEventArgs<CheckState> args)
        {
            if (AdornmentToEdit is not Border border)
            {
                return;
            }

            if (args.Result == CheckState.Checked)

            {
                border.Settings |= BorderSettings.Title;
            }
            else

            {
                border.Settings &= ~BorderSettings.Title;
            }
        }

        void OnCkbGradientOnToggle (object? _, ResultEventArgs<CheckState> args)
        {
            if (AdornmentToEdit is not Border border)
            {
                return;
            }

            if (args.Result == CheckState.Checked)

            {
                border.Settings |= BorderSettings.Gradient;
            }
            else

            {
                border.Settings &= ~BorderSettings.Gradient;
            }
        }
    }
}
