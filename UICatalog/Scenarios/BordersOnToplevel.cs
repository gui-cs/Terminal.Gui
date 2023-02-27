using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Borders on Toplevel", Description: "Demonstrates Toplevel borders manipulation.")]
	[ScenarioCategory ("Layout")]
	[ScenarioCategory ("Borders")]
	public class BordersOnToplevel : Scenario {
		public override void Init (ColorScheme colorScheme)
		{
			Application.Init ();

			var boc = new BordersOnContainers (
				$"CTRL-Q to Close - Scenario: {GetName ()}",
				"Toplevel",
				new FrameView ());

			Application.Run (boc);
			Application.Shutdown ();
		}

		public override void Run ()
		{
		}
	}
}