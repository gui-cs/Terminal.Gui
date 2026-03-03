global using Attribute = Terminal.Gui.Drawing.Attribute;
global using Color = Terminal.Gui.Drawing.Color;
global using Terminal.Gui.App;
global using Terminal.Gui.ViewBase;
global using Terminal.Gui.Drivers;
global using Terminal.Gui.Input;
global using Terminal.Gui.Configuration;
global using Terminal.Gui.Views;
global using Terminal.Gui.Drawing;
global using Terminal.Gui.Text;
global using Terminal.Gui.FileServices;
global using Terminal.Gui.Resources;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
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
    public static string LogFilePath { get; set; } = string.Empty;
    public static LoggingLevelSwitch LogLevelSwitch { get; } = new ();
    public const string LOGFILE_LOCATION = "logs";
    public static UICatalogCommandLineOptions Options { get; set; }

    /// <summary>
    ///     Gets the in-memory log capture for scenario debugging.
    ///     This captures all log output and allows retrieving logs since a scenario started.
    /// </summary>
    public static ScenarioLogCapture LogCapture { get; } = new ();

    private static int Main (string [] args)
    {
        Console.OutputEncoding = Encoding.Default;

        if (Debugger.IsAttached)
        {
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo ("en-US");
        }

        UICatalogRunnable.CachedScenarios = Scenario.GetScenarios ();
        UICatalogRunnable.CachedCategories = Scenario.GetAllCategories ();

        // Process command line args

        // If no driver is provided, the default driver is used.
        // Get allowed driver names
        string? [] allowedDrivers = DriverRegistry.GetDriverNames ().ToArray ();

        Option<string> driverOption = new ("--driver")
        {
            Description = "The IDriver to use."
        };
        driverOption.AcceptOnlyFromAmong (allowedDrivers!);
        driverOption.DefaultValueFactory = _ => string.Empty;
        driverOption.Aliases.Add ("-d");
        driverOption.Aliases.Add ("--d");

        // Add validator separately (not chained)
        driverOption.Validators.Add (result =>
                                   {
                                       var value = result.GetValueOrDefault<string> ();

                                       if (result.Tokens.Count > 0 && !allowedDrivers.Contains (value))
                                       {
                                           result.AddError ($"Invalid driver name '{value}'. Allowed values: {string.Join (", ", allowedDrivers)}");
                                       }
                                   });

        // Configuration Management
        Option<bool> disableConfigManagement = new ("--disable-cm")
        {
            Description = "Indicates Configuration Management should not be enabled. Only `ConfigLocations.HardCoded` settings will be loaded."
        };
        disableConfigManagement.Aliases.Add ("-dcm");
        disableConfigManagement.Aliases.Add ("--dcm");

        Option<bool> benchmarkFlag = new ("--benchmark")
        {
            Description = "Enables benchmarking. If a Scenario is specified, just that Scenario will be benchmarked."
        };
        benchmarkFlag.Aliases.Add ("-b");
        benchmarkFlag.Aliases.Add ("--b");

        Option<bool> force16ColorsOption = new ("--force-16-colors") { Description = "Forces the driver to use 16-color mode instead of TrueColor." };
        force16ColorsOption.Aliases.Add ("-16");

        Option<uint> benchmarkTimeout = new ("--timeout")
        {
            Description = $"The maximum time in milliseconds to run a benchmark for. Default is {Scenario.BenchmarkTimeout}ms.",
            DefaultValueFactory = _ => Scenario.BenchmarkTimeout
        };
        benchmarkTimeout.Aliases.Add ("-t");
        benchmarkTimeout.Aliases.Add ("--t");

        Option<string> resultsFile = new ("--file")
        {
            Description = "The file to save benchmark results to. If not specified, the results will be displayed in a TableView.",
            DefaultValueFactory = _ => string.Empty
        };
        resultsFile.Aliases.Add ("-f");
        resultsFile.Aliases.Add ("--f");

        // what's the app name?
        LogFilePath = $"{LOGFILE_LOCATION}/{Assembly.GetExecutingAssembly ().GetName ().Name}.log";

        Option<string> debugLogLevel = new ("--debug-log-level")
        {
            Description = $"The level to use for logging (debug console and {LogFilePath})"
        };
        debugLogLevel.AcceptOnlyFromAmong (Enum.GetNames<LogLevel> ());
        debugLogLevel.DefaultValueFactory = _ => "Warning";
        debugLogLevel.Aliases.Add ("-dl");
        debugLogLevel.Aliases.Add ("--dl");

        Argument<string> scenarioArgument = new ("scenario")
        {
            Description = "The name of the Scenario to run. If not provided, the UI Catalog UI will be shown.", DefaultValueFactory = _ => "none"
        };
        scenarioArgument.AcceptOnlyFromAmong (UICatalogRunnable.CachedScenarios.Select (s => s.GetName ()).Append ("none").ToArray ());

        var rootCommand = new RootCommand ("A comprehensive sample library and test app for Terminal.Gui")
        {
            scenarioArgument,
            debugLogLevel,
            benchmarkFlag,
            benchmarkTimeout,
            resultsFile,
            driverOption,
            disableConfigManagement,
            force16ColorsOption
        };

        rootCommand.SetAction (context =>
                                {
                                    bool force16 = context.GetRequiredValue (force16ColorsOption);

                                    UICatalogCommandLineOptions options = new ()
                                    {
                                        Scenario = context.GetRequiredValue (scenarioArgument),
                                        Driver = context.GetRequiredValue (driverOption) ?? string.Empty,
                                        DontEnableConfigurationManagement = context.GetRequiredValue (disableConfigManagement),
                                        Benchmark = context.GetRequiredValue (benchmarkFlag),
                                        BenchmarkTimeout = context.GetRequiredValue (benchmarkTimeout),
                                        ResultsFile = context.GetRequiredValue (resultsFile) ?? string.Empty,
                                        DebugLogLevel = context.GetRequiredValue (debugLogLevel) ?? "Warning",

                                        // Only set Force16Colors if explicitly specified on command line
                                        Force16Colors = force16 ? true : null
                                    };

                                    // See https://github.com/dotnet/command-line-api/issues/796 for the rationale behind this hackery
                                    Options = options;
                                });

        var helpShown = false;

        rootCommand.Parse (args).Invoke ();

        if (helpShown)
        {
            return 0;
        }

        ParseResult parseResult = rootCommand.Parse (args);

        if (parseResult.Errors.Count > 0)
        {
            foreach (ParseError error in parseResult.Errors)
            {
                Console.Error.WriteLine (error.Message);
            }

            return 1; // Non-zero exit code for error
        }

        Scenario.BenchmarkTimeout = Options.BenchmarkTimeout;

        Logging.Logger = CreateLogger ();

        UICatalogMain (Options);

        return 0;
    }

    public static LogEventLevel LogLevelToLogEventLevel (LogLevel logLevel) =>
        logLevel switch
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

    private static ILogger CreateLogger ()
    {
        // Configure Serilog to write logs to a file
        LogLevelSwitch.MinimumLevel = LogLevelToLogEventLevel (Enum.Parse<LogLevel> (Options.DebugLogLevel));

        Log.Logger = new LoggerConfiguration ().MinimumLevel.ControlledBy (LogLevelSwitch)
                                               .Enrich.FromLogContext () // Enables dynamic enrichment
                                               .WriteTo.Debug ()
                                               .WriteTo.File (LogFilePath,
                                                              rollingInterval: RollingInterval.Day,
                                                              outputTemplate:
                                                              "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                                               .CreateLogger ();

        // Create a logger factory compatible with Microsoft.Extensions.Logging
        // Note: Don't use 'using' here - we need the factory to stay alive for ScenarioLogCapture
        ILoggerFactory loggerFactory = LoggerFactory.Create (builder =>
                                                             {
                                                                 builder.AddSerilog (dispose: true) // Integrate Serilog with ILogger
                                                                        .AddProvider (LogCapture) // Add in-memory capture for scenario debugging
                                                                        .SetMinimumLevel (LogLevel.Trace); // Set minimum log level
                                                             });

        // Get an ILogger instance
        return loggerFactory.CreateLogger ("UICatalog");
    }

    private static void UICatalogMain (UICatalogCommandLineOptions options)
    {
        // Create the runner for executing scenarios with runtime config options
        Runner runner = new ();

        if (!Options.DontEnableConfigurationManagement)
        {
            runner.SetRuntimeConfig (options.Driver, options.Force16Colors);
            ConfigurationManager.Enable (ConfigLocations.All);
        }
        else
        {
            Application.ForceDriver = options.Driver;

            if (options.Force16Colors is { })
            {
                Driver.Force16Colors = options.Force16Colors.Value;
            }
        }

        // If a Scenario name has been provided on the commandline
        // run it and exit when done.
        if (options.Scenario != "none")
        {
            BenchmarkResults? results = runner.RunScenario (options.Scenario, options.Benchmark);

            if (results is { })
            {
                Console.WriteLine (JsonSerializer.Serialize (results, new JsonSerializerOptions { WriteIndented = true }));
            }

#if DEBUG_IDISPOSABLE
            View.VerifyViewsWereDisposed ();
#endif

            return;
        }

        // Benchmark all Scenarios
        if (options.Benchmark)
        {
            List<BenchmarkResults> results = runner.BenchmarkAllScenarios (UICatalogRunnable.CachedScenarios!);

            if (results.Count <= 0)
            {
                return;
            }

            if (!string.IsNullOrEmpty (Options.ResultsFile))
            {
                Runner.SaveResultsToFile (results, Options.ResultsFile);
            }
            else
            {
                Runner.DisplayResultsUI (results);
            }

            return;
        }

        runner.RunInteractive<UICatalogRunnable> (!Options.DontEnableConfigurationManagement);
    }
}
