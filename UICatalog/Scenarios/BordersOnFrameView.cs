﻿using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Borders on FrameView", Description: "Demonstrate FrameView borders manipulation.")]
	[ScenarioCategory ("Layout")]
	[ScenarioCategory ("Borders")]
	public class BordersOnFrameView : Scenario {
		public override void Init (ColorScheme colorScheme)
		{
			Application.Init ();

			var boc = new BordersOnContainers (
				$"CTRL-Q to Close - Scenario: {GetName ()}",
				"FrameView",
				new FrameView ());

			Application.Run (boc);
			Application.Shutdown ();
		}

		public override void Run ()
		{
		}
	}
}