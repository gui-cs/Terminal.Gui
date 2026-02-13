using System;
using System.Collections.Generic;
using System.Diagnostics;
using Terminal.Gui.Views;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("TableViewTest", "Demonstrates and tests TableView.")]
[ScenarioCategory ("TableView")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Dialogs")]

public class TableViewTest : Scenario
{
    private TableView tableView;
    private View optionsView;

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);

        using IApplication app = Application.Create ();
        app.Init ();

        using Window win = new ()
        {
            Title = GetName (),
            Y = 1, // menu
            Height = Dim.Fill (1) // status bar
        };

        optionsView = new View ()
        {
            Y = 0, X = 0,
            Width = Dim.Fill (), Height = Dim.Auto(),
            BorderStyle = LineStyle.Single,
            Title = "Options",
        };

        View.Diagnostics = ViewDiagnosticFlags.DrawIndicator;

        var offsetLabel = new Label ()
        {
            X = 0, Y = Pos.Bottom (optionsView),
            Text = "Offset",
        };

        var colOffsetUpDown = new NumericUpDown<int> ()
        {
            X = Pos.Right (offsetLabel), Y = Pos.Bottom (optionsView),
        };
        colOffsetUpDown.Padding.Thickness = new Thickness (1, 0, 1, 0);

        var setColOffsetButton = new Button ()
        {
            X = Pos.Right (colOffsetUpDown), Y = Pos.Bottom (optionsView),
            Text = "Set",
        };
        setColOffsetButton.Padding.Thickness = new Thickness (1,0,1,0);
        setColOffsetButton.Accepting += (sender, args) => tableView.ColumnOffset = colOffsetUpDown.Value;

        var rowOffsetUpDown = new NumericUpDown<int> ()
        {
            X = Pos.Right (setColOffsetButton), Y = Pos.Bottom (optionsView),
        };
        rowOffsetUpDown.Padding.Thickness = new Thickness (1, 0, 1, 0);

        var setRowOffsetButton = new Button ()
        {
            X = Pos.Right (rowOffsetUpDown), Y = Pos.Bottom (optionsView),
            Text = "Set",
        };
        setRowOffsetButton.Padding.Thickness = new Thickness (1, 0, 1, 0);
        setRowOffsetButton.Accepting += (sender, args) => tableView.RowOffset = rowOffsetUpDown.Value;

        tableView = new TableView
        {
            X = 0, Y = Pos.Bottom(offsetLabel),
            Width = Dim.Fill (), Height = Dim.Fill (),

            Table = new DataTableSource (TableView.BuildDemoDataTable (6, 30))
        };

        tableView.DrawComplete += (sender, args) => offsetLabel.Text = $"{tableView.ColumnOffset} - {tableView.RowOffset}  {tableView.Viewport.Location}";

        tableView.Style.ColumnStyles [2] = new ColumnStyle () {Alignment = Alignment.End};
        tableView.Style.ColumnStyles [6] = new ColumnStyle ();

        (string text, Func<bool> iv, Action<bool> hndlr) [] options =
        [
            ("Scrollbars Auto", () => tableView.HorizontalScrollBar.AutoShow, b => { tableView.HorizontalScrollBar.AutoShow = b; tableView.VerticalScrollBar.AutoShow = b; }),
            ("AlwaysShowHeaders", () => tableView.Style.AlwaysShowHeaders, b => tableView.Style.AlwaysShowHeaders = b),
            ("ShowHeaders", () => tableView.Style.ShowHeaders, b => tableView.Style.ShowHeaders = b),
            ("ShowHorizontalHeaderOverline", () => tableView.Style.ShowHorizontalHeaderOverline, b => tableView.Style.ShowHorizontalHeaderOverline = b),
            ("ShowVerticalHeaderLines", () => tableView.Style.ShowVerticalHeaderLines, b => tableView.Style.ShowVerticalHeaderLines = b),
            ("ShowHorizontalHeaderUnderline", () => tableView.Style.ShowHorizontalHeaderUnderline, b => tableView.Style.ShowHorizontalHeaderUnderline = b),
            ("ShowVerticalCellLines", () => tableView.Style.ShowVerticalCellLines, b => tableView.Style.ShowVerticalCellLines = b),
            ("InvertSelectedCellFirstCharacter", () => tableView.Style.InvertSelectedCellFirstCharacter, b => tableView.Style.InvertSelectedCellFirstCharacter = b),
            ("ShowHorizontalBottomline", () => tableView.Style.ShowHorizontalBottomline, b => tableView.Style.ShowHorizontalBottomline = b),
            ("ExpandLastColumn", () => tableView.Style.ExpandLastColumn, b => tableView.Style.ExpandLastColumn = b),
            ("FullRowSelect", () => tableView.FullRowSelect, b => tableView.FullRowSelect = b),
            ("SmoothHorizontalScrolling", () => tableView.Style.SmoothHorizontalScrolling, b => tableView.Style.SmoothHorizontalScrolling = b),
            ("UseAllRowsForContentCalculation", () => tableView.UseAllRowsForContentCalculation, b => tableView.UseAllRowsForContentCalculation = b),
            ("MinAcceptableWidth (limit col 6 = 15)", () => tableView.Style.ColumnStyles[6].MinAcceptableWidth < TableView.DEFAULT_MIN_ACCEPTABLE_WIDTH, b => tableView.Style.ColumnStyles[6].MinAcceptableWidth = b ? 15 : TableView.DEFAULT_MIN_ACCEPTABLE_WIDTH),
        ];

        View? priorView = null;

        foreach ((string text, Func<bool> iv, Action<bool> hndlr) tuple in options)
        {
            CheckBox cb = new CheckBox()
            {
                X = 0, Y = priorView != null ? Pos.Bottom(priorView) : 0,
                Text = tuple.text,
                Value = tuple.iv () ? CheckState.Checked : CheckState.UnChecked,
            };

            cb.ValueChanged += (s, e) =>
                               {
                                   tuple.hndlr (cb.Value == CheckState.Checked);

                                   //ToDo: Investigate why this is needed to refresh the TableView layout
                                   // without it some changes do not reflect until the next user interaction
                                   // some cases here might work, but only because a redraw is forced when Clicking the checkbox
                                   // which seems to be not correct! Changing the checkbox should redraw the checkbox, but not all views
                                   tableView.Update();
                               };
            priorView = cb;
            optionsView.Add (cb);
        }



        win.Add (optionsView, offsetLabel, colOffsetUpDown, setColOffsetButton, rowOffsetUpDown, setRowOffsetButton, tableView);

        app.Run (win);
    }
}

public class RedrawLabel : View
{
    int redrawCount = 0;

    ///// <inheritdoc />
    //public override string Text
    //{
    //    get => redrawCount.ToString ();
    //    set
    //    {
    //        base.Text = value;
    //    }
    //}

    /// <inheritdoc />
    //protected override bool OnDrawingContent (DrawContext context)
    //{
    //    redrawCount++;
    //    return base.OnDrawingContent (context);
    //}
    /// <inheritdoc />
    protected override bool OnDrawingContent (DrawContext context)
    {
        base.OnDrawingContent (context);

        redrawCount++;
        Move (0, 0);
        AddStr ($"Draws:{redrawCount}");

        return true;
    }
}
