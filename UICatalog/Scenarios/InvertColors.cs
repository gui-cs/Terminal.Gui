using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Invert Colors", Description: "Invert the foreground and the background colors.")]
	[ScenarioCategory ("Colors")]
	class InvertColors : Scenario {
		public override void Setup ()
		{
			Win.ColorScheme = Colors.TopLevel;

			var color = Application.Driver.MakeAttribute (Color.Red, Color.Blue);

			var label = new Label ("Test") {
				ColorScheme = new ColorScheme()
			};
			label.ColorScheme.Normal = color;
			Win.Add (label);

			var button = new Button ("Invert color!") {
				X = Pos.Center (),
				Y = Pos.Center (),
			};
			button.Clicked += () => {
				color = Application.Driver.MakeAttribute (color.Background, color.Foreground);

				label.ColorScheme.Normal = color;
				label.SetNeedsDisplay ();
			};
			Win.Add (button);
		}
	}
}