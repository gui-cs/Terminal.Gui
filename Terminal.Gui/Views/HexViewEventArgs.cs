//
// HexView.cs: A hexadecimal viewer
//
// TODO:
// - Support searching and highlighting of the search result
// - Bug showing the last line
// 
using System;
using System.IO;

namespace Terminal.Gui {
	/// <summary>
	/// Defines the event arguments for <see cref="HexView.PositionChanged"/> event.
	/// </summary>
	public class HexViewEventArgs : EventArgs {
		/// <summary>
		/// Gets the current character position starting at one, related to the <see cref="Stream"/>.
		/// </summary>
		public long Position { get; private set; }
		/// <summary>
		/// Gets the current cursor position starting at one for both, line and column.
		/// </summary>
		public Point CursorPosition { get; private set; }

		/// <summary>
		/// The bytes length per line.
		/// </summary>
		public int BytesPerLine { get; private set; }

		/// <summary>
		/// Initializes a new instance of <see cref="HexViewEventArgs"/>
		/// </summary>
		/// <param name="pos">The character position.</param>
		/// <param name="cursor">The cursor position.</param>
		/// <param name="lineLength">Line bytes length.</param>
		public HexViewEventArgs (long pos, Point cursor, int lineLength)
		{
			Position = pos;
			CursorPosition = cursor;
			BytesPerLine = lineLength;
		}
	}
}
