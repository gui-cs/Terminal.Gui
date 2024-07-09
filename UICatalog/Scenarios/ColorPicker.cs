using System;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Color Picker", "Illustrates ColorPicker View.")]
[ScenarioCategory ("Colors")]
[ScenarioCategory ("Controls")]
public class ColorPickers : Scenario
{
    /// <summary>
    ///     Foreground ColorPicker.
    /// </summary>
    private ColorPicker _foregroundColorPicker;

    /// <summary>
    ///     Background ColorPicker.
    /// </summary>
    private ColorPicker _backgroundColorPicker;

    /// <summary>Setup the scenario.</summary>
    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}"
        };

        // Foreground ColorPicker.
        _foregroundColorPicker = new()
        {
            Text = "Foreground:",
            X = 0,
            Y = 0,
            Height = Dim.Fill (),
            Width = Dim.Percent (60),
            BoxHeight = 1,
            BoxWidth = 1,
            BorderStyle = LineStyle.Single,
            Style = ColorPickerStyle.Rgb
        };
        _foregroundColorPicker.ColorChanged += ColorChanged;
        app.Add (_foregroundColorPicker);

        var _blueSlider = new ScrollBarView (_foregroundColorPicker, true, false)
        {
            IsVertical = true,
            Visible = true,
            ShowScrollIndicator = true,
            Width = 1,
            Size = 255
        };
        _blueSlider.ChangedPosition += (s, e) => { _foregroundColorPicker.BlueValue = _blueSlider.Position; };
        app.Add (_blueSlider);

        // Background ColorPicker.
        _backgroundColorPicker = new()
        {
            Text = "Background:",
            Y = 0,
            X = 0,
            BoxHeight = 1,
            BoxWidth = 4,
            BorderStyle = LineStyle.Single
        };
        _backgroundColorPicker.X = Pos.AnchorEnd () - (Pos.Right (_backgroundColorPicker) - Pos.Left (_backgroundColorPicker));
        _backgroundColorPicker.ColorChanged += ColorChanged;
        app.Add (_backgroundColorPicker);

        app.Initialized += (s, e) => app.LayoutSubviews ();

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }

    /// <summary>
    ///     Fired when foreground color is changed.
    /// </summary>
    private void ColorChanged (object sender, EventArgs e)
    {
        Color color = ((ColorPicker)sender).SelectedColor;
        ((ColorPicker)sender).Title = $"{color} ({(int)color.GetClosestNamedColor ()}) #{color.R:X2}{color.G:X2}{color.B:X2}";

        ((ColorPicker)sender).ColorScheme = new (new Attribute (GetCompliment (color), color))
        {
            Normal = new (GetCompliment (color), color),
            Focus = new (GetCompliment (color), color)
        };

        //UpdateDemoLabel ();
    }

    public Color GetCompliment (Color original)
    {
        int inverseRed = 255 - original.R;
        int inverseGreen = 255 - original.G;
        int inverseBlue = 255 - original.B;

        return new (inverseRed, inverseGreen, inverseBlue);
    }

    public Color GetMonochromatic (Color original)
    {
        var brightnessFactor = 0.5f; // 50% reduction for this example
        var adjustedRed = (int)(original.R * brightnessFactor);
        var adjustedGreen = (int)(original.G * brightnessFactor);
        var adjustedBlue = (int)(original.B * brightnessFactor);

        return new (adjustedRed, adjustedGreen, adjustedBlue);
    }

    /// <summary>
    /// Update Demo Label.
    /// </summary>
    //private void UpdateDemoLabel () => _demoView.ColorScheme = new ColorScheme () {
    //	Normal = new Attribute (_foregroundColorPicker.SelectedColor, _backgroundColorPicker.SelectedColor)
    //};
}
