using Terminal.Gui;
using System;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Color Picker Demo", Description: "Color Picker Demo.")]
	[ScenarioCategory ("Colors")]
	public class ColorPickers : Scenario {
		/// <summary>
		/// Foreground ColorPicker.
		/// </summary>
		private ColorPicker foregroundColorPicker;

		/// <summary>
		/// Background ColorPicker.
		/// </summary>
		private ColorPicker backgroundColorPicker;

		/// <summary>
		/// Foreground color label.
		/// </summary>
		private Label foregroundColorLabel;

		/// <summary>
		/// Background color Label.
		/// </summary>
		private Label backgroundColorLabel;

		/// <summary>
		/// Demo label.
		/// </summary>
		private Label demoLabel;

		/// <summary>
		/// Setup the scenario.
		/// </summary>
		public override void Setup ()
		{
			// Scenario Window's.
			Win.Title = this.GetName ();

			// Forground ColorPicker.
			foregroundColorPicker = new ColorPicker (1, 1, "Foreground color");
			foregroundColorPicker.ColorChanged += ForegroundColor_ColorChanged;
			Win.Add (foregroundColorPicker);

			foregroundColorLabel = new Label (1, 10, string.Empty);
			Win.Add (foregroundColorLabel);

			// Background ColorPicker.
			backgroundColorPicker = new ColorPicker (85, 1, "Background color");
			backgroundColorPicker.ColorChanged += BackgroundColor_ColorChanged;
			Win.Add (backgroundColorPicker);

			backgroundColorLabel = new Label (85, 10, string.Empty);
			Win.Add (backgroundColorLabel);

			// Demo Label.
			demoLabel = new Label (50, 10, "Lorem Ipsum");
			Win.Add (demoLabel);

			// Set default colors.
			backgroundColorPicker.SelectedColor = demoLabel.SuperView.ColorScheme.Normal.Background;
			foregroundColorPicker.SelectedColor = demoLabel.SuperView.ColorScheme.Normal.Foreground;
		}

		/// <summary>
		/// Fired when foreground color is changed.
		/// </summary>
		private void ForegroundColor_ColorChanged ()
		{
			UpdateColorLabel (foregroundColorLabel, foregroundColorPicker);
			UpdateDemoLabel ();
		}

		/// <summary>
		/// Fired when background color is changed.
		/// </summary>
		private void BackgroundColor_ColorChanged ()
		{
			UpdateColorLabel (backgroundColorLabel, backgroundColorPicker);
			UpdateDemoLabel ();
		}

		/// <summary>
		/// Update a color label from his ColorPicker.
		/// </summary>
		private void UpdateColorLabel (Label label, ColorPicker colorPicker)
		{
			label.Clear ();
			label.Text = $"{colorPicker.SelectedColor} - {(int)colorPicker.SelectedColor}";
		}

		/// <summary>
		/// Update Demo Label.
		/// </summary>
		private void UpdateDemoLabel () => demoLabel.ColorScheme = new ColorScheme () {
			Normal = new Terminal.Gui.Attribute (foregroundColorPicker.SelectedColor, backgroundColorPicker.SelectedColor)
		};
	}
}