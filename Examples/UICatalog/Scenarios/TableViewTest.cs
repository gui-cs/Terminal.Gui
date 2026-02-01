using System;
using System.Collections.Generic;
using System.Diagnostics;

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
            Title = "Options"
        };

        tableView = new TableView
        {
            //X = 0, Y = Pos.Bottom(optionsView),
            //Width = Dim.Fill (), Height = Dim.Fill (),
            X = 0, Y = 17,
            Width = Dim.Fill (), Height = Dim.Fill (),
            Table = new DataTableSource (TableView.BuildDemoDataTable (5, 30))
        };

        tableView.Style.ColumnStyles [2] = new ColumnStyle () {Alignment = Alignment.End};
        tableView.Style.ColumnStyles [6] = new ColumnStyle ();

        (string text, Func<bool> iv, Action<bool> hndlr) [] options =
        [
            ("UseScrollbars", () => tableView.UseScrollbars, b => tableView.UseScrollbars = b),
            ("Scrollbars Auto", () => tableView.HorizontalScrollBar.AutoShow, b => { tableView.HorizontalScrollBar.AutoShow = b; tableView.VerticalScrollBar.AutoShow = b; }),
            ("AlwaysShowHeaders", () => tableView.Style.AlwaysShowHeaders, b => tableView.Style.AlwaysShowHeaders = b),
            ("ShowHeaders", () => tableView.Style.ShowHeaders, b => tableView.Style.ShowHeaders = b),
            ("ShowHorizontalHeaderOverline", () => tableView.Style.ShowHorizontalHeaderOverline, b => tableView.Style.ShowHorizontalHeaderOverline = b),
            ("ShowVerticalHeaderLines", () => tableView.Style.ShowVerticalHeaderLines, b => tableView.Style.ShowVerticalHeaderLines = b),
            ("ShowHorizontalHeaderUnderline", () => tableView.Style.ShowHorizontalHeaderUnderline, b => tableView.Style.ShowHorizontalHeaderUnderline = b),
            ("ShowVerticalCellLines", () => tableView.Style.ShowVerticalCellLines, b => tableView.Style.ShowVerticalCellLines = b),
            ("InvertSelectedCellFirstCharacter", () => tableView.Style.InvertSelectedCellFirstCharacter, b => tableView.Style.InvertSelectedCellFirstCharacter = b),
            ("ShowHorizontalBottomline", () => tableView.Style.ShowHorizontalBottomline, b => tableView.Style.ShowHorizontalBottomline = b),
            ("FullRowSelect", () => tableView.FullRowSelect, b => tableView.FullRowSelect = b),
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
            cb.ValueChanged += (s, e) => tuple.hndlr (cb.Value == CheckState.Checked);
            priorView = cb;
            optionsView.Add (cb);
        }

        RedrawLabel redrawLabel = new RedrawLabel()
        {
            X = 0, Y = 15,
            Width = 10,
            Height = 1,
        };

        win.Add (optionsView, redrawLabel, tableView);

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
