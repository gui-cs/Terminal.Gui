using System;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Time And Date", Description: "Illustrates TimeField and time & date handling")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Bug Repro")] // Issue #246
	class TimeAndDate : Scenario {
		public override void Setup ()
		{
			// NOTE: The TimeField control is not ready for prime-time. See #246

			var longTime = new TimeField (0, 0, DateTime.Now, isShort: false) {
				// BUGBUG: TimeField does not support Computed Layout
				X = Pos.Center (),
				Y = 2,
				ReadOnly = false,
			};
			Win.Add (longTime);

			var shortTime = new TimeField (0, 2, DateTime.Now, isShort: true) {
				// BUGBUG: TimeField does not support Computed Layout
				X = Pos.Center (),
				Y = Pos.Bottom(longTime) + 1,
				ReadOnly = true,
			};
			Win.Add (shortTime);

			var shortDate = new DateField (0, 2, DateTime.Now, isShort: true) {
				// BUGBUG: TimeField does not support Computed Layout
				X = Pos.Center (),
				Y = Pos.Bottom (shortTime) + 1,
				ReadOnly = true,
			};
			Win.Add (shortDate);

			var longDate = new TimeField (0, 2, DateTime.Now, isShort: true) {
				// BUGBUG: TimeField does not support Computed Layout
				X = Pos.Center (),
				Y = Pos.Bottom (shortDate) + 1,
				ReadOnly = true,
			};
			Win.Add (longDate);

			Win.Add (new Button ("Swap Long/Short & Read/Read Only") {
				X = Pos.Center (),
				Y = Pos.Bottom (Win) - 5,
				Clicked = () => {
					longTime.ReadOnly = !longTime.ReadOnly;
					shortTime.ReadOnly = !shortTime.ReadOnly;

					//longTime.IsShortFormat = !longTime.IsShortFormat;
					//shortTime.IsShortFormat = !shortTime.IsShortFormat;

					longDate.ReadOnly = !longDate.ReadOnly;
					shortDate.ReadOnly = !shortDate.ReadOnly;

					//longDate.IsShortFormat = !longDate.IsShortFormat;
					//shortDate.IsShortFormat = !shortDate.IsShortFormat;
				}
			});
		}
	}
}