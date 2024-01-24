using System;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Color Picker", "Illustrates ColorPicker View.")]
[ScenarioCategory ("Colors"), ScenarioCategory ("Controls")]
public class ColorPickers : Scenario {
	/// <summary>
	/// Foreground ColorPicker.
	/// </summary>
	ColorPicker _foregroundColorPicker;

	/// <summary>
	/// Background ColorPicker.
	/// </summary>
	ColorPicker _backgroundColorPicker;

	/// <summary>
	/// Setup the scenario.
	/// </summary>
	public override void Setup ()
	{
		// Scenario Window's.
		Win.Title = GetName ();

		// Foreground ColorPicker.
		_foregroundColorPicker = new ColorPicker () {
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
		Win.Add (_foregroundColorPicker);

		var _blueSlider = new ScrollBarView (_foregroundColorPicker, true, false) {
			IsVertical = true,
			Visible = true,
			ShowScrollIndicator = true,
			Width = 1,
			Size = 255
		};
		_blueSlider.ChangedPosition += (s, e) => {
			_foregroundColorPicker.BlueValue = _blueSlider.Position;
		};
		Win.Add (_blueSlider);

		// Background ColorPicker.
		_backgroundColorPicker = new ColorPicker () {
			Text = "Background:",
			Y = 0,
			X = 0,
			BoxHeight = 1,
			BoxWidth = 4,
			BorderStyle = LineStyle.Single
		};
		_backgroundColorPicker.X = Pos.AnchorEnd () - (Pos.Right (_backgroundColorPicker) - Pos.Left (_backgroundColorPicker));
		_backgroundColorPicker.ColorChanged += ColorChanged;
		Win.Add (_backgroundColorPicker);

		// Set default colors.
		//_foregroundColorPicker.SelectedColor = _demoView.SuperView.ColorScheme.Normal.Foreground;
		//_backgroundColorPicker.SelectedColor = _demoView.SuperView.ColorScheme.Normal.Background;
	}

	/// <summary>
	/// Fired when foreground color is changed.
	/// </summary>
	void ColorChanged (object sender, EventArgs e)
	{
		var color = (Color)((ColorPicker)sender).SelectedColor;
		((ColorPicker)sender).Title = $"{color} ({(int)color.GetClosestNamedColor ()}) #{color.R:X2}{color.G:X2}{color.B:X2}";
		((ColorPicker)sender).ColorScheme = new ColorScheme (new Attribute (GetCompliment (color), color)) {
			Normal = new Attribute (GetCompliment (color), color),
			Focus = new Attribute (GetCompliment (color), color)
		};
		//UpdateDemoLabel ();
	}

	public Color GetCompliment (Color original)
	{
		int inverseRed = 255 - original.R;
		int inverseGreen = 255 - original.G;
		int inverseBlue = 255 - original.B;

		return new Color (inverseRed, inverseGreen, inverseBlue);
	}

	public Color GetMonochromatic (Color original)
	{
		float brightnessFactor = 0.5f; // 50% reduction for this example
		int adjustedRed = (int)(original.R * brightnessFactor);
		int adjustedGreen = (int)(original.G * brightnessFactor);
		int adjustedBlue = (int)(original.B * brightnessFactor);

		return new Color (adjustedRed, adjustedGreen, adjustedBlue);
	}


	/// <summary>
	/// Update Demo Label.
	/// </summary>
	//private void UpdateDemoLabel () => _demoView.ColorScheme = new ColorScheme () {
	//	Normal = new Attribute (_foregroundColorPicker.SelectedColor, _backgroundColorPicker.SelectedColor)
	//};
}