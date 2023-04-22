using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Borders on Window with title", Description: "Demonstrates Window borders with title manipulation.")]
	[ScenarioCategory ("Layout")]
	[ScenarioCategory ("Borders")]
	public class BordersOnWindowWithTitle : Scenario {
		public override void Init ()
		{
			Application.Init ();

			var win = new Window () { Title = "Window" };
			win.Border.ColorScheme = new ColorScheme () {
				Normal = new Terminal.Gui.Attribute (Color.Red, Color.White),
				HotNormal = new Terminal.Gui.Attribute (Color.Magenta, Color.White),
				Disabled = new Terminal.Gui.Attribute (Color.Gray, Color.White),
				Focus = new Terminal.Gui.Attribute (Color.Blue, Color.White),
				HotFocus = new Terminal.Gui.Attribute (Color.BrightBlue, Color.White),
			};
			win.Padding.ColorScheme = new ColorScheme () {
				Normal = new Terminal.Gui.Attribute (Color.White, Color.Red),
				HotNormal = new Terminal.Gui.Attribute (Color.White, Color.Magenta),
				Disabled = new Terminal.Gui.Attribute (Color.White, Color.Gray),
				Focus = new Terminal.Gui.Attribute (Color.White, Color.Blue),
				HotFocus = new Terminal.Gui.Attribute (Color.White, Color.BrightBlue),
			};
			var boc = new BordersOnContainers (
				$"{Application.QuitKey} to Quit - Scenario: {GetName ()}",
				"Window",
				win);

			Application.Run (boc);
			Application.Shutdown ();
		}

		public override void Run ()
		{
		}
	}
}