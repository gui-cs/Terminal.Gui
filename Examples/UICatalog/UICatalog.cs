global using Attribute = Terminal.Gui.Drawing.Attribute;
global using Color = Terminal.Gui.Drawing.Color;
global using CM = Terminal.Gui.Configuration.ConfigurationManager;
global using Terminal.Gui.App;
global using Terminal.Gui.ViewBase;
global using Terminal.Gui.Drivers;
global using Terminal.Gui.Input;
global using Terminal.Gui.Configuration;
global using Terminal.Gui.Views;
global using Terminal.Gui.Drawing;
global using Terminal.Gui.Text;
global using Terminal.Gui.FileServices;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Command = Terminal.Gui.Input.Command;
using ILogger = Microsoft.Extensions.Logging.ILogger;

#nullable enable

namespace UICatalog;

/// <summary>
///     UI Catalog is a comprehensive sample library and test app for Terminal.Gui. It provides a simple UI for adding to
///     the
///     catalog of scenarios.
/// </summary>
/// <remarks>
///     <para>UI Catalog attempts to satisfy the following goals:</para>
///     <para>
///         <list type="number">
///             <item>
///                 <description>Be an easy-to-use showcase for Terminal.Gui concepts and features.</description>
///             </item>
///             <item>
///                 <description>Provide sample code that illustrates how to properly implement said concepts & features.</description>
///             </item>
///             <item>
///                 <description>Make it easy for contributors to add additional samples in a structured way.</description>
///             </item>
///         </list>
///     </para>
/// </remarks>
public class UICatalog
{
    private static string? _forceDriver = null;

    public static string LogFilePath { get; set; } = string.Empty;
    public static LoggingLevelSwitch LogLevelSwitch { get; } = new ();
    public const string LOGFILE_LOCATION = "logs";
    public static UICatalogCommandLineOptions Options { get; set; }

    private static int Main (string [] args)
    {
        Console.OutputEncoding = Encoding.Default;

        if (Debugger.IsAttached)
        {
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo ("en-US");
        }

        UICatalogTop.CachedScenarios = Scenario.GetScenarios ();
        UICatalogTop.CachedCategories = Scenario.GetAllCategories ();

        // Process command line args

        // If no driver is provided, the default driver is used.
        Option<string> driverOption = new Option<string> ("--driver", "The IConsoleDriver to use.").FromAmong (
             Application.GetDriverTypes ().Item2.ToArray ()!
            );
        driverOption.AddAlias ("-d");
        driverOption.AddAlias ("--d");

        // Configuration Management
        Option<bool> disableConfigManagement = new (
                                                    "--disable-cm",
                                                    "Indicates Configuration Management should not be enabled. Only `ConfigLocations.HardCoded` settings will be loaded.");
        disableConfigManagement.AddAlias ("-dcm");
        disableConfigManagement.AddAlias ("--dcm");

        Option<bool> benchmarkFlag = new ("--benchmark", "Enables benchmarking. If a Scenario is specified, just that Scenario will be benchmarked.");
        benchmarkFlag.AddAlias ("-b");
        benchmarkFlag.AddAlias ("--b");

        Option<uint> benchmarkTimeout = new (
                                             "--timeout",
                                             () => Scenario.BenchmarkTimeout,
                                             $"The maximum time in milliseconds to run a benchmark for. Default is {Scenario.BenchmarkTimeout}ms.");
        benchmarkTimeout.AddAlias ("-t");
        benchmarkTimeout.AddAlias ("--t");

        Option<string> resultsFile = new ("--file", "The file to save benchmark results to. If not specified, the results will be displayed in a TableView.");
        resultsFile.AddAlias ("-f");
        resultsFile.AddAlias ("--f");

        // what's the app name?
        LogFilePath = $"{LOGFILE_LOCATION}/{Assembly.GetExecutingAssembly ().GetName ().Name}";

        Option<string> debugLogLevel = new Option<string> ("--debug-log-level", $"The level to use for logging (debug console and {LogFilePath})").FromAmong (
             Enum.GetNames<LogLevel> ()
            );
        debugLogLevel.SetDefaultValue ("Warning");
        debugLogLevel.AddAlias ("-dl");
        debugLogLevel.AddAlias ("--dl");

        Argument<string> scenarioArgument = new Argument<string> (
                                                                  "scenario",
                                                                  description:
                                                                  "The name of the Scenario to run. If not provided, the UI Catalog UI will be shown.",
                                                                  getDefaultValue: () => "none"
                                                                 ).FromAmong (
                                                                              UICatalogTop.CachedScenarios.Select (s => s.GetName ())
                                                                                          .Append ("none")
                                                                                          .ToArray ()
                                                                             );

        var rootCommand = new RootCommand ("A comprehensive sample library and test app for Terminal.Gui")
        {
            scenarioArgument, debugLogLevel, benchmarkFlag, benchmarkTimeout, resultsFile, driverOption, disableConfigManagement
        };

        rootCommand.SetHandler (
                                context =>
                                {
                                    var options = new UICatalogCommandLineOptions
                                    {
                                        Scenario = context.ParseResult.GetValueForArgument (scenarioArgument),
                                        Driver = context.ParseResult.GetValueForOption (driverOption) ?? string.Empty,
                                        DontEnableConfigurationManagement = context.ParseResult.GetValueForOption (disableConfigManagement),
                                        Benchmark = context.ParseResult.GetValueForOption (benchmarkFlag),
                                        BenchmarkTimeout = context.ParseResult.GetValueForOption (benchmarkTimeout),
                                        ResultsFile = context.ParseResult.GetValueForOption (resultsFile) ?? string.Empty,
                                        DebugLogLevel = context.ParseResult.GetValueForOption (debugLogLevel) ?? "Warning"
                                        /* etc. */
                                    };

                                    // See https://github.com/dotnet/command-line-api/issues/796 for the rationale behind this hackery
                                    Options = options;
                                }
                               );

        var helpShown = false;

        Parser parser = new CommandLineBuilder (rootCommand)
                        .UseHelp (ctx => helpShown = true)
                        .Build ();

        parser.Invoke (args);

        if (helpShown)
        {
            return 0;
        }

        Scenario.BenchmarkTimeout = Options.BenchmarkTimeout;

        Logging.Logger = CreateLogger ();

        UICatalogMain (Options);

        return 0;
    }

