using System;
using Terminal.Gui;
using Terminal.Gui.Views;

namespace UICatalog.Scenarios;
[ScenarioMetadata (Name: "Date Picker", Description: "Demonstrates how to use DatePicker class")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("DateTime")]
public class DatePickers : Scenario {

	DatePicker datePicker;
	Label currentlySelectedDateLabel;

	public override void Setup ()
	{
		var frameView = new FrameView ("Date Picker") {
			X = Pos.Center (),
			Y = Pos.Center (),
			Width = Dim.Percent (75),
			Height = Dim.Percent (50),
		};
		var label = new Label ("Click the dot to open the date picker.") {
			X = Pos.Center (),
			Y = Pos.Center () - 3,
			Width = Dim.Percent (50),
			Height = 1,
		};

		datePicker = new DatePicker () {
			X = Pos.Center (),
			Y = Pos.Bottom (label) + 1,
			Width = Dim.Percent (50),
			Height = 1,
		};

		currentlySelectedDateLabel = new Label ($"Currently selected date: {datePicker.Date.ToString (datePicker.Format)}") {
			X = Pos.Center (),
			Y = Pos.Bottom (datePicker) + 1,
			Width = Dim.Percent (50),
			Height = 1,
		};

		datePicker.TextChanged += DateChanged;

		var formatLabel = new Label ($"Format: {datePicker.Format}") {
			X = Pos.Center (),
			Y = Pos.Bottom (currentlySelectedDateLabel) + 1,
			Width = Dim.Percent (50),
			Height = 1,
		};

		frameView.Add (label, datePicker, currentlySelectedDateLabel, formatLabel);
		Win.Add (frameView);
	}

	private void DateChanged (object sender, TextChangedEventArgs e)
	{
		try {
			string parsedNewDate = datePicker.Date.ToString (datePicker.Format);
			currentlySelectedDateLabel.Text = $"Currently selected date: {parsedNewDate}";
		} catch (FormatException) {
			MessageBox.ErrorQuery ("Error", "Unable to parse date", "Ok");
		}
	}
}

