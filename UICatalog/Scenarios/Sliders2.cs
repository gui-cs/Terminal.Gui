using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;



using S = Terminal.Gui.Sliders;

namespace UICatalog {
	[ScenarioMetadata (Name: "Slider2", Description: "Demonstrates all sorts of Sliders")]
	//[ScenarioCategory ("Controls")]
	[ScenarioCategory ("")]

	class Sliders2Scenario : Scenario {
		public override void Setup ()
		{
			var header = "NUMBERS";
			var numbers = new List<int> { 0, 250, 500, 750, 1000 };



			var slider1 = new Slider<int> (header, numbers);
			slider1.LegendsOrientation = S.Orientation.Vertical;
			slider1.InnerSpacing = 3;
			Win.Add (slider1);

			var slider2 = new Slider<int> (header, numbers) {
				Type = S.SliderType.Range,
				Y = Pos.Bottom (slider1) + 1,
				RangeAllowSingle = true
			};
			Win.Add (slider2);

			var slider3 = new Slider<int> (header, numbers, S.Orientation.Vertical) {
				Y = Pos.Bottom (slider2) + 1,
				X = 0,
				LegendsOrientation = S.Orientation.Horizontal
			};
			slider3.ColorScheme = new ColorScheme {
				Normal = new Terminal.Gui.Attribute (Color.White, Color.Black),
				Focus = new Terminal.Gui.Attribute (Color.BrightRed, Color.White)
			};
			slider3.Style.HeaderStyle.NormalAttribute = new Terminal.Gui.Attribute (Color.White, Color.Red);
			slider3.Style.HeaderStyle.FocusAttribute = new Terminal.Gui.Attribute (Color.Red, Color.White);
			slider3.Style.SetChar.Attribute = new Terminal.Gui.Attribute (Color.BrightRed, Color.White);
			Win.Add (slider3);

			var slider4 = new Slider<int> (header, numbers, S.Orientation.Vertical) {
				X = Pos.Right (slider3) + 1,
				Y = Pos.Y (slider3),
				Type = S.SliderType.Single,
			};
			Win.Add (slider4);
		}
	}
}
