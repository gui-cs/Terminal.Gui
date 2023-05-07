using NStack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Management;

namespace Terminal.Gui {
	/// <summary>
	/// <see cref="ITableSource"/> implementation that wraps 
	/// a <see cref="System.Collections.IList"/>.  This class is
	/// mutable: changes are permitted to the wrapped <see cref="IList"/>.
	/// </summary>
	public class ListTableSource : ITableSource {
		/// <summary>
		/// The list this source wraps.
		/// </summary>
		public IList List;

		/// <summary>
		/// The style this source uses.
		/// </summary>
		public ListColumnStyle Style;

		/// <summary>
		/// The data table this source wraps.
		/// </summary>
		public DataTable DataTable { get; private set; }

		private TableView tableView;

		private Rect lastBounds;
		private int lastMaxCellWidth;
		private int lastMinCellWidth;
		private ListColumnStyle lastStyle;
		private IList lastList;

		/// <summary>
		/// Creates a new table instance based on the data in <paramref name="list"/>.
		/// </summary>
		/// <param name="tableView"></param>
		/// <param name="list"></param>
		/// <param name="style"></param>
		public ListTableSource (IList list, TableView tableView, ListColumnStyle style)
		{
			this.List = list;
			this.tableView = tableView;
			Style = style;

			this.DataTable = CreateTable (CalculateColumns ());

			// TODO: Determine the best event for this
			tableView.DrawContent += TableView_DrawContent;
		}

		/// <inheritdoc/>
		public ListTableSource (IList list, TableView tableView) : this (list, tableView, new ListColumnStyle ()) { }

		private void TableView_DrawContent (object sender, DrawEventArgs e)
		{
			if ((!tableView.Bounds.Equals (lastBounds)) ||
				tableView.MaxCellWidth != lastMaxCellWidth ||
				tableView.MinCellWidth != lastMinCellWidth ||
				Style != lastStyle ||
				this.List != lastList) {

				this.DataTable = CreateTable (CalculateColumns ());
			}
			lastBounds = tableView.Bounds;
			lastMinCellWidth = tableView.MaxCellWidth;
			lastMaxCellWidth = tableView.MaxCellWidth;
			lastStyle = Style;
			lastList = this.List;
		}

		/// <inheritdoc/>
		public object this [int row, int col] {
			get {
				int idx;
				if (Style.VerticalOrientation) {
					idx = (col * Rows) + row;
				} else {
					idx = (row * Columns) + col;
				}
				if (idx < 0 || idx >= Count) {
					return null;
				}
				return this.List [idx];
			}
		}

		/// <summary>
		/// The number of items in the IList source
		/// </summary>
		public int Count => this.List.Count;

		/// <inheritdoc/>
		public int Rows => this.DataTable.Rows.Count;

		/// <inheritdoc/>
		public int Columns => this.DataTable.Columns.Count;

		/// <inheritdoc/>
		public string [] ColumnNames => Enumerable.Range (0, Columns).Select (n => n.ToString ()).ToArray ();

		/// <summary>
		/// Creates a DataTable from an IList to display in a <see cref="TableView"/>
		/// </summary>
		private DataTable CreateTable (int cols = 1)
		{
			var table = new DataTable ();
			for (int col = 0; col < cols; col++) {
				table.Columns.Add (new DataColumn (col.ToString ()));
			}
			for (int row = 0; row < (Count / table.Columns.Count); row++) {
				table.Rows.Add ();
			}
			// return partial row
			if (Count % table.Columns.Count != 0) {
				table.Rows.Add ();
			}

			return table;
		}

		private DataTable TransposeTable (DataTable initialTable, bool includeColumnNames = false)
		{
			var newTable = new DataTable ();
			
			int offset = 0;
			if (includeColumnNames) {
				// This creates a first column to contain existing column names
				// (TODO: Allow formatting such a column like a header)
				newTable.Columns.Add (new DataColumn ("0"));
				offset++;
			}

			for (int i = 0; i < initialTable.Columns.Count; i++) {
				DataRow row = newTable.NewRow ();
				if (includeColumnNames) {
					row [0] = initialTable.Columns [i].ColumnName;
				}
				for (int j = offset; j < initialTable.Rows.Count + offset; j++) {
					if (newTable.Columns.Count < initialTable.Rows.Count + offset)
						newTable.Columns.Add (new DataColumn (j.ToString ()));
					row [j] = initialTable.Rows [j - offset] [i];
				}
				newTable.Rows.Add (row);
			}

			return newTable;
		}

		/// <summary>
		/// Returns the size in characters of the longest value read from <see cref="ListTableSource.List"/>
		/// </summary>
		/// <returns></returns>
		private int CalculateMaxLength ()
		{
			if (List == null || Count == 0) {
				return 0;
			}

			int maxLength = 0;
			foreach (var t in List) {
				int l;
				if (t is ustring u) {
					l = TextFormatter.GetTextWidth (u);
				} else if (t is string s) {
					l = s.Length;
				} else {
					l = t.ToString ().Length;
				}

				if (l > maxLength) {
					maxLength = l;
				}
			}

			return maxLength;
		}

		private int CalculateColumns ()
		{
			int cols;

			int colWidth = CalculateMaxLength ();
			if (colWidth > tableView.MaxCellWidth) {
				colWidth = tableView.MaxCellWidth;
			}

			if (tableView.MinCellWidth > 0 && colWidth < tableView.MinCellWidth) {
				if (tableView.MinCellWidth > tableView.MaxCellWidth) {
					colWidth = tableView.MaxCellWidth;
				} else {
					colWidth = tableView.MinCellWidth;
				}
			}
			if (Style.VerticalOrientation != Style.ScrollParallel) {
				float f = (float)tableView.Bounds.Height - tableView.GetHeaderHeight ();
				cols = (int)Math.Ceiling (Count / f);
			} else {
				cols = ((int)Math.Ceiling (((float)tableView.Bounds.Width - 1) / colWidth)) - 2;
			}

			return (cols > 1) ? cols : 1;
		}

		/// <summary>
		/// Defines rendering options that affect how the view is displayed.
		/// </summary>
		public class ListColumnStyle {

			/// <summary>
			/// Gets or sets a flag indicating whether to populate data down each column rather than across each row.
			/// Defaults to <see langword="false"/>.
			/// </summary>
			public bool VerticalOrientation { get; set; } = false;

			/// <summary>
			/// Gets or sets a flag indicating whether to scroll in the same direction as item population.
			/// Defaults to <see langword="false"/>.
			/// </summary>
			public bool ScrollParallel { get; set; } = false;
		}
	}
}
