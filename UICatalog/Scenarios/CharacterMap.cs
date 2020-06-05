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
			var charMap = new CharMap () { X = 0, Y = 0, Width = CharMap.RowWidth + 2, Height = Dim.Fill(), Start = 0x2500, 
				ColorScheme = Colors.Dialog};

			Win.Add (charMap);

			Button CreateBlock(Window win, ustring title, int start, int end, View align)
			{
				var button = new Button ($"{title} (U+{start:x5}-{end:x5})") {
					X = Pos.X (align),
					Y = Pos.Bottom (align),
					Clicked = (o, e) => {
						charMap.Start = start;
					},
				};
				win.Add (button);
				return button;
			};

			var label = new Label ("Unicode Blocks:") { X = Pos.Right (charMap) + 2, Y = Pos.Y (charMap) };
			Win.Add (label);
			var button = CreateBlock (Win, "Currency Symbols", 0x20A0, 0x20CF, label);
			button = CreateBlock (Win, "Letterlike Symbols", 0x2100, 0x214F, button);
			button = CreateBlock (Win, "Arrows", 0x2190, 0x21ff, button);
			button = CreateBlock (Win, "Mathematical symbols", 0x2200, 0x22ff, button);
			button = CreateBlock (Win, "Miscellaneous Technical", 0x2300, 0x23ff, button);
			button = CreateBlock (Win, "Box Drawing & Geometric Shapes", 0x2500, 0x25ff, button);
			button = CreateBlock (Win, "Miscellaneous Symbols", 0x2600, 0x26ff, button);
			button = CreateBlock (Win, "Dingbats", 0x2700, 0x27ff, button);
			button = CreateBlock (Win, "Braille", 0x2800, 0x28ff, button);
			button = CreateBlock (Win, "Miscellaneous Symbols and Arrows", 0x2b00, 0x2bff, button);
			button = CreateBlock (Win, "Alphabetic Presentation Forms", 0xFB00, 0xFb4f, button);
			button = CreateBlock (Win, "Cuneiform Numbers and Punctuation[1", 0x12400, 0x1240f, button);
			button = CreateBlock (Win, "Chess Symbols", 0x1FA00, 0x1FA0f, button);
			button = CreateBlock (Win, "End", CharMap.MaxCodePointVal - 16, CharMap.MaxCodePointVal, button);
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

		public static int MaxCodePointVal => 0xE0FFF;

		// Row Header + space + (space + char + space)
		public static int RowHeaderWidth => $"U+{MaxCodePointVal:x5}".Length;
		public static int RowWidth => RowHeaderWidth + 1 + (" c ".Length * 16);

		public CharMap ()
		{
			ContentSize = new Size (CharMap.RowWidth, MaxCodePointVal / 16);
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
				int val = (-viewport.Y + row) * 16;
				if (val < MaxCodePointVal) {
					var rowLabel = $"U+{val / 16:x4}x";
					Move (0, row + 1);
					Driver.AddStr (rowLabel);
					for (int col = 0; col < 16; col++) {
						Move (viewport.X + RowHeaderWidth + 1 + (col * 3), 0 + row + 1);
						Driver.AddStr ($" {(char)((-viewport.Y + row) * 16 + col)} ");
					}
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
