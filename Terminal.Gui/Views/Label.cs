//
// Label.cs: Label control
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

using System;
using System.Collections.Generic;
using System.Linq;
using NStack;

namespace Terminal.Gui {
	/// <summary>
	/// Text alignment enumeration, controls how text is displayed.
	/// </summary>
	public enum TextAlignment {
		/// <summary>
		/// Aligns the text to the left of the frame.
		/// </summary>
		Left, 
		/// <summary>
		/// Aligns the text to the right side of the frame.
		/// </summary>
		Right, 
		/// <summary>
		/// Centers the text in the frame.
		/// </summary>
		Centered, 
		/// <summary>
		/// Shows the line as justified text in the line.
		/// </summary>
		Justified
	}

	/// <summary>
	/// Label view, displays a string at a given position, can include multiple lines.
	/// </summary>
	public class Label : View {
		List<ustring> lines = new List<ustring> ();
		bool recalcPending = true;
		ustring text;
		TextAlignment textAlignment;

		static Rect CalcRect (int x, int y, ustring s)
		{
			int mw = 0;
			int ml = 1;

			int cols = 0;
			foreach (var rune in s) {
				if (rune == '\n') {
					ml++;
					if (cols > mw)
						mw = cols;
					cols = 0;
				} else
					cols++;
			}
			return new Rect (x, y, cols, ml);
		}

		/// <summary>
		///   Public constructor: creates a label at the given
		///   coordinate with the given string, computes the bounding box
		///   based on the size of the string, assumes that the string contains
		///   newlines for multiple lines, no special breaking rules are used.
		/// </summary>
		public Label (int x, int y, ustring text) : this (CalcRect (x, y, text), text)
		{
		}

		/// <summary>
		///   Public constructor: creates a label at the given
		///   coordinate with the given string and uses the specified
		///   frame for the string.
		/// </summary>
		public Label (Rect rect, ustring text) : base (rect)
		{
			this.text = text;
		}

		/// <summary>
		/// Public constructor: creates a label and configures the default Width and Height based on the text, the result is suitable for Computed layout.
		/// </summary>
		/// <param name="text">Text.</param>
		public Label (ustring text) : base ()
		{
			this.text = text;
			var r = CalcRect (0, 0, text);
			Width = r.Width;
			Height = r.Height;
		}

		static char [] whitespace = new char [] { ' ', '\t' };

		static ustring ClipAndJustify (ustring str, int width, TextAlignment talign)
		{
			int slen = str.Length;
			if (slen > width)
				return str [0, width];
			else {
				if (talign == TextAlignment.Justified) {
					// TODO: ustring needs this
			               	var words = str.ToString ().Split (whitespace, StringSplitOptions.RemoveEmptyEntries);
					int textCount = words.Sum (arg => arg.Length);

					var spaces = (width- textCount) / (words.Length - 1);
					var extras = (width - textCount) % words.Length;

					var s = new System.Text.StringBuilder ();
					//s.Append ($"tc={textCount} sp={spaces},x={extras} - ");
					for (int w = 0; w < words.Length; w++) {
						var x = words [w];
						s.Append (x);
						if (w + 1 < words.Length)
							for (int i = 0; i < spaces; i++)
								s.Append (' ');
						if (extras > 0) {
							s.Append ('_');
							extras--;
						}
					}
					return ustring.Make (s.ToString ());
				}
				return str;
			}
		}

		void Recalc ()
		{
			recalcPending = false;
			Recalc (text, lines, Frame.Width, textAlignment);
		}

		static void Recalc (ustring textStr, List<ustring> lineResult, int width, TextAlignment talign)
		{
			lineResult.Clear ();
			if (textStr.IndexOf ('\n') == -1) {
				lineResult.Add (ClipAndJustify (textStr, width, talign));
				return;
			}
			int textLen = textStr.Length;
			int lp = 0;
			for (int i = 0; i < textLen; i++) {
				Rune c = textStr [i];

				if (c == '\n') {
					lineResult.Add (ClipAndJustify (textStr [lp, i], width, talign));
					lp = i + 1;
				}
			}
		}

		public override void Redraw (Rect region)
		{
			if (recalcPending)
				Recalc ();

			if (TextColor != -1)
				Driver.SetAttribute (TextColor);
			else
				Driver.SetAttribute (ColorScheme.Normal);

			Clear ();
			Move (Frame.X, Frame.Y);
			for (int line = 0; line < lines.Count; line++) {
				if (line < region.Top || line >= region.Bottom)
					continue;
				var str = lines [line];
				int x;
				switch (textAlignment) {
				case TextAlignment.Left:
				case TextAlignment.Justified:
					x = 0;
					break;
				case TextAlignment.Right:
					x = Frame.Right - str.Length;
					break;
				case TextAlignment.Centered:
					x = Frame.Left + (Frame.Width - str.Length) / 2;
					break;
				default:
					throw new ArgumentOutOfRangeException ();
				}
				Move (x, line);
				Driver.AddStr (str);
			}
		}

		/// <summary>
		/// Computes the number of lines needed to render the specified text by the Label control
		/// </summary>
		/// <returns>Number of lines.</returns>
		/// <param name="text">Text, may contain newlines.</param>
		/// <param name="width">The width for the text.</param>
		public static int MeasureLines (ustring text, int width)
		{
			var result = new List<ustring> ();
			Recalc (text, result, width, TextAlignment.Left);
			return result.Count;
		}

		/// <summary>
		///   The text displayed by this widget.
		/// </summary>
		public virtual ustring Text {
			get => text;
			set {
				text = value;
				recalcPending = true;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Controls the text-alignemtn property of the label, changing it will redisplay the label.
		/// </summary>
		/// <value>The text alignment.</value>
		public TextAlignment TextAlignment {
			get => textAlignment;
			set {
				textAlignment = value;
				SetNeedsDisplay ();
			}
		}

		Attribute textColor = -1;
		/// <summary>
		///   The color used for the label
		/// </summary>        
		public Attribute TextColor {
			get => textColor;
			set {
				textColor = value;
				SetNeedsDisplay ();
			}
		}
	}

}
