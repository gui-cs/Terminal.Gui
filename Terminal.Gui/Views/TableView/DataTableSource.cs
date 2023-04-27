using System.Data;
using System.Linq;

namespace Terminal.Gui {
	/// <summary>
	/// <see cref="ITableDataSource"/> implementation that wraps 
	/// a <see cref="System.Data.DataTable"/>.  This class is
	/// mutable: changes are permitted to the wrapped <see cref="DataTable"/>.
	/// </summary>
	public class DataTableSource : ITableDataSource
	{
		private readonly DataTable table;

		/// <summary>
		/// Creates a new instance based on the data in <paramref name="table"/>.
		/// </summary>
		/// <param name="table"></param>
		public DataTableSource(DataTable table)
		{
			this.table = table;
		}

		/// <inheritdoc/>
		public object this [int row, int col] => table.Rows[row][col];

		/// <inheritdoc/>
		public int Rows => table.Rows.Count;

		/// <inheritdoc/>
		public int Columns => table.Columns.Count;

		/// <inheritdoc/>
		public string [] ColumnNames => table.Columns.Cast<DataColumn>().Select (c => c.ColumnName).ToArray ();
	}
}
