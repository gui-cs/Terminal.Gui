using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Terminal.Gui.ViewsTests;

[TestSubject (typeof (TableView))]
public class TableViewTests
{
    [Fact]
    public void TableView_CollectionNavigatorMatcher_KeybindingsOverrideNavigator ()
    {
        var dt = new DataTable ();
        dt.Columns.Add ("blah");

        dt.Rows.Add ("apricot");
        dt.Rows.Add ("arm");
        dt.Rows.Add ("bat");
        dt.Rows.Add ("batman");
        dt.Rows.Add ("bates hotel");
        dt.Rows.Add ("candle");

        var tableView = new TableView ();
        tableView.Table = new DataTableSource (dt);
        tableView.HasFocus = true;
        tableView.KeyBindings.Add (Key.B, Command.Down);

        Assert.Equal (0, tableView.SelectedRow);

        // Keys should be consumed to move down the navigation i.e. to apricot
        Assert.True (tableView.NewKeyDownEvent (Key.B));
        Assert.Equal (1, tableView.SelectedRow);

        Assert.True (tableView.NewKeyDownEvent (Key.B));
        Assert.Equal (2, tableView.SelectedRow);

        // There is no keybinding for Key.C so it hits collection navigator i.e. we jump to candle
        Assert.True (tableView.NewKeyDownEvent (Key.C));
        Assert.Equal (5, tableView.SelectedRow);
    }
}
