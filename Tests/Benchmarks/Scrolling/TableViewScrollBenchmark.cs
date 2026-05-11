using System.Data;
using System.Drawing;
using BenchmarkDotNet.Attributes;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.Testing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Terminal.Gui.Benchmarks.Scrolling;

/// <summary>
///     Measures end-to-end scrolling performance for <see cref="TableView"/>.
///     Covers cell rendering, column alignment, and header drawing at varying row counts.
/// </summary>
/// <remarks>
///     <para>
///         Run:
///         <code>dotnet run --project Tests/Benchmarks -c Release -- --filter '*TableViewScroll*'</code>
///     </para>
/// </remarks>
[MemoryDiagnoser]
[BenchmarkCategory ("Scrolling", "TableView")]
public class TableViewScrollBenchmark
{
    private const int ColumnCount = 10;
    private const int ScreenWidth = 120;
    private const int ScreenHeight = 25;

    private IApplication _app = null!;
    private IInputInjector _injector = null!;
    private Runnable _runnable = null!;
    private SessionToken? _session;
    private TableView _tableView = null!;

    /// <summary>Total number of data rows loaded into the <see cref="TableView"/>.</summary>
    [Params (100, 1_000)]
    public int Rows { get; set; }

    /// <summary>Creates the application, builds the data table, and warms up the view.</summary>
    [GlobalSetup]
    public void Setup ()
    {
        _app = Application.Create ();
        _app.Init (DriverRegistry.Names.ANSI);
        _app.Driver!.SetScreenSize (ScreenWidth, ScreenHeight);

        _runnable = new () { Width = ScreenWidth, Height = ScreenHeight };
        _session = _app.Begin (_runnable);

        DataTable dt = BuildDataTable (Rows, ColumnCount);
        _tableView = new (new DataTableSource (dt))
        {
            X = 0,
            Y = 0,
            Width = ScreenWidth,
            Height = ScreenHeight
        };
        _runnable.Add (_tableView);

        // Focus so key bindings resolve.
        _tableView.SetFocus ();

        // Cache injector to avoid per-call lookup overhead.
        _injector = _app.GetInputInjector ();

        // Warm up.
        _app.LayoutAndDraw (true);
    }

    /// <summary>
    ///     Positions the selected row at the last visible data row so the next down-arrow scrolls.
    /// </summary>
    [IterationSetup]
    public void IterationSetup ()
    {
        // TableView reserves row 0 for the header; data rows start at display row 1.
        // Place selection at the last visible data row.
        int visibleDataRows = ScreenHeight - 1; // subtract header row
        _tableView.RowOffset = 0;
        _tableView.Value = new TableSelection (new Point (0, Math.Min (visibleDataRows - 1, Rows - 1)));
        _tableView.SetNeedsDraw ();
    }

    /// <summary>
    ///     Injects a single <see cref="Key.CursorDown"/> keystroke and redraws.
    ///     With the selection at the viewport boundary this triggers a row scroll.
    /// </summary>
    [Benchmark (Baseline = true)]
    public void ScrollDown_OneStep ()
    {
        _injector.InjectKey (Key.CursorDown);
        _app.LayoutAndDraw ();
    }

    /// <summary>
    ///     Injects a single <see cref="Key.CursorUp"/> keystroke and redraws.
    /// </summary>
    [Benchmark]
    public void ScrollUp_OneStep ()
    {
        _injector.InjectKey (Key.CursorUp);
        _app.LayoutAndDraw ();
    }

    /// <summary>
    ///     Injects a single <see cref="Key.PageDown"/> keystroke and redraws.
    /// </summary>
    [Benchmark]
    public void PageDown_OneStep ()
    {
        _injector.InjectKey (Key.PageDown);
        _app.LayoutAndDraw ();
    }

    /// <summary>
    ///     Injects a single <see cref="Key.CursorRight"/> keystroke and redraws.
    ///     Measures horizontal scrolling / column navigation.
    /// </summary>
    [Benchmark]
    public void ScrollRight_OneStep ()
    {
        _injector.InjectKey (Key.CursorRight);
        _app.LayoutAndDraw ();
    }

    /// <summary>Disposes the application after all iterations.</summary>
    [GlobalCleanup]
    public void Cleanup ()
    {
        if (_session is not null)
        {
            _app.End (_session);
        }

        _app.Dispose ();
    }

    private static DataTable BuildDataTable (int rows, int cols)
    {
        DataTable dt = new ();

        for (var colIndex = 0; colIndex < cols; colIndex++)
        {
            dt.Columns.Add ($"Col{colIndex,2}", typeof (string));
        }

        for (var rowIndex = 0; rowIndex < rows; rowIndex++)
        {
            object [] row = new object [cols];

            for (var colIndex = 0; colIndex < cols; colIndex++)
            {
                row [colIndex] = $"R{rowIndex}C{colIndex}";
            }

            dt.Rows.Add (row);
        }

        return dt;
    }
}
