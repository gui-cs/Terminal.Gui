//
// HexView.cs: A hexadecimal viewer
//
// TODO:
// - Support an operation to switch between hex and values
// - Tab perhaps to switch?
// - Support nibble-based navigation
// - Support editing, perhaps via list of changes?
// - Support selection with highlighting
// - Redraw should support just repainted affected region
// - Process Key needs to just queue affected region for cursor changes (as we repaint the text)

using System;
using System.IO;

namespace Terminal.Gui {
	public class HexView : View {
		Stream source;
		long displayStart, position;

		/// <summary>
		/// Creates and instance of the HexView that will render a seekable stream in hex on the allocated view region.
		/// </summary>
		/// <param name="source">Source stream, this stream should support seeking, or this will raise an exceotion.</param>
		public HexView (Stream source) : base()
		{
			Source = source;
			this.source = source;
			CanFocus = true;
		}

		/// <summary>
		/// The source stream to display on the hex view, the stream should support seeking.
		/// </summary>
		/// <value>The source.</value>
		public Stream Source {
			get => source;
			set {
				if (value == null)
					throw new ArgumentNullException ("source");
				if (!value.CanSeek)
					throw new ArgumentException ("The source stream must be seekable (CanSeek property)", "source");
				source = value;

				SetNeedsDisplay ();
			}
		}

		internal void SetDisplayStart (long value)
		{
			if (value >= source.Length)
				displayStart = source.Length - 1;
			else if (value < 0)
				displayStart = 0;
			else
				displayStart = value;
			SetNeedsDisplay ();
		}

		/// <summary>
		/// Configures the initial offset to be displayed at the top
		/// </summary>
		/// <value>The display start.</value>
		public long DisplayStart {
			get => displayStart;
			set {
				position = value;

				SetDisplayStart (value);
			}
		}

		const int displayWidth = 9;
		const int bsize = 4;
		int bytesPerLine;

		public override Rect Frame {
			get => base.Frame;
			set {
				base.Frame = value;

				// Small buffers will just show the position, with 4 bytes
				bytesPerLine = 4;
				if (value.Width - displayWidth > 17)
					bytesPerLine = 4 * ((value.Width - displayWidth) / 18);
			}
		}

		public override void Redraw (Rect region)
		{
			Attribute currentAttribute;
			var current = ColorScheme.Focus;
			Driver.SetAttribute (current);
			Move (0, 0);

			var frame = Frame;

			var nblocks = bytesPerLine / 4;
			var data = new byte [nblocks * 4 * frame.Height];
			Source.Position = displayStart;
			var n = source.Read (data, 0, data.Length);

			for (int line = 0; line < frame.Height; line++) {
				Move (0, line);
				Driver.SetAttribute (ColorScheme.HotNormal);
				Driver.AddStr (string.Format ("{0:x8} ", displayStart + line * nblocks * 4));

				currentAttribute = ColorScheme.HotNormal;
				SetAttribute (ColorScheme.Normal);

				for (int block = 0; block < nblocks; block++) {
					for (int b = 0; b < 4; b++) {
						var offset = (line * nblocks * 4) + block * 4 + b;
						if (offset + displayStart == position)
							SetAttribute (ColorScheme.HotNormal);
						else
							SetAttribute (ColorScheme.Normal);

						Driver.AddStr (offset >= n ? "   " : string.Format ("{0:x2} ", data [offset]));
					}
					Driver.AddStr (block + 1 == nblocks ? " " : "| ");
				}
				for (int bitem = 0; bitem < nblocks * 4; bitem++) {
					var offset = line * nblocks * 4 + bitem;

					if (offset + displayStart == position)
						SetAttribute (ColorScheme.HotFocus);
					else
						SetAttribute (ColorScheme.Normal);
					
					Rune c = ' ';
					if (offset >= n)
						c = ' ';
					else {
						var b = data [offset];
						if (b < 32)
							c = '.';
						else if (b > 127)
							c = '.';
						else
							c = b;
					}
					Driver.AddRune (c);
				}
			}

			void SetAttribute (Attribute attribute)
			{
				if (currentAttribute != attribute) {
					currentAttribute = attribute;
					Driver.SetAttribute (attribute);
				}
			}

		}

		public override void PositionCursor ()
		{
			var delta = (int)(position - displayStart);
			var line = delta / bytesPerLine;
			var item = delta % bytesPerLine;
			var block = item / 4;
			var column = (item % 4) * 3;

			Move (displayWidth + block * 14 + column, line);
		}

		public override bool ProcessKey (KeyEvent keyEvent)
		{
			switch (keyEvent.Key) {
			case Key.CursorLeft:
				if (position == 0)
					return true;
				if (position - 1 < DisplayStart) {
					SetDisplayStart (displayStart - bytesPerLine);
					SetNeedsDisplay ();
				}
				position--;
				break;
			case Key.CursorRight:
				if (position < source.Length)
					position++;
				if (position >= (DisplayStart + bytesPerLine * Frame.Height)) {
					SetDisplayStart (DisplayStart + bytesPerLine);
					SetNeedsDisplay ();
				}
				break;
			case Key.CursorDown:
				if (position + bytesPerLine < source.Length)
					position += bytesPerLine;
				if (position >= (DisplayStart + bytesPerLine * Frame.Height)) {
					SetDisplayStart (DisplayStart + bytesPerLine);
					SetNeedsDisplay ();
				}
				break;
			case Key.CursorUp:
				position -= bytesPerLine;
				if (position < 0)
					position = 0;
				if (position < DisplayStart) {
					SetDisplayStart (DisplayStart - bytesPerLine);
					SetNeedsDisplay ();
				} 
				break;
			default:
				return false;
			}
			// TODO: just se the NeedDispay for the affected region, not all
			SetNeedsDisplay ();
			PositionCursor ();
			return false;
		}
	}
}
