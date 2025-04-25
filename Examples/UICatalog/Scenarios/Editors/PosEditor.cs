#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

/// <summary>
///     Provides an editor UI for the Margin, Border, and Padding of a View.
/// </summary>
public class PosEditor : EditorBase
{
    public PosEditor ()
    {
        Title = "Pos";

        Initialized += PosEditor_Initialized;
    }

    private int _value;
    private RadioGroup? _posRadioGroup;
    private TextField? _valueEdit;

    protected override void OnUpdateSettings ()
    {
        Enabled = ViewToEdit is not Adornment;

        if (ViewToEdit is null)
        {
            return;
        }

        Pos? pos;

        if (Dimension == Dimension.Width)
        {
            pos = ViewToEdit.X;
        }
        else
        {
            pos = ViewToEdit.Y;
        }

        try
        {
            _posRadioGroup!.SelectedItem = _posNames.IndexOf (_posNames.First (s => pos.ToString ().Contains (s)));
        }
        catch (InvalidOperationException e)
        {
            // This is a hack to work around the fact that the Pos enum doesn't have an "Align" value yet
            Debug.WriteLine ($"{e}");
        }

        _valueEdit!.Enabled = false;

        switch (pos)
        {
            case PosPercent percent:
                _valueEdit.Enabled = true;
                _value = percent.Percent;
                _valueEdit!.Text = _value.ToString ();

                break;
            case PosAbsolute absolute:
                _valueEdit.Enabled = true;
                _value = absolute.Position;
                _valueEdit!.Text = _value.ToString ();

                break;
            case PosFunc func:
                _valueEdit.Enabled = true;
                _value = func.Fn ();
                _valueEdit!.Text = _value.ToString ();

                break;
            default:
                _valueEdit!.Text = pos.ToString ();

                break;
        }
    }

    public Dimension Dimension { get; set; }

    private void PosEditor_Initialized (object? sender, EventArgs e)
    {
        var label = new Label
        {
            X = 0, Y = 0,
            Text = $"{Title}:"
        };
        Add (label);
        _posRadioGroup = new () { X = 0, Y = Pos.Bottom (label), RadioLabels = _radioItems };
        _posRadioGroup.SelectedItemChanged += OnRadioGroupOnSelectedItemChanged;

        _valueEdit = new ()
        {
            X = Pos.Right (label) + 1,
            Y = 0,
            Width = Dim.Func (() => _radioItems.Max (i => i.GetColumns ()) - label.Frame.Width + 1),
            Text = $"{_value}"
        };

        _valueEdit.Accepting += (s, args) =>
                                {
                                    try
                                    {
                                        _value = int.Parse (_valueEdit.Text);
                                        PosChanged ();
                                    }
                                    catch
                                    {
                                        // ignored
                                    }

                                    args.Cancel = true;
                                };
        Add (_valueEdit);

        Add (_posRadioGroup);
    }

    private void OnRadioGroupOnSelectedItemChanged (object? s, SelectedItemChangedArgs selected) { PosChanged (); }

    // These need to have same order 
    private readonly List<string> _posNames = ["Absolute", "Align", "AnchorEnd", "Center", "Func", "Percent"];
    private readonly string [] _radioItems = ["Absolute(n)", "Align", "AnchorEnd", "Center", "Func(()=>n)", "Percent(n)"];

    private void PosChanged ()
    {
        if (ViewToEdit == null || UpdatingSettings)
        {
            return;
        }

        try
        {
            Pos? pos = _posRadioGroup!.SelectedItem switch
                       {
                           0 => Pos.Absolute (_value),
                           1 => Pos.Align (Alignment.Start),
                           2 => new PosAnchorEnd (),
                           3 => Pos.Center (),
                           4 => Pos.Func (() => _value),
                           5 => Pos.Percent (_value),
                           _ => Dimension == Dimension.Width ? ViewToEdit.X : ViewToEdit.Y
                       };

            if (Dimension == Dimension.Width)
            {
                ViewToEdit.X = pos;
            }
            else
            {
                ViewToEdit.Y = pos;
            }

            SetNeedsLayout ();
        }
        catch (Exception e)
        {
            MessageBox.ErrorQuery ("Exception", e.Message, "Ok");
        }
    }
}
