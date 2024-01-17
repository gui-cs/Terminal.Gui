using Terminal.Gui;

namespace UICatalog.Scenarios;
[ScenarioMetadata (Name: "Date Picker", Description: "Demonstrates how to use DatePicker class")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("DateTime")]
public class DatePickers : Scenario {


	public override void Setup ()
	{
		var datePicker = new DatePicker () {
			Y = Pos.Center (),
			X = Pos.Center ()
		};


		Win.Add (datePicker);
	}
}

