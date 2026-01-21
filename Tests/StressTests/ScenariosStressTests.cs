using System.Diagnostics;
using UICatalog;
using Xunit.Abstractions;

namespace StressTests;

public class ScenariosStressTests
{
    public ScenariosStressTests (ITestOutputHelper output)
    {
#if DEBUG_IDISPOSABLE
        View.EnableDebugIDisposableAsserts = true;
        View.Instances.Clear ();
#endif
        _output = output;
    }

    private readonly ITestOutputHelper _output;

    private object? _timeoutLock;

    /// <summary>
    ///     <para>
    ///         This runs through all Scenarios defined in UI Catalog, calling Init, Setup, and Run and measuring the perf of
    ///         each.
    ///     </para>
    /// </summary>
    [Theory]
    [MemberData (nameof (AllScenarioTypes))]
    public void All_Scenarios_Benchmark (Type scenarioType)
    {
        Assert.Null (_timeoutLock);
        _timeoutLock = new ();

        ConfigurationManager.Disable(true);

        // If a previous test failed, this will ensure that the Application is in a clean state
        Application.ResetState (true);

        uint maxIterations = 25;
        uint abortTime = 2000;
        object? timeout = null;
        IApplication? app = null;

        var iterationCount = 0;
        var clearedContentCount = 0;
        var refreshedCount = 0;
        var updatedCount = 0;
        var drawCompleteCount = 0;

        var addedCount = 0;
        var laidOutCount = 0;

        _output.WriteLine ($"Running Scenario '{scenarioType}'");
        Scenario? scenario = (Scenario)Activator.CreateInstance (scenarioType)!;

        Stopwatch? stopwatch = null;

        Application.InstanceInitialized += OnApplicationInstanceInitialized;
        Application.InstanceDisposed += OnApplicationInstanceDisposed;
        Application.ForceDriver = DriverRegistry.Names.ANSI;
        scenario!.Main ();
        scenario.Dispose ();
        scenario = null;
        Application.ForceDriver = string.Empty;
        Application.InstanceInitialized -= OnApplicationInstanceInitialized;
        Application.InstanceDisposed -= OnApplicationInstanceDisposed;

        lock (_timeoutLock)
        {
            if (timeout is { })
            {
                timeout = null;
            }
        }

        lock (_timeoutLock)
        {
            _timeoutLock = null;
        }

        _output.WriteLine ($"Scenario {scenarioType}");
        _output.WriteLine ($"  took {stopwatch!.ElapsedMilliseconds} ms to run.");
        _output.WriteLine ($"  called Driver.ClearContents {clearedContentCount} times.");
        _output.WriteLine ($"  called Driver.Refresh {refreshedCount} times.");
        _output.WriteLine ($"    which updated the screen {updatedCount} times.");
        _output.WriteLine ($"  called View.Draw {drawCompleteCount} times.");
        _output.WriteLine ($"  added {addedCount} views.");
        _output.WriteLine ($"  called View.LayoutComplete {laidOutCount} times.");

        return;

        void OnApplicationInstanceInitialized (object? s, EventArgs<IApplication> a)
        {
            app = a.Value;
            
            lock (_timeoutLock)
            {
                timeout = app.AddTimeout (TimeSpan.FromMilliseconds (abortTime), ForceCloseCallback);
            }

            app.Iteration += OnApplicationOnIteration;
            app.Driver!.ClearedContents += OnClearedContents;
            app.SessionBegun += OnApplicationSessionBegun;

            stopwatch = Stopwatch.StartNew ();
            _output.WriteLine ($"Application instance initialized");
        }

        void OnClearedContents (object? sender, EventArgs args) { clearedContentCount++; }

        void OnApplicationInstanceDisposed (object? s, EventArgs<IApplication> a)
        {
            if (a.Value is null || app is null || a.Value != app)
            {
                return;
            }

            app.Driver!.ClearedContents -= OnClearedContents;
            app.SessionBegun -= OnApplicationSessionBegun;
            app.Iteration -= OnApplicationOnIteration;
            stopwatch!.Stop ();
            _output.WriteLine ($"Application instance disposed");
        }

        void OnApplicationOnIteration (object? s, EventArgs<IApplication?> a)
        {
            iterationCount++;

            if (iterationCount > maxIterations)
            {
                // Press QuitKey
                _output.WriteLine ("Attempting to quit scenario with RequestStop");
                app?.RequestStop ();
            }
        }

        void OnApplicationSessionBegun (object? sender, SessionTokenEventArgs e)
        {
            // Get a list of all subviews under Application.TopRunnable (and their subviews, etc.)
            // and subscribe to their DrawComplete event
            void SubscribeAllSubViews (View view)
            {
                view.DrawComplete += (s, a) => drawCompleteCount++;
                view.SubViewsLaidOut += (s, a) => laidOutCount++;
                view.SuperViewChanged += (s, a) => addedCount++;

                foreach (View subview in view.SubViews)
                {
                    SubscribeAllSubViews (subview);
                }
            }

            SubscribeAllSubViews (app!.TopRunnableView!);
        }

        // If the scenario doesn't close within the abort time, this will force it to quit
        bool ForceCloseCallback ()
        {
            lock (_timeoutLock)
            {
                if (timeout is { })
                {
                    timeout = null;
                }
            }

            _output.WriteLine (
                               $"'{scenario!.GetName ()}' failed to Quit with {Application.QuitKey} after {abortTime}ms and {iterationCount} iterations. Force quit.");

            app?.RequestStop ();

            return false;
        }
    }

    public static IEnumerable<object []> AllScenarioTypes =>
        typeof (Scenario).Assembly
                         .GetTypes ()
                         .Where (type => type.IsClass && !type.IsAbstract && type.IsSubclassOf (typeof (Scenario)))
                         .Select (type => new object [] { type });
}
