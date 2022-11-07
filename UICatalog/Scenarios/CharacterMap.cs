#define DRAW_CONTENT
//#define BASE_DRAW_CONTENT

using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terminal.Gui;
using Rune = System.Rune;

namespace UICatalog.Scenarios {
	/// <summary>
	/// This Scenario demonstrates building a custom control (a class deriving from View) that:
	///   - Provides a "Character Map" application (like Windows' charmap.exe).
	///   - Helps test unicode character rendering in Terminal.Gui
	///   - Illustrates how to use ScrollView to do infinite scrolling
	/// </summary>
	[ScenarioMetadata (Name: "Character Map",
		Description: "A Unicode character set viewier built as a custom control using the ScrollView control.")]
	[ScenarioCategory ("Text and Formatting")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("ScrollView")]
	public class CharacterMap : Scenario {
		CharMap _charMap;
		public override void Setup ()
		{
			_charMap = new CharMap () {
				X = 0,
				Y = 0,
				Height = Dim.Fill (),
			};

			var radioItems = new (ustring radioLabel, int start, int end) [] {
				CreateRadio("ASCII Control Characters", 0x00, 0x1F),
				CreateRadio("C0 Control Characters", 0x80, 0x9f),
				CreateRadio("Hangul Jamo", 0x1100, 0x11ff),	// This is where wide chars tend to start
				CreateRadio("Currency Symbols", 0x20A0, 0x20CF),
				CreateRadio("Letter-like Symbols", 0x2100, 0x214F),
				CreateRadio("Arrows", 0x2190, 0x21ff),
				CreateRadio("Mathematical symbols", 0x2200, 0x22ff),
				CreateRadio("Miscellaneous Technical", 0x2300, 0x23ff),
				CreateRadio("Box Drawing & Geometric Shapes", 0x2500, 0x25ff),
				CreateRadio("Miscellaneous Symbols", 0x2600, 0x26ff),
				CreateRadio("Dingbats", 0x2700, 0x27ff),
				CreateRadio("Braille", 0x2800, 0x28ff),
				CreateRadio("Miscellaneous Symbols & Arrows", 0x2b00, 0x2bff),
				CreateRadio("Alphabetic Pres. Forms", 0xFB00, 0xFb4f),
				CreateRadio("Cuneiform Num. and Punct.", 0x12400, 0x1240f),
				CreateRadio("Chess Symbols", 0x1FA00, 0x1FA0f),
				CreateRadio("End", CharMap.MaxCodePointVal - 16, CharMap.MaxCodePointVal),
			};
			(ustring radioLabel, int start, int end) CreateRadio (ustring title, int start, int end)
			{
				return ($"{title} (U+{start:x5}-{end:x5})", start, end);
			}

			Win.Add (_charMap);
			var label = new Label ("Jump To Unicode Block:") { X = Pos.Right (_charMap) + 1, Y = Pos.Y (_charMap) };
			Win.Add (label);

			var jumpList = new RadioGroup (radioItems.Select (t => t.radioLabel).ToArray ()) {
				X = Pos.X (label),
				Y = Pos.Bottom (label),
				Width = radioItems.Max (r => r.radioLabel.Length) + 3,
				SelectedItem = 8
			};
			jumpList.SelectedItemChanged += (args) => {
				_charMap.Start = radioItems [args.SelectedItem].start;
			};

			Win.Add (jumpList);

			jumpList.Refresh ();
			jumpList.SetFocus ();

			_charMap.Width = Dim.Fill () - jumpList.Width;

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

		public const int COLUMN_WIDTH = 3;
		public const int ROW_HEIGHT = 1;

		public static int MaxCodePointVal => 0x10FFFF;

		public static int RowLabelWidth => $"U+{MaxCodePointVal:x5}".Length;
		public static int RowWidth => RowLabelWidth + (COLUMN_WIDTH * 16);

		public CharMap ()
		{
			ColorScheme = Colors.Dialog;
			CanFocus = true;

			ContentSize = new Size (CharMap.RowWidth, MaxCodePointVal / 16 + 1);
			ShowVerticalScrollIndicator = true;
			ShowHorizontalScrollIndicator = false;
			LayoutComplete += (args) => {
				if (Bounds.Width < RowWidth) {
					ShowHorizontalScrollIndicator = true;
				} else {
					ShowHorizontalScrollIndicator = false;
					// Snap 1st column into view if it's been scrolled horizontally 
					ContentOffset = new Point (0, ContentOffset.Y);
					SetNeedsDisplay ();
				}
			};
			DrawContent += CharMap_DrawContent;

			AddCommand (Command.ScrollUp, () => { ScrollUp (1); return true; });
			AddCommand (Command.ScrollDown, () => { ScrollDown (1); return true; });
			AddCommand (Command.ScrollLeft, () => { ScrollLeft (1); return true; });
			AddCommand (Command.ScrollRight, () => { ScrollRight (1); return true; });
			AddCommand (Command.PageUp, () => ScrollUp (Bounds.Height - 1));
			AddCommand (Command.PageDown, () => ScrollDown (Bounds.Height - 1));
		}

		private void CharMap_DrawContent (Rect viewport)
		{
			var oldClip = Driver.Clip;
			Driver.Clip = Frame;
			// Redraw doesn't know about the scroll indicators, so if off, add one to height
			if (!ShowHorizontalScrollIndicator) {
				Driver.Clip = new Rect (Frame.X, Frame.Y, Frame.Width, Frame.Height + 1);
			}
			Driver.SetAttribute (HasFocus ? ColorScheme.HotFocus : ColorScheme.Focus);
			Move (0, 0);
			Driver.AddStr (new string (' ', RowLabelWidth + 1));
			for (int hexDigit = 0; hexDigit < 16; hexDigit++) {
				var x = ContentOffset.X + RowLabelWidth + (hexDigit * COLUMN_WIDTH);
				if (x > RowLabelWidth - 2) {
					Move (x, 0);
					Driver.AddStr ($" {hexDigit:x} ");
				}
			}
			//Move (RowWidth, 0);
			//Driver.AddRune (' ');

			var firstColumnX = viewport.X + RowLabelWidth;
			for (int row = -ContentOffset.Y, y = 0; row <= (-ContentOffset.Y) + (Bounds.Height / ROW_HEIGHT); row++, y += ROW_HEIGHT) {
				int val = (row) * 16;
				Driver.SetAttribute (GetNormalColor ());
				Move (firstColumnX, y + 1);
				Driver.AddStr (new string (' ', 16 * COLUMN_WIDTH));
				if (val < MaxCodePointVal) {
					Driver.SetAttribute (GetNormalColor ());
					for (int col = 0; col < 16; col++) {
						var rune = new Rune ((uint)((uint)val + col));
						//if (rune >= 0x00D800 && rune <= 0x00DFFF) {
						//	if (col == 0) {
						//		Driver.AddStr ("Reserved for surrogate pairs.");
						//	}
						//	continue;
						//}						
						Move (firstColumnX + (col * COLUMN_WIDTH) + 1, y + 1);
						Driver.AddRune (rune);
					}
					Move (0, y + 1);
					Driver.SetAttribute (HasFocus ? ColorScheme.HotFocus : ColorScheme.Focus);
					var rowLabel = $"U+{val / 16:x4}x ";
					Driver.AddStr (rowLabel);
				}
			}
			Driver.Clip = oldClip;
		}

		protected override void Dispose (bool disposing)
		{
			DrawContent -= CharMap_DrawContent;
			base.Dispose (disposing);
		}
	}
}
