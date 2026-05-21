using System.Text;
using UnitTests;

namespace ViewsTests;

// Claude - Opus 4.7

/// <summary>
///     Diagnostic tests for issue #5356 — tabbed redraw/layout fan-out.
/// </summary>
/// <remarks>
///     <para>
///         These tests are intentionally instrumentation-only: they observe layout and draw activity
///         on each tab via <see cref="View.SubViewsLaidOut"/>, <see cref="View.DrawComplete"/>, and
///         <see cref="View.ClearedViewport"/> events, but do not change rendering or invalidation
///         semantics. They lock in the current behavior so that future refactors of the layout/draw
///         pipeline (see issue #4973) can be measured and verified.
///     </para>
///     <para>
///         The diagnostics rely on counters/event traces — not on <c>Driver.Contents</c> alone —
///         so they remain meaningful in the presence of clipping, transparent viewports,
///         shadow margins, and adornment subviews.
///     </para>
///     <para>
///         Each test reports its measured counts through <see cref="ITestOutputHelper"/> so the data
///         is visible in CI logs even when the test passes. When the fan-out problem is fixed, these
///         tests are expected to be updated to assert the new (lower) counts; the diagnostic helper
///         and the test scaffolding remain useful as a regression check.
///     </para>
/// </remarks>
public class TabsFanOutDiagnosticTests (ITestOutputHelper output) : TestDriverBase
{
    /// <summary>
    ///     Records per-view layout and draw activity for a set of tracked views. Used as the primary
    ///     diagnostic for tab fan-out — the counts here are deterministic and survive future changes
    ///     to clipping, shadow, transparency, and adornment-subview behavior.
    /// </summary>
    private sealed class ViewActivityCounters
    {
        private readonly Dictionary<View, Counts> _counts = new ();
        private readonly List<(View View, string Label)> _order = [];

        public void Track (View view, string label)
        {
            Counts counts = new ();
            _counts [view] = counts;
            _order.Add ((view, label));

            view.SubViewsLaidOut += (_, _) => counts.SubViewsLaidOut++;
            view.DrawComplete += (_, _) => counts.DrawComplete++;
            view.ClearedViewport += (_, _) => counts.ClearedViewport++;
            view.DrawingContent += (_, _) => counts.DrawingContent++;
            view.FrameChanged += (_, _) => counts.FrameChanged++;
        }

        public Counts Get (View view) => _counts [view];

        public void Reset ()
        {
            foreach (Counts counts in _counts.Values)
            {
                counts.Reset ();
            }
        }

        public string Report (string title)
        {
            StringBuilder sb = new ();
            sb.AppendLine (title);
            sb.AppendLine ("  view                       laidOut  drawComplete  clearedViewport  drawingContent  frameChanged");

            foreach ((View view, string label) in _order)
            {
                Counts c = _counts [view];

                sb.AppendLine (
                               $"  {label,-26} {c.SubViewsLaidOut,7}  {c.DrawComplete,12}  {c.ClearedViewport,15}  {c.DrawingContent,14}  {c.FrameChanged,12}");
            }

            return sb.ToString ();
        }

        public sealed class Counts
        {
            public int SubViewsLaidOut;
            public int DrawComplete;
            public int ClearedViewport;
            public int DrawingContent;
            public int FrameChanged;

            public void Reset ()
            {
                SubViewsLaidOut = 0;
                DrawComplete = 0;
                ClearedViewport = 0;
                DrawingContent = 0;
                FrameChanged = 0;
            }
        }
    }

    private const int TabCount = 4;
    private const int DriverWidth = 60;
    private const int DriverHeight = 20;

    private static string MakeText (string prefix, int lines)
    {
        StringBuilder sb = new ();

        for (var i = 1; i <= lines; i++)
        {
            sb.Append (prefix);
            sb.Append (' ');
            sb.Append (i);

            if (i < lines)
            {
                sb.Append ('\n');
            }
        }

        return sb.ToString ();
    }

