using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Net.Mime;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.Drivers;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace MyFirstView;

class Program
{
    static void Main (string [] args)
    {
        ConfigurationManager.Enable (ConfigLocations.All);

        IApplication app = Application.Create ();
        var drivers = DriverRegistry.GetDriverNames ().ToList();
        app.Init (drivers [1]);

        using Window exampleWindow = new Window ()
        {
            Title = "Example Window"
        };

        var viewportLabel = new Label () {X = 1, Y = 1, Text = "ViewPort"};
        var contentSizeLabel = new Label () {X = 1, Y = Pos.Bottom(viewportLabel), Text = "ContentSize"};
        var viewportLabel2 = new Label () { X = Pos.AnchorEnd(), Y = 1, Text = "ViewPort" };
        var contentSizeLabel2 = new Label () { X = Pos.AnchorEnd(), Y = Pos.Bottom (viewportLabel2), Text = "ContentSize" };
        exampleWindow.Add (viewportLabel, contentSizeLabel, viewportLabel2, contentSizeLabel2);

        var firstView = new FirstView ()
        {
            X = Pos.Absolute(0), 
            Y = Pos.Center (), 
            Width = Dim.Absolute (20), 
            Height = Dim.Absolute (20),
            BorderStyle = LineStyle.Single,
            Title = "First View",
            VerticalScrollBar = { Visible = true }
        };

        firstView.ViewportChanged += (sender, e) => viewportLabel.Text = $"ViewPort: {e.OldViewport} -> {e.NewViewport}";
        firstView.ContentSizeChanged += (sender, e) => contentSizeLabel.Text = $"ContentSize: {e.OldValue} -> {e.NewValue}";

        firstView.Count = 40;

        var tableView = new TableView ()
        {
            X = Pos.AnchorEnd(),
            Y = Pos.Center (),
            Width = Dim.Absolute (30),
            Height = Dim.Absolute (20),
            BorderStyle = LineStyle.Single,
            Title = "table View",
            VerticalScrollBar = { Visible = true },
            UseScrollbars = true,
            MultiSelect = false,
        };
        tableView.Style.ShowHorizontalBottomline = true;
        tableView.Style.AlwaysShowHeaders = true;
        tableView.Arrangement = ViewArrangement.BottomResizable | ViewArrangement.LeftResizable;

        var dataTable = new DataTable ();
        dataTable.Columns.Add ("Items");
        dataTable.Columns.Add ("Value");
        dataTable.Columns.Add ("Col3");
        dataTable.Columns.Add ("Col4");

        string DummyText(int len)
        {
            string result = string.Empty;

            for (int i = 1; i <= len; i++)
            {
                result += i % 10;
            }

            return result;
        }

        foreach (int i in Enumerable.Range (1, 40))
        {
            dataTable.Rows.Add ($"Item {i}", DummyText(i), $"Col3-{i}", $"Col4-{i}");
        }

        tableView.ViewportChanged += (sender, e) => viewportLabel2.Text = $"ViewPort: {e.OldViewport} -> {e.NewViewport}";
        tableView.ContentSizeChanged += (sender, e) => contentSizeLabel2.Text = $"ContentSize: {e.OldValue} -> {e.NewValue}";

        tableView.Table = new DataTableSource(dataTable);

        var buttonCellToScreen = new Button ()
        {
            Text = "Cell To Screen",
            X = Pos.AnchorEnd (),
            Y = Pos.Bottom (tableView) + 1,
        };

        buttonCellToScreen.Accepting += (sender, eventArgs) =>
                            {
                                var pos = tableView.CellToScreen (1, 1);
                                MessageBox.Query (app, "Test", $"Pos: {pos}");
                            };

        var buttonEnsureSelectedCellIsVisible = new Button ()
        {
            Text = "Scroll To Selected",
            X = Pos.AnchorEnd (),
            Y = Pos.Bottom (buttonCellToScreen) + 1,
        };

        buttonEnsureSelectedCellIsVisible.Accepting += (sender, eventArgs) =>
                            {
                                tableView.EnsureSelectedCellIsVisible();
                            };

        var checkBoxHeaderAlwaysVisible = new CheckBox ()
        {
            Text = "Always Show Header",
            X = Pos.Left (tableView),
            Y = Pos.Top (tableView) - 1,
            Value = CheckState.Checked
        };

        checkBoxHeaderAlwaysVisible.ValueChanged += (sender, args) =>
        {
            tableView.Style.AlwaysShowHeaders = checkBoxHeaderAlwaysVisible.Value == CheckState.Checked;
        };
        exampleWindow.Add (firstView, tableView, buttonCellToScreen, buttonEnsureSelectedCellIsVisible, checkBoxHeaderAlwaysVisible);

        string? userName = app.Run (exampleWindow) as string;


        // Shutdown the application in order to free resources and clean up the terminal
        app.Dispose ();
    }

    private static void Button_Accepting (object? sender, Terminal.Gui.Input.CommandEventArgs e)
    {
        throw new NotImplementedException ();
    }
}
