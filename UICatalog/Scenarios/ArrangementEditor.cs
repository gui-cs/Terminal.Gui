#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

/// <summary>
///     Provides an editor UI for the Margin, Border, and Padding of a View.
/// </summary>
public sealed class ArrangementEditor : View
{
    public ArrangementEditor ()
    {
        Title = "ArrangementEditor";

        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);

        CanFocus = true;

        TabStop = TabBehavior.TabGroup;

        Initialized += ArrangementEditor_Initialized;

        _arrangementSlider.Options = new List<SliderOption<ViewArrangement>> ();

        _arrangementSlider.Options.Add (new SliderOption<ViewArrangement>
        {
            Legend = ViewArrangement.Movable.ToString (),
            Data = ViewArrangement.Movable
        });

        _arrangementSlider.Options.Add (new SliderOption<ViewArrangement>
        {
            Legend = ViewArrangement.LeftResizable.ToString (),
            Data = ViewArrangement.LeftResizable
        });

        _arrangementSlider.Options.Add (new SliderOption<ViewArrangement>
        {
            Legend = ViewArrangement.RightResizable.ToString (),
            Data = ViewArrangement.RightResizable
        });

        _arrangementSlider.Options.Add (new SliderOption<ViewArrangement>
        {
            Legend = ViewArrangement.TopResizable.ToString (),
            Data = ViewArrangement.TopResizable
        });

        _arrangementSlider.Options.Add (new SliderOption<ViewArrangement>
        {
            Legend = ViewArrangement.BottomResizable.ToString (),
            Data = ViewArrangement.BottomResizable
        });

        _arrangementSlider.Options.Add (new SliderOption<ViewArrangement>
        {
            Legend = ViewArrangement.Overlapped.ToString (),
            Data = ViewArrangement.Overlapped
        });

        Add (_arrangementSlider);
    }

    private View? _viewToEdit;

    private Label? _lblView; // Text describing the view being edited

    private Slider<ViewArrangement> _arrangementSlider = new Slider<ViewArrangement> ()
    {
        Orientation = Orientation.Vertical,
        UseMinimumSize = true,
        Type = SliderType.Multiple,
        AllowEmpty = true,
        BorderStyle = LineStyle.Dotted,
        Title = "_Arrangement",
    };

    /// <summary>
    ///     Gets or sets whether the ArrangementEditor should automatically select the View to edit
    ///     based on the values of <see cref="AutoSelectSuperView"/>.
    /// </summary>
    public bool AutoSelectViewToEdit { get; set; }

    /// <summary>
    ///     Gets or sets the View that will scope the behavior of <see cref="AutoSelectViewToEdit"/>.
    /// </summary>
    public View? AutoSelectSuperView { get; set; }

    public View? ViewToEdit
    {
        get => _viewToEdit;
        set
        {
            if (_viewToEdit == value)
            {
                return;
            }

            _arrangementSlider.OptionsChanged -= ArrangementSliderOnOptionsChanged;

            _viewToEdit = value;
            // Set the appropriate options in the slider based on _viewToEdit.Arrangement
            if (_viewToEdit is { })
            {
                _arrangementSlider.Options.ForEach (option =>
                                                    {
                                                        _arrangementSlider.ChangeOption (_arrangementSlider.Options.IndexOf (option), (_viewToEdit.Arrangement & option.Data) == option.Data);
                                                    });
            }

            _arrangementSlider.OptionsChanged += ArrangementSliderOnOptionsChanged;

            if (_lblView is { })
            {
                _lblView.Text = $"{_viewToEdit?.GetType ().Name}: {_viewToEdit?.Id}" ?? string.Empty;
            }
        }
    }


    private void NavigationOnFocusedChanged (object? sender, EventArgs e)
    {
        if (AutoSelectSuperView is null)
        {
            return;
        }

        View? view = Application.Navigation!.GetFocused ();

        if (ApplicationNavigation.IsInHierarchy (this, view))
        {
            return;
        }

        if (!ApplicationNavigation.IsInHierarchy (AutoSelectSuperView, view))
        {
            return;
        }

        if (view is { } and not Adornment)
        {
            ViewToEdit = view;
        }
    }

    private void ApplicationOnMouseEvent (object? sender, MouseEventArgs e)
    {
        if (e.Flags != MouseFlags.Button1Clicked || !AutoSelectViewToEdit)
        {
            return;
        }

        if ((AutoSelectSuperView is { } && !AutoSelectSuperView.FrameToScreen ().Contains (e.Position))
            || FrameToScreen ().Contains (e.Position))
        {
            return;
        }

        View? view = e.View;

        if (view is Adornment adornment)
        {
            view = adornment.Parent;
        }

        if (view is { } and not Adornment)
        {
            ViewToEdit = view;
        }
    }

    private void ArrangementEditor_Initialized (object? sender, EventArgs e)
    {
        BorderStyle = LineStyle.Dotted;

        var expandButton = new ExpanderButton
        {
            Orientation = Orientation.Horizontal
        };
        Border.Add (expandButton);

        _lblView = new ()
        {
            X = 0,
            Y = 0,
            Height = 2
        };
        _lblView.TextFormatter.WordWrap = true;
        _lblView.TextFormatter.MultiLine = true;
        _lblView.HotKeySpecifier = (Rune)'\uffff';
        _lblView.Width = Dim.Width (_arrangementSlider);
        Add (_lblView);

        _arrangementSlider.Y = Pos.Bottom (_lblView);

        _arrangementSlider.OptionsChanged += ArrangementSliderOnOptionsChanged;

        Application.MouseEvent += ApplicationOnMouseEvent;
        Application.Navigation!.FocusedChanged += NavigationOnFocusedChanged;
    }

    private void ArrangementSliderOnOptionsChanged (object? sender, SliderEventArgs<ViewArrangement> e)
    {
        if (_viewToEdit is { })
        {
            // Set the arrangement based on the selected options
            ViewArrangement arrangement = ViewArrangement.Fixed;
            foreach (var option in e.Options)
            {
                arrangement |= option.Value.Data;
            }

            _viewToEdit.Arrangement = arrangement;

            if (_viewToEdit.Arrangement.HasFlag (ViewArrangement.Overlapped))
            {
                _viewToEdit.ShadowStyle = ShadowStyle.Transparent;
                _viewToEdit.ColorScheme = Colors.ColorSchemes ["Toplevel"];
            }
            else
            {
                _viewToEdit.ShadowStyle = ShadowStyle.None;
                _viewToEdit.ColorScheme = _viewToEdit!.SuperView!.ColorScheme;
            }

            if (_viewToEdit.Arrangement.HasFlag (ViewArrangement.Movable))
            {
                _viewToEdit.BorderStyle = LineStyle.Double;
            }
            else
            {
                _viewToEdit.BorderStyle = LineStyle.Single;
            }
        }
    }
}
