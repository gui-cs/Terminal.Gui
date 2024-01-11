using System;
using Terminal.Gui;
using Terminal.Gui.Views;

namespace UICatalog.Scenarios;
[ScenarioMetadata (Name: "Date Picker", Description: "Demonstrates how to use DatePicker class")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("DateTime")]
public class DatePickers : Scenario {

	DatePicker _datePicker;
	Label _currentlySelectedDateLabel;

	public override void Setup ()
	{
		var dp = new DatePicker () {
			Format = "dd/MM/yyyy",
		};

		Win.Add (dp);
	}

	private void DateChanged (object sender, TextChangedEventArgs e)
	{
		try {
			string parsedNewDate = _datePicker.Date.ToString (_datePicker.Format);
			_currentlySelectedDateLabel.Text = $"Currently selected date: {parsedNewDate}";
		} catch (FormatException) {
			MessageBox.ErrorQuery ("Error", "Unable to parse date", "Ok");
		}
	}
}

