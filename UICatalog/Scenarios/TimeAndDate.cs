using System;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Time And Date", Description: "Illustrates TimeField and time & date handling")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Bug Repro")] // Issue #246
	class TimeAndDate : Scenario {
		public override void Setup ()
		{
			var longTime = new TimeField (DateTime.Now) {
				X = Pos.Center (),
				Y = 2,
				IsShortFormat = false,
				ReadOnly = false,
			};
			Win.Add (longTime);

			var shortTime = new TimeField (DateTime.Now) {
				X = Pos.Center (),
				Y = Pos.Bottom(longTime) + 1,
				IsShortFormat = true,
				ReadOnly = false,
			};
			Win.Add (shortTime);

			var shortDate = new DateField (DateTime.Now) {
				X = Pos.Center (),
				Y = Pos.Bottom (shortTime) + 1,
				IsShortFormat = true,
				ReadOnly = true,
			};
			Win.Add (shortDate);

			var longDate = new DateField (DateTime.Now) {
				X = Pos.Center (),
				Y = Pos.Bottom (shortDate) + 1,
				IsShortFormat = false,
				ReadOnly = true,
			};
			Win.Add (longDate);

			Win.Add (new Button ("Swap Long/Short & Read/Read Only") {
				X = Pos.Center (),
				Y = Pos.Bottom (Win) - 5,
				Clicked = () => {
					longTime.ReadOnly = !longTime.ReadOnly;
					shortTime.ReadOnly = !shortTime.ReadOnly;

					longTime.IsShortFormat = !longTime.IsShortFormat;
					shortTime.IsShortFormat = !shortTime.IsShortFormat;

					longDate.ReadOnly = !longDate.ReadOnly;
					shortDate.ReadOnly = !shortDate.ReadOnly;

					longDate.IsShortFormat = !longDate.IsShortFormat;
					shortDate.IsShortFormat = !shortDate.IsShortFormat;
				}
			});
		}
	}
}