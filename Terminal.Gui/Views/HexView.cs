//
// HexView.cs: A hexadecimal viewer
//
// TODO:
// - Support searching and highlighting of the search result
// - Bug showing the last line
// 
using System;
using System.Collections.Generic;
using System.IO;

namespace Terminal.Gui {
	/// <summary>
	/// An hex viewer and editor <see cref="View"/> over a <see cref="System.IO.Stream"/>
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="HexView"/> provides a hex editor on top of a seekable <see cref="Stream"/> with the left side showing an hex
	/// dump of the values in the <see cref="Stream"/> and the right side showing the contents (filtered to 
	/// non-control sequence ASCII characters).    
	/// </para>
	/// <para>
	/// Users can switch from one side to the other by using the tab key.  
	/// </para>
	/// <para>
	/// To enable editing, set <see cref="AllowEdits"/> to true. When <see cref="AllowEdits"/> is true 
	/// the user can make changes to the hexadecimal values of the <see cref="Stream"/>. Any changes are tracked
	/// in the <see cref="Edits"/> property (a <see cref="SortedDictionary{TKey, TValue}"/>) indicating 
	/// the position where the changes were made and the new values. A convenience method, <see cref="ApplyEdits"/>
	/// will apply the edits to the <see cref="Stream"/>.
	/// </para>
	/// <para>
	/// Control the first byte shown by setting the <see cref="DisplayStart"/> property 
	/// to an offset in the stream.
	/// </para>
	/// </remarks>
	public class HexView : View {
		SortedDictionary<long, byte> edits = new SortedDictionary<long, byte> ();
		Stream source;
		long displayStart, position;
		bool firstNibble, leftSide;

		/// <summary>
		/// Initialzies a <see cref="HexView"/> class using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		/// <param name="source">The <see cref="Stream"/> to view and edit as hex, this <see cref="Stream"/> must support seeking, or an exception will be thrown.</param>
		public HexView (Stream source) : base ()
		{
			Source = source;
			this.source = source;
			CanFocus = true;
			leftSide = true;
			firstNibble = true;
		}

		/// <summary>
		/// Initialzies a <see cref="HexView"/> class using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		public HexView () : this (source: new MemoryStream ()) { }

		/// <summary>
		/// Sets or gets the <see cref="Stream"/> the <see cref="HexView"/> is operating on; the stream must support seeking (<see cref="Stream.CanSeek"/> == true).
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
		/// Sets or gets the offset into the <see cref="Stream"/> that will displayed at the top of the <see cref="HexView"/>
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

		/// <inheritdoc/>
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

