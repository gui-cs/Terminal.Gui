#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace UICatalog.Scenarios;

/// <summary>
///     Provides an editor UI for the Margin, Border, and Padding of a View.
/// </summary>
public class DimEditor : EditorBase
{
    public DimEditor ()
    {
        Title = "Dim";
        Initialized += DimEditor_Initialized;
    }

    private int _value;
    private RadioGroup? _dimRadioGroup;
    private TextField? _valueEdit;

    /// <inheritdoc />
    protected override void OnViewToEditChanged ()
    {
        if (ViewToEdit is { })
        {
            ViewToEdit.SubViewsLaidOut += (_, _) => { OnUpdateLayoutSettings (); };
        }
    }

    protected override void OnUpdateLayoutSettings ()
    {
        Enabled = ViewToEdit is not Adornment;

        if (ViewToEdit is null)
        {
            return;
        }

        Dim? dim;
        dim = Dimension == Dimension.Width ? ViewToEdit.Width : ViewToEdit.Height;

        try
        {
            _dimRadioGroup!.SelectedItem = _dimNames.IndexOf (_dimNames.First (s => dim!.ToString ().StartsWith (s)));
        }
        catch (InvalidOperationException e)
        {
            // This is a hack to work around the fact that the Pos enum doesn't have an "Align" value yet
            Debug.WriteLine ($"{e}");
        }

        _valueEdit!.Enabled = false;
        switch (dim)
        {
            case DimAbsolute absolute:
                _valueEdit.Enabled = true;
                _value = absolute.Size;
                _valueEdit!.Text = _value.ToString ();
                break;
            case DimFill fill:
                var margin = fill.Margin as DimAbsolute;
                _valueEdit.Enabled = margin is { };
                _value = margin?.Size ?? 0;
                _valueEdit!.Text = _value.ToString ();
                break;
            case DimFunc func:
                _valueEdit.Enabled = true;
                _value = func.Fn (null);
                _valueEdit!.Text = _value.ToString ();
                break;
            case DimPercent percent:
                _valueEdit.Enabled = true;
                _value = percent.Percentage;
                _valueEdit!.Text = _value.ToString ();
                break;
            default:
                _valueEdit!.Text = dim!.ToString ();
                break;
        }
    }

    public Dimension Dimension { get; set; }

    private void DimEditor_Initialized (object? sender, EventArgs e)
    {
        var label = new Label
        {
            X = 0, Y = 0,
            Text = $"{Title}:"
        };
        Add (label);
        _dimRadioGroup = new () { X = 0, Y = Pos.Bottom (label), RadioLabels = _radioItems };
        _dimRadioGroup.SelectedItemChanged += OnRadioGroupOnSelectedItemChanged;
        _valueEdit = new ()
        {
            X = Pos.Right (label) + 1,
            Y = 0,
            Width = Dim.Func (_ => _radioItems.Max (i => i.GetColumns ()) - label.Frame.Width + 1),
            Text = $"{_value}"
        };

        _valueEdit.Accepting += (s, args) =>
        {
            try
            {
                _value = int.Parse (_valueEdit.Text);
                DimChanged ();
            }
            catch
            {
                // ignored
            }
            args.Handled = true;
        };
        Add (_valueEdit);

        Add (_dimRadioGroup);

    }

    private void OnRadioGroupOnSelectedItemChanged (object? s, SelectedItemChangedArgs selected) { DimChanged (); }

    // These need to have same order 
    private readonly List<string> _dimNames = ["Absolute", "Auto", "Fill", "Func", "Percent",];
    private readonly string [] _radioItems = ["Absolute(n)", "Auto", "Fill(n)", "Func(()=>n)", "Percent(n)",];

    private void DimChanged ()
    {
        if (ViewToEdit == null || UpdatingLayoutSettings)
        {
            return;
        }

        try
        {
            Dim? dim = _dimRadioGroup!.SelectedItem switch
            {
                0 => Dim.Absolute (_value),
                1 => Dim.Auto (),
                2 => Dim.Fill (_value),
                3 => Dim.Func (_ => _value),
                4 => Dim.Percent (_value),
                _ => Dimension == Dimension.Width ? ViewToEdit.Width : ViewToEdit.Height
            };

            if (Dimension == Dimension.Width)
            {
                ViewToEdit.Width = dim;
            }
            else
            {
                ViewToEdit.Height = dim;
            }
        }
        catch (Exception e)
        {
            MessageBox.ErrorQuery ("Exception", e.Message, "Ok");
        }
    }
}
