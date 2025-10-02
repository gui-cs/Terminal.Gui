using System.Diagnostics;
using UICatalog;
using UnitTests;
using Xunit.Abstractions;

namespace StressTests;

public class ScenariosStressTests : TestsAllViews
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

        ConfigurationManager.Disable();

        // If a previous test failed, this will ensure that the Application is in a clean state
        Application.ResetState (true);

        uint maxIterations = 1000;
        uint abortTime = 2000;
        object? timeout = null;

        var iterationCount = 0;
        var clearedContentCount = 0;
        var refreshedCount = 0;
        var updatedCount = 0;
        var drawCompleteCount = 0;

        var addedCount = 0;
        var laidOutCount = 0;

        _output.WriteLine ($"Running Scenario '{scenarioType}'");
        var scenario = (Scenario)Activator.CreateInstance (scenarioType)!;

        Stopwatch? stopwatch = null;

        Application.InitializedChanged += OnApplicationOnInitializedChanged;
        Application.ForceDriver = "FakeDriver";
        scenario!.Main ();
        scenario.Dispose ();
        scenario = null;
        Application.ForceDriver = string.Empty;
        Application.InitializedChanged -= OnApplicationOnInitializedChanged;

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

        void OnApplicationOnInitializedChanged (object? s, EventArgs<bool> a)
        {
            if (a.Value)
            {
                lock (_timeoutLock)
                {
                    timeout = Application.AddTimeout (TimeSpan.FromMilliseconds (abortTime), ForceCloseCallback);
                }

                Application.Iteration += OnApplicationOnIteration;
                Application.Driver!.ClearedContents += (sender, args) => clearedContentCount++;

                if (Application.Driver is ConsoleDriver cd)
                {
                    cd!.Refreshed += (sender, args) =>
                                     {
                                         refreshedCount++;

                                         if (args.Value)
                                         {
                                             updatedCount++;
                                         }
                                     };
                }

                Application.NotifyNewRunState += OnApplicationNotifyNewRunState;

                stopwatch = Stopwatch.StartNew ();
            }
            else
            {
                Application.NotifyNewRunState -= OnApplicationNotifyNewRunState;
                Application.Iteration -= OnApplicationOnIteration;
                stopwatch!.Stop ();
            }

            _output.WriteLine ($"Initialized == {a.Value}");
        }

        void OnApplicationOnIteration (object? s, IterationEventArgs a)
        {
            iterationCount++;

            if (iterationCount > maxIterations)
            {
                // Press QuitKey
                _output.WriteLine ("Attempting to quit scenario with RequestStop");
                Application.RequestStop ();
            }
        }

        void OnApplicationNotifyNewRunState (object? sender, RunStateEventArgs e)
        {
            // Get a list of all subviews under Application.Top (and their subviews, etc.)
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

            SubscribeAllSubViews (Application.Top!);
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

            Application.RequestStop ();

            return false;
        }
    }

    public static IEnumerable<object []> AllScenarioTypes =>
        typeof (Scenario).Assembly
                         .GetTypes ()
                         .Where (type => type.IsClass && !type.IsAbstract && type.IsSubclassOf (typeof (Scenario)))
                         .Select (type => new object [] { type });
}
