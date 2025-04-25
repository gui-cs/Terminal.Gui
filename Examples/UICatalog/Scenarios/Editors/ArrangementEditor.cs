#nullable enable
using System;
using System.Collections.Generic;
using Terminal.Gui;

namespace UICatalog.Scenarios;

/// <summary>
///     Provides an editor UI for the Margin, Border, and Padding of a View.
/// </summary>
public sealed class ArrangementEditor : EditorBase
{
    public ArrangementEditor ()
    {
        Title = "ArrangementEditor";
        TabStop = TabBehavior.TabGroup;

        Initialized += ArrangementEditor_Initialized;

        _arrangementSlider.Options =
        [
            new SliderOption<ViewArrangement>
            {
                Legend = ViewArrangement.Movable.ToString (),
                Data = ViewArrangement.Movable
            },

            new SliderOption<ViewArrangement>
            {
                Legend = ViewArrangement.LeftResizable.ToString (),
                Data = ViewArrangement.LeftResizable
            },

            new SliderOption<ViewArrangement>
            {
                Legend = ViewArrangement.RightResizable.ToString (),
                Data = ViewArrangement.RightResizable
            },

            new SliderOption<ViewArrangement>
            {
                Legend = ViewArrangement.TopResizable.ToString (),
                Data = ViewArrangement.TopResizable
            },

            new SliderOption<ViewArrangement>
            {
                Legend = ViewArrangement.BottomResizable.ToString (),
                Data = ViewArrangement.BottomResizable
            },

            new SliderOption<ViewArrangement>
            {
                Legend = ViewArrangement.Overlapped.ToString (),
                Data = ViewArrangement.Overlapped
            }
        ];

        Add (_arrangementSlider);
    }

    private readonly Slider<ViewArrangement> _arrangementSlider = new()
    {
        Orientation = Orientation.Vertical,
        UseMinimumSize = true,
        Type = SliderType.Multiple,
        AllowEmpty = true,
    };

    protected override void OnViewToEditChanged ()
    {
        _arrangementSlider.Enabled = ViewToEdit is not Adornment;

        _arrangementSlider.OptionsChanged -= ArrangementSliderOnOptionsChanged;

        // Set the appropriate options in the slider based on _viewToEdit.Arrangement
        if (ViewToEdit is { })
        {
            _arrangementSlider.Options.ForEach (
                                                option =>
                                                {
                                                    _arrangementSlider.ChangeOption (
                                                                                     _arrangementSlider.Options.IndexOf (option),
                                                                                     (ViewToEdit.Arrangement & option.Data) == option.Data);
                                                });
        }

        _arrangementSlider.OptionsChanged += ArrangementSliderOnOptionsChanged;
    }

    private void ArrangementEditor_Initialized (object? sender, EventArgs e) { _arrangementSlider.OptionsChanged += ArrangementSliderOnOptionsChanged; }

    private void ArrangementSliderOnOptionsChanged (object? sender, SliderEventArgs<ViewArrangement> e)
    {
        if (ViewToEdit is { })
        {
            // Set the arrangement based on the selected options
            var arrangement = ViewArrangement.Fixed;

            foreach (KeyValuePair<int, SliderOption<ViewArrangement>> option in e.Options)
            {
                arrangement |= option.Value.Data;
            }

            ViewToEdit.Arrangement = arrangement;

            if (ViewToEdit.Arrangement.HasFlag (ViewArrangement.Overlapped))
            {
                ViewToEdit.ShadowStyle = ShadowStyle.Transparent;
                ViewToEdit.ColorScheme = Colors.ColorSchemes ["Toplevel"];
            }
            else
            {
                ViewToEdit.ShadowStyle = ShadowStyle.None;
                ViewToEdit.ColorScheme = ViewToEdit!.SuperView!.ColorScheme;
            }

            if (ViewToEdit.Arrangement.HasFlag (ViewArrangement.Movable))
            {
                ViewToEdit.BorderStyle = LineStyle.Double;
            }
            else
            {
                ViewToEdit.BorderStyle = LineStyle.Single;
            }
        }
    }
}
