﻿using System;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Color Picker", "Color Picker.")]
[ScenarioCategory ("Colors")]
[ScenarioCategory ("Controls")]
public class ColorPickers : Scenario
{
    /// <summary>Background color Label.</summary>
    private Label _backgroundColorLabel;

    /// <summary>Demo label.</summary>
    private View _demoView;

    /// <summary>Foreground color label.</summary>
    private Label _foregroundColorLabel;

    /// <summary>Background ColorPicker.</summary>
    private ColorPicker backgroundColorPicker;

    /// <summary>Foreground ColorPicker.</summary>
    private ColorPicker foregroundColorPicker;

    /// <summary>Background ColorPicker.</summary>
    private ColorPicker16 backgroundColorPicker16;

    /// <summary>Foreground ColorPicker.</summary>
    private ColorPicker16 foregroundColorPicker16;

    /// <summary>Setup the scenario.</summary>
    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = GetQuitKeyAndName (),
        };

        ///////////////////////////////////////
        // True Color Pickers
        ///////////////////////////////////////

        // Foreground ColorPicker.
        foregroundColorPicker = new ColorPicker {
            Title = "Foreground Color",
            BorderStyle = LineStyle.Single,
            Width = Dim.Percent (50)
        };
        foregroundColorPicker.ColorChanged += ForegroundColor_ColorChanged;
        app.Add (foregroundColorPicker);

        _foregroundColorLabel = new Label
        {
            X = Pos.Left (foregroundColorPicker), Y = Pos.Bottom (foregroundColorPicker) + 1
        };
        app.Add (_foregroundColorLabel);

        // Background ColorPicker.
        backgroundColorPicker = new ColorPicker
        {
            Title = "Background Color",
            X = Pos.AnchorEnd (),
            Width = Dim.Percent (50),
            BorderStyle = LineStyle.Single
        };

        backgroundColorPicker.ColorChanged += BackgroundColor_ColorChanged;
        app.Add (backgroundColorPicker);
        _backgroundColorLabel = new Label ()
        {
            X = Pos.AnchorEnd (),
            Y = Pos.Bottom (backgroundColorPicker) + 1
        };

        app.Add (_backgroundColorLabel);


        ///////////////////////////////////////
        // 16 Color Pickers
        ///////////////////////////////////////


        // Foreground ColorPicker 16.
        foregroundColorPicker16 = new ColorPicker16
        {
            Title = "Foreground Color",
            BorderStyle = LineStyle.Single,
            Width = Dim.Percent (50),
            Visible = false  // We default to HSV so hide old one
        };
        foregroundColorPicker16.ColorChanged += ForegroundColor_ColorChanged;
        app.Add (foregroundColorPicker16);

        // Background ColorPicker 16.
        backgroundColorPicker16 = new ColorPicker16
        {
            Title = "Background Color",
            X = Pos.AnchorEnd (),
            Width = Dim.Percent (50),
            BorderStyle = LineStyle.Single,
            Visible = false  // We default to HSV so hide old one
        };

        backgroundColorPicker16.ColorChanged += BackgroundColor_ColorChanged;
        app.Add (backgroundColorPicker16);


        // Demo Label.
        _demoView = new View
        {
            Title = "Color Sample",
            Text = "Lorem Ipsum",
            TextAlignment = Alignment.Center,
            VerticalTextAlignment = Alignment.Center,
            BorderStyle = LineStyle.Heavy,
            X = Pos.Center (),
            Y = Pos.Center (),
            Height = 5,
            Width = 20
        };
        app.Add (_demoView);


        // Radio for switching color models
        var rgColorModel = new RadioGroup ()
        {
            Y = Pos.Bottom (_demoView),
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            RadioLabels = new []
            {
                "RGB",
                "HSV",
                "HSL",
                "16 Colors"
            },
            SelectedItem = (int)foregroundColorPicker.Style.ColorModel,
        };

        rgColorModel.SelectedItemChanged += (_, e) =>
                                            {
                                                // 16 colors
                                                if (e.SelectedItem == 3)
                                                {

                                                    foregroundColorPicker16.Visible = true;
                                                    foregroundColorPicker.Visible = false;

                                                    backgroundColorPicker16.Visible = true;
                                                    backgroundColorPicker.Visible = false;

                                                    // Switching to 16 colors
                                                    ForegroundColor_ColorChanged (null,null);
                                                    BackgroundColor_ColorChanged (null, null);
                                                }
                                                else
                                                {
                                                    foregroundColorPicker16.Visible = false;
                                                    foregroundColorPicker.Visible = true;
                                                    foregroundColorPicker.Style.ColorModel = (ColorModel)e.SelectedItem;
                                                    foregroundColorPicker.ApplyStyleChanges ();

                                                    backgroundColorPicker16.Visible = false;
                                                    backgroundColorPicker.Visible = true;
                                                    backgroundColorPicker.Style.ColorModel = (ColorModel)e.SelectedItem;
                                                    backgroundColorPicker.ApplyStyleChanges ();


                                                    // Switching to true colors
                                                    foregroundColorPicker.SelectedColor = foregroundColorPicker16.SelectedColor;
                                                    backgroundColorPicker.SelectedColor = backgroundColorPicker16.SelectedColor;
                                                }
                                            };

        app.Add (rgColorModel);

        // Checkbox for switching show text fields on and off
        var cbShowTextFields = new CheckBox ()
        {
            Text = "Show Text Fields",
            Y = Pos.Bottom (rgColorModel)+1,
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            CheckedState = foregroundColorPicker.Style.ShowTextFields ? CheckState.Checked: CheckState.UnChecked,
        };

        cbShowTextFields.CheckedStateChanging += (_, e) =>
                                                {
                                                    foregroundColorPicker.Style.ShowTextFields = e.NewValue == CheckState.Checked;
                                                    foregroundColorPicker.ApplyStyleChanges ();
                                                    backgroundColorPicker.Style.ShowTextFields = e.NewValue == CheckState.Checked;
                                                    backgroundColorPicker.ApplyStyleChanges ();
                                                };
        app.Add (cbShowTextFields);

        // Checkbox for switching show text fields on and off
        var cbShowName = new CheckBox ()
        {
            Text = "Show Color Name",
            Y = Pos.Bottom (cbShowTextFields) + 1,
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            CheckedState = foregroundColorPicker.Style.ShowColorName ? CheckState.Checked : CheckState.UnChecked,
        };

        cbShowName.CheckedStateChanging += (_, e) =>
                                           {
                                               foregroundColorPicker.Style.ShowColorName = e.NewValue == CheckState.Checked;
                                               foregroundColorPicker.ApplyStyleChanges ();
                                               backgroundColorPicker.Style.ShowColorName = e.NewValue == CheckState.Checked;
                                               backgroundColorPicker.ApplyStyleChanges ();
                                           };
        app.Add (cbShowName);

        // Set default colors.
        foregroundColorPicker.SelectedColor = _demoView.SuperView.ColorScheme.Normal.Foreground.GetClosestNamedColor ();
        backgroundColorPicker.SelectedColor = _demoView.SuperView.ColorScheme.Normal.Background.GetClosestNamedColor ();
        app.Initialized += (s, e) => app.LayoutSubviews ();

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }

    /// <summary>Fired when background color is changed.</summary>
    private void BackgroundColor_ColorChanged (object sender, EventArgs e)
    {
        UpdateColorLabel (_backgroundColorLabel,
                          backgroundColorPicker.Visible ?
                              backgroundColorPicker.SelectedColor :
                              backgroundColorPicker16.SelectedColor
                          );
        UpdateDemoLabel ();
    }

    /// <summary>Fired when foreground color is changed.</summary>
    private void ForegroundColor_ColorChanged (object sender, EventArgs e)
    {
        UpdateColorLabel (_foregroundColorLabel,
                          foregroundColorPicker.Visible ?
                                 foregroundColorPicker.SelectedColor :
                                 foregroundColorPicker16.SelectedColor
                          );
        UpdateDemoLabel ();
    }

    /// <summary>Update a color label from his ColorPicker.</summary>
    private void UpdateColorLabel (Label label, Color color)
    {
        label.Clear ();

        label.Text =
            $"{color} ({(int)color}) #{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    /// <summary>Update Demo Label.</summary>
    private void UpdateDemoLabel ()
    {
        _demoView.ColorScheme = new ColorScheme
        {
            Normal = new Attribute (
                                    foregroundColorPicker.Visible ?
                                        foregroundColorPicker.SelectedColor :
                                        foregroundColorPicker16.SelectedColor,
                                    backgroundColorPicker.Visible ?
                                        backgroundColorPicker.SelectedColor :
                                        backgroundColorPicker16.SelectedColor
                                   )
        };
    }
}
