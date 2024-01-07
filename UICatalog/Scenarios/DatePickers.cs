using System;
using Terminal.Gui;
using Terminal.Gui.Views;

namespace UICatalog.Scenarios;
[ScenarioMetadata (Name: "Date Picker", Description: "Demonstrates how to use DatePicker class")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("DateTime")]
public class DatePickers : Scenario {

	public override void Setup ()
	{
		var datePicker = new DatePicker (DateTime.Now, "MM/dd/yyyy") {
			X = 0,
			Y = 0,
			Width = Dim.Fill (),
			Height = Dim.Fill (),
		};
		Win.Add (datePicker);
	}
}

