#nullable enable

namespace UICatalog.Scenarios;

public class ViewPropertiesEditor : EditorBase
{
    private CheckBox? _canFocusCheckBox;
    private CheckBox? _enabledCheckBox;
    private RadioGroup? _orientation;
    private TextView? _text;

    /// <inheritdoc/>
    public override void EndInit ()
    {
        _canFocusCheckBox = new ()
        {
            Title = "CanFocus",
            X = 0,
            Y = 0,
            CheckedState = ViewToEdit is { } ? ViewToEdit.CanFocus ? CheckState.Checked : CheckState.UnChecked : CheckState.UnChecked
        };

        _canFocusCheckBox.CheckedStateChanged += (s, args) =>
                                                 {
                                                     if (ViewToEdit is { })
                                                     {
                                                         ViewToEdit.CanFocus = _canFocusCheckBox.CheckedState == CheckState.Checked;
                                                     }
                                                 };
        base.Add (_canFocusCheckBox);

        _enabledCheckBox = new ()
        {
            Title = "Enabled",
            X = Pos.Right (_canFocusCheckBox) + 1,
            Y = Pos.Top (_canFocusCheckBox),
            CheckedState = ViewToEdit is { } ? ViewToEdit.Enabled ? CheckState.Checked : CheckState.UnChecked : CheckState.UnChecked
        };

        _enabledCheckBox.CheckedStateChanged += (s, args) =>
                                                {
                                                    if (ViewToEdit is { })
                                                    {
                                                        ViewToEdit.Enabled = _enabledCheckBox.CheckedState == CheckState.Checked;
                                                    }
                                                };
        base.Add (_enabledCheckBox);

        Label label = new () { X = Pos.Right (_enabledCheckBox) + 1, Y = Pos.Top (_enabledCheckBox), Text = "Orientation:" };

        _orientation = new ()
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            RadioLabels = new [] { "Horizontal", "Vertical" },
            Orientation = Orientation.Horizontal
        };

        _orientation.SelectedItemChanged += (s, selected) =>
                                            {
                                                if (ViewToEdit is IOrientation orientatedView)
                                                {
                                                    orientatedView.Orientation = (Orientation)_orientation.SelectedItem!;
                                                }
                                            };
        Add (label, _orientation);

        label = new () { X = 0, Y = Pos.Bottom (_orientation), Text = "Text:" };

        _text = new ()
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = Dim.Fill (),
            Height = Dim.Auto (minimumContentDim: 2),
            Text = "This is demo text"
        };

        _text.ContentsChanged += (s, e) =>
                                 {
                                     if (ViewToEdit is { })
                                     {
                                         ViewToEdit.Text = _text.Text;
                                     }
                                 };

        Add (label, _text);

        base.EndInit ();
    }

    public string DemoText
    {
        get
        {
            if (_text is null)
            {
                return string.Empty;
            }

            return _text!.Text;
        }
        set => _text!.Text = value;
    }

    protected override void OnViewToEditChanged ()
    {
        Enabled = ViewToEdit is not Adornment;

        if (ViewToEdit is { } and not Adornment)
        {
            _canFocusCheckBox!.CheckedState = ViewToEdit.CanFocus ? CheckState.Checked : CheckState.UnChecked;
            _enabledCheckBox!.CheckedState = ViewToEdit.Enabled ? CheckState.Checked : CheckState.UnChecked;

            if (ViewToEdit is IOrientation orientatedView)
            {
                _orientation!.SelectedItem = (int)orientatedView.Orientation;
                _orientation.Enabled = true;
            }
            else
            {
                _orientation!.Enabled = false;
            }
        }
    }
}
