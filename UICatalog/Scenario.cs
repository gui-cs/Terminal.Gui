#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Terminal.Gui;

namespace UICatalog;

/// <summary>
///     <para>Base class for each demo/scenario.</para>
///     <para>
///         To define a new scenario:
///         <list type="number">
///             <item>
///                 <description>
///                     Create a new <c>.cs</c> file in the <cs>Scenarios</cs> directory that derives from
///                     <see cref="Scenario"/>.
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     Annotate the <see cref="Scenario"/> derived class with a
///                     <see cref="ScenarioMetadata"/> attribute specifying the scenario's name and description.
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     Add one or more <see cref="ScenarioCategory"/> attributes to the class specifying
///                     which categories the scenario belongs to. If you don't specify a category the scenario will show up
///                     in "_All".
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     Implement the <see cref="Main"/> override which will be called when a user selects the
///                     scenario to run.
///                 </description>
///             </item>
///         </list>
///     </para>
///     <para>
///         The UI Catalog program uses reflection to find all scenarios and adds them to the ListViews. Press ENTER to
///         run the selected scenario. Press the default quit key to quit.
///     </para>
/// </summary>
/// <example>
///     The example below is provided in the `Scenarios` directory as a generic sample that can be copied and re-named:
///     <code>
/// using Terminal.Gui;
/// 
/// namespace UICatalog.Scenarios;
/// 
/// [ScenarioMetadata ("Generic", "Generic sample - A template for creating new Scenarios")]
/// [ScenarioCategory ("Controls")]
/// public sealed class MyScenario : Scenario
/// {
///     public override void Main ()
///     {
///         // Init
///         Application.Init ();
/// 
///         // Setup - Create a top-level application window and configure it.
///         Window appWindow = new ()
///         {
///             Title = GetQuitKeyAndName (),
///         };
/// 
///         var button = new Button { X = Pos.Center (), Y = Pos.Center (), Text = "Press me!" };
///         button.Accept += (s, e) => MessageBox.ErrorQuery ("Error", "You pressed the button!", "Ok");
///         appWindow.Add (button);
/// 
///         // Run - Start the application.
///         Application.Run (appWindow);
///         appWindow.Dispose ();
/// 
///         // Shutdown - Calling Application.Shutdown is required.
///         Application.Shutdown ();
///     }
/// }
///  </code>
/// </example>
public class Scenario : IDisposable
{
    private static int _maxScenarioNameLen = 30;
    public string TopLevelColorScheme { get; set; } = "Base";

    public BenchmarkResults BenchmarkResults
    {
        get { return _benchmarkResults; }
    }

    private bool _disposedValue;

    /// <summary>
    ///     Helper function to get the list of categories a <see cref="Scenario"/> belongs to (defined in
    ///     <see cref="ScenarioCategory"/>)
    /// </summary>
    /// <returns>list of category names</returns>
    public List<string> GetCategories () { return ScenarioCategory.GetCategories (GetType ()); }

    /// <summary>Helper to get the <see cref="Scenario"/> Description (defined in <see cref="ScenarioMetadata"/>)</summary>
    /// <returns></returns>
    public string GetDescription () { return ScenarioMetadata.GetDescription (GetType ()); }

    /// <summary>Helper to get the <see cref="Scenario"/> Name (defined in <see cref="ScenarioMetadata"/>)</summary>
    /// <returns></returns>
    public string GetName () { return ScenarioMetadata.GetName (GetType ()); }

    /// <summary>
    ///     Helper to get the <see cref="Application.QuitKey"/> and the <see cref="Scenario"/> Name (defined in
    ///     <see cref="ScenarioMetadata"/>)
    /// </summary>
    /// <returns></returns>
    public string GetQuitKeyAndName () { return $"{Application.QuitKey} to Quit - Scenario: {GetName ()}"; }

    /// <summary>
    ///     Returns a list of all <see cref="Scenario"/> instanaces defined in the project, sorted by
    ///     <see cref="ScenarioMetadata.Name"/>.
    ///     https://stackoverflow.com/questions/5411694/get-all-inherited-classes-of-an-abstract-class
    /// </summary>
    public static ObservableCollection<Scenario> GetScenarios ()
    {
        List<Scenario> objects = [];

        foreach (Type type in typeof (Scenario).Assembly.ExportedTypes
                                               .Where (
                                                       myType => myType is { IsClass: true, IsAbstract: false }
                                                                 && myType.IsSubclassOf (typeof (Scenario))
                                                      ))
        {
            if (Activator.CreateInstance (type) is not Scenario { } scenario)
            {
                continue;
            }

            objects.Add (scenario);
            _maxScenarioNameLen = Math.Max (_maxScenarioNameLen, scenario.GetName ().Length + 1);
        }

        return new (objects.OrderBy (s => s.GetName ()).ToList ());
    }

    /// <summary>
    ///     Called by UI Catalog to run the <see cref="Scenario"/>. This is the main entry point for the <see cref="Scenario"/>
    ///     .
    /// </summary>
    public virtual void Main () { }

    private const uint BENCHMARK_MAX_NATURAL_ITERATIONS = 500; // not including needed for demo keys
    private const int BENCHMARK_KEY_PACING = 10; // Must be non-zero

    public static uint BenchmarkTimeout { get; set; } = 2500;

    private readonly object _timeoutLock = new ();
    private object? _timeout;
    private Stopwatch? _stopwatch;
    private readonly BenchmarkResults _benchmarkResults = new ();

