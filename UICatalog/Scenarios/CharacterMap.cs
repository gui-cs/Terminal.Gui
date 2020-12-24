#define DRAW_CONTENT
//#define BASE_DRAW_CONTENT

using NStack;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terminal.Gui;
using Rune = System.Rune;

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
		CharMap _charMap;
		public override void Setup ()
		{
			_charMap = new CharMap () {
				X = 0,
				Y = 0,
				Width = CharMap.RowWidth + 2,
				Height = Dim.Fill (),
				Start = 0x2500,
				ColorScheme = Colors.Dialog,
				CanFocus = true,
			};

			Win.Add (_charMap);
			var label = new Label ("Jump To Unicode Block:") { X = Pos.Right (_charMap) + 1, Y = Pos.Y (_charMap) };
			Win.Add (label);

			(ustring radioLabel, int start, int end) CreateRadio (ustring title, int start, int end)
			{
				return ($"{title} (U+{start:x5}-{end:x5})", start, end);
			}

			var radioItems = new (ustring radioLabel, int start, int end) [] {
				CreateRadio("ASCII Control Characterss", 0x00, 0x1F),
				CreateRadio("C0 Control Characters", 0x80, 0x9f),
				CreateRadio("Hangul Jamo", 0x1100, 0x11ff),	// This is where wide chars tend to start
				CreateRadio("Currency Symbols", 0x20A0, 0x20CF),
				CreateRadio("Letterlike Symbols", 0x2100, 0x214F),
				CreateRadio("Arrows", 0x2190, 0x21ff),
				CreateRadio("Mathematical symbols", 0x2200, 0x22ff),
				CreateRadio("Miscellaneous Technical", 0x2300, 0x23ff),
				CreateRadio("Box Drawing & Geometric Shapes", 0x2500, 0x25ff),
				CreateRadio("Miscellaneous Symbols", 0x2600, 0x26ff),
				CreateRadio("Dingbats", 0x2700, 0x27ff),
				CreateRadio("Braille", 0x2800, 0x28ff),
				CreateRadio("Miscellaneous Symbols and Arrows", 0x2b00, 0x2bff),
				CreateRadio("Alphabetic Presentation Forms", 0xFB00, 0xFb4f),
				CreateRadio("Cuneiform Numbers and Punctuation", 0x12400, 0x1240f),
				CreateRadio("Chess Symbols", 0x1FA00, 0x1FA0f),
				CreateRadio("End", CharMap.MaxCodePointVal - 16, CharMap.MaxCodePointVal),
			};

			var jumpList = new RadioGroup (radioItems.Select (t => t.radioLabel).ToArray ()) {
				X = Pos.X (label),
				Y = Pos.Bottom (label),
				Width = Dim.Fill (),
				SelectedItem = 8
			};
			jumpList.SelectedItemChanged += (args) => {
				_charMap.Start = radioItems [args.SelectedItem].start;
			};

			Win.Add (jumpList);

			jumpList.Refresh ();
			jumpList.SetFocus ();
		}

		public override void Run ()
		{
			base.Run ();
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

		public const int H_SPACE = 2;
		public const int V_SPACE = 2;

		public static int MaxCodePointVal => 0xE0FFF;

		// Row Header + space + (space + char + space)
		public static int RowHeaderWidth => $"U+{MaxCodePointVal:x5}".Length;
		public static int RowWidth => RowHeaderWidth + (H_SPACE * 16);

		public CharMap ()
		{
			ContentSize = new Size (CharMap.RowWidth, MaxCodePointVal / 16);
			ShowVerticalScrollIndicator = true;
			ShowHorizontalScrollIndicator = false;
			LayoutComplete += (args) => {
				if (Bounds.Width <= RowWidth) {
					ShowHorizontalScrollIndicator = true;
				} else {
					ShowHorizontalScrollIndicator = false;
				}
			};
#if DRAW_CONTENT

			DrawContent += CharMap_DrawContent;
#endif
		}

		private void CharMap_DrawContent (Rect viewport)
		{
			//Rune ReplaceNonPrintables (Rune c)
			//{
			//	if (c < 0x20) {
			//		return new Rune (c + 0x2400);         // U+25A1 □ WHITE SQUARE
			//	} else {
			//		return c;
			//	}
			//}

			ContentSize = new Size (CharMap.RowWidth, MaxCodePointVal / 16 + Frame.Height - 1);

			for (int header = 0; header < 16; header++) {
				Move (viewport.X + RowHeaderWidth + (header * H_SPACE), 0);
				Driver.AddStr ($" {header:x} ");
			}
			for (int row = 0, y = 0; row < viewport.Height / 2 - 1; row++, y+= V_SPACE) {
				int val = (-viewport.Y + row) * 16;
				if (val < MaxCodePointVal) {
					var rowLabel = $"U+{val / 16:x4}x";
					Move (0, y + 1);
					Driver.AddStr (rowLabel);
					var prevColWasWide = false;
					for (int col = 0; col < 16; col++) {
						var rune = new Rune ((uint)((uint)(-viewport.Y + row) * 16 + col));
						Move (viewport.X + RowHeaderWidth + (col * H_SPACE) + (prevColWasWide ? 0 : 1), y + 1);
						Driver.AddRune (rune);
						//prevColWasWide = Rune.ColumnWidth (rune) > 1;
					}
				}
			}
		}
#if BASE_DRAW_CONTENT
		public override void OnDrawContent (Rect viewport)
		{
			CharMap_DrawContent (viewport);
			base.OnDrawContent (viewport);
		}
#endif
	}
}
