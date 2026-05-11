using BenchmarkDotNet.Attributes;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.Testing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Terminal.Gui.Benchmarks.Scrolling;

/// <summary>
///     Measures end-to-end scrolling performance for <see cref="TextView"/>.
///     Covers the most complex rendering path in Terminal.Gui: line tracking, word-wrap decisions,
///     tab expansion, selection, and caret movement.
/// </summary>
/// <remarks>
///     <para>
///         Run:
///         <code>dotnet run --project Tests/Benchmarks -c Release -- --filter '*TextViewScroll*'</code>
///     </para>
/// </remarks>
[MemoryDiagnoser]
[BenchmarkCategory ("Scrolling", "TextView")]
public class TextViewScrollBenchmark
{
    private const int ScreenWidth = 80;
    private const int ScreenHeight = 25;

    private IApplication _app = null!;
    private IInputInjector _injector = null!;
    private Runnable _runnable = null!;
    private SessionToken? _session;
    private TextView _textView = null!;

    /// <summary>Total number of lines loaded into the <see cref="TextView"/>.</summary>
    [Params (1_000, 5_000)]
    public int Lines { get; set; }

    /// <summary>Creates the application, builds the document, and warms up the view.</summary>
    [GlobalSetup]
    public void Setup ()
    {
        _app = Application.Create ();
        _app.Init (DriverRegistry.Names.ANSI);
        _app.Driver!.SetScreenSize (ScreenWidth, ScreenHeight);

        _runnable = new () { Width = ScreenWidth, Height = ScreenHeight };
        _session = _app.Begin (_runnable);

        string text = BuildText (Lines);
        _textView = new ()
        {
            X = 0,
            Y = 0,
            Width = ScreenWidth,
            Height = ScreenHeight,
            Text = text,
            WordWrap = false,
            ReadOnly = true
        };
        _runnable.Add (_textView);

        // Focus the text view so key bindings resolve to it.
        _textView.SetFocus ();

        // Cache injector to avoid per-call lookup overhead.
        _injector = _app.GetInputInjector ();

        // Warm up: prime JIT and layout caches.
        _app.LayoutAndDraw (true);
    }

    /// <summary>
    ///     Resets the caret to the last line in the first visible page so the next
    ///     <see cref="ScrollDown_OneStep"/> call triggers a viewport scroll.
    /// </summary>
    [IterationSetup]
    public void IterationSetup ()
    {
        // Place caret at bottom-right of the visible viewport so CursorDown scrolls.
        _textView.InsertionPoint = new (0, ScreenHeight - 1);
        _textView.SetNeedsDraw ();
    }

    /// <summary>
    ///     Injects a single <see cref="Key.CursorDown"/> keystroke and redraws.
    ///     With the caret at the bottom edge of the viewport this always triggers a viewport scroll.
    /// </summary>
    [Benchmark (Baseline = true)]
    public void ScrollDown_OneStep ()
    {
        _injector.InjectKey (Key.CursorDown);
        _app.LayoutAndDraw ();
    }

    /// <summary>
    ///     Injects a single <see cref="Key.CursorUp"/> keystroke and redraws.
    ///     Symmetric reverse-scroll measurement.
    /// </summary>
    [Benchmark]
    public void ScrollUp_OneStep ()
    {
        _injector.InjectKey (Key.CursorUp);
        _app.LayoutAndDraw ();
    }

    /// <summary>
    ///     Injects a single <see cref="Key.PageDown"/> keystroke and redraws.
    ///     Each iteration rebuilds a full page — measures viewport-sized jump cost.
    /// </summary>
    [Benchmark]
    public void PageDown_OneStep ()
    {
        _injector.InjectKey (Key.PageDown);
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

    private static string BuildText (int lineCount)
    {
        System.Text.StringBuilder sb = new (lineCount * 85);

        for (var lineIndex = 0; lineIndex < lineCount; lineIndex++)
        {
            // ~80-character lines matching the issue specification.
            sb.AppendLine ($"Line {lineIndex,6}: The quick brown fox jumps over the lazy dog. Extra padding {lineIndex % 10}.");
        }

        return sb.ToString ();
    }
}
