using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "System Console", Description: "Not working - #518 - Enables System Console and exercises things")]
	[ScenarioCategory ("Bug Repro")]
	[ScenarioCategory ("Console")]
	class UseSystemConsole : Scenario {
		public override void Init (Toplevel top, ColorScheme colorScheme)
		{
			Application.UseSystemConsole = true;
			base.Init (top, colorScheme);
		}

		public override void RequestStop ()
		{
			base.RequestStop ();
			Application.UseSystemConsole = false;
		}

		public override void Run ()
		{
			base.Run ();
		}

		public override void Setup ()
		{
			var pressMe = new Button ("Press me!") {
				X = Pos.Center (),
				Y = Pos.Center (),
			};
			pressMe.Clicked += () => MessageBox.Query (20, 7, "Hi", "Neat?", "Yes", "No");
			Win.Add (pressMe);
		}
	}
}