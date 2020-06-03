using NStack;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui;

namespace UICatalog {
	/// <summary>
	/// This Scenario demonstrates building a custom control (a class deriving from View) that:
	///   - Provides a simple "Character Map" application (like Windows' charmap.exe).
	///   - Helps test unicode character rendering in Terminal.Gui
	///   - Illustrates how to use ScrollView to do infinite scrolling
	/// </summary>
	[ScenarioMetadata (Name: "Character Map", Description: "Illustrates a custom control and Unicode")]
	[ScenarioCategory ("Text")]
	[ScenarioCategory ("Controls")]
	class CharacterMap : Scenario {
		public override void Setup ()
		{
			var charMap = new CharMap () { X = 1, Y = 1, Width = Dim.Percent(75), Height = 16, Start = 0x2500 };

			Win.Add (charMap);

			var button = new Button ("Box Drawing & Geometric Shapes U+2500-25ff") { X = Pos.X (charMap), Y = Pos.Bottom (charMap) + 1,
				Clicked = () => {
					charMap.Start = 0x2500;
				}
			};
			Win.Add (button);

			button = new Button ("Dingbats U+2700-27ff") {
				X = Pos.X (charMap),
				Y = Pos.Bottom (button),
				Clicked = () => {
					charMap.Start = 0x2700;
				}
			};
			Win.Add (button);

			button = new Button ("Miscellaneous Symbols and Arrows U+2b00-2bff") {
				X = Pos.X (charMap),
				Y = Pos.Bottom (button),
				Clicked = () => {
					charMap.Start = 0x2b00;
				}
			};
			Win.Add (button);

			button = new Button ("Arrows U+2190-21ff") {
				X = Pos.X (charMap),
				Y = Pos.Bottom (button),
				Clicked = () => {
					charMap.Start = 0x2190;
				}
			};
			Win.Add (button);
		}
	}

	class CharMap : ScrollView {

		/// <summary>
		/// Specifies the starting offset for the character map. The default is 0x2500 
		/// which is the Box Drawing characters.
		/// </summary>
		public int Start {
			get => _start;
			set {
				_start = value;
				ContentOffset = new Point (0, _start / 16);

				SetNeedsDisplay ();
			}
		}
		int _start = 0x2500;

		// Row Header + space + (space + char + space)
		public static int RowHeaderWidth => "U+000x".Length;
		public static int RowWidth => RowHeaderWidth + 1 + (" c ".Length * 16);

		public CharMap ()
		{
			ContentSize = new Size (CharMap.RowWidth, int.MaxValue / 16);
			ShowVerticalScrollIndicator = true;
			ShowHorizontalScrollIndicator = false;
			LayoutComplete += (sender, args) => {
				if (Bounds.Width <= RowWidth) {
					ShowHorizontalScrollIndicator = true;
				} else {
					ShowHorizontalScrollIndicator = false;
				}
			};

			DrawContent += CharMap_DrawContent;
		}

#if true
		private void CharMap_DrawContent (object sender, Rect viewport)
		{
			for (int header = 0; header < 16; header++) {
				Move (viewport.X + RowHeaderWidth + 1 + (header * 3), 0);
				Driver.AddStr ($" {header:x} ");
			}
			for (int row = 0; row < viewport.Height - 1; row++) {
				var rowLabel = $"U+{(((-viewport.Y + row) * 16)) / 16:x}x";
				Move (0, row + 1);
				Driver.AddStr (rowLabel);
				for (int col = 0; col < 16; col++) {
					Move (viewport.X + (RowHeaderWidth + 1 + (col * 3)), 0 + row + 1);
					Driver.AddStr ($" {(char)(((-viewport.Y + row) * 16) + col)} ");
				}
			}
		}
#else
		public override void OnDrawContent (Rect viewport)
		{
			CharMap_DrawContent(this, viewport);
			base.OnDrawContent (viewport);
		}
#endif
	}
}