    public void StartBenchmark ()
    {
        BenchmarkResults.Scenario = GetName ();
        Application.InitializedChanged += OnApplicationOnInitializedChanged;
    }

    public BenchmarkResults EndBenchmark ()
    {
        Application.InitializedChanged -= OnApplicationOnInitializedChanged;

        lock (_timeoutLock)
        {
            if (_timeout is { })
            {
                _timeout = null;
            }
        }

        return _benchmarkResults;
    }

    private List<Key>? _demoKeys;
    private int _currentDemoKey = 0;

    private void OnApplicationOnInitializedChanged (object? s, EventArgs<bool> a)
    {
        if (a.CurrentValue)
        {
            lock (_timeoutLock!)
            {
                _timeout = Application.AddTimeout (TimeSpan.FromMilliseconds (BenchmarkTimeout), ForceCloseCallback);
            }

            Application.Iteration += OnApplicationOnIteration;
            Application.Driver!.ClearedContents += (sender, args) => BenchmarkResults.ClearedContentCount++;

            if (Application.Driver is ConsoleDriver cd)
            {
                cd.Refreshed += (sender, args) =>
                                                 {
                                                     BenchmarkResults.RefreshedCount++;
                                                     if (args.CurrentValue)
                                                     {
                                                         BenchmarkResults.UpdatedCount++;
                                                     }
                                                 };

            }
            Application.NotifyNewRunState += OnApplicationNotifyNewRunState;


            _stopwatch = Stopwatch.StartNew ();
        }
        else
        {
            Application.NotifyNewRunState -= OnApplicationNotifyNewRunState;
            Application.Iteration -= OnApplicationOnIteration;
            BenchmarkResults.Duration = _stopwatch!.Elapsed;
            _stopwatch?.Stop ();
        }
    }

    private void OnApplicationOnIteration (object? s, IterationEventArgs a)
    {
        BenchmarkResults.IterationCount++;
        if (BenchmarkResults.IterationCount > BENCHMARK_MAX_NATURAL_ITERATIONS + (_demoKeys!.Count * BENCHMARK_KEY_PACING))
        {
            Application.RequestStop ();
        }
    }

    private void OnApplicationNotifyNewRunState (object? sender, RunStateEventArgs e)
    {
        SubscribeAllSubviews (Application.Top!);

        _currentDemoKey = 0;
        _demoKeys = GetDemoKeyStrokes ();

        Application.AddTimeout (
                                new TimeSpan (0, 0, 0, 0, BENCHMARK_KEY_PACING),
                                () =>
                                {
                                    if (_currentDemoKey >= _demoKeys.Count)
                                    {
                                        return false;
                                    }

                                    Application.RaiseKeyDownEvent (_demoKeys [_currentDemoKey++]);

                                    return true;
                                });

        return;

        // Get a list of all subviews under Application.Top (and their subviews, etc.)
        // and subscribe to their DrawComplete event
        void SubscribeAllSubviews (View view)
        {
            view.DrawComplete += (s, a) => BenchmarkResults.DrawCompleteCount++;
            view.SubviewsLaidOut += (s, a) => BenchmarkResults.LaidOutCount++;
            foreach (View subview in view.Subviews)
            {
                SubscribeAllSubviews (subview);
            }
        }
    }

    // If the scenario doesn't close within the abort time, this will force it to quit
    private bool ForceCloseCallback ()
    {
        lock (_timeoutLock)
        {
            if (_timeout is { })
            {
                _timeout = null;
            }
        }

        Logging.Trace ($@"  Failed to Quit with {Application.QuitKey} after {BenchmarkTimeout}ms and {BenchmarkResults.IterationCount} iterations. Force quit.");

        Application.RequestStop ();

        return false;
    }

    /// <summary>Gets the Scenario Name + Description with the Description padded based on the longest known Scenario name.</summary>
    /// <returns></returns>
    public override string ToString () { return $"{GetName ().PadRight (_maxScenarioNameLen)}{GetDescription ()}"; }

    #region IDispose

    public void Dispose ()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose (true);
        GC.SuppressFinalize (this);
    }

    protected virtual void Dispose (bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            { }

            _disposedValue = true;
        }
    }

    #endregion IDispose

    /// <summary>Returns a list of all Categories set by all of the <see cref="Scenario"/>s defined in the project.</summary>
    internal static ObservableCollection<string> GetAllCategories ()
    {
        List<string> aCategories = [];

        aCategories = typeof (Scenario).Assembly.GetTypes ()
                                       .Where (
                                               myType => myType is { IsClass: true, IsAbstract: false }
                                                         && myType.IsSubclassOf (typeof (Scenario)))
                                       .Select (type => System.Attribute.GetCustomAttributes (type).ToList ())
                                       .Aggregate (
                                                   aCategories,
                                                   (current, attrs) => current
                                                                       .Union (
                                                                               attrs.Where (a => a is ScenarioCategory)
                                                                                    .Select (a => ((ScenarioCategory)a).Name))
                                                                       .ToList ());

        // Sort
        ObservableCollection<string> categories = new (aCategories.OrderBy (c => c).ToList ());

        // Put "All" at the top
        categories.Insert (0, "All Scenarios");

        return categories;

    }

    public virtual List<Key> GetDemoKeyStrokes () => new List<Key> ();

}