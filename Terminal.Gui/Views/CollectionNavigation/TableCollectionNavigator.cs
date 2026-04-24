namespace Terminal.Gui.Views;

/// <summary>Collection navigator for cycling selections in a <see cref="TableView"/>.</summary>
internal class TableCollectionNavigator : CollectionNavigatorBase
{
    private readonly TableView _tableView;

    /// <summary>Creates a new instance for navigating the data in the wrapped <paramref name="tableView"/>.</summary>
    public TableCollectionNavigator (TableView tableView) => _tableView = tableView;

    /// <inheritdoc/>
    protected override object ElementAt (int idx)
    {
        int col = _tableView.FullRowSelect ? 0 : _tableView.Value?.Cursor.X ?? 0;
        object? rawValue = _tableView.Table? [idx, col];

        ColumnStyle? style = _tableView.Style.GetColumnStyleIfAny (col);

        if (rawValue is { })
        {
            return (style?.RepresentationGetter?.Invoke (rawValue) ?? rawValue) ?? throw new InvalidOperationException ();
        }

        throw new InvalidOperationException ();
    }

    /// <inheritdoc/>
    protected override int GetCollectionLength () => _tableView.Table?.Rows ?? throw new InvalidOperationException ();
}
