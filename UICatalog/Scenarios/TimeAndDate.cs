using System;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Time And Date", Description: "Illustrates TimeField and time & date handling")]
	[ScenarioCategory ("Controls")]
	class TimeAndDate : Scenario {
		public override void Setup ()
		{
			// NOTE: The TimeField control is not ready for prime-time.

			Win.Add (new TimeField (0, 0, DateTime.Now, isShort: false) {
				// BUGBUG: TimeField does not support Computed Layout
				//X = Pos.Center (),
				//Y = Pos.Center () - 1,
				X = 10,
				Y = 2,
			});

			Win.Add (new TimeField (0, 2, DateTime.Now, isShort: true) {
				// BUGBUG: TimeField does not support Computed Layout
				//X = Pos.Center (),
				//Y = Pos.Center () + 1,
				X = 10,
				Y = 3,
			});
		}
	}
}