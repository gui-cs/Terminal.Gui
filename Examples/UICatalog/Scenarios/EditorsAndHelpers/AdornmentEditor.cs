#nullable enable
using Terminal.Gui.ViewBase;

namespace UICatalog.Scenarios;

/// <summary>
///     Provides a composable UI for editing the settings of an Adornment.
/// </summary>
public class AdornmentEditor : EditorBase
{
    private readonly ColorPicker16 _backgroundColorPicker = new ()
    {
        Title = "_BG",
        BoxWidth = 1,
        BoxHeight = 1,
        BorderStyle = LineStyle.Single,
        SuperViewRendersLineCanvas = true
    };

    private readonly ColorPicker16 _foregroundColorPicker = new ()
    {
        Title = "_FG",
        BoxWidth = 1,
        BoxHeight = 1,
        BorderStyle = LineStyle.Single,
        SuperViewRendersLineCanvas = true
    };

    private CheckBox? _diagThicknessCheckBox;
    private CheckBox? _diagRulerCheckBox;

    private AdornmentImpl? _adornment;

    public AdornmentImpl? AdornmentToEdit
    {
        get => _adornment;
        set
        {
            Enabled = value is not null;

            if (value == _adornment)
            {
                return;
            }

            _adornment = value;

            if (_adornment is null)
            {
                return;
            }

            if (IsInitialized)
            {
                _topEdit!.Value = _adornment.Thickness.Top;
                _leftEdit!.Value = _adornment.Thickness.Left;
                _bottomEdit!.Value = _adornment.Thickness.Bottom;
                _rightEdit!.Value = _adornment.Thickness.Right;

                if (_adornment.View is { } adornmentView)
                {
                    adornmentView.Initialized += (_, _) =>
                                                 {
                                                     _foregroundColorPicker.SelectedColor =
                                                         adornmentView.GetAttributeForRole (VisualRole.Normal).Foreground.GetClosestNamedColor16 ();

                                                     _backgroundColorPicker.SelectedColor =
                                                         adornmentView.GetAttributeForRole (VisualRole.Normal).Background.GetClosestNamedColor16 ();
                                                 };
                }
            }

            OnAdornmentChanged ();
        }
    }

    public event EventHandler<EventArgs>? AdornmentChanged;

    public void OnAdornmentChanged () { AdornmentChanged?.Invoke (this, EventArgs.Empty); }

    /// <inheritdoc/>
    protected override void OnViewToEditChanged ()
    {
        base.OnViewToEditChanged ();
        AdornmentToEdit = (ViewToEdit as AdornmentView)?.Adornment as AdornmentImpl;
    }

    private NumericUpDown<int>? _topEdit;
    private NumericUpDown<int>? _leftEdit;
    private NumericUpDown<int>? _bottomEdit;
    private NumericUpDown<int>? _rightEdit;

    public AdornmentEditor ()
    {
        CanFocus = true;
        Initialized += AdornmentEditor_Initialized;
    }

    private void AdornmentEditor_Initialized (object? sender, EventArgs e)
    {
        _topEdit = new ()
        {
            X = Pos.Center (), Y = 0,
            Format = "{0, 2}"
        };

        _topEdit.ValueChanging += Top_ValueChanging;
        Add (_topEdit);

        _leftEdit = new ()
        {
            X = Pos.Left (_topEdit) - Pos.Func (_ => _topEdit.Text.Length) - 2, Y = Pos.Bottom (_topEdit),
            Format = _topEdit.Format
        };

        _leftEdit.ValueChanging += Left_ValueChanging;
        Add (_leftEdit);

        _rightEdit = new ()
        {
            X = Pos.Right (_leftEdit) + 5, Y = Pos.Bottom (_topEdit),
            Format = _topEdit.Format
        };

        _rightEdit.ValueChanging += Right_ValueChanging;
        Add (_rightEdit);

        _bottomEdit = new ()
        {
            X = Pos.Center (), Y = Pos.Bottom (_leftEdit),
            Format = _topEdit.Format
        };

        _bottomEdit.ValueChanging += Bottom_ValueChanging;
        Add (_bottomEdit);

        Button copyTop = new ()
        {
            X = Pos.Center (), Y = Pos.Bottom (_bottomEdit), Text = "Cop_y Top"
        };

        copyTop.Accepting += (_, _) =>
                             {
                                 AdornmentToEdit!.Thickness = new (_topEdit.Value);
                                 _leftEdit.Value = _rightEdit.Value = _bottomEdit.Value = _topEdit.Value;
                             };
        Add (copyTop);

        // Foreground ColorPicker.
        _foregroundColorPicker.X = 0;
        _foregroundColorPicker.Y = Pos.Bottom (copyTop);

        _foregroundColorPicker.ValueChanged += ColorPickerColorChanged ();
        Add (_foregroundColorPicker);

        // Background ColorPicker.
        _backgroundColorPicker.X = Pos.Right (_foregroundColorPicker) - 1;
        _backgroundColorPicker.Y = Pos.Top (_foregroundColorPicker);

        _backgroundColorPicker.ValueChanged += ColorPickerColorChanged ();
        Add (_backgroundColorPicker);

        _topEdit.Value = AdornmentToEdit?.Thickness.Top ?? 0;
        _leftEdit.Value = AdornmentToEdit?.Thickness.Left ?? 0;
        _rightEdit.Value = AdornmentToEdit?.Thickness.Right ?? 0;
        _bottomEdit.Value = AdornmentToEdit?.Thickness.Bottom ?? 0;

        _diagThicknessCheckBox = new () { Text = "_Thickness Diag." };

        if (AdornmentToEdit is not null)
        {
            _diagThicknessCheckBox.Value =
                AdornmentToEdit.Diagnostics.FastHasFlags (ViewDiagnosticFlags.Thickness) ? CheckState.Checked : CheckState.UnChecked;
        }
        else
        {
            _diagThicknessCheckBox.Value = Diagnostics.FastHasFlags (ViewDiagnosticFlags.Thickness) ? CheckState.Checked : CheckState.UnChecked;
        }

        _diagThicknessCheckBox.ValueChanging += (_, args) =>
                                                       {
                                                           if (args.NewValue == CheckState.Checked)
                                                           {
                                                               AdornmentToEdit!.Diagnostics |= ViewDiagnosticFlags.Thickness;
                                                           }
                                                           else
                                                           {
                                                               AdornmentToEdit!.Diagnostics &= ~ViewDiagnosticFlags.Thickness;
                                                           }
                                                       };

        Add (_diagThicknessCheckBox);
        _diagThicknessCheckBox.Y = Pos.Bottom (_backgroundColorPicker);

        _diagRulerCheckBox = new () { Text = "_Ruler" };

        if (AdornmentToEdit is not null)
        {
            _diagRulerCheckBox.Value = AdornmentToEdit.Diagnostics.FastHasFlags (ViewDiagnosticFlags.Ruler) ? CheckState.Checked : CheckState.UnChecked;
        }
        else
        {
            _diagRulerCheckBox.Value = Diagnostics.FastHasFlags (ViewDiagnosticFlags.Ruler) ? CheckState.Checked : CheckState.UnChecked;
        }

        _diagRulerCheckBox.ValueChanging += (_, args) =>
                                                   {
                                                       if (args.NewValue == CheckState.Checked)
                                                       {
                                                           AdornmentToEdit!.Diagnostics |= ViewDiagnosticFlags.Ruler;
                                                       }
                                                       else
                                                       {
                                                           AdornmentToEdit!.Diagnostics &= ~ViewDiagnosticFlags.Ruler;
                                                       }
                                                   };

        Add (_diagRulerCheckBox);
        _diagRulerCheckBox.Y = Pos.Bottom (_diagThicknessCheckBox);
    }

    private EventHandler<ValueChangedEventArgs<ColorName16>> ColorPickerColorChanged ()
    {
        return (_, _) =>
               {
                   if (AdornmentToEdit is null)
                   {
                       return;
                   }

                   AdornmentToEdit.SetScheme (
                                              new (AdornmentToEdit.GetScheme ())
                                              {
                                                  Normal = new (_foregroundColorPicker.SelectedColor, _backgroundColorPicker.SelectedColor)
                                              })
                       ;
               };
    }

    private void Top_ValueChanging (object? sender, ValueChangingEventArgs<int> e)
    {
        if (e.NewValue < 0 || AdornmentToEdit is null)
        {
            e.Handled = true;

            return;
        }

        AdornmentToEdit.Thickness = new (AdornmentToEdit.Thickness.Left, e.NewValue, AdornmentToEdit.Thickness.Right, AdornmentToEdit.Thickness.Bottom);
    }

    private void Left_ValueChanging (object? sender, ValueChangingEventArgs<int> e)
    {
        if (e.NewValue < 0 || AdornmentToEdit is null)
        {
            e.Handled = true;

            return;
        }

        AdornmentToEdit.Thickness = new (e.NewValue, AdornmentToEdit.Thickness.Top, AdornmentToEdit.Thickness.Right, AdornmentToEdit.Thickness.Bottom);
    }

    private void Right_ValueChanging (object? sender, ValueChangingEventArgs<int> e)
    {
        if (e.NewValue < 0 || AdornmentToEdit is null)
        {
            e.Handled = true;

            return;
        }

        AdornmentToEdit.Thickness = new (AdornmentToEdit.Thickness.Left, AdornmentToEdit.Thickness.Top, e.NewValue, AdornmentToEdit.Thickness.Bottom);
    }

    private void Bottom_ValueChanging (object? sender, ValueChangingEventArgs<int> e)
    {
        if (e.NewValue < 0 || AdornmentToEdit is null)
        {
            e.Handled = true;

            return;
        }

        AdornmentToEdit.Thickness = new (AdornmentToEdit.Thickness.Left, AdornmentToEdit.Thickness.Top, AdornmentToEdit.Thickness.Right, e.NewValue);
    }
}
