namespace Terminal.Gui.Views;

/// <summary>Collection navigator for cycling selections in a <see cref="TableView"/>.</summary>
internal class TableCollectionNavigator : CollectionNavigatorBase
{
    private readonly TableView _tableView;

    /// <summary>Creates a new instance for navigating the data in the wrapped <paramref name="tableView"/>.</summary>
    public TableCollectionNavigator (TableView tableView) { this._tableView = tableView; }

    /// <inheritdoc/>
    protected override object ElementAt (int idx)
    {
        int col = _tableView.FullRowSelect ? 0 : _tableView.SelectedColumn;
        object rawValue = _tableView.Table [idx, col];

        ColumnStyle style = _tableView.Style.GetColumnStyleIfAny (col);

        return style?.RepresentationGetter?.Invoke (rawValue) ?? rawValue;
    }

    /// <inheritdoc/>
    protected override int GetCollectionLength () { return _tableView.Table.Rows; }
}
