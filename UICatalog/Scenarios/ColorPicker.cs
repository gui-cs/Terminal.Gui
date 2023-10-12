using Terminal.Gui;
using System;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Color Picker", Description: "Illustrates ColorPicker View.")]
	[ScenarioCategory ("Colors"), ScenarioCategory ("Controls")]
	public class ColorPickers : Scenario {
		/// <summary>
		/// Foreground ColorPicker.
		/// </summary>
		private ColorPicker _foregroundColorPicker;

		/// <summary>
		/// Background ColorPicker.
		/// </summary>
		private ColorPicker _backgroundColorPicker;

		/// <summary>
		/// Foreground color label.
		/// </summary>
		private Label _foregroundColorLabel;

		/// <summary>
		/// Background color Label.
		/// </summary>
		private Label _backgroundColorLabel;

		/// <summary>
		/// Demo label.
		/// </summary>
		private View _demoView;

		/// <summary>
		/// Setup the scenario.
		/// </summary>
		public override void Setup ()
		{
			// Scenario Window's.
			Win.Title = this.GetName ();

			// Foreground ColorPicker.
			_foregroundColorPicker = new ColorPicker () {
				Text = "Foreground:",
				X = 0,
				Y = 0,
				BoxHeight = 3,
				BoxWidth = 6,
				BorderStyle = LineStyle.Single,
				Style = ColorPickerStyle.Rgb
			};
			_foregroundColorPicker.ColorChanged += ColorChanged;
			Win.Add (_foregroundColorPicker);

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

			// Demo Label.
			_demoView = new View () {
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
			_foregroundColorPicker.SelectedColor = _demoView.SuperView.ColorScheme.Normal.Foreground;
			_backgroundColorPicker.SelectedColor = _demoView.SuperView.ColorScheme.Normal.Background;
		}

		/// <summary>
		/// Fired when foreground color is changed.
		/// </summary>
		private void ColorChanged (object sender, EventArgs e)
		{
			var color = (Color)((ColorPicker)sender).SelectedColor;
			((ColorPicker)sender).Title = $"{color} ({(int)color.ColorName}) #{color.R:X2}{color.G:X2}{color.B:X2}";
			UpdateDemoLabel ();
		}

		/// <summary>
		/// Update Demo Label.
		/// </summary>
		private void UpdateDemoLabel () => _demoView.ColorScheme = new ColorScheme () {
			Normal = new Attribute (_foregroundColorPicker.SelectedColor, _backgroundColorPicker.SelectedColor)
		};
	}
}