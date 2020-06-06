using System;
using System.Text;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Text Input Controls", Description: "Tests all text input controls")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Bug Repro")]
	class Text : Scenario {
		public override void Setup ()
		{
			var s = "This is a test intended to show how TAB key works (or doesn't) across text fields.";
			Win.Add (new TextField (s) {
				X = 5,
				Y = 1,
				Width = Dim.Percent (80),
				ColorScheme = Colors.Dialog
			});

			var textView = new TextView () {
				X = 5,
				Y = 3,
				Width = Dim.Percent (80),
				Height = Dim.Percent (40),
				ColorScheme = Colors.Dialog
			};
			textView.Text = s;
			Win.Add (textView);

			// BUGBUG: 531 - TAB doesn't go to next control from HexView
			var hexView = new HexView (new System.IO.MemoryStream(Encoding.ASCII.GetBytes (s))) {
				X = 5,
				Y = Pos.Bottom(textView) + 1,
				Width = Dim.Percent(80),
				Height = Dim.Percent(40),
				ColorScheme = Colors.Dialog
			};
			Win.Add (hexView);

			var dateField = new DateField (System.DateTime.Now) {
				X = 5,
				Y = Pos.Bottom (hexView) + 1,
				Width = Dim.Percent (40),
				ColorScheme = Colors.Dialog,
				IsShortFormat = false
			};
			Win.Add (dateField);

			var timeField = new TimeField (DateTime.Now.TimeOfDay) {
				X = Pos.Right (dateField) + 5,
				Y = Pos.Bottom (hexView) + 1,
				Width = Dim.Percent (40),
				ColorScheme = Colors.Dialog,
				IsShortFormat = false
			};
			Win.Add (timeField);

		}
	}
}