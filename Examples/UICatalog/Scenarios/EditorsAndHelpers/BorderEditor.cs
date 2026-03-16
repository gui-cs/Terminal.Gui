#nullable enable
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
        _ckbTitle!.Value = ((Border)AdornmentToEdit!).Settings.FastHasFlags (BorderSettings.Title) ? CheckState.Checked : CheckState.UnChecked;
        _osBorderStyle!.Value = ((Border)AdornmentToEdit).LineStyle;
        _ckbGradient!.Value = ((Border)AdornmentToEdit).Settings.FastHasFlags (BorderSettings.Gradient) ? CheckState.Checked : CheckState.UnChecked;
    }

    private void BorderEditor_Initialized (object? sender, EventArgs e)
    {
        _osBorderStyle = new OptionSelector<LineStyle>
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

        _ckbTitle = new CheckBox
        {
            X = 0,
            Y = Pos.Bottom (_osBorderStyle),
            Value = CheckState.Checked,
            SuperViewRendersLineCanvas = true,
            Text = "Title"
        };

        _ckbTitle.ValueChanging += OnCkbTitleOnToggle;
        Add (_ckbTitle);

        _ckbGradient = new CheckBox
        {
            X = 0,
            Y = Pos.Bottom (_ckbTitle),
            Value = CheckState.Checked,
            SuperViewRendersLineCanvas = true,
            Text = "Gradient"
        };

        _ckbGradient.ValueChanging += OnCkbGradientOnToggle;
        Add (_ckbGradient);

        return;

        void OnRbBorderStyleOnValueChanged (object? s, EventArgs<LineStyle?> args)
        {
            if (AdornmentToEdit is not Border border)
            {
                return;
            }

            if (args.Value is { })
            {
                border.LineStyle = (LineStyle)args.Value;
            }

            border.SetNeedsDraw ();
            SetNeedsLayout ();
        }

        void OnCkbTitleOnToggle (object? _, ValueChangingEventArgs<CheckState> args)
        {
            if (AdornmentToEdit is not Border border)
            {
                return;
            }

            if (args.NewValue == CheckState.Checked)

            {
                border.Settings |= BorderSettings.Title;
            }
            else

            {
                border.Settings &= ~BorderSettings.Title;
            }
        }

        void OnCkbGradientOnToggle (object? _, ValueChangingEventArgs<CheckState> args)
        {
            if (AdornmentToEdit is not Border border)
            {
                return;
            }

            if (args.NewValue == CheckState.Checked)

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
