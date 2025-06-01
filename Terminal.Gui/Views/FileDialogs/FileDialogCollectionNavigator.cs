namespace Terminal.Gui.Views;

internal class FileDialogCollectionNavigator (FileDialog fileDialog, TableView tableView) : CollectionNavigatorBase
{
    protected override object ElementAt (int idx)
    {
        object val = FileDialogTableSource.GetRawColumnValue (
                                                              tableView.SelectedColumn,
                                                              fileDialog.State?.Children [idx]
                                                             );

        if (val is null)
        {
            return string.Empty;
        }

        return val.ToString ().Trim ('.');
    }

    protected override int GetCollectionLength () { return fileDialog.State?.Children.Length ?? 0; }
}
