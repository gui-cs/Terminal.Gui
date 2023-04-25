using NStack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using static Terminal.Gui.TableView;

namespace Terminal.Gui {

	/// <summary>
	/// View for columned data based on a <see cref="TableView"/>.
	/// 
	/// <a href="https://gui-cs.github.io/Terminal.Gui/articles/tableview.html">See TableView Deep Dive for more information</a>.
	/// </summary>
	public partial class ListColumnView : TableView {

		/// <summary>
		/// Returns the number of elements to display
		/// </summary>
		public int Count { get => ListData.Count; }

		/// <summary>
		/// Returns the maximum length of elements to display
		/// </summary>
		public int Length { get => CalculateMaxLengthListItem (); }

		private IList listData;
		private ListColumnStyle listStyle = new ListColumnStyle () {
			ShowHeaders = false,
			ShowHorizontalHeaderOverline = false,
			ShowHorizontalHeaderUnderline = false,
			ShowHorizontalBottomline = false,
		};

		/// <summary>
		/// A list to render in the view.  Setting this property automatically updates and redraws the control.
		/// </summary>
		public IList ListData { get => listData; set { listData = value; ListUpdate (); } }

		/// <summary>
		/// Contains options for changing how the table is rendered when set via <see cref="ListColumnView.ListData"/>
		/// </summary>
		public ListColumnStyle ListStyle { get => listStyle; set { listStyle = value; ListUpdate (); } }

		/// <summary>
		/// Initializes a <see cref="ListColumnView"/> class using <see cref="LayoutStyle.Computed"/> layout. 
		/// </summary>
		/// <param name="listData">The list of items to display in columns in the control</param>
		public ListColumnView (IList listData) : this ()
		{
			this.ListData = listData;
		}

		/// <summary>
		/// Initializes a <see cref="ListColumnView"/> class using <see cref="LayoutStyle.Computed"/> layout.
		/// Set the <see cref="ListData"/> property to begin editing.
		/// </summary>
		public ListColumnView () : base ()
		{
			ListData?.Clear ();
			Update ();
		}

		/// <summary>
		/// Returns the size in characters of the longest value read from <see cref="ListColumnView.ListData"/>
		/// </summary>
		/// <returns></returns>
		private int CalculateMaxLengthListItem ()
		{
			if (listData == null || listData?.Count == 0) {
				return 0;
			}

			int maxLength = 0;
			for (int i = 0; i < listData.Count; i++) {
				var t = listData [i];
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

		/// <summary>
		/// Creates a columned list from <see cref="ListColumnView.ListData"/> to display in a <see cref="ListColumnView"/>
		/// </summary>
		private void MakeTableFromListData (IList list)
		{
			if (list == null) {
				return;
			}
			int width = CalculateMaxLengthListItem ();
			if (width == 0) {
				return;
			}

			if (MinCellWidth > 0 && width < MinCellWidth) {
				if (MinCellWidth > MaxCellWidth) {
					width = MaxCellWidth;
				} else {
					width = MinCellWidth;
				}
			}

			DataTable listTable = new ();

			int itemsPerSublist = 1;
			if (listStyle.PopulateVertical && listStyle.ScrollParallel) {
				decimal m = (decimal)this.Bounds.Width / width - 1;
				itemsPerSublist = (int)Math.Ceiling (list.Count / m); ;
			} else if (listStyle.PopulateVertical && !listStyle.ScrollParallel) {
				itemsPerSublist = this.Bounds.Height - GetHeaderHeight ();
			} else if (!listStyle.PopulateVertical && listStyle.ScrollParallel) {
				decimal m = (decimal)this.Bounds.Height - GetHeaderHeight ();
				itemsPerSublist = (int)Math.Ceiling (list.Count / m);
			} else if (!listStyle.PopulateVertical && !listStyle.ScrollParallel) {
				itemsPerSublist = (int)Math.Ceiling ((decimal)this.Bounds.Width / width) - 1;
			}
			if (itemsPerSublist < 1) itemsPerSublist = 1;

			int i;
			for (i = 0; i < itemsPerSublist; i++) {
				listTable.Columns.Add (new DataColumn (i.ToString ()));
			}

			var sublist = new List<string> ();
			int j;
			for (j = 0; j < list.Count; j++) {
				if (j % itemsPerSublist == 0 && sublist.Count > 0) {
					listTable.Rows.Add (sublist.ToArray ());
					sublist.Clear ();
				}
				sublist.Add (list [j].ToString ());
			}
			listTable.Rows.Add (sublist.ToArray ());

			if (listStyle.PopulateVertical) {
				Table = GetTransposedTable (listTable);
			} else {
				Table = listTable;
			}
		}

		// <summary>
		// Interchange rows to columns and columns to rows
		// </summary>
		// <remarks>From http://codemaverick.blogspot.com/2008/02/transpose-datagrid-or-gridview-by.html</remarks>
		private DataTable GetTransposedTable (DataTable dt, bool includeColumnNames = false)
		{
			var tt = new DataTable ();
			int offset = 0;
			if (includeColumnNames) {
				tt.Columns.Add (new DataColumn ("0"));
				offset++;
			}
			for (int i = 0; i < dt.Columns.Count; i++) {
				DataRow row = tt.NewRow ();
				if (includeColumnNames) {
					row [0] = dt.Columns [i].ColumnName;
				}
				for (int j = offset; j < dt.Rows.Count + offset; j++) {
					if (tt.Columns.Count < dt.Rows.Count + offset)
						tt.Columns.Add (new DataColumn (j.ToString ()));
					row [j] = dt.Rows [j - offset] [i];
				}
				tt.Rows.Add (row);
			}
			return tt;
		}

		/// <summary>
		/// Updates the view to reflect changes to <see cref="ListData"/>
		/// </summary>
		/// <remarks>This always calls <see cref="View.SetNeedsDisplay()"/></remarks>
		public void ListUpdate ()
		{
			MakeTableFromListData (listData);
			Update ();
		}

		/// TODO: Update TableView Deep Dive
		/// <summary>
		/// Defines rendering options that affect how the view is displayed.
		/// 
		/// <a href="https://gui-cs.github.io/Terminal.Gui/articles/tableview.html">See TableView Deep Dive for more information</a>.
		/// </summary>
		public class ListColumnStyle : TableStyle {

			/// <summary>
			/// Gets or sets a flag indicating whether to populate data of a <see cref="ListColumnView"/> down its columns rather than across its rows.
			/// Defaults to <see langword="false"/>.
			/// </summary>
			public bool PopulateVertical { get; set; } = false;

			/// <summary>
			/// Gets or sets a flag indicating whether to scroll a <see cref="ListColumnView"/> in the same direction as item population.
			/// Defaults to <see langword="false"/>.
			/// </summary>
			public bool ScrollParallel { get; set; } = false;
		}
	}
}
