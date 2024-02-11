namespace Terminal.Gui;

/// <summary>Collection navigator for cycling selections in a <see cref="TableView"/>.</summary>
public class TableCollectionNavigator : CollectionNavigatorBase
{
    /// <summary>Creates a new instance for navigating the data in the wrapped <paramref name="tableView"/>.</summary>
    public TableCollectionNavigator (TableView tableView) { this.tableView = tableView; }

    private readonly TableView tableView;

    /// <inheritdoc/>
    protected override object ElementAt (int idx)
    {
        int col = tableView.SelectedColumn;
        object rawValue = tableView.Table [idx, col];

        ColumnStyle style = tableView.Style.GetColumnStyleIfAny (col);

        return style?.RepresentationGetter?.Invoke (rawValue) ?? rawValue;
    }

    /// <inheritdoc/>
    protected override int GetCollectionLength () { return tableView.Table.Rows; }
}
