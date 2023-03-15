using System;

namespace Terminal.Gui {
	/// <summary>
	/// <see cref="EventArgs"/> used by the <see cref="ListView.RowRender"/> event.
	/// </summary>
	public class ListViewRowEventArgs : EventArgs {
		/// <summary>
		/// The current row being rendered.
		/// </summary>
		public int Row { get; }
		/// <summary>
		/// The <see cref="Attribute"/> used by current row or
		/// null to maintain the current attribute.
		/// </summary>
		public Attribute? RowAttribute { get; set; }

		/// <summary>
		/// Initializes with the current row.
		/// </summary>
		/// <param name="row"></param>
		public ListViewRowEventArgs (int row)
		{
			Row = row;
		}
	}
}
