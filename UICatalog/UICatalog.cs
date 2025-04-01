global using Attribute = Terminal.Gui.Attribute;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using static Terminal.Gui.ConfigurationManager;
using Command = Terminal.Gui.Command;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using RuntimeEnvironment = Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment;
using Terminal.Gui;

#nullable enable

namespace UICatalog;

/// <summary>
///     UI Catalog is a comprehensive sample library and test app for Terminal.Gui. It provides a simple UI for adding to the
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
public class UICatalogApp
{
    private static int _cachedCategoryIndex;

    // When a scenario is run, the main app is killed. These items
    // are therefore cached so that when the scenario exits the
    // main app UI can be restored to previous state
    private static int _cachedScenarioIndex;
    private static ObservableCollection<string>? _categories;

    [SuppressMessage ("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    private static readonly FileSystemWatcher _currentDirWatcher = new ();

    private static ViewDiagnosticFlags _diagnosticFlags;
    private static FlagSelector? _diagnosticFlagsSelector;
    private static CheckBox? _disableMouseCb;

    private static string _forceDriver = string.Empty;

    [SuppressMessage ("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    private static readonly FileSystemWatcher _homeDirWatcher = new ();

    private static bool _isFirstRunning = true;
    private static Options _options;
    private static ObservableCollection<Scenario>? _scenarios;

    private const string LOGFILE_LOCATION = "logs";
    private static string _logFilePath = string.Empty;
    private static readonly LoggingLevelSwitch _logLevelSwitch = new ();
    private static RadioGroup? _logLevelRg;

    // If set, holds the scenario the user selected
    private static Scenario? _selectedScenario;

    private static CheckBox? _force16ColorsMenuItemCb;
    private static CheckBox? _force16ColorsShortcutCb;

    private static string? _cachedTheme;
    private static RadioGroup? _themesRg;
    private static RadioGroup? _topSchemeRg;
    private static string? _topLevelColorScheme;

    [SerializableConfigurationProperty (Scope = typeof (AppScope), OmitClassName = true)]
    [JsonPropertyName ("UICatalog.StatusBar")]
    public static bool ShowStatusBar { get; set; } = true;

    /// <summary>
    ///     Gets the message displayed in the About Box. `public` so it can be used from Unit tests.
    /// </summary>
    /// <returns></returns>
    public static string GetAboutBoxMessage ()
    {
        // NOTE: Do not use multiline verbatim strings here.
        // WSL gets all confused.
        StringBuilder msg = new ();
        msg.AppendLine ("UI Catalog: A comprehensive sample library and test app for");
        msg.AppendLine ();

        msg.AppendLine (
                        """
                         _______                  _             _   _____       _ 
                        |__   __|                (_)           | | / ____|     (_)
                           | | ___ _ __ _ __ ___  _ _ __   __ _| || |  __ _   _ _ 
                           | |/ _ \ '__| '_ ` _ \| | '_ \ / _` | || | |_ | | | | |
                           | |  __/ |  | | | | | | | | | | (_| | || |__| | |_| | |
                           |_|\___|_|  |_| |_| |_|_|_| |_|\__,_|_(_)_____|\__,_|_|
                        """);
        msg.AppendLine ();
        msg.AppendLine ("v2 - Pre-Alpha");
        msg.AppendLine ();
        msg.AppendLine ("https://github.com/gui-cs/Terminal.Gui");

        return msg.ToString ();
    }

    private static void ConfigFileChanged (object sender, FileSystemEventArgs e)
    {
        if (Application.Top == null)
        {
            return;
        }

        // TODO: This is a hack. Figure out how to ensure that the file is fully written before reading it.
        //Thread.Sleep (500);
        Load ();
        Apply ();
    }

    private static int Main (string [] args)
    {
        Console.OutputEncoding = Encoding.Default;

        if (Debugger.IsAttached)
        {
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo ("en-US");
        }

        _scenarios = Scenario.GetScenarios ();
        _categories = Scenario.GetAllCategories ();

        // Process command line args

        // If no driver is provided, the default driver is used.
        Option<string> driverOption = new Option<string> ("--driver", "The IConsoleDriver to use.").FromAmong (
             Application.GetDriverTypes ()
                            .Where (d => !typeof (IConsoleDriverFacade).IsAssignableFrom (d))
                            .Select (d => d!.Name)
                            .Union (["v2", "v2win", "v2net"])
                            .ToArray ()
            );
        driverOption.AddAlias ("-d");
        driverOption.AddAlias ("--d");

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
        _logFilePath = $"{LOGFILE_LOCATION}/{Assembly.GetExecutingAssembly ().GetName ().Name}";
        Option<string> debugLogLevel = new Option<string> ("--debug-log-level", $"The level to use for logging (debug console and {_logFilePath})").FromAmong (
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
                                                                              _scenarios.Select (s => s.GetName ())
                                                                                        .Append ("none")
                                                                                        .ToArray ()
                                                                             );

        var rootCommand = new RootCommand ("A comprehensive sample library and test app for Terminal.Gui")
        {
            scenarioArgument, debugLogLevel, benchmarkFlag, benchmarkTimeout, resultsFile, driverOption
        };

        rootCommand.SetHandler (
                                context =>
                                {
                                    var options = new Options
                                    {
                                        Scenario = context.ParseResult.GetValueForArgument (scenarioArgument),
                                        Driver = context.ParseResult.GetValueForOption (driverOption) ?? string.Empty,
                                        Benchmark = context.ParseResult.GetValueForOption (benchmarkFlag),
                                        BenchmarkTimeout = context.ParseResult.GetValueForOption (benchmarkTimeout),
                                        ResultsFile = context.ParseResult.GetValueForOption (resultsFile) ?? string.Empty,
                                        DebugLogLevel = context.ParseResult.GetValueForOption (debugLogLevel) ?? "Warning"
                                        /* etc. */
                                    };

                                    // See https://github.com/dotnet/command-line-api/issues/796 for the rationale behind this hackery
                                    _options = options;
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

        Scenario.BenchmarkTimeout = _options.BenchmarkTimeout;

        Logging.Logger = CreateLogger ();

        UICatalogMain (_options);

        return 0;
    }

    private static LogEventLevel LogLevelToLogEventLevel (LogLevel logLevel)
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
        _logLevelSwitch.MinimumLevel = LogLevelToLogEventLevel (Enum.Parse<LogLevel> (_options.DebugLogLevel));
        Log.Logger = new LoggerConfiguration ()
                     .MinimumLevel.ControlledBy (_logLevelSwitch)
                     .Enrich.FromLogContext () // Enables dynamic enrichment
                     .WriteTo.Debug ()
                     .WriteTo.File (
                                    _logFilePath,
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

    public static void OpenUrl (string url)
    {
        if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows))
        {
            url = url.Replace ("&", "^&");
            Process.Start (new ProcessStartInfo ("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
        else if (RuntimeInformation.IsOSPlatform (OSPlatform.Linux))
        {
            using var process = new Process
            {
                StartInfo = new ()
                {
                    FileName = "xdg-open",
                    Arguments = url,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false
                }
            };
            process.Start ();
        }
        else if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX))
        {
            Process.Start ("open", url);
        }
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

        if (string.IsNullOrWhiteSpace (_cachedTheme))
        {
            _cachedTheme = Themes?.Theme;
        }
        else
        {
            Themes!.Theme = _cachedTheme;
            Apply ();
        }

        Application.Run<UICatalogTopLevel> ().Dispose ();
        Application.Shutdown ();

        return _selectedScenario!;
    }

    private static void StartConfigFileWatcher ()
    {
        // Setup a file system watcher for `./.tui/`
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

        // Setup a file system watcher for `~/.tui/`
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

    private static void UICatalogMain (Options options)
    {
        StartConfigFileWatcher ();

        // By setting _forceDriver we ensure that if the user has specified a driver on the command line, it will be used
        // regardless of what's in a config file.
        Application.ForceDriver = _forceDriver = options.Driver;

        // If a Scenario name has been provided on the commandline
        // run it and exit when done.
        if (options.Scenario != "none")
        {
            int item = _scenarios!.IndexOf (
                                            _scenarios!.FirstOrDefault (
                                                                        s =>
                                                                            s.GetName ()
                                                                             .Equals (options.Scenario, StringComparison.OrdinalIgnoreCase)
                                                                       )!);
            _selectedScenario = (Scenario)Activator.CreateInstance (_scenarios [item].GetType ())!;

            BenchmarkResults? results = RunScenario (_selectedScenario, options.Benchmark);

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

        while (RunUICatalogTopLevel () is { } scenario)
        {
            VerifyObjectsWereDisposed ();
            Themes!.Theme = _cachedTheme!;
            Apply ();
            scenario.TopLevelColorScheme = _topLevelColorScheme;

#if DEBUG_IDISPOSABLE
            View.DebugIDisposable = true;
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
                if (e.CurrentValue)
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
        scenario.TopLevelColorScheme = _topLevelColorScheme;

        if (benchmark)
        {
            Application.Screen = new (0, 0, 120, 40);
        }

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
        List<BenchmarkResults> resultsList = new ();

        var maxScenarios = 5;

        foreach (Scenario s in _scenarios!)
        {
            resultsList.Add (RunScenario (s, true)!);
            maxScenarios--;

            if (maxScenarios == 0)
            {
                // break;
            }
        }

        if (resultsList.Count > 0)
        {
            if (!string.IsNullOrEmpty (_options.ResultsFile))
            {
                string output = JsonSerializer.Serialize (
                                                          resultsList,
                                                          new JsonSerializerOptions
                                                          {
                                                              WriteIndented = true
                                                          });

                using StreamWriter file = File.CreateText (_options.ResultsFile);
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

            /* By default TableView lays out columns at render time and only
             * measures y rows of data at a time.  Where y is the height of the
             * console. This is for the following reasons:
             *
             * - Performance, when tables have a large amount of data
             * - Defensive, prevents a single wide cell value pushing other
             *   columns off screen (requiring horizontal scrolling
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
    }

    private static void VerifyObjectsWereDisposed ()
    {
#if DEBUG_IDISPOSABLE
        if (!View.DebugIDisposable)
        {
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

    /// <summary>
    ///     This is the main UI Catalog app view. It is run fresh when the app loads (if a Scenario has not been passed on
    ///     the command line) and each time a Scenario ends.
    /// </summary>
    public class UICatalogTopLevel : Toplevel
    {
        public ListView? CategoryList;

        //public Shortcut? ShDiagnostics;
        public Shortcut? ShVersion;

        // UI Catalog uses TableView for the scenario list instead of a ListView to demonstate how
        // TableView works. There's no real reason not to use ListView. Because we use TableView, and TableView
        // doesn't (currently) have CollectionNavigator support built in, we implement it here, within the app.
        public TableView ScenarioList;

        private readonly StatusBar? _statusBar;

        private readonly CollectionNavigator _scenarioCollectionNav = new ();

        public UICatalogTopLevel ()
        {
            _diagnosticFlags = Diagnostics;

            MenuBarv2 menuBar = new (
            [
                    new MenuBarItemv2 (
                                      "_File",
                                      [
                                          new MenuItemv2 (
                                                            "_Quit",
                                                            "Quit UI Catalog",
                                                            RequestStop
                                                           )
                                      ]),
                    new MenuBarItemv2 ("_Themes", CreateThemeMenuItems ()),
                    new MenuBarItemv2 ("Diag_nostics", CreateDiagnosticMenuItems ()),
                    new MenuBarItemv2 ("_Logging", CreateLoggingMenuItems ()),
                    new MenuBarItemv2 (
                                      "_Help",
                                      [
                                          new MenuItemv2 (
                                                         "_Documentation",
                                                         "",
                                                         () => OpenUrl ("https://gui-cs.github.io/Terminal.GuiV2Docs"),
                                                         Key.F1
                                                        ),
                                          new MenuItemv2(
                                                         "_README",
                                                         "",
                                                         () => OpenUrl ("https://github.com/gui-cs/Terminal.Gui"),
                                                         Key.F2
                                                        ),
                                          new MenuItemv2 (
                                                         "_About...",
                                                         "About UI Catalog",
                                                         () => MessageBox.Query (
                                                                                 "",
                                                                                 GetAboutBoxMessage (),
                                                                                 wrapMessage: false,
                                                                                 buttons: "_Ok"
                                                                                ),
                                                         Key.A.WithCtrl
                                                        )
                                          ])
                    ]);

            _statusBar = new ()
            {
                Visible = ShowStatusBar,
                AlignmentModes = AlignmentModes.IgnoreFirstOrLast,
                CanFocus = false
            };
            _statusBar.Height = Dim.Auto (DimAutoStyle.Auto, minimumContentDim: Dim.Func (() => _statusBar.Visible ? 1 : 0), maximumContentDim: Dim.Func (() => _statusBar.Visible ? 1 : 0));

            ShVersion = new ()
            {
                Title = "Version Info",
                CanFocus = false
            };

            var statusBarShortcut = new Shortcut
            {
                Key = Key.F10,
                Title = "Show/Hide Status Bar",
                CanFocus = false
            };

            statusBarShortcut.Accepting += (sender, args) =>
            {
                _statusBar.Visible = !_statusBar.Visible;
                args.Cancel = true;
            };

            _force16ColorsShortcutCb = new CheckBox
            {
                Title = "16 color mode",
                CheckedState = Application.Force16Colors ? CheckState.Checked : CheckState.UnChecked,
                CanFocus = false
            };
            _force16ColorsShortcutCb.CheckedStateChanging += (sender, args) =>
                                                             {
                                                                 Application.Force16Colors = args.NewValue == CheckState.Checked;
                                                                 _force16ColorsMenuItemCb!.CheckedState = args.NewValue;
                                                                 Application.LayoutAndDraw ();
                                                             };


            _statusBar.Add (
                new Shortcut
                {
                    CanFocus = false,
                    Title = "Quit",
                    Key = Application.QuitKey
                },
                statusBarShortcut,
                new Shortcut ()
                {
                    CanFocus = false,
                    CommandView = _force16ColorsShortcutCb,
                    HelpText = "",
                    BindKeyToApplication = true,
                    Key = Key.F7
                },
                ShVersion
            );

            // Create the Category list view. This list never changes.
            CategoryList = new ()
            {
                X = 0,
                Y = Pos.Bottom (menuBar),
                Width = Dim.Auto (),
                Height = Dim.Fill (
                               Dim.Func (
                                         () =>
                                         {
                                             if (_statusBar.NeedsLayout)
                                             {
                                                 throw new LayoutException ("DimFunc.Fn aborted because dependent View needs layout.");

                                                 //_statusBar.Layout ();
                                             }

                                             return _statusBar.Frame.Height;
                                         })),
                AllowsMarking = false,
                CanFocus = true,
                Title = "_Categories",
                BorderStyle = LineStyle.Rounded,
                SuperViewRendersLineCanvas = true,
                Source = new ListWrapper<string> (_categories)
            };
            CategoryList.OpenSelectedItem += (s, a) => { ScenarioList!.SetFocus (); };
            CategoryList.SelectedItemChanged += CategoryView_SelectedChanged;

            // This enables the scrollbar by causing lazy instantiation to happen
            CategoryList.VerticalScrollBar.AutoShow = true;

            // Create the scenario list. The contents of the scenario list changes whenever the
            // Category list selection changes (to show just the scenarios that belong to the selected
            // category).
            ScenarioList = new ()
            {
                X = Pos.Right (CategoryList) - 1,
                Y = Pos.Bottom (menuBar),
                Width = Dim.Fill (),
                Height = Dim.Fill (
                               Dim.Func (
                                         () =>
                                         {
                                             if (_statusBar.NeedsLayout)
                                             {
                                                 throw new LayoutException ("DimFunc.Fn aborted because dependent View needs layout.");

                                                 //_statusBar.Layout ();
                                             }

                                             return _statusBar.Frame.Height;
                                         })),

                //AllowsMarking = false,
                CanFocus = true,
                Title = "_Scenarios",
                BorderStyle = CategoryList.BorderStyle,
                SuperViewRendersLineCanvas = true
            };

            //ScenarioList.VerticalScrollBar.AutoHide = false;
            //ScenarioList.HorizontalScrollBar.AutoHide = false;

            // TableView provides many options for table headers. For simplicity we turn all 
            // of these off. By enabling FullRowSelect and turning off headers, TableView looks just
            // like a ListView
            ScenarioList.FullRowSelect = true;
            ScenarioList.Style.ShowHeaders = false;
            ScenarioList.Style.ShowHorizontalHeaderOverline = false;
            ScenarioList.Style.ShowHorizontalHeaderUnderline = false;
            ScenarioList.Style.ShowHorizontalBottomline = false;
            ScenarioList.Style.ShowVerticalCellLines = false;
            ScenarioList.Style.ShowVerticalHeaderLines = false;

            /* By default TableView lays out columns at render time and only
             * measures y rows of data at a time.  Where y is the height of the
             * console. This is for the following reasons:
             *
             * - Performance, when tables have a large amount of data
             * - Defensive, prevents a single wide cell value pushing other
             *   columns off screen (requiring horizontal scrolling
             *
             * In the case of UICatalog here, such an approach is overkill so
             * we just measure all the data ourselves and set the appropriate
             * max widths as ColumnStyles
             */
            int longestName = _scenarios!.Max (s => s.GetName ().Length);

            ScenarioList.Style.ColumnStyles.Add (
            0,
            new () { MaxWidth = longestName, MinWidth = longestName, MinAcceptableWidth = longestName }
            );
            ScenarioList.Style.ColumnStyles.Add (1, new () { MaxWidth = 1 });
            ScenarioList.CellActivated += ScenarioView_OpenSelectedItem;

            // TableView typically is a grid where nav keys are biased for moving left/right.
            ScenarioList.KeyBindings.Remove (Key.Home);
            ScenarioList.KeyBindings.Add (Key.Home, Command.Start);
            ScenarioList.KeyBindings.Remove (Key.End);
            ScenarioList.KeyBindings.Add (Key.End, Command.End);

            // Ideally, TableView.MultiSelect = false would turn off any keybindings for
            // multi-select options. But it currently does not. UI Catalog uses Ctrl-A for
            // a shortcut to About.
            ScenarioList.MultiSelect = false;
            ScenarioList.KeyBindings.Remove (Key.A.WithCtrl);

            Add (menuBar);
            Add (CategoryList);
            Add (ScenarioList);
            Add (_statusBar);

            Loaded += LoadedHandler;
            Unloaded += UnloadedHandler;

            // Restore previous selections
            CategoryList.SelectedItem = _cachedCategoryIndex;
            ScenarioList.SelectedRow = _cachedScenarioIndex;

            Applied += ConfigAppliedHandler;
        }

        public void ConfigChanged ()
        {
            if (MenuBar == null)
            {
                // View is probably disposed
                return;
            }

            _cachedTheme = Themes?.Theme;

            UpdateThemesMenu ();

            ColorScheme = Colors.ColorSchemes [_topLevelColorScheme];

            MenuBar!.Menus [0].Children! [0]!.ShortcutKey = Application.QuitKey;

            ((Shortcut)_statusBar!.SubViews.ElementAt (0)).Key = Application.QuitKey;
            _statusBar.Visible = ShowStatusBar;

            _disableMouseCb!.CheckedState = Application.IsMouseDisabled ? CheckState.Checked : CheckState.UnChecked;
            _force16ColorsShortcutCb!.CheckedState = Application.Force16Colors ? CheckState.Checked : CheckState.UnChecked;

            Application.Top!.SetNeedsDraw ();
        }

        private View [] CreateThemeMenuItems ()
        {
            List<View> menuItems = [];

            _force16ColorsMenuItemCb = new ()
            {
                Title = "Force _16 Colors",
                CheckedState = Application.Force16Colors ? CheckState.Checked : CheckState.UnChecked
            };

            _force16ColorsMenuItemCb.CheckedStateChanged += (sender, args) =>
                                                            {
                                                                Application.Force16Colors = args.CurrentValue == CheckState.Checked;

                                                                _force16ColorsShortcutCb!.CheckedState = args.CurrentValue;
                                                                Application.LayoutAndDraw ();
                                                            };

            menuItems.Add (
                           new MenuItemv2 ()
                           {
                               CommandView = _force16ColorsMenuItemCb,
                           });

            menuItems.Add (new Line ());

            _themesRg = new RadioGroup ();
            _themesRg.SelectedItemChanged += (_, args) =>
                                             {
                                                 Themes!.Theme = Themes!.Keys.ToArray () [args.SelectedItem];
                                                 _cachedTheme = Themes!.Keys.ToArray () [args.SelectedItem];
                                                 Apply ();
                                                 SetNeedsDraw ();
                                             };

            MenuItemv2 menuItem = new MenuItemv2 ()
            {
                CommandView = _themesRg,
                HelpText = "Cycle Through Themes",
                Key = Key.T.WithCtrl,
            };
            menuItems.Add (menuItem);

            menuItems.Add (new Line ());

            _topSchemeRg = new RadioGroup ();
            _topSchemeRg.SelectedItemChanged += (_, args) =>
                                                {
                                                    _topLevelColorScheme = Colors.ColorSchemes.Keys.ToArray () [args.SelectedItem];
                                                    ColorScheme = Colors.ColorSchemes [_topLevelColorScheme];
                                                    SetNeedsDraw ();
                                                };
            menuItem = new MenuItemv2 ()
            {
                Title = "_Color Scheme for Application.Top",
                SubMenu = new Menuv2 ([new ()
                {
                    CommandView = _topSchemeRg,
                    HelpText = "Cycle Through Color Schemes",
                    Key = Key.S.WithCtrl,
                }])
            };
            menuItems.Add (menuItem);

            UpdateThemesMenu ();

            return menuItems.ToArray ();
        }

        private void UpdateThemesMenu ()
        {
            if (_themesRg is null)
            {
                return;
            }

            _themesRg.AssignHotKeysToRadioLabels = true;
            _themesRg.UsedHotKeys.Clear ();
            _themesRg.RadioLabels = Colors.ColorSchemes.Keys.ToArray ();

            _themesRg.RadioLabels = Themes!.Keys.ToArray ();
            _themesRg.SelectedItem = Themes.Keys.ToList ().IndexOf (_cachedTheme!.Replace ("_", string.Empty));

            if (_topSchemeRg is null)
            {
                return;
            }


            _topSchemeRg.AssignHotKeysToRadioLabels = true;
            _topSchemeRg.UsedHotKeys.Clear ();
            _topSchemeRg.RadioLabels = Colors.ColorSchemes.Keys.ToArray ();

            if (_topLevelColorScheme is null || !Colors.ColorSchemes.ContainsKey (_topLevelColorScheme))
            {
                _topLevelColorScheme = "Base";
            }

            _topSchemeRg.SelectedItem = Array.IndexOf (Colors.ColorSchemes.Keys.ToArray (), _topLevelColorScheme);

        }

        private void CategoryView_SelectedChanged (object? sender, ListViewItemEventArgs? e)
        {
            string item = _categories! [e!.Item];
            ObservableCollection<Scenario> newlist;

            if (e.Item == 0)
            {
                // First category is "All"
                newlist = _scenarios!;
            }
            else
            {
                newlist = new (_scenarios!.Where (s => s.GetCategories ().Contains (item)).ToList ());
            }

            ScenarioList.Table = new EnumerableTableSource<Scenario> (
                                                                      newlist,
                                                                      new ()
                                                                      {
                                                                          { "Name", s => s.GetName () }, { "Description", s => s.GetDescription () }
                                                                      }
                                                                     );

            // Create a collection of just the scenario names (the 1st column in our TableView)
            // for CollectionNavigator. 
            List<object> firstColumnList = new ();

            for (var i = 0; i < ScenarioList.Table.Rows; i++)
            {
                firstColumnList.Add (ScenarioList.Table [i, 0]);
            }

            _scenarioCollectionNav.Collection = firstColumnList;
        }

        private void ConfigAppliedHandler (object? sender, ConfigurationManagerEventArgs? a) { ConfigChanged (); }

        [SuppressMessage ("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        private View [] CreateDiagnosticMenuItems ()
        {
            List<View> menuItems = new ();

            _diagnosticFlagsSelector = new ()
            {
                CanFocus = false,
                Styles = FlagSelectorStyles.ShowNone,
                HighlightStyle = HighlightStyle.None
            };
            _diagnosticFlagsSelector.SetFlags<ViewDiagnosticFlags> ();

            _diagnosticFlagsSelector.ValueChanged += (sender, args) =>
            {
                _diagnosticFlags = (ViewDiagnosticFlags)_diagnosticFlagsSelector.Value;
                Diagnostics = _diagnosticFlags;
            };

            menuItems.Add (new MenuItemv2 ()
            {
                CommandView = _diagnosticFlagsSelector,
                HelpText = "View Diagnostics",
            });

            menuItems.Add (new Line ());

            _disableMouseCb = new CheckBox ()
            {
                Title = "_Disable Mouse",
                CheckedState = Application.IsMouseDisabled ? CheckState.Checked : CheckState.UnChecked
            };

            _disableMouseCb.CheckedStateChanged += (_, args) =>
                                      {
                                          Application.IsMouseDisabled = args.CurrentValue == CheckState.Checked;
                                      };
            menuItems.Add (
                           new MenuItemv2 ()
                           {
                               CommandView = _disableMouseCb,
                               HelpText = "Disable Mouse",
                           });

            return menuItems.ToArray ();
        }

        //private MenuItemv2 [] CreateDiagnosticMenuItems ()
        //{
        //    List<MenuItemv2 []> menuItems = new ()
        //    {
        //        CreateDiagnosticFlagsMenuItems (),
        //        //new MenuItem [] { null! },
        //        //CreateDisabledEnabledMouseItems (),
        //        //CreateDisabledEnabledMenuBorder (),
        //    };

        //    return menuItems;
        //}

        [SuppressMessage ("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        private View? [] CreateLoggingMenuItems ()
        {
            List<View?> menuItems = new ();

            LogLevel [] logLevels = Enum.GetValues<LogLevel> ();
            _logLevelRg = new RadioGroup ()
            {
                AssignHotKeysToRadioLabels = true,
                RadioLabels = Enum.GetNames<LogLevel> (),
                SelectedItem = logLevels.ToList ().IndexOf (Enum.Parse<LogLevel> (_options.DebugLogLevel))
            };
            _logLevelRg.SelectedItemChanged += (_, args) =>
                                               {
                                                   _options.DebugLogLevel = Enum.GetName (logLevels [args.SelectedItem])!;
                                                   _logLevelSwitch.MinimumLevel = LogLevelToLogEventLevel (Enum.Parse<LogLevel> (_options.DebugLogLevel));
                                               };

            menuItems.Add (new MenuItemv2 ()
            {
                CommandView = _logLevelRg,
                HelpText = "Cycle Through Log Levels",
                Key = Key.L.WithCtrl
            });

            // add a separator
            menuItems.Add (new Line ());

            menuItems.Add (
                           new MenuItemv2 (
                                             $"_Open Log Folder",
                                             string.Empty,
                                             () => OpenUrl (LOGFILE_LOCATION)
                                            ));

            return menuItems.ToArray ()!;
        }

        private void LoadedHandler (object? sender, EventArgs? args)
        {
            ConfigChanged ();

            if (_disableMouseCb is { })
            {
                _disableMouseCb.CheckedState = Application.IsMouseDisabled ? CheckState.Checked : CheckState.UnChecked;
            }

            if (ShVersion is { })
            {
                ShVersion.Title = $"{RuntimeEnvironment.OperatingSystem} {RuntimeEnvironment.OperatingSystemVersion}, {Driver!.GetVersionInfo ()}";
            }

            if (_selectedScenario != null)
            {
                _selectedScenario = null;
                _isFirstRunning = false;
            }

            if (!_isFirstRunning)
            {
                ScenarioList.SetFocus ();
            }

            if (_statusBar is { })
            {
                _statusBar.VisibleChanged += (s, e) => { ShowStatusBar = _statusBar.Visible; };
            }

            Loaded -= LoadedHandler;
            CategoryList!.EnsureSelectedItemVisible ();
            ScenarioList.EnsureSelectedCellIsVisible ();
        }

        /// <summary>Launches the selected scenario, setting the global _selectedScenario</summary>
        /// <param name="e"></param>
        private void ScenarioView_OpenSelectedItem (object? sender, EventArgs? e)
        {
            if (_selectedScenario is null)
            {
                // Save selected item state
                _cachedCategoryIndex = CategoryList!.SelectedItem;
                _cachedScenarioIndex = ScenarioList.SelectedRow;

                // Create new instance of scenario (even though Scenarios contains instances)
                var selectedScenarioName = (string)ScenarioList.Table [ScenarioList.SelectedRow, 0];

                _selectedScenario = (Scenario)Activator.CreateInstance (
                                                                        _scenarios!.FirstOrDefault (
                                                                                                    s => s.GetName ()
                                                                                                         == selectedScenarioName
                                                                                                   )!
                                                                                   .GetType ()
                                                                       )!;

                // Tell the main app to stop
                Application.RequestStop ();
            }
        }

        private void UnloadedHandler (object? sender, EventArgs? args)
        {
            Applied -= ConfigAppliedHandler;
            Unloaded -= UnloadedHandler;
            Dispose ();
        }
    }

    private struct Options
    {
        public string Driver;

        public string Scenario;

        public uint BenchmarkTimeout;

        public bool Benchmark;

        public string ResultsFile;

        public string DebugLogLevel;
        /* etc. */
    }
}
