#region

using System;
using Terminal.Gui;

#endregion

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Color Picker", "Color Picker.")]
[ScenarioCategory ("Colors")]
[ScenarioCategory ("Controls")]
public class ColorPickers : Scenario {
    /// <summary>
    /// Background color Label.
    /// </summary>
    Label _backgroundColorLabel;

    /// <summary>
    /// Demo label.
    /// </summary>
    View _demoView;

    /// <summary>
    /// Foreground color label.
    /// </summary>
    Label _foregroundColorLabel;

    /// <summary>
    /// Background ColorPicker.
    /// </summary>
    ColorPicker backgroundColorPicker;

    /// <summary>
    /// Foreground ColorPicker.
    /// </summary>
    ColorPicker foregroundColorPicker;

    /// <summary>
    /// Setup the scenario.
    /// </summary>
    public override void Setup () {
        // Scenario Window's.
        Win.Title = GetName ();

        // Foreground ColorPicker.
        foregroundColorPicker = new ColorPicker {
                                                    Title = "Foreground Color",
                                                    BorderStyle = LineStyle.Single
                                                };
        foregroundColorPicker.ColorChanged += ForegroundColor_ColorChanged;
        Win.Add (foregroundColorPicker);

        _foregroundColorLabel = new Label {
                                              X = Pos.Left (foregroundColorPicker),
                                              Y = Pos.Bottom (foregroundColorPicker) + 1
                                          };
        Win.Add (_foregroundColorLabel);

        // Background ColorPicker.
        backgroundColorPicker = new ColorPicker {
                                                    Title = "Background Color",
                                                    Y = Pos.Center (),
                                                    X = Pos.Center (),
                                                    BoxHeight = 1,
                                                    BoxWidth = 4,
                                                    BorderStyle = LineStyle.Single
                                                };

        //backgroundColorPicker.X = Pos.AnchorEnd () - (Pos.Right (backgroundColorPicker) - Pos.Left (backgroundColorPicker));
        backgroundColorPicker.ColorChanged += BackgroundColor_ColorChanged;
        Win.Add (backgroundColorPicker);
        _backgroundColorLabel = new Label ();
        _backgroundColorLabel.X =
            Pos.AnchorEnd () - (Pos.Right (_backgroundColorLabel) - Pos.Left (_backgroundColorLabel));
        _backgroundColorLabel.Y = Pos.Bottom (backgroundColorPicker) + 1;
        Win.Add (_backgroundColorLabel);

        // Demo Label.
        _demoView = new View {
                                 Title = "Color Sample",
                                 Text = "Lorem Ipsum",
                                 TextAlignment = TextAlignment.Centered,
                                 VerticalTextAlignment = VerticalTextAlignment.Middle,
                                 BorderStyle = LineStyle.Heavy,
                                 X = Pos.Center (),
                                 Y = Pos.Center (),
                                 Height = 5,
                                 Width = 20
                             };
        Win.Add (_demoView);

        // Set default colors.
        foregroundColorPicker.SelectedColor = _demoView.SuperView.ColorScheme.Normal.Foreground.GetClosestNamedColor ();
        backgroundColorPicker.SelectedColor = _demoView.SuperView.ColorScheme.Normal.Background.GetClosestNamedColor ();
        Win.Initialized += (s, e) => Win.LayoutSubviews ();
    }

    /// <summary>
    /// Fired when foreground color is changed.
    /// </summary>
    void ForegroundColor_ColorChanged (object sender, EventArgs e) {
        UpdateColorLabel (_foregroundColorLabel, foregroundColorPicker);
        UpdateDemoLabel ();
    }

    /// <summary>
    /// Fired when background color is changed.
    /// </summary>
    void BackgroundColor_ColorChanged (object sender, EventArgs e) {
        UpdateColorLabel (_backgroundColorLabel, backgroundColorPicker);
        UpdateDemoLabel ();
    }

    /// <summary>
    /// Update a color label from his ColorPicker.
    /// </summary>
    void UpdateColorLabel (Label label, ColorPicker colorPicker) {
        label.Clear ();
        var color = new Color (colorPicker.SelectedColor);
        label.Text =
            $"{colorPicker.SelectedColor} ({(int)colorPicker.SelectedColor}) #{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    /// <summary>
    /// Update Demo Label.
    /// </summary>
    void UpdateDemoLabel () => _demoView.ColorScheme = new ColorScheme {
                                                                           Normal = new Attribute (
                                                                            foregroundColorPicker.SelectedColor,
                                                                            backgroundColorPicker.SelectedColor)
                                                                       };
}
