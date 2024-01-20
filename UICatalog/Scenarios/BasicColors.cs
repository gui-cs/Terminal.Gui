using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Basic Colors", Description: "Show all basic colors.")]
	[ScenarioCategory ("Colors")]
	[ScenarioCategory ("Text and Formatting")]
	public class BasicColors : Scenario {
		public override void Setup ()
		{
			var vx = 30;
			var x = 30;
			var y = 14;
			var colors = System.Enum.GetValues (typeof (ColorName));

		
			foreach (ColorName bg in colors) {
				Attribute attr = new Attribute (bg, colors.Length - 1 - bg);
				var vl = new Label (bg.ToString (), TextDirection.TopBottom_LeftRight) {
					X = vx,
					Y = 0,
					Width = 1,
					Height = 13,
					VerticalTextAlignment = VerticalTextAlignment.Bottom,
					ColorScheme = new ColorScheme () { Normal = attr }
				};
				Win.Add (vl);
				var hl = new Label (bg.ToString ()) {
					X = 15,
					Y = y,
					Width = 13,
					Height = 1,
					TextAlignment = TextAlignment.Right,
					ColorScheme = new ColorScheme () { Normal = attr }
				};
				Win.Add (hl);
				vx++;
				foreach (ColorName fg in colors) {
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

			Win.Add (new Label ("Mouse over to get the Attribute:") {
				X = Pos.AnchorEnd (36),
			});
			Win.Add (new Label ("Foreground:") {
				X = Pos.AnchorEnd (35),
				Y = 2
			});

			var lblForeground = new Label () {
				X = Pos.AnchorEnd (23),
				Y = 2
			};
			Win.Add (lblForeground);

			var viewForeground = new View ("  ") {
				X = Pos.AnchorEnd (2),
				Y = 2,
				ColorScheme = new ColorScheme ()
			};
			Win.Add (viewForeground);

			Win.Add (new Label ("Background:") {
				X = Pos.AnchorEnd (35),
				Y = 4
			});

			var lblBackground = new Label () {
				X = Pos.AnchorEnd (23),
				Y = 4
			};
			Win.Add (lblBackground);

			var viewBackground = new View ("  ") {
				X = Pos.AnchorEnd (2),
				Y = 4,
				ColorScheme = new ColorScheme ()
			};
			Win.Add (viewBackground);

			Application.MouseEvent += (s, e) => {
				if (e.MouseEvent.View != null) {
					var fore = e.MouseEvent.View.GetNormalColor ().Foreground;
					var back = e.MouseEvent.View.GetNormalColor ().Background;
					lblForeground.Text = $"#{fore.R:X2}{fore.G:X2}{fore.B:X2} {fore.ColorName} ";
					viewForeground.ColorScheme = new ColorScheme (viewForeground.ColorScheme) { Normal = new Attribute (fore, fore) };

					lblBackground.Text = $"#{back.R:X2}{back.G:X2}{back.B:X2} {back.ColorName} ";
					viewBackground.ColorScheme = new ColorScheme (viewBackground.ColorScheme) { Normal = new Attribute (back, back) };
				}
			};
		}
	}
}