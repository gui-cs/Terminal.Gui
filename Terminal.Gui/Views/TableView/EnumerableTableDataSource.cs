using System;
using System.Collections.Generic;
using System.Linq;

namespace Terminal.Gui {

	/// <summary>
	/// <see cref="ITableDataSource"/> implementation that wraps arbitrary data.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class EnumerableTableDataSource<T> : ITableDataSource {
		private T [] data;
		private string [] cols;
		private Dictionary<string, Func<T, object>> lamdas;

		/// <summary>
		/// Creates a new instance of the class that presents <paramref name="data"/>
		/// collection as a table.
		/// </summary>
		/// <remarks>The elements of the <paramref name="data"/> collection are recorded during
		/// construction (immutable) but the properties of those objects are permitted to
		/// change.</remarks>
		/// <param name="data"></param>
		/// <param name="columnDefinitions"></param>
		public EnumerableTableDataSource (IEnumerable<T> data, Dictionary<string, Func<T, object>> columnDefinitions)
		{
			this.data = data.ToArray ();
			this.cols = columnDefinitions.Keys.ToArray ();
			this.lamdas = columnDefinitions;
		}

		/// <inheritdoc/>
		public object this [int row, int col] {
			get => this.lamdas [ColumnNames [col]] (this.data [row]);
		}

		/// <inheritdoc/>
		public int Rows => data.Length;

		/// <inheritdoc/>
		public int Columns => cols.Length;

		/// <inheritdoc/>
		public string [] ColumnNames => cols;
	}
}