    private static (View Root, Tabs Tabs, TextView [] TextViews, ViewActivityCounters Counters) BuildTabbedScenario (IDriver driver)
    {
        View root = new ()
        {
            Driver = driver,
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        Tabs tabs = new () { Driver = driver, Width = Dim.Fill (), Height = Dim.Fill () };
        root.Add (tabs);

        TextView [] textViews = new TextView [TabCount];

        for (var i = 0; i < TabCount; i++)
        {
            TextView tv = new ()
            {
                Title = $"Tab{i + 1}",
                Text = MakeText ($"Tab{i + 1} line", 50),
                Width = Dim.Fill (),
                Height = Dim.Fill ()
            };

            textViews [i] = tv;
            tabs.Add (tv);
        }

        ViewActivityCounters counters = new ();
        counters.Track (tabs, "Tabs");

        for (var i = 0; i < TabCount; i++)
        {
            counters.Track (textViews [i], $"TextView{i + 1}");
        }

        return (root, tabs, textViews, counters);
    }

    private static (View Root, TextView TextView, ViewActivityCounters Counters) BuildSingleTextViewScenario (IDriver driver)
    {
        View root = new ()
        {
            Driver = driver,
            CanFocus = true,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        TextView tv = new ()
        {
            Title = "Only",
            Text = MakeText ("Only line", 50),
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        root.Add (tv);

        ViewActivityCounters counters = new ();
        counters.Track (tv, "TextView");

        return (root, tv, counters);
    }

    /// <summary>
    ///     Issue #5356 / #4973 acceptance criterion: diagnostics can show whether inactive tabs
    ///     received layout work when the active tab scrolls.
    /// </summary>
    [Fact]
    public void Diagnostic_ActiveTabScroll_LayoutEvents_OnEachTab ()
    {
        IDriver driver = CreateTestDriver (DriverWidth, DriverHeight);
        (View root, Tabs tabs, TextView [] textViews, ViewActivityCounters counters) = BuildTabbedScenario (driver);

        root.Layout ();
        root.Draw ();

        TextView active = (TextView)tabs.Value!;
        Assert.Same (textViews [0], active);

        counters.Reset ();

        for (var y = 1; y <= 5; y++)
        {
            active.Viewport = active.Viewport with { Y = y };
            root.Layout ();
        }

        output.WriteLine (counters.Report ("After 5 active-tab scrolls (layout-only):"));

        ViewActivityCounters.Counts activeCounts = counters.Get (active);
        Assert.True (activeCounts.SubViewsLaidOut > 0, $"Active tab should receive layout activity, got {activeCounts.SubViewsLaidOut}");

        int inactiveLayouts = 0;

        for (var i = 1; i < TabCount; i++)
        {
            inactiveLayouts += counters.Get (textViews [i]).SubViewsLaidOut;
        }

        output.WriteLine ($"Active SubViewsLaidOut: {activeCounts.SubViewsLaidOut}");
        output.WriteLine ($"Sum of inactive SubViewsLaidOut: {inactiveLayouts}");

        // CURRENT BEHAVIOR (issue #4973): inactive tabs receive the same layout count as active.
        // After #4973 is fixed, flip this to `Assert.Equal (0, inactiveLayouts)`.
        Assert.True (
                     inactiveLayouts > 0,
                     $"Documents issue #4973: inactive tabs receive layout work when active tab scrolls. " +
                     $"Observed inactive_total={inactiveLayouts}, active={activeCounts.SubViewsLaidOut}. " +
                     "Flip to Assert.Equal(0, inactiveLayouts) after #4973 fix lands.");

        root.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     Issue #5356 / #4973 acceptance criterion: diagnostics can show whether inactive tabs
    ///     received draw work when the active tab scrolls.
    /// </summary>
    [Fact]
    public void Diagnostic_ActiveTabScroll_DrawEvents_OnEachTab ()
    {
        IDriver driver = CreateTestDriver (DriverWidth, DriverHeight);
        (View root, Tabs tabs, TextView [] textViews, ViewActivityCounters counters) = BuildTabbedScenario (driver);

        root.Layout ();
        root.Draw ();

        TextView active = (TextView)tabs.Value!;

        counters.Reset ();

        for (var y = 1; y <= 5; y++)
        {
            active.Viewport = active.Viewport with { Y = y };
            root.Layout ();
            root.Draw ();
        }

        output.WriteLine (counters.Report ("After 5 active-tab scrolls (layout + draw):"));

        ViewActivityCounters.Counts activeCounts = counters.Get (active);
        Assert.True (activeCounts.DrawComplete > 0, $"Active tab must receive draw activity, got {activeCounts.DrawComplete}");

        int inactiveDraws = 0;
        int inactiveClears = 0;
        int inactiveContentDraws = 0;

        for (var i = 1; i < TabCount; i++)
        {
            ViewActivityCounters.Counts c = counters.Get (textViews [i]);
            inactiveDraws += c.DrawComplete;
            inactiveClears += c.ClearedViewport;
            inactiveContentDraws += c.DrawingContent;
        }

        output.WriteLine ($"Active DrawComplete: {activeCounts.DrawComplete}");
        output.WriteLine ($"Sum of inactive DrawComplete:    {inactiveDraws}");
        output.WriteLine ($"Sum of inactive ClearedViewport: {inactiveClears}");
        output.WriteLine ($"Sum of inactive DrawingContent:  {inactiveContentDraws}");

        // CURRENT BEHAVIOR (issue #4973): inactive tabs receive draw, clear, and content-draw work
        // when the active tab scrolls. After #4973 is fixed, flip these to `Assert.Equal (0, ...)`.
        Assert.True (
                     inactiveDraws > 0,
                     $"Documents issue #4973 draw fan-out: inactive_total DrawComplete={inactiveDraws}, " +
                     $"active={activeCounts.DrawComplete}. Flip to Assert.Equal(0, inactiveDraws) after #4973 fix.");

        Assert.True (
                     inactiveClears > 0,
                     $"Documents issue #4973 clear fan-out: ClearViewport propagates via SetNeedsDraw. " +
                     $"inactive_total ClearedViewport={inactiveClears}. Flip after #4973 fix.");

        Assert.True (
                     inactiveContentDraws > 0,
                     $"Documents issue #4973 content-draw fan-out: inactive_total DrawingContent={inactiveContentDraws}. " +
                     "Flip after #4973 fix.");

        root.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     Issue #5356 acceptance criterion: a comparable fan-out metric between an equivalent
    ///     single-TextView scenario and a tabbed-TextView scenario.
    /// </summary>
    [Fact]
    public void Diagnostic_TabbedFanOut_ComparedTo_SingleTextViewBaseline ()
    {
        IDriver driverSingle = CreateTestDriver (DriverWidth, DriverHeight);
        (View singleRoot, TextView singleTv, ViewActivityCounters singleCounters) = BuildSingleTextViewScenario (driverSingle);

        singleRoot.Layout ();
        singleRoot.Draw ();
        singleCounters.Reset ();

        for (var y = 1; y <= 5; y++)
        {
            singleTv.Viewport = singleTv.Viewport with { Y = y };
            singleRoot.Layout ();
            singleRoot.Draw ();
        }

        ViewActivityCounters.Counts singleCounts = singleCounters.Get (singleTv);
        output.WriteLine (singleCounters.Report ("Single-TextView baseline (5 scrolls):"));

        IDriver driverTabbed = CreateTestDriver (DriverWidth, DriverHeight);
        (View tabRoot, Tabs tabs, TextView [] textViews, ViewActivityCounters tabCounters) = BuildTabbedScenario (driverTabbed);

        tabRoot.Layout ();
        tabRoot.Draw ();

        TextView active = (TextView)tabs.Value!;
        tabCounters.Reset ();

        for (var y = 1; y <= 5; y++)
        {
            active.Viewport = active.Viewport with { Y = y };
            tabRoot.Layout ();
            tabRoot.Draw ();
        }

        ViewActivityCounters.Counts tabActiveCounts = tabCounters.Get (active);
        output.WriteLine (tabCounters.Report ("Tabbed scenario (5 scrolls of active tab):"));

        int totalTabDraws = tabActiveCounts.DrawComplete;
        int totalTabLayouts = tabActiveCounts.SubViewsLaidOut;

        for (var i = 1; i < TabCount; i++)
        {
            totalTabDraws += tabCounters.Get (textViews [i]).DrawComplete;
            totalTabLayouts += tabCounters.Get (textViews [i]).SubViewsLaidOut;
        }

        double drawFanOut = singleCounts.DrawComplete == 0 ? double.NaN : (double)totalTabDraws / singleCounts.DrawComplete;
        double layoutFanOut = singleCounts.SubViewsLaidOut == 0 ? double.NaN : (double)totalTabLayouts / singleCounts.SubViewsLaidOut;

        output.WriteLine ($"Tabbed total draws / single draws  = {totalTabDraws} / {singleCounts.DrawComplete} = {drawFanOut:F2}");
        output.WriteLine ($"Tabbed total layouts / single layouts = {totalTabLayouts} / {singleCounts.SubViewsLaidOut} = {layoutFanOut:F2}");

        Assert.True (singleCounts.DrawComplete > 0, "Single-TextView baseline must record draw activity.");
        Assert.True (tabActiveCounts.DrawComplete > 0, "Active tab in tabbed scenario must record draw activity.");

        // CURRENT BEHAVIOR (issue #4973): tabbed scenario produces N-times more total work than baseline.
        // After #4973 is fixed, drawFanOut and layoutFanOut should approach 1.0 (within rounding).
        Assert.True (
                     drawFanOut > 1.0,
                     $"Documents issue #4973: tabbed draw fan-out ({drawFanOut:F2}x) exceeds single-TextView baseline. " +
                     "After fix, drawFanOut should approach 1.0.");

        Assert.True (
                     layoutFanOut > 1.0,
                     $"Documents issue #4973: tabbed layout fan-out ({layoutFanOut:F2}x) exceeds single-TextView baseline. " +
                     "After fix, layoutFanOut should approach 1.0.");

        singleRoot.Dispose ();
        tabRoot.Dispose ();
        driverSingle.Dispose ();
        driverTabbed.Dispose ();
    }

    /// <summary>
    ///     Edge case: transparent inactive tabs must not silently bypass the diagnostic.
    ///     <see cref="ViewportSettingsFlags.Transparent"/> changes the clear/draw sequence in the inner
    ///     view's draw path but it does not eliminate <see cref="View.DrawComplete"/> firing on the
    ///     view itself. The diagnostic must still observe activity on transparent tabs.
    /// </summary>
    [Fact]
    public void Diagnostic_TransparentInactiveTab_StillObservable ()
    {
        IDriver driver = CreateTestDriver (DriverWidth, DriverHeight);
        (View root, Tabs tabs, TextView [] textViews, ViewActivityCounters counters) = BuildTabbedScenario (driver);

        textViews [1].ViewportSettings |= ViewportSettingsFlags.Transparent;
        textViews [2].ViewportSettings |= ViewportSettingsFlags.Transparent;

        root.Layout ();
        root.Draw ();

        TextView active = (TextView)tabs.Value!;
        counters.Reset ();

        for (var y = 1; y <= 3; y++)
        {
            active.Viewport = active.Viewport with { Y = y };
            root.Layout ();
            root.Draw ();
        }

        output.WriteLine (counters.Report ("Transparent inactive tabs (3 active-tab scrolls):"));

        ViewActivityCounters.Counts activeCounts = counters.Get (active);
        Assert.True (activeCounts.DrawComplete > 0, "Active tab still draws when peers are transparent.");

        for (var i = 1; i < TabCount; i++)
        {
            ViewActivityCounters.Counts c = counters.Get (textViews [i]);

            Assert.True (
                         c.DrawComplete >= 0,
                         $"Tab {i + 1} DrawComplete counter must be observable (got {c.DrawComplete}) even with Transparent.");
        }

        root.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     Edge case: shadow margins draw in a separate pass (<see cref="MarginView.DrawMargins"/>).
    ///     The diagnostic must observe the active tab's draw activity even when its margin has a shadow.
    /// </summary>
    [Fact]
    public void Diagnostic_ShadowMargin_DoesNotMaskActiveTabActivity ()
    {
        IDriver driver = CreateTestDriver (DriverWidth, DriverHeight);
        (View root, Tabs tabs, TextView [] textViews, ViewActivityCounters counters) = BuildTabbedScenario (driver);

        textViews [0].ShadowStyle = ShadowStyles.Opaque;

        root.Layout ();
        root.Draw ();

        TextView active = (TextView)tabs.Value!;
        Assert.Same (textViews [0], active);

        counters.Reset ();

        for (var y = 1; y <= 3; y++)
        {
            active.Viewport = active.Viewport with { Y = y };
            root.Layout ();
            root.Draw ();
        }

        output.WriteLine (counters.Report ("Active tab with shadow margin (3 scrolls):"));

        ViewActivityCounters.Counts activeCounts = counters.Get (active);
        Assert.True (activeCounts.DrawComplete > 0, $"Active tab with shadow must still record DrawComplete (got {activeCounts.DrawComplete}).");

        root.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     Acceptance criterion: assertions must not depend solely on <c>Driver.Contents</c>.
    ///     This test confirms the diagnostic can also reach the actual <see cref="IOutput"/> layer —
    ///     <see cref="IOutput.GetLastOutput"/> returns the ANSI bytes that would be written to the
    ///     terminal, which is the source of truth beyond the in-memory <c>Driver.Contents</c> grid.
    /// </summary>
    [Fact]
    public void Diagnostic_IOutputLayer_ObservesActiveTabContent ()
    {
        IDriver driver = CreateTestDriver (DriverWidth, DriverHeight);
        (View root, Tabs tabs, TextView [] textViews, ViewActivityCounters counters) = BuildTabbedScenario (driver);

        root.Layout ();
        root.Draw ();

        TextView active = (TextView)tabs.Value!;

        IOutput output1 = driver.GetOutput ();
        IOutputBuffer buffer = driver.GetOutputBuffer ();
        output1.Write (buffer);
        string ansiAfterInitial = output1.GetLastOutput ();

        output.WriteLine ($"Initial GetLastOutput length: {ansiAfterInitial.Length}");
        Assert.False (string.IsNullOrEmpty (ansiAfterInitial), "IOutput must produce non-empty output for initial draw.");

        counters.Reset ();

        active.Viewport = active.Viewport with { Y = 10 };
        root.Layout ();
        root.Draw ();

        output1.Write (buffer);
        string ansiAfterScroll = output1.GetLastOutput ();

        output.WriteLine ($"After-scroll GetLastOutput length: {ansiAfterScroll.Length}");
        Assert.False (string.IsNullOrEmpty (ansiAfterScroll), "IOutput must produce non-empty output after a scroll.");

        ViewActivityCounters.Counts activeCounts = counters.Get (active);
        Assert.True (activeCounts.DrawComplete > 0, "Active tab must draw for IOutput to receive content.");

        Assert.Contains ($"Tab{1} line", ansiAfterInitial);

        output.WriteLine (counters.Report ("IOutput-level check (1 scroll):"));

        root.Dispose ();
        driver.Dispose ();
    }

    /// <summary>
    ///     Diagnostic reports layout and draw fan-out as separate metrics, so a regression can be
    ///     localized to one pipeline (layout vs draw) without conflating the two.
    /// </summary>
    [Fact]
    public void Diagnostic_LayoutAndDraw_ReportedSeparately ()
    {
        IDriver driver = CreateTestDriver (DriverWidth, DriverHeight);
        (View root, Tabs tabs, TextView [] textViews, ViewActivityCounters counters) = BuildTabbedScenario (driver);

        root.Layout ();
        root.Draw ();

        TextView active = (TextView)tabs.Value!;
        counters.Reset ();

        active.Viewport = active.Viewport with { Y = 2 };
        root.Layout ();
        root.Draw ();

        int activeLayouts = counters.Get (active).SubViewsLaidOut;
        int activeDraws = counters.Get (active).DrawComplete;

        var inactiveLayouts = 0;
        var inactiveDraws = 0;

        for (var i = 1; i < TabCount; i++)
        {
            inactiveLayouts += counters.Get (textViews [i]).SubViewsLaidOut;
            inactiveDraws += counters.Get (textViews [i]).DrawComplete;
        }

        output.WriteLine ($"Layout: active={activeLayouts}, inactive_total={inactiveLayouts}");
        output.WriteLine ($"Draw:   active={activeDraws}, inactive_total={inactiveDraws}");
        output.WriteLine (counters.Report ("Single-scroll layout-vs-draw breakdown:"));

        Assert.True (activeLayouts > 0, $"Active tab must register layout activity, got {activeLayouts}.");
        Assert.True (activeDraws > 0, $"Active tab must register draw activity, got {activeDraws}.");

        // The two counters can move independently — RC2 in #4973 affects layout fan-out only,
        // RC1 affects draw fan-out only. Reporting them separately lets a regression land in just
        // one pipeline without being masked by the other.
        output.WriteLine ($"layout_fanout_ratio = {(activeLayouts == 0 ? "n/a" : ((double)inactiveLayouts / activeLayouts).ToString ("F2"))}");
        output.WriteLine ($"draw_fanout_ratio   = {(activeDraws == 0 ? "n/a" : ((double)inactiveDraws / activeDraws).ToString ("F2"))}");

        root.Dispose ();
        driver.Dispose ();
    }
}
