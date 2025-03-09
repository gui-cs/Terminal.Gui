namespace Terminal.Gui;

/// <summary>Collection navigator for cycling selections in a <see cref="TableView"/>.</summary>
public class TableCollectionNavigator : CollectionNavigatorBase
{
    private readonly TableView tableView;

    /// <summary>Creates a new instance for navigating the data in the wrapped <paramref name="tableView"/>.</summary>
    public TableCollectionNavigator (TableView tableView) { this.tableView = tableView; }

    /// <inheritdoc/>
    protected override object ElementAt (int idx)
    {
        int col = tableView.FullRowSelect ? 0 : tableView.SelectedColumn;
        object rawValue = tableView.Table [idx, col];

        ColumnStyle style = tableView.Style.GetColumnStyleIfAny (col);

        return style?.RepresentationGetter?.Invoke (rawValue) ?? rawValue;
    }

    /// <inheritdoc/>
    protected override int GetCollectionLength () { return tableView.Table.Rows; }
}
