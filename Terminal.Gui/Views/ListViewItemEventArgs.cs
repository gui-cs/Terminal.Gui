using System;

namespace Terminal.Gui {
	/// <summary>
	/// <see cref="EventArgs"/> for <see cref="ListView"/> events.
	/// </summary>
	public class ListViewItemEventArgs : EventArgs {
		/// <summary>
		/// The index of the <see cref="ListView"/> item.
		/// </summary>
		public int Item { get; }
		/// <summary>
		/// The <see cref="ListView"/> item.
		/// </summary>
		public object Value { get; }

		/// <summary>
		/// Initializes a new instance of <see cref="ListViewItemEventArgs"/>
		/// </summary>
		/// <param name="item">The index of the <see cref="ListView"/> item.</param>
		/// <param name="value">The <see cref="ListView"/> item</param>
		public ListViewItemEventArgs (int item, object value)
		{
			Item = item;
			Value = value;
		}
	}
}
