

namespace Terminal.Gui {

    /// <summary>
    /// Collection navigator for cycling selections in a <see cref="TableView"/>.
    /// </summary>
	public class TableCollectionNavigator : CollectionNavigatorBase
	{
		readonly TableView tableView;

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="Terminal.Gui.TableView.ColumnStyle.RepresentationGetter"/>
        /// should be respected (defaults to true).
        /// </summary>
        public bool RespectStyles {get;set;} = true;

        /// <summary>
        /// Creates a new instance for navigating the data in the wrapped <paramref name="tableView"/>.
        /// </summary>
		public TableCollectionNavigator(TableView tableView)
        {
			this.tableView = tableView;
		}

        /// <inheritdoc/>
		protected override object ElementAt (int idx)
		{
            var col = tableView.SelectedColumn;
			var rawValue = tableView.Table[idx,col];

            if(!RespectStyles)
            {
                return rawValue;
            }

            var style = this.tableView.Style.GetColumnStyleIfAny(col);
            return style?.RepresentationGetter?.Invoke(rawValue) ?? rawValue;
		}

        /// <inheritdoc/>
		protected override int GetCollectionLength ()
		{
			return tableView.Table.Rows;
		}
	}
}
