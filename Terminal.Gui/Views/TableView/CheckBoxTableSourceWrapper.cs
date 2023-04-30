using System;
using System.Collections.Generic;
using System.Linq;

namespace Terminal.Gui {
	public class CheckBoxTableSourceWrapper : ITableSource {

		private readonly TableView tableView;

		public CheckBoxTableSourceWrapper (TableView tableView, ITableSource toWrap)
		{
			this.Wrapping = toWrap;
			this.tableView = tableView;

			CheckedRune = new Rune (Application.Driver != null ? Application.Driver.Checked : '√');
			UnCheckedRune = new Rune (Application.Driver != null ? Application.Driver.UnChecked : '╴');

			tableView.MouseClick += TableView_MouseClick;
			tableView.CellToggled += TableView_CellToggled;
		}


		public Rune CheckedRune { get; set; }
		public Rune UnCheckedRune { get; set; }
		public ITableSource Wrapping { get; }
		public HashSet<int> CheckedRows { get; private set; } = new HashSet<int> ();

		private void TableView_MouseClick (object sender, MouseEventEventArgs e)
		{
			// we only care about clicks (not movements)
			if(!e.MouseEvent.Flags.HasFlag(MouseFlags.Button1Clicked)) {
				return;
			}

			var hit = tableView.ScreenToCell (e.MouseEvent.X,e.MouseEvent.Y, out int? headerIfAny);

			if(headerIfAny.HasValue && headerIfAny.Value == 0) {
				ToggleAllRows ();
				e.Handled = true;
				tableView.SetNeedsDisplay ();
			}
			else
			if(hit.HasValue && hit.Value.X == 0) {
				ToggleRow (hit.Value.Y);
				e.Handled = true;
				tableView.SetNeedsDisplay ();
			}
		}

		private void TableView_CellToggled (object sender, CellToggledEventArgs e)
		{
			// Suppress default toggle behavior when using checkboxes
			// and instead handle ourselves

			var range = tableView.GetAllSelectedCells ().Select (c => c.Y).Distinct ().ToArray();

			ToggleRows (range);
			e.Cancel = true;
			tableView.SetNeedsDisplay ();
		}

		private void ToggleRows (int [] range)
		{
			// if all are ticked untick them
			if (range.All(CheckedRows.Contains)) {
				// select none
				foreach(var r in range) {
					CheckedRows.Remove (r);
				}
			} else {
				// otherwise tick all
				foreach (var r in range) {
					CheckedRows.Add (r);
				}
			}
		}

		private void ToggleRow (int y)
		{
			if (CheckedRows.Contains (y)) {
				CheckedRows.Remove (y);
			} else {
				CheckedRows.Add (y);
			}
		}
		private void ToggleAllRows ()
		{
			if(CheckedRows.Count == Rows) {
				// select none
				CheckedRows.Clear ();
			}
			else {
				// select all
				CheckedRows = new HashSet<int> (Enumerable.Range (0, Rows));
			}
		}


		public object this [int row, int col] {
			get {
				if (col == 0) {
					return CheckedRows.Contains (row) ? CheckedRune : UnCheckedRune;
				}

				return Wrapping [row, col - 1];
			}
		}

		public int Rows => Wrapping.Rows;

		public int Columns => Wrapping.Columns + 1;

		public string [] ColumnNames {
			get {
				var toReturn = Wrapping.ColumnNames.ToList ();
				toReturn.Insert (0, " ");
				return toReturn.ToArray();
			}
		}

	}
}
