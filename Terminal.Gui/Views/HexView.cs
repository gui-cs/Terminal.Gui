﻿//
// HexView.cs: A hexadecimal viewer
//
// TODO:
// - Support searching and highlighting of the search result
// - Bug showing the last line
// 
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Terminal.Gui;

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
public partial class HexView : View {
	SortedDictionary<long, byte> edits = new SortedDictionary<long, byte> ();
	Stream source;
	long displayStart, pos;
	bool firstNibble, leftSide;

	long position {
		get => pos;
		set {
			pos = value;
			OnPositionChanged ();
		}
	}

	/// <summary>
	/// Initializes a <see cref="HexView"/> class using <see cref="LayoutStyle.Computed"/> layout.
	/// </summary>
	/// <param name="source">The <see cref="Stream"/> to view and edit as hex, this <see cref="Stream"/> must support seeking, or an exception will be thrown.</param>
	public HexView (Stream source) : base ()
	{
		Source = source;
		CanFocus = true;
		leftSide = true;
		firstNibble = true;

		// Things this view knows how to do
		AddCommand (Command.Left, () => MoveLeft ());
		AddCommand (Command.Right, () => MoveRight ());
		AddCommand (Command.LineDown, () => MoveDown (bytesPerLine));
		AddCommand (Command.LineUp, () => MoveUp (bytesPerLine));
		AddCommand (Command.ToggleChecked, () => ToggleSide ());
		AddCommand (Command.PageUp, () => MoveUp (bytesPerLine * Frame.Height));
		AddCommand (Command.PageDown, () => MoveDown (bytesPerLine * Frame.Height));
		AddCommand (Command.TopHome, () => MoveHome ());
		AddCommand (Command.BottomEnd, () => MoveEnd ());
		AddCommand (Command.StartOfLine, () => MoveStartOfLine ());
		AddCommand (Command.EndOfLine, () => MoveEndOfLine ());
		AddCommand (Command.StartOfPage, () => MoveUp (bytesPerLine * ((int)(position - displayStart) / bytesPerLine)));
		AddCommand (Command.EndOfPage, () => MoveDown (bytesPerLine * (Frame.Height - 1 - (int)(position - displayStart) / bytesPerLine)));

		// Default keybindings for this view
		KeyBindings.Add (ConsoleDriverKey.CursorLeft, Command.Left);
		KeyBindings.Add (ConsoleDriverKey.CursorRight, Command.Right);
		KeyBindings.Add (ConsoleDriverKey.CursorDown, Command.LineDown);
		KeyBindings.Add (ConsoleDriverKey.CursorUp, Command.LineUp);
		KeyBindings.Add (ConsoleDriverKey.Enter, Command.ToggleChecked);

		KeyBindings.Add ('v' + ConsoleDriverKey.AltMask, Command.PageUp);
		KeyBindings.Add (ConsoleDriverKey.PageUp, Command.PageUp);

		KeyBindings.Add (ConsoleDriverKey.V | ConsoleDriverKey.CtrlMask, Command.PageDown);
		KeyBindings.Add (ConsoleDriverKey.PageDown, Command.PageDown);

		KeyBindings.Add (ConsoleDriverKey.Home, Command.TopHome);
		KeyBindings.Add (ConsoleDriverKey.End, Command.BottomEnd);
		KeyBindings.Add (ConsoleDriverKey.CursorLeft | ConsoleDriverKey.CtrlMask, Command.StartOfLine);
		KeyBindings.Add (ConsoleDriverKey.CursorRight | ConsoleDriverKey.CtrlMask, Command.EndOfLine);
		KeyBindings.Add (ConsoleDriverKey.CursorUp | ConsoleDriverKey.CtrlMask, Command.StartOfPage);
		KeyBindings.Add (ConsoleDriverKey.CursorDown | ConsoleDriverKey.CtrlMask, Command.EndOfPage);
	}

	/// <summary>
	/// Initializes a <see cref="HexView"/> class using <see cref="LayoutStyle.Computed"/> layout.
	/// </summary>
	public HexView () : this (source: new MemoryStream ()) { }

	/// <summary>
	/// Event to be invoked when an edit is made on the <see cref="Stream"/>.
	/// </summary>
	public event EventHandler<HexViewEditEventArgs> Edited;

	/// <summary>
	/// Event to be invoked when the position and cursor position changes.
	/// </summary>
	public event EventHandler<HexViewEventArgs> PositionChanged;

