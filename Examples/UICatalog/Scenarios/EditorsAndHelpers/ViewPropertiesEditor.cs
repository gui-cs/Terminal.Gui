#nullable enable

namespace UICatalog.Scenarios;

public class ViewPropertiesEditor : EditorBase
{
    private CheckBox? _canFocusCheckBox;
    private CheckBox? _enabledCheckBox;
    private OptionSelector<Orientation>? _orientationOptionSelector;
    private TextView? _text;

    /// <inheritdoc/>
    public override void EndInit ()
    {
        _canFocusCheckBox = new ()
        {
            Title = "CanFocus",
            X = 0,
            Y = 0,
            CheckedState = ViewToEdit is not null ? ViewToEdit.CanFocus ? CheckState.Checked : CheckState.UnChecked : CheckState.UnChecked
        };

        _canFocusCheckBox.CheckedStateChanged += (_, _) =>
                                                 {
                                                     if (ViewToEdit is not null)
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
            CheckedState = ViewToEdit is not null ? ViewToEdit.Enabled ? CheckState.Checked : CheckState.UnChecked : CheckState.UnChecked
        };

        _enabledCheckBox.CheckedStateChanged += (_, _) =>
                                                {
                                                    if (ViewToEdit is not null)
                                                    {
                                                        ViewToEdit.Enabled = _enabledCheckBox.CheckedState == CheckState.Checked;
                                                    }
                                                };
        base.Add (_enabledCheckBox);

        Label label = new () { X = Pos.Right (_enabledCheckBox) + 1, Y = Pos.Top (_enabledCheckBox), Text = "Orientation:" };

        _orientationOptionSelector = new ()
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Orientation = Orientation.Horizontal
        };

        _orientationOptionSelector.ValueChanged += (_, _) =>
                                                   {
                                                       if (ViewToEdit is IOrientation orientatedView)
                                                       {
                                                           orientatedView.Orientation = _orientationOptionSelector.Value!.Value;
                                                       }
                                                   };
        Add (label, _orientationOptionSelector);

        label = new () { X = 0, Y = Pos.Bottom (_orientationOptionSelector), Text = "Text:" };

        _text = new ()
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = Dim.Fill (),
            Height = Dim.Auto (minimumContentDim: 2),
            Text = "This is demo text"
        };

        _text.ContentsChanged += (_, _) =>
                                 {
                                     if (ViewToEdit is not null)
                                     {
                                         ViewToEdit.Text = _text.Text;
                                     }
                                 };

        Add (label, _text);

        base.EndInit ();
    }

    public string DemoText
    {
        get => _text is null ? string.Empty : _text!.Text;
        set => _text!.Text = value;
    }

    protected override void OnViewToEditChanged ()
    {
        Enabled = ViewToEdit is not Adornment;

        if (ViewToEdit is not null and not Adornment)
        {
            _canFocusCheckBox!.CheckedState = ViewToEdit.CanFocus ? CheckState.Checked : CheckState.UnChecked;
            _enabledCheckBox!.CheckedState = ViewToEdit.Enabled ? CheckState.Checked : CheckState.UnChecked;

            if (ViewToEdit is IOrientation orientatedView)
            {
                _orientationOptionSelector!.Value = orientatedView.Orientation;
                _orientationOptionSelector.Enabled = true;
            }
            else
            {
                _orientationOptionSelector!.Enabled = false;
            }
        }
    }
}
