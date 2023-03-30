using System;

namespace Terminal.Gui{

	/// <summary>
	/// Event arguments for the <see cref="Application.Resized"/> event.
	/// </summary>
	public class ResizedEventArgs : EventArgs {
		/// <summary>
		/// The number of rows in the resized terminal.
		/// </summary>
		public int Rows { get; set; }
		/// <summary>
		/// The number of columns in the resized terminal.
		/// </summary>
		public int Cols { get; set; }
	}
}
