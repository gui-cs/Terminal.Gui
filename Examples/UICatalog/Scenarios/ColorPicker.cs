using System;
using System.Collections.Generic;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ColorPicker", "Color Picker.")]
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
        foregroundColorPicker = new ColorPicker
        {
            Title = "_Foreground Color",
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
            Title = "_Background Color",
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
            Title = "_Foreground Color",
            BorderStyle = LineStyle.Single,
            Width = Dim.Percent (50),
            Visible = false  // We default to HSV so hide old one
        };
        foregroundColorPicker16.ColorChanged += ForegroundColor_ColorChanged;
        app.Add (foregroundColorPicker16);

        // Background ColorPicker 16.
        backgroundColorPicker16 = new ColorPicker16
        {
            Title = "_Background Color",
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
                "_RGB",
                "_HSV",
                "H_SL",
                "_16 Colors"
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
                                                    ForegroundColor_ColorChanged (null, null);
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
            Text = "Show _Text Fields",
            Y = Pos.Bottom (rgColorModel) + 1,
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            CheckedState = foregroundColorPicker.Style.ShowTextFields ? CheckState.Checked : CheckState.UnChecked,
        };

        cbShowTextFields.CheckedStateChanging += (_, e) =>
                                                {
                                                    foregroundColorPicker.Style.ShowTextFields = e.Result == CheckState.Checked;
                                                    foregroundColorPicker.ApplyStyleChanges ();
                                                    backgroundColorPicker.Style.ShowTextFields = e.Result == CheckState.Checked;
                                                    backgroundColorPicker.ApplyStyleChanges ();
                                                };
        app.Add (cbShowTextFields);

        // Checkbox for switching show text fields on and off
        var cbShowName = new CheckBox ()
        {
            Text = "Show Color _Name",
            Y = Pos.Bottom (cbShowTextFields) + 1,
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            CheckedState = foregroundColorPicker.Style.ShowColorName ? CheckState.Checked : CheckState.UnChecked,
        };

        cbShowName.CheckedStateChanging += (_, e) =>
                                           {
                                               foregroundColorPicker.Style.ShowColorName = e.Result == CheckState.Checked;
                                               foregroundColorPicker.ApplyStyleChanges ();
                                               backgroundColorPicker.Style.ShowColorName = e.Result == CheckState.Checked;
                                               backgroundColorPicker.ApplyStyleChanges ();
                                           };
        app.Add (cbShowName);

        // Set default colors.
        foregroundColorPicker.SelectedColor = _demoView.SuperView!.GetAttributeForRole (VisualRole.Normal).Foreground.GetClosestNamedColor16 ();
        backgroundColorPicker.SelectedColor = _demoView.SuperView.GetAttributeForRole (VisualRole.Normal).Background.GetClosestNamedColor16 ();

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }

    /// <summary>Fired when background color is changed.</summary>
    private void BackgroundColor_ColorChanged (object sender, ResultEventArgs<Color> e)
    {
        UpdateColorLabel (_backgroundColorLabel,
                          backgroundColorPicker.Visible ?
                              backgroundColorPicker.SelectedColor :
                              backgroundColorPicker16.SelectedColor
                          );
        UpdateDemoLabel ();
    }

    /// <summary>Fired when foreground color is changed.</summary>
    private void ForegroundColor_ColorChanged (object sender, ResultEventArgs<Color> e)
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
        label.ClearViewport (null);

        label.Text =
            $"{color} ({(int)color}) #{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    /// <summary>Update Demo Label.</summary>
    private void UpdateDemoLabel ()
    {
        _demoView.SetScheme (new Scheme
        {
            Normal = new Attribute (
                                    foregroundColorPicker.Visible ?
                                        foregroundColorPicker.SelectedColor :
                                        foregroundColorPicker16.SelectedColor,
                                    backgroundColorPicker.Visible ?
                                        backgroundColorPicker.SelectedColor :
                                        backgroundColorPicker16.SelectedColor
                                   )
        });
    }

    public override List<Key> GetDemoKeyStrokes ()
    {
        List<Key> keys =
        [
            Key.B.WithAlt
        ];

        for (int i = 0; i < 200; i++)
        {
            keys.Add (Key.CursorRight);
        }

        keys.Add (Key.Tab);
        keys.Add (Key.Tab);

        for (int i = 0; i < 200; i++)
        {
            keys.Add (Key.CursorLeft);
        }

        keys.Add (Key.Tab);
        keys.Add (Key.Tab);

        for (int i = 0; i < 200; i++)
        {
            keys.Add (Key.CursorLeft);
        }

        keys.Add (Key.N.WithAlt);
        keys.Add (Key.R.WithAlt);
        keys.Add (Key.H.WithAlt);
        keys.Add (Key.S.WithAlt);
        keys.Add (Key.D1.WithAlt);

        return keys;
    }
}