    public static LogEventLevel LogLevelToLogEventLevel (LogLevel logLevel)
    {
        return logLevel switch
               {
                   LogLevel.Trace => LogEventLevel.Verbose,
                   LogLevel.Debug => LogEventLevel.Debug,
                   LogLevel.Information => LogEventLevel.Information,
                   LogLevel.Warning => LogEventLevel.Warning,
                   LogLevel.Error => LogEventLevel.Error,
                   LogLevel.Critical => LogEventLevel.Fatal,
                   LogLevel.None => LogEventLevel.Fatal, // Default to Fatal if None is specified
                   _ => LogEventLevel.Fatal // Default to Information for any unspecified LogLevel
               };
    }

    private static ILogger CreateLogger ()
    {
        // Configure Serilog to write logs to a file
        LogLevelSwitch.MinimumLevel = LogLevelToLogEventLevel (Enum.Parse<LogLevel> (Options.DebugLogLevel));

        Log.Logger = new LoggerConfiguration ()
                     .MinimumLevel.ControlledBy (LogLevelSwitch)
                     .Enrich.FromLogContext () // Enables dynamic enrichment
                     .WriteTo.Debug ()
                     .WriteTo.File (
                                    LogFilePath,
                                    rollingInterval: RollingInterval.Day,
                                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                     .CreateLogger ();

        // Create a logger factory compatible with Microsoft.Extensions.Logging
        using ILoggerFactory loggerFactory = LoggerFactory.Create (
                                                                   builder =>
                                                                   {
                                                                       builder
                                                                           .AddSerilog (dispose: true) // Integrate Serilog with ILogger
                                                                           .SetMinimumLevel (LogLevel.Trace); // Set minimum log level
                                                                   });

        // Get an ILogger instance
        return loggerFactory.CreateLogger ("Global Logger");
    }

    /// <summary>
    ///     Shows the UI Catalog selection UI. When the user selects a Scenario to run, the UI Catalog main app UI is
    ///     killed and the Scenario is run as though it were Application.Top. When the Scenario exits, this function exits.
    /// </summary>
    /// <returns></returns>
    private static Scenario RunUICatalogTopLevel ()
    {
        // Run UI Catalog UI. When it exits, if _selectedScenario is != null then
        // a Scenario was selected. Otherwise, the user wants to quit UI Catalog.

        // If the user specified a driver on the command line then use it,
        // ignoring Config files.

        Application.Init (driverName: _forceDriver);

        var top = Application.Run<UICatalogTop> ();
        top.Dispose ();
        Application.Shutdown ();
        VerifyObjectsWereDisposed ();

        return UICatalogTop.CachedSelectedScenario!;
    }

    [SuppressMessage ("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    private static readonly FileSystemWatcher _currentDirWatcher = new ();

    [SuppressMessage ("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    private static readonly FileSystemWatcher _homeDirWatcher = new ();

    private static void StartConfigFileWatcher ()
    {
        // Set up a file system watcher for `./.tui/`
        _currentDirWatcher.NotifyFilter = NotifyFilters.LastWrite;

        string assemblyLocation = Assembly.GetExecutingAssembly ().Location;
        string tuiDir;

        if (!string.IsNullOrEmpty (assemblyLocation))
        {
            var assemblyFile = new FileInfo (assemblyLocation);
            tuiDir = Path.Combine (assemblyFile.Directory!.FullName, ".tui");
        }
        else
        {
            tuiDir = Path.Combine (AppContext.BaseDirectory, ".tui");
        }

        if (!Directory.Exists (tuiDir))
        {
            Directory.CreateDirectory (tuiDir);
        }

        _currentDirWatcher.Path = tuiDir;
        _currentDirWatcher.Filter = "*config.json";

        // Set up a file system watcher for `~/.tui/`
        _homeDirWatcher.NotifyFilter = NotifyFilters.LastWrite;
        var f = new FileInfo (Environment.GetFolderPath (Environment.SpecialFolder.UserProfile));
        tuiDir = Path.Combine (f.FullName, ".tui");

        if (!Directory.Exists (tuiDir))
        {
            Directory.CreateDirectory (tuiDir);
        }

        _homeDirWatcher.Path = tuiDir;
        _homeDirWatcher.Filter = "*config.json";

        _currentDirWatcher.Changed += ConfigFileChanged;

        //_currentDirWatcher.Created += ConfigFileChanged;
        _currentDirWatcher.EnableRaisingEvents = true;

        _homeDirWatcher.Changed += ConfigFileChanged;

        //_homeDirWatcher.Created += ConfigFileChanged;
        _homeDirWatcher.EnableRaisingEvents = true;
    }

    private static void StopConfigFileWatcher ()
    {
        _currentDirWatcher.EnableRaisingEvents = false;
        _currentDirWatcher.Changed -= ConfigFileChanged;
        _currentDirWatcher.Created -= ConfigFileChanged;

        _homeDirWatcher.EnableRaisingEvents = false;
        _homeDirWatcher.Changed -= ConfigFileChanged;
        _homeDirWatcher.Created -= ConfigFileChanged;
    }

    private static void ConfigFileChanged (object sender, FileSystemEventArgs e)
    {
        if (Application.Top == null)
        {
            return;
        }

        Logging.Debug ($"{e.FullPath} {e.ChangeType} - Loading and Applying");
        ConfigurationManager.Load (ConfigLocations.All);
        ConfigurationManager.Apply ();
    }

    private static void UICatalogMain (UICatalogCommandLineOptions options)
    {
        // By setting _forceDriver we ensure that if the user has specified a driver on the command line, it will be used
        // regardless of what's in a config file.
        Application.ForceDriver = _forceDriver = options.Driver;

        // If a Scenario name has been provided on the commandline
        // run it and exit when done.
        if (options.Scenario != "none")
        {
            if (!Options.DontEnableConfigurationManagement)
            {
                ConfigurationManager.Enable (ConfigLocations.All);
            }

            int item = UICatalogTop.CachedScenarios!.IndexOf (
                                                              UICatalogTop.CachedScenarios!.FirstOrDefault (
                                                                   s =>
                                                                       s.GetName ()
                                                                        .Equals (options.Scenario, StringComparison.OrdinalIgnoreCase)
                                                                  )!);
            UICatalogTop.CachedSelectedScenario = (Scenario)Activator.CreateInstance (UICatalogTop.CachedScenarios [item].GetType ())!;

            BenchmarkResults? results = RunScenario (UICatalogTop.CachedSelectedScenario, options.Benchmark);

            if (results is { })
            {
                Console.WriteLine (
                                   JsonSerializer.Serialize (
                                                             results,
                                                             new JsonSerializerOptions
                                                             {
                                                                 WriteIndented = true
                                                             }));
            }

            VerifyObjectsWereDisposed ();

            return;
        }

        // Benchmark all Scenarios
        if (options.Benchmark)
        {
            BenchmarkAllScenarios ();

            return;
        }

#if DEBUG_IDISPOSABLE
        View.EnableDebugIDisposableAsserts = true;
#endif

        if (!Options.DontEnableConfigurationManagement)
        {
            ConfigurationManager.Enable (ConfigLocations.All);
            StartConfigFileWatcher ();
        }

        while (RunUICatalogTopLevel () is { } scenario)
        {
#if DEBUG_IDISPOSABLE
            VerifyObjectsWereDisposed ();

            // Measure how long it takes for the app to shut down
            var sw = new Stopwatch ();
            string scenarioName = scenario.GetName ();
            Application.InitializedChanged += ApplicationOnInitializedChanged;
#endif

            scenario.Main ();
            scenario.Dispose ();

            // This call to Application.Shutdown brackets the Application.Init call
            // made by Scenario.Init() above
            // TODO: Throw if shutdown was not called already
            Application.Shutdown ();

            VerifyObjectsWereDisposed ();

#if DEBUG_IDISPOSABLE
            Application.InitializedChanged -= ApplicationOnInitializedChanged;

            void ApplicationOnInitializedChanged (object? sender, EventArgs<bool> e)
            {
                if (e.Value)
                {
                    sw.Start ();
                }
                else
                {
                    sw.Stop ();
                    Logging.Trace ($"Shutdown of {scenarioName} Scenario took {sw.ElapsedMilliseconds}ms");
                }
            }
#endif
        }

        StopConfigFileWatcher ();
        VerifyObjectsWereDisposed ();
    }

    private static BenchmarkResults? RunScenario (Scenario scenario, bool benchmark)
    {
        if (benchmark)
        {
            scenario.StartBenchmark ();
        }

        Application.Init (driverName: _forceDriver);

        scenario.Main ();

        BenchmarkResults? results = null;

        if (benchmark)
        {
            results = scenario.EndBenchmark ();
        }

        scenario.Dispose ();

        // TODO: Throw if shutdown was not called already
        Application.Shutdown ();

        return results;
    }

    private static void BenchmarkAllScenarios ()
    {
        List<BenchmarkResults> resultsList = [];

        var maxScenarios = 5;

        foreach (Scenario s in UICatalogTop.CachedScenarios!)
        {
            resultsList.Add (RunScenario (s, true)!);
            maxScenarios--;

            if (maxScenarios == 0)
            {
                // break;
            }
        }

        if (resultsList.Count <= 0)
        {
            return;
        }

        if (!string.IsNullOrEmpty (Options.ResultsFile))
        {
            string output = JsonSerializer.Serialize (
                                                      resultsList,
                                                      new JsonSerializerOptions
                                                      {
                                                          WriteIndented = true
                                                      });

            using StreamWriter file = File.CreateText (Options.ResultsFile);
            file.Write (output);
            file.Close ();

            return;
        }

        Application.Init ();

        var benchmarkWindow = new Window
        {
            Title = "Benchmark Results"
        };

        if (benchmarkWindow.Border is { })
        {
            benchmarkWindow.Border.Thickness = new (0, 0, 0, 0);
        }

        TableView resultsTableView = new ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        // TableView provides many options for table headers. For simplicity we turn all 
        // of these off. By enabling FullRowSelect and turning off headers, TableView looks just
        // like a ListView
        resultsTableView.FullRowSelect = true;
        resultsTableView.Style.ShowHeaders = true;
        resultsTableView.Style.ShowHorizontalHeaderOverline = false;
        resultsTableView.Style.ShowHorizontalHeaderUnderline = true;
        resultsTableView.Style.ShowHorizontalBottomline = false;
        resultsTableView.Style.ShowVerticalCellLines = true;
        resultsTableView.Style.ShowVerticalHeaderLines = true;

        /* By default, TableView lays out columns at render time and only
         * measures y rows of data at a time.  Where y is the height of the
         * console. This is for the following reasons:
         *
         * - Performance, when tables have a large amount of data
         * - Defensive, prevents a single wide cell value pushing other
         *   columns off-screen (requiring horizontal scrolling
         *
         * In the case of UICatalog here, such an approach is overkill so
         * we just measure all the data ourselves and set the appropriate
         * max widths as ColumnStyles
         */
        //int longestName = _scenarios!.Max (s => s.GetName ().Length);

        //resultsTableView.Style.ColumnStyles.Add (
        //                                     0,
        //                                     new () { MaxWidth = longestName, MinWidth = longestName, MinAcceptableWidth = longestName }
        //                                    );
        //resultsTableView.Style.ColumnStyles.Add (1, new () { MaxWidth = 1 });
        //resultsTableView.CellActivated += ScenarioView_OpenSelectedItem;

        // TableView typically is a grid where nav keys are biased for moving left/right.
        resultsTableView.KeyBindings.Remove (Key.Home);
        resultsTableView.KeyBindings.Add (Key.Home, Command.Start);
        resultsTableView.KeyBindings.Remove (Key.End);
        resultsTableView.KeyBindings.Add (Key.End, Command.End);

        // Ideally, TableView.MultiSelect = false would turn off any keybindings for
        // multi-select options. But it currently does not. UI Catalog uses Ctrl-A for
        // a shortcut to About.
        resultsTableView.MultiSelect = false;

        var dt = new DataTable ();

        dt.Columns.Add (new DataColumn ("Scenario", typeof (string)));
        dt.Columns.Add (new DataColumn ("Duration", typeof (TimeSpan)));
        dt.Columns.Add (new DataColumn ("Refreshed", typeof (int)));
        dt.Columns.Add (new DataColumn ("LaidOut", typeof (int)));
        dt.Columns.Add (new DataColumn ("ClearedContent", typeof (int)));
        dt.Columns.Add (new DataColumn ("DrawComplete", typeof (int)));
        dt.Columns.Add (new DataColumn ("Updated", typeof (int)));
        dt.Columns.Add (new DataColumn ("Iterations", typeof (int)));

        foreach (BenchmarkResults r in resultsList)
        {
            dt.Rows.Add (
                         r.Scenario,
                         r.Duration,
                         r.RefreshedCount,
                         r.LaidOutCount,
                         r.ClearedContentCount,
                         r.DrawCompleteCount,
                         r.UpdatedCount,
                         r.IterationCount
                        );
        }

        BenchmarkResults totalRow = new ()
        {
            Scenario = "TOTAL",
            Duration = new (resultsList.Sum (r => r.Duration.Ticks)),
            RefreshedCount = resultsList.Sum (r => r.RefreshedCount),
            LaidOutCount = resultsList.Sum (r => r.LaidOutCount),
            ClearedContentCount = resultsList.Sum (r => r.ClearedContentCount),
            DrawCompleteCount = resultsList.Sum (r => r.DrawCompleteCount),
            UpdatedCount = resultsList.Sum (r => r.UpdatedCount),
            IterationCount = resultsList.Sum (r => r.IterationCount)
        };

        dt.Rows.Add (
                     totalRow.Scenario,
                     totalRow.Duration,
                     totalRow.RefreshedCount,
                     totalRow.LaidOutCount,
                     totalRow.ClearedContentCount,
                     totalRow.DrawCompleteCount,
                     totalRow.UpdatedCount,
                     totalRow.IterationCount
                    );

        dt.DefaultView.Sort = "Duration";
        DataTable sortedCopy = dt.DefaultView.ToTable ();

        resultsTableView.Table = new DataTableSource (sortedCopy);

        benchmarkWindow.Add (resultsTableView);

        Application.Run (benchmarkWindow);
        benchmarkWindow.Dispose ();
        Application.Shutdown ();
    }

    private static void VerifyObjectsWereDisposed ()
    {
#if DEBUG_IDISPOSABLE
        if (!View.EnableDebugIDisposableAsserts)
        {
            View.Instances.Clear ();
            RunState.Instances.Clear ();

            return;
        }

        // Validate there are no outstanding View instances 
        // after a scenario was selected to run. This proves the main UI Catalog
        // 'app' closed cleanly.
        foreach (View? inst in View.Instances)
        {
            Debug.Assert (inst.WasDisposed);
        }

        View.Instances.Clear ();

        // Validate there are no outstanding Application.RunState-based instances 
        // after a scenario was selected to run. This proves the main UI Catalog
        // 'app' closed cleanly.
        foreach (RunState? inst in RunState.Instances)
        {
            Debug.Assert (inst.WasDisposed);
        }

        RunState.Instances.Clear ();
#endif
    }
}
