//
// HexView.cs: A hexadecimal viewer
//
// TODO:
// - Support searching and highlighting of the search result
// - Bug showing the last line
// 
using System;

namespace Terminal.Gui {
	/// <summary>
	/// Defines the event arguments for <see cref="HexView.Edited"/> event.
	/// </summary>
	public class HexViewEditEventArgs : EventArgs {

		/// <summary>
		/// Creates a new instance of the <see cref="HexViewEditEventArgs"/> class.
		/// </summary>
		/// <param name="position"></param>
		/// <param name="newValue"></param>
		public HexViewEditEventArgs (long position, byte newValue)
		{
			Position = position;
			NewValue = newValue;
		}

		/// <summary>
		/// Gets the location of the edit.
		/// </summary>
		public long Position { get; }

		/// <summary>
		/// Gets the new value for that <see cref="Position"/>.
		/// </summary>
		public byte NewValue { get; }
	}
}