		//
		// This is used to support editing of the buffer on a peer List<>, 
		// the offset corresponds to an offset relative to DisplayStart, and
		// the buffer contains the contents of a screenful of data, so the 
		// offset is relative to the buffer.
		//
		// 
		byte GetData (byte [] buffer, int offset, out bool edited)
		{
			var pos = DisplayStart + offset;
			if (edits.TryGetValue (pos, out byte v)) {
				edited = true;
				return v;
			}
			edited = false;
			return buffer [offset];
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
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

			int activeColor = ColorScheme.HotNormal;
			int trackingColor = ColorScheme.HotFocus;

			for (int line = 0; line < frame.Height; line++) {
				var lineRect = new Rect (0, line, frame.Width, 1);
				if (!bounds.Contains (lineRect))
					continue;

				Move (0, line);
				Driver.SetAttribute (ColorScheme.HotNormal);
				Driver.AddStr (string.Format ("{0:x8} ", displayStart + line * nblocks * 4));

				currentAttribute = ColorScheme.HotNormal;
				SetAttribute (GetNormalColor ());

				for (int block = 0; block < nblocks; block++) {
					for (int b = 0; b < 4; b++) {
						var offset = (line * nblocks * 4) + block * 4 + b;
						bool edited;
						var value = GetData (data, offset, out edited);
						if (offset + displayStart == position || edited)
							SetAttribute (leftSide ? activeColor : trackingColor);
						else
							SetAttribute (GetNormalColor ());

						Driver.AddStr (offset >= n ? "  " : string.Format ("{0:x2}", value));
						SetAttribute (GetNormalColor ());
						Driver.AddRune (' ');
					}
					Driver.AddStr (block + 1 == nblocks ? " " : "| ");
				}


				for (int bitem = 0; bitem < nblocks * 4; bitem++) {
					var offset = line * nblocks * 4 + bitem;

					bool edited = false;
					Rune c = ' ';
					if (offset >= n)
						c = ' ';
					else {
						var b = GetData (data, offset, out edited);
						if (b < 32)
							c = '.';
						else if (b > 127)
							c = '.';
						else
							c = b;
					}
					if (offset + displayStart == position || edited)
						SetAttribute (leftSide ? trackingColor : activeColor);
					else
						SetAttribute (GetNormalColor ());

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

		///<inheritdoc/>
		public override void PositionCursor ()
		{
			var delta = (int)(position - displayStart);
			var line = delta / bytesPerLine;
			var item = delta % bytesPerLine;
			var block = item / 4;
			var column = (item % 4) * 3;

			if (leftSide)
				Move (displayWidth + block * 14 + column + (firstNibble ? 0 : 1), line);
			else
				Move (displayWidth + (bytesPerLine / 4) * 14 + item - 1, line);
		}

		void RedisplayLine (long pos)
		{
			var delta = (int)(pos - DisplayStart);
			var line = delta / bytesPerLine;

			SetNeedsDisplay (new Rect (0, line, Frame.Width, 1));
		}

		void CursorRight ()
		{
			RedisplayLine (position);
			if (leftSide) {
				if (firstNibble) {
					firstNibble = false;
					return;
				} else
					firstNibble = true;
			}
			if (position < source.Length)
				position++;
			if (position >= (DisplayStart + bytesPerLine * Frame.Height)) {
				SetDisplayStart (DisplayStart + bytesPerLine);
				SetNeedsDisplay ();
			} else
				RedisplayLine (position);
		}

		void MoveUp (int bytes)
		{
			RedisplayLine (position);
			position -= bytes;
			if (position < 0)
				position = 0;
			if (position < DisplayStart) {
				SetDisplayStart (DisplayStart - bytes);
				SetNeedsDisplay ();
			} else
				RedisplayLine (position);

		}

		void MoveDown (int bytes)
		{
			RedisplayLine (position);
			if (position + bytes < source.Length)
				position += bytes;
			if (position >= (DisplayStart + bytesPerLine * Frame.Height)) {
				SetDisplayStart (DisplayStart + bytes);
				SetNeedsDisplay ();
			} else
				RedisplayLine (position);
		}

		/// <inheritdoc/>
		public override bool ProcessKey (KeyEvent keyEvent)
		{
			switch (keyEvent.Key) {
			case Key.CursorLeft:
				RedisplayLine (position);
				if (leftSide) {
					if (!firstNibble) {
						firstNibble = true;
						return true;
					}
					firstNibble = false;
				}
				if (position == 0)
					return true;
				if (position - 1 < DisplayStart) {
					SetDisplayStart (displayStart - bytesPerLine);
					SetNeedsDisplay ();
				} else
					RedisplayLine (position);
				position--;
				break;
			case Key.CursorRight:
				CursorRight ();
				break;
			case Key.CursorDown:
				MoveDown (bytesPerLine);
				break;
			case Key.CursorUp:
				MoveUp (bytesPerLine);
				break;
			case Key.Enter:
				leftSide = !leftSide;
				RedisplayLine (position);
				firstNibble = true;
				break;
			case ((int)'v' + Key.AltMask):
			case Key.PageUp:
				MoveUp (bytesPerLine * Frame.Height);
				break;
			case Key.V | Key.CtrlMask:
			case Key.PageDown:
				MoveDown (bytesPerLine * Frame.Height);
				break;
			case Key.Home:
				DisplayStart = 0;
				SetNeedsDisplay ();
				break;
			default:
				if (leftSide) {
					int value = -1;
					var k = (char)keyEvent.Key;
					if (k >= 'A' && k <= 'F')
						value = k - 'A' + 10;
					else if (k >= 'a' && k <= 'f')
						value = k - 'a' + 10;
					else if (k >= '0' && k <= '9')
						value = k - '0';
					else
						return false;

					byte b;
					if (!edits.TryGetValue (position, out b)) {
						source.Position = position;
						b = (byte)source.ReadByte ();
					}
					RedisplayLine (position);
					if (firstNibble) {
						firstNibble = false;
						b = (byte)(b & 0xf | (value << 4));
						edits [position] = b;
					} else {
						b = (byte)(b & 0xf0 | value);
						edits [position] = b;
						CursorRight ();
					}
					return true;
				} else
					return false;
			}
			PositionCursor ();
			return true;
		}

		/// <summary>
		/// Gets or sets whether this <see cref="HexView"/> allow editing of the <see cref="Stream"/> 
		/// of the underlying <see cref="Stream"/>.
		/// </summary>
		/// <value><c>true</c> if allow edits; otherwise, <c>false</c>.</value>
		public bool AllowEdits { get; set; }

		/// <summary>
		/// Gets a <see cref="SortedDictionary{TKey, TValue}"/> describing the edits done to the <see cref="HexView"/>. 
		/// Each Key indicates an offset where an edit was made and the Value is the changed byte.
		/// </summary>
		/// <value>The edits.</value>
		public IReadOnlyDictionary<long, byte> Edits => edits;

		/// <summary>
		/// This method applies andy edits made to the <see cref="Stream"/> and resets the 
		/// contents of the <see cref="Edits"/> property
		/// </summary>
		public void ApplyEdits ()
		{
			foreach (var kv in edits) {
				source.Position = kv.Key;
				source.WriteByte (kv.Value);
			}
			edits = new SortedDictionary<long, byte> ();
		}

		private CursorVisibility desiredCursorVisibility = CursorVisibility.Default;

		/// <summary>
		/// Get / Set the wished cursor when the field is focused
		/// </summary>
		public CursorVisibility DesiredCursorVisibility {
			get => desiredCursorVisibility;
			set {
				if (desiredCursorVisibility != value && HasFocus) {
					Application.Driver.SetCursorVisibility (value);
				}

				desiredCursorVisibility = value;
			}
		}
	}
}
