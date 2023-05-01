using System.Collections.Generic;
using System.Linq;

namespace Terminal.Gui {
	/// <summary>
	/// Implementation of <see cref="CheckBoxTableSourceWrapperBase"/> which records toggled rows
	/// by their row number.
	/// </summary>
	public class CheckBoxTableSourceWrapperByIndex : CheckBoxTableSourceWrapperBase {
		public CheckBoxTableSourceWrapperByIndex (TableView tableView, ITableSource toWrap) : base (tableView, toWrap)
		{
		}

		/// <summary>
		/// Gets the collection of all the checked rows in the <see cref="Wrapping"/> <see cref="ITableSource"/>.
		/// </summary>
		public HashSet<int> CheckedRows { get; private set; } = new HashSet<int> ();

		protected override bool IsChecked (int row)
		{
			return CheckedRows.Contains (row);
		}

		

		protected override void ToggleRows (int [] range)
		{
			// if all are ticked untick them
			if (range.All (CheckedRows.Contains)) {
				// select none
				foreach (var r in range) {
					CheckedRows.Remove (r);
				}
			} else {
				// otherwise tick all
				foreach (var r in range) {
					CheckedRows.Add (r);
				}
			}
		}
		protected override void ToggleRow (int row)
		{
			if (CheckedRows.Contains (row)) {
				CheckedRows.Remove (row);
			} else {
				CheckedRows.Add (row);
			}
		}

		protected override void ToggleAllRows ()
		{
			if (CheckedRows.Count == Rows) {
				// select none
				CheckedRows.Clear ();
			} else {
				// select all
				CheckedRows = new HashSet<int> (Enumerable.Range (0, Rows));
			}
		}
	}
}
