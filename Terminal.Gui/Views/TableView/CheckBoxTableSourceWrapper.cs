using System;
using System.Data;
using System.Linq;

namespace Terminal.Gui {

	/// <summary>
	/// <see cref="ITableSource"/> for a <see cref="TableView"/> which adds a
	/// checkbox column as an additional column in the table.
	/// </summary>
	/// <remarks>This class wraps another <see cref="ITableSource"/> and dynamically
	/// serves its rows/cols plus an extra column. Data in the wrapped source can be
	/// dynamic (change over time).</remarks>
	public abstract class CheckBoxTableSourceWrapperBase : ITableSource {

		private readonly TableView tableView;

		/// <summary>
		/// Creates a new instance of the class presenting the data in <paramref name="toWrap"/>
		/// plus an additional checkbox column.
		/// </summary>
		/// <param name="tableView">The <see cref="TableView"/> this source will be used with.
		/// This is required for event registration.</param>
		/// <param name="toWrap">The original data source of the <see cref="TableView"/> that you
		/// want to add checkboxes to.</param>
		public CheckBoxTableSourceWrapperBase (TableView tableView, ITableSource toWrap)
		{
			this.Wrapping = toWrap;
			this.tableView = tableView;

			tableView.AddKeyBinding (Key.Space, Command.ToggleChecked);

			tableView.MouseClick += TableView_MouseClick;
			tableView.CellToggled += TableView_CellToggled;
		}


		/// <summary>
		/// Gets or sets the character to use for checked entries. Defaults to <see cref="ConsoleDriver.Checked"/>
		/// </summary>
		public Rune CheckedRune { get; set; } = new Rune (Application.Driver != null ? Application.Driver.Checked : '√');

		/// <summary>
		/// Gets or sets the character to use for UnChecked entries. Defaults to <see cref="ConsoleDriver.UnChecked"/>
		/// </summary>
		public Rune UnCheckedRune { get; set; } = new Rune (Application.Driver != null ? Application.Driver.UnChecked : '╴');

		/// <summary>
		/// Gets the <see cref="ITableSource"/> that this instance is wrapping.
		/// </summary>
		public ITableSource Wrapping { get; }


		/// <inheritdoc/>
		public object this [int row, int col] {
			get {
				if (col == 0) {
					return IsChecked(row) ? CheckedRune : UnCheckedRune;
				}

				return Wrapping [row, col - 1];
			}
		}


		/// <inheritdoc/>
		public int Rows => Wrapping.Rows;

		/// <inheritdoc/>
		public int Columns => Wrapping.Columns + 1;

		/// <inheritdoc/>
		public string [] ColumnNames {
			get {
				var toReturn = Wrapping.ColumnNames.ToList ();
				toReturn.Insert (0, " ");
				return toReturn.ToArray ();
			}
		}

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

		protected abstract bool IsChecked (int row);

		protected abstract void ToggleRows (int [] range);

		protected abstract void ToggleRow (int row);

		protected abstract void ToggleAllRows ();
	}
}