	/// <summary>
	/// Sets or gets the <see cref="Stream"/> the <see cref="HexView"/> is operating on; the stream must support seeking (<see cref="Stream.CanSeek"/> == true).
	/// </summary>
	/// <value>The source.</value>
	public Stream Source {
		get => source;
		set {
			if (value == null) {
				throw new ArgumentNullException ("source");
			}
			if (!value.CanSeek) {
				throw new ArgumentException ("The source stream must be seekable (CanSeek property)", "source");
			}
			source = value;

			if (displayStart > source.Length) {
				DisplayStart = 0;
			}
			if (position > source.Length) {
				position = 0;
			}
			SetNeedsDisplay ();
		}
	}

	internal void SetDisplayStart (long value)
	{
		if (value > 0 && value >= source.Length) {
			displayStart = source.Length - 1;
		} else if (value < 0) {
			displayStart = 0;
		} else {
			displayStart = value;
		}
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
	int bpl;

	int bytesPerLine {
		get => bpl;
		set {
			bpl = value;
			OnPositionChanged ();
		}
	}

	/// <inheritdoc/>
	public override Rect Frame {
		get => base.Frame;
		set {
			base.Frame = value;

			// Small buffers will just show the position, with the bsize field value (4 bytes)
			bytesPerLine = bsize;
			if (value.Width - displayWidth > 17) {
				bytesPerLine = bsize * ((value.Width - displayWidth) / 18);
			}
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
		long pos = DisplayStart + offset;
		if (edits.TryGetValue (pos, out byte v)) {
			edited = true;
			return v;
		}
		edited = false;
		return buffer [offset];
	}

	///<inheritdoc/>
	public override void OnDrawContent (Rect contentArea)
	{
		Attribute currentAttribute;
		var current = ColorScheme.Focus;
		Driver.SetAttribute (current);
		Move (0, 0);

		var frame = Frame;

		int nblocks = bytesPerLine / bsize;
		byte [] data = new byte [nblocks * bsize * frame.Height];
		Source.Position = displayStart;
		int n = source.Read (data, 0, data.Length);

		var activeColor = ColorScheme.HotNormal;
		var trackingColor = ColorScheme.HotFocus;

		for (int line = 0; line < frame.Height; line++) {
			var lineRect = new Rect (0, line, frame.Width, 1);
			if (!Bounds.Contains (lineRect)) {
				continue;
			}

			Move (0, line);
			Driver.SetAttribute (ColorScheme.HotNormal);
			Driver.AddStr (string.Format ("{0:x8} ", displayStart + line * nblocks * bsize));

			currentAttribute = ColorScheme.HotNormal;
			SetAttribute (GetNormalColor ());

			for (int block = 0; block < nblocks; block++) {
				for (int b = 0; b < bsize; b++) {
					int offset = line * nblocks * bsize + block * bsize + b;
					byte value = GetData (data, offset, out bool edited);
					if (offset + displayStart == position || edited) {
						SetAttribute (leftSide ? activeColor : trackingColor);
					} else {
						SetAttribute (GetNormalColor ());
					}

					Driver.AddStr (offset >= n && !edited ? "  " : string.Format ("{0:x2}", value));
					SetAttribute (GetNormalColor ());
					Driver.AddRune ((Rune)' ');
				}
				Driver.AddStr (block + 1 == nblocks ? " " : "| ");
			}

			for (int bitem = 0; bitem < nblocks * bsize; bitem++) {
				int offset = line * nblocks * bsize + bitem;
				byte b = GetData (data, offset, out bool edited);
				Rune c;
				if (offset >= n && !edited) {
					c = (Rune)' ';
				} else {
					if (b < 32) {
						c = (Rune)'.';
					} else if (b > 127) {
						c = (Rune)'.';
					} else {
						Rune.DecodeFromUtf8 (new ReadOnlySpan<byte> (b), out c, out _);
					}
				}
				if (offset + displayStart == position || edited) {
					SetAttribute (leftSide ? trackingColor : activeColor);
				} else {
					SetAttribute (GetNormalColor ());
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

	///<inheritdoc/>
	public override void PositionCursor ()
	{
		int delta = (int)(position - displayStart);
		int line = delta / bytesPerLine;
		int item = delta % bytesPerLine;
		int block = item / bsize;
		int column = item % bsize * 3;

		if (leftSide) {
			Move (displayWidth + block * 14 + column + (firstNibble ? 0 : 1), line);
		} else {
			Move (displayWidth + bytesPerLine / bsize * 14 + item - 1, line);
		}
	}

	void RedisplayLine (long pos)
	{
		int delta = (int)(pos - DisplayStart);
		int line = delta / bytesPerLine;

		SetNeedsDisplay (new Rect (0, line, Frame.Width, 1));
	}

	bool MoveEndOfLine ()
	{
		position = Math.Min (position / bytesPerLine * bytesPerLine + bytesPerLine - 1, source.Length);
		SetNeedsDisplay ();

		return true;
	}

	bool MoveStartOfLine ()
	{
		position = position / bytesPerLine * bytesPerLine;
		SetNeedsDisplay ();

		return true;
	}

	bool MoveEnd ()
	{
		position = source.Length;
		if (position >= DisplayStart + bytesPerLine * Frame.Height) {
			SetDisplayStart (position);
			SetNeedsDisplay ();
		} else {
			RedisplayLine (position);
		}

		return true;
	}

	bool MoveHome ()
	{
		DisplayStart = 0;
		SetNeedsDisplay ();

		return true;
	}

	bool ToggleSide ()
	{
		leftSide = !leftSide;
		RedisplayLine (position);
		firstNibble = true;

		return true;
	}

	bool MoveLeft ()
	{
		RedisplayLine (position);
		if (leftSide) {
			if (!firstNibble) {
				firstNibble = true;
				return true;
			}
			firstNibble = false;
		}
		if (position == 0) {
			return true;
		}
		if (position - 1 < DisplayStart) {
			SetDisplayStart (displayStart - bytesPerLine);
			SetNeedsDisplay ();
		} else {
			RedisplayLine (position);
		}
		position--;

		return true;
	}

	bool MoveRight ()
	{
		RedisplayLine (position);
		if (leftSide) {
			if (firstNibble) {
				firstNibble = false;
				return true;
			} else {
				firstNibble = true;
			}
		}
		if (position < source.Length) {
			position++;
		}
		if (position >= DisplayStart + bytesPerLine * Frame.Height) {
			SetDisplayStart (DisplayStart + bytesPerLine);
			SetNeedsDisplay ();
		} else {
			RedisplayLine (position);
		}

		return true;
	}

	bool MoveUp (int bytes)
	{
		RedisplayLine (position);
		if (position - bytes > -1) {
			position -= bytes;
		}
		if (position < DisplayStart) {
			SetDisplayStart (DisplayStart - bytes);
			SetNeedsDisplay ();
		} else {
			RedisplayLine (position);
		}

		return true;
	}

	bool MoveDown (int bytes)
	{
		RedisplayLine (position);
		if (position + bytes < source.Length) {
			position += bytes;
		} else if (bytes == bytesPerLine * Frame.Height && source.Length >= DisplayStart + bytesPerLine * Frame.Height
			|| bytes <= bytesPerLine * Frame.Height - bytesPerLine && source.Length <= DisplayStart + bytesPerLine * Frame.Height) {
			long p = position;
			while (p + bytesPerLine < source.Length) {
				p += bytesPerLine;
			}
			position = p;
		}
		if (position >= DisplayStart + bytesPerLine * Frame.Height) {
			SetDisplayStart (DisplayStart + bytes);
			SetNeedsDisplay ();
		} else {
			RedisplayLine (position);
		}

		return true;
	}

	//// BUGBUG: Why is this not handled by a key binding???
	/// <inheritdoc/>
	public override bool OnKeyPressed (KeyEventArgs keyEvent)
	{
		if (!AllowEdits) {
			return false;
		}

		// Ignore control characters and other special keys
		if (keyEvent.ConsoleDriverKey < ConsoleDriverKey.Space || keyEvent.ConsoleDriverKey > ConsoleDriverKey.CharMask) {
			return false;
		}

		if (leftSide) {
			int value;
			char k = (char)keyEvent.ConsoleDriverKey;
			if (k >= 'A' && k <= 'F') {
				value = k - 'A' + 10;
			} else if (k >= 'a' && k <= 'f') {
				value = k - 'a' + 10;
			} else if (k >= '0' && k <= '9') {
				value = k - '0';
			} else {
				return false;
			}

			byte b;
			if (!edits.TryGetValue (position, out b)) {
				source.Position = position;
				b = (byte)source.ReadByte ();
			}
			RedisplayLine (position);
			if (firstNibble) {
				firstNibble = false;
				b = (byte)(b & 0xf | value << bsize);
				edits [position] = b;
				OnEdited (new HexViewEditEventArgs (position, edits [position]));
			} else {
				b = (byte)(b & 0xf0 | value);
				edits [position] = b;
				OnEdited (new HexViewEditEventArgs (position, edits [position]));
				MoveRight ();
			}
			return true;
		} else {
			return false;
		}
	}

	/// <summary>
	/// Method used to invoke the <see cref="Edited"/> event passing the <see cref="KeyValuePair{TKey, TValue}"/>.
	/// </summary>
	/// <param name="e">The key value pair.</param>
	public virtual void OnEdited (HexViewEditEventArgs e)
	{
		Edited?.Invoke (this, e);
	}

	/// <summary>
	/// Method used to invoke the <see cref="PositionChanged"/> event passing the <see cref="HexViewEventArgs"/> arguments.
	/// </summary>
	public virtual void OnPositionChanged ()
	{
		PositionChanged?.Invoke (this, new HexViewEventArgs (Position, CursorPosition, BytesPerLine));
	}

	/// <inheritdoc/>
	public override bool MouseEvent (MouseEvent me)
	{
		if (!me.Flags.HasFlag (MouseFlags.Button1Clicked) && !me.Flags.HasFlag (MouseFlags.Button1DoubleClicked)
								&& !me.Flags.HasFlag (MouseFlags.WheeledDown) && !me.Flags.HasFlag (MouseFlags.WheeledUp)) {
			return false;
		}

		if (!HasFocus) {
			SetFocus ();
		}

		if (me.Flags == MouseFlags.WheeledDown) {
			DisplayStart = Math.Min (DisplayStart + bytesPerLine, source.Length);
			return true;
		}

		if (me.Flags == MouseFlags.WheeledUp) {
			DisplayStart = Math.Max (DisplayStart - bytesPerLine, 0);
			return true;
		}

		if (me.X < displayWidth) {
			return true;
		}
		int nblocks = bytesPerLine / bsize;
		int blocksSize = nblocks * 14;
		int blocksRightOffset = displayWidth + blocksSize - 1;
		if (me.X > blocksRightOffset + bytesPerLine - 1) {
			return true;
		}
		leftSide = me.X >= blocksRightOffset;
		long lineStart = me.Y * bytesPerLine + displayStart;
		int x = me.X - displayWidth + 1;
		int block = x / 14;
		x -= block * 2;
		int empty = x % 3;
		int item = x / 3;
		if (!leftSide && item > 0 && (empty == 0 || x == block * 14 + 14 - 1 - block * 2)) {
			return true;
		}
		firstNibble = true;
		if (leftSide) {
			position = Math.Min (lineStart + me.X - blocksRightOffset, source.Length);
		} else {
			position = Math.Min (lineStart + item, source.Length);
		}

		if (me.Flags == MouseFlags.Button1DoubleClicked) {
			leftSide = !leftSide;
			if (leftSide) {
				firstNibble = empty == 1;
			} else {
				firstNibble = true;
			}
		}
		SetNeedsDisplay ();

		return true;
	}

	/// <summary>
	/// Gets or sets whether this <see cref="HexView"/> allow editing of the <see cref="Stream"/> 
	/// of the underlying <see cref="Stream"/>.
	/// </summary>
	/// <value><c>true</c> if allow edits; otherwise, <c>false</c>.</value>
	public bool AllowEdits { get; set; } = true;

	/// <summary>
	/// Gets a <see cref="SortedDictionary{TKey, TValue}"/> describing the edits done to the <see cref="HexView"/>. 
	/// Each Key indicates an offset where an edit was made and the Value is the changed byte.
	/// </summary>
	/// <value>The edits.</value>
	public IReadOnlyDictionary<long, byte> Edits => edits;

	/// <summary>
	/// Gets the current character position starting at one, related to the <see cref="Stream"/>.
	/// </summary>
	public long Position => position + 1;

	/// <summary>
	/// Gets the current cursor position starting at one for both, line and column.
	/// </summary>
	public Point CursorPosition {
		get {
			int delta = (int)position;
			int line = delta / bytesPerLine + 1;
			int item = delta % bytesPerLine + 1;

			return new Point (item, line);
		}
	}

	/// <summary>
	/// The bytes length per line.
	/// </summary>
	public int BytesPerLine => bytesPerLine;

	/// <summary>
	/// This method applies and edits made to the <see cref="Stream"/> and resets the 
	/// contents of the <see cref="Edits"/> property.
	/// </summary>
	/// <param name="stream">If provided also applies the changes to the passed <see cref="Stream"/></param>.
	public void ApplyEdits (Stream stream = null)
	{
		foreach (var kv in edits) {
			source.Position = kv.Key;
			source.WriteByte (kv.Value);
			source.Flush ();
			if (stream != null) {
				stream.Position = kv.Key;
				stream.WriteByte (kv.Value);
				stream.Flush ();
			}
		}
		edits = new SortedDictionary<long, byte> ();
		SetNeedsDisplay ();
	}

	/// <summary>
	/// This method discards the edits made to the <see cref="Stream"/> by resetting the 
	/// contents of the <see cref="Edits"/> property.
	/// </summary>
	public void DiscardEdits ()
	{
		edits = new SortedDictionary<long, byte> ();
	}

	CursorVisibility desiredCursorVisibility = CursorVisibility.Default;

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

	///<inheritdoc/>
	public override bool OnEnter (View view)
	{
		Application.Driver.SetCursorVisibility (DesiredCursorVisibility);

		return base.OnEnter (view);
	}
}