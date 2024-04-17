using System;
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

    /// <summary>Setup the scenario.</summary>
    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}",
        };

        // Foreground ColorPicker.
        foregroundColorPicker = new ColorPicker { Title = "Foreground Color", BorderStyle = LineStyle.Single };
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
            BoxHeight = 1,
            BoxWidth = 4,
            BorderStyle = LineStyle.Single,
        };

        backgroundColorPicker.ColorChanged += BackgroundColor_ColorChanged;
        app.Add (backgroundColorPicker);
        _backgroundColorLabel = new Label ()
        {
            X = Pos.AnchorEnd (),
            Y = Pos.Bottom (backgroundColorPicker) + 1
        };

        app.Add (_backgroundColorLabel);

        // Demo Label.
        _demoView = new View
        {
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
        app.Add (_demoView);

        // Set default colors.
        foregroundColorPicker.SelectedColor = _demoView.SuperView.ColorScheme.Normal.Foreground.GetClosestNamedColor ();
        backgroundColorPicker.SelectedColor = _demoView.SuperView.ColorScheme.Normal.Background.GetClosestNamedColor ();
        app.Initialized += (s, e) => app.LayoutSubviews ();

        Application.Run (app);
        app.Dispose ();
    }

    /// <summary>Fired when background color is changed.</summary>
    private void BackgroundColor_ColorChanged (object sender, EventArgs e)
    {
        UpdateColorLabel (_backgroundColorLabel, backgroundColorPicker);
        UpdateDemoLabel ();
    }

    /// <summary>Fired when foreground color is changed.</summary>
    private void ForegroundColor_ColorChanged (object sender, EventArgs e)
    {
        UpdateColorLabel (_foregroundColorLabel, foregroundColorPicker);
        UpdateDemoLabel ();
    }

    /// <summary>Update a color label from his ColorPicker.</summary>
    private void UpdateColorLabel (Label label, ColorPicker colorPicker)
    {
        label.Clear ();
        var color = new Color (colorPicker.SelectedColor);

        label.Text =
            $"{colorPicker.SelectedColor} ({(int)colorPicker.SelectedColor}) #{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    /// <summary>Update Demo Label.</summary>
    private void UpdateDemoLabel ()
    {
        _demoView.ColorScheme = new ColorScheme
        {
            Normal = new Attribute (
                                    foregroundColorPicker.SelectedColor,
                                    backgroundColorPicker.SelectedColor
                                   )
        };
    }
}
