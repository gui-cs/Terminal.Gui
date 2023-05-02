using NStack;
using System;
using System.Collections;

namespace Terminal.Gui {

	/// <summary>
	/// View for columned data based on an <see cref="IList"/>.
	/// 
	/// <a href="https://gui-cs.github.io/Terminal.Gui/articles/tableview.html">See TableView Deep Dive for more information</a>.
	/// </summary>
	public partial class ListColumnView : TableView {

		private IList listData;

		/// <summary>
		/// Returns the number of elements to display
		/// </summary>
		public int Count { get => ListData.Count; }

		/// <summary>
		/// Returns the maximum length of elements to display
		/// </summary>
		public int Length { get => CalculateMaxLength (); }

		private ListColumnStyle listStyle = new ListColumnStyle ();

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
		/// <param name="list">The list of items to display in columns in the control</param>
		public ListColumnView (ListTableSource list) : this ()
		{
			for (var i = 0; i < list.Rows; i++) {
				this.ListData.Add (list [i, 0]);
			}
			this.Table = (ITableSource)list.DataTable;
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
		private int CalculateMaxLength ()
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
		/// Creates a columned list from an IList to display in a <see cref="TableView"/>
		/// </summary>
		public void FlowTable ()
		{
			if (listData == null || listData?.Count == 0) {
				return;
			}
			int colWidth = CalculateMaxLength ();
			if (colWidth == 0) {
				return;
			}

			if (colWidth > MaxCellWidth) {
				colWidth = MaxCellWidth;
			}

			if (MinCellWidth > 0 && colWidth < MinCellWidth) {
				if (MinCellWidth > MaxCellWidth) {
					colWidth = MaxCellWidth;
				} else {
					colWidth = MinCellWidth;
				}
			}

			int itemsPerSublist = 1;
			if (listStyle.VerticalOrientation && listStyle.ScrollParallel) {
				decimal m = ((decimal)(this.Bounds.Width - 1) / colWidth) - 2;
				itemsPerSublist = (int)Math.Ceiling (listData.Count / m);
			} else if (listStyle.VerticalOrientation && !listStyle.ScrollParallel) {
				itemsPerSublist = this.Bounds.Height - GetHeaderHeight ();
			} else if (!listStyle.VerticalOrientation && listStyle.ScrollParallel) {
				decimal m = (decimal)this.Bounds.Height - GetHeaderHeight ();
				itemsPerSublist = (int)Math.Ceiling (listData.Count / m);
			} else if (!listStyle.VerticalOrientation && !listStyle.ScrollParallel) {
				itemsPerSublist = ((int)Math.Ceiling (((decimal)this.Bounds.Width - 1) / colWidth)) - 2;
			}
			if (itemsPerSublist < 1) itemsPerSublist = 1;

			if (listStyle.VerticalOrientation) {
				this.Table = new ListTableSource (listData, itemsPerSublist, true);
			} else {
				this.Table = new ListTableSource (listData, itemsPerSublist, false);
			}
		}

		/// <summary>
		/// Updates the view to reflect changes to <see cref="ListData"/>
		/// </summary>
		public void ListUpdate ()
		{
			FlowTable ();
			Update ();
		}

		/// TODO: Update TableView Deep Dive
		/// <summary>
		/// Defines rendering options that affect how the view is displayed.
		/// 
		/// <a href="https://gui-cs.github.io/Terminal.Gui/articles/tableview.html">See TableView Deep Dive for more information</a>.
		/// </summary>
		public class ListColumnStyle {

			/// <summary>
			/// Gets or sets a flag indicating whether to populate data of a <see cref="ListColumnView"/> down its columns rather than across its rows.
			/// Defaults to <see langword="false"/>.
			/// </summary>
			public bool VerticalOrientation { get; set; } = false;

			/// <summary>
			/// Gets or sets a flag indicating whether to scroll a <see cref="ListColumnView"/> in the same direction as item population.
			/// Defaults to <see langword="false"/>.
			/// </summary>
			public bool ScrollParallel { get; set; } = false;
		}
	}
}
