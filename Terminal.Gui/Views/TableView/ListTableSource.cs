using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace Terminal.Gui {
	/// <summary>
	/// <see cref="ITableSource"/> implementation that wraps 
	/// a <see cref="System.Collections.IList"/>.  This class is
	/// mutable: changes are permitted to the wrapped <see cref="IList"/>.
	/// </summary>
	public class ListTableSource : ITableSource
	{
		/// <summary>
		/// The list this source wraps.
		/// </summary>
		public IList List;

		/// <summary>
		/// The data table this source creates.
		/// </summary>
		public DataTable DataTable;

		/// <summary>
		/// Creates a new table instance based on the data in <paramref name="list"/>.
		/// </summary>
		/// <param name="list"></param>
		public ListTableSource (IList list)
		{
			this.List = list;
			this.DataTable = CreateTable ();
		}

		/// <summary>
		/// Creates a new table instance based on the data in <paramref name="list"/>.
		/// </summary>
		/// <param name="list"></param>
		/// <param name="cols"></param>
		/// <param name="transpose"></param>
		public ListTableSource (IList list, int cols = 1, bool transpose = false)
		{
			this.List = list;
			this.DataTable = transpose ? CreateTransposedTable (cols) : CreateTable (cols);
		}

		/*
		/// <inheritdoc/>
		public object this [int cell] => list [cell];
		*/

		/// <inheritdoc/>
		public object this [int row, int col] => DataTable.Rows[row][col];

		/// <inheritdoc/>
		public int Count => List.Count;

		/// <inheritdoc/>
		public int Rows => DataTable.Rows.Count;

		/// <inheritdoc/>
		public int Columns => DataTable.Columns.Count;

		/// <inheritdoc/>
		public string [] ColumnNames => GenerateColumnNumbers ();

		private string [] GenerateColumnNumbers()
		{
			string [] names = new string [Count];
			for (int i = 0; i < Count; i++) {
				names [i] = i.ToString ();
			}
			return names;
		}

		/// <summary>
		/// Creates a DataTable from an IList to display in a <see cref="TableView"/>
		/// </summary>
		protected DataTable CreateTable (int cols = 1)
		{
			var newTable = new DataTable ();

			int i;
			for (i = 0; i < cols; i++) {
				newTable.Columns.Add (new DataColumn (i.ToString ()));
			}

			var sublist = new List<string> ();
			int j;
			for (j = 0; j < List.Count; j++) {
				if (j % cols == 0 && sublist.Count > 0) {
					newTable.Rows.Add (sublist.ToArray ());
					sublist.Clear ();
				}
				sublist.Add (List [j].ToString ());
			}
			newTable.Rows.Add (sublist.ToArray ());

			return newTable;
		}

		private DataTable CreateTransposedTable (int cols = 1, bool includeColumnNames = false)
		{
			var it = CreateTable (cols);
			var tt = new DataTable ();
			int offset = 0;
			if (includeColumnNames) {
				tt.Columns.Add (new DataColumn ("0"));
				offset++;
			}
			for (int i = 0; i < it.Columns.Count; i++) {
				DataRow row = tt.NewRow ();
				if (includeColumnNames) {
					row [0] = it.Columns [i].ColumnName;
				}
				for (int j = offset; j < it.Rows.Count + offset; j++) {
					if (tt.Columns.Count < it.Rows.Count + offset)
						tt.Columns.Add (new DataColumn (j.ToString ()));
					row [j] = it.Rows [j - offset] [i];
				}
				tt.Rows.Add (row);
			}
			return tt;
		}
	}
}
