using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Basic Colors", Description: "Show all basic colors.")]
	[ScenarioCategory ("Colors")]
	class BasicColors : Scenario {
		public override void Setup ()
		{
			var vx = 30;
			var x = 30;
			var y = 14;
			var colors = System.Enum.GetValues (typeof (Color));

			foreach (Color bg in colors) {
				var vl = new Label (bg.ToString (), TextDirection.TopBottom_LeftRight) {
					X = vx,
					Y = 0,
					Width = 1,
					Height = 13,
					VerticalTextAlignment = VerticalTextAlignment.Bottom,
					ColorScheme = new ColorScheme () { Normal = new Attribute (bg, colors.Length - 1 - bg) }
				};
				Win.Add (vl);
				var hl = new Label (bg.ToString ()) {
					X = 15,
					Y = y,
					Width = 13,
					Height = 1,
					TextAlignment = TextAlignment.Right,
					ColorScheme = new ColorScheme () { Normal = new Attribute (bg, colors.Length - 1 - bg) }
				};
				Win.Add (hl);
				vx++;
				foreach (Color fg in colors) {
					var c = new Attribute (fg, bg);
					var t = x.ToString ();
					var l = new Label (x, y, t [t.Length - 1].ToString ()) {
						ColorScheme = new ColorScheme () { Normal = c }
					};
					Win.Add (l);
					x++;
				}
				x = 30;
				y++;
			}
		}
	}
}