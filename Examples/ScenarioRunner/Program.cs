#nullable enable
using System.Collections.ObjectModel;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drivers;
using UICatalog;

namespace ScenarioRunner;

/// <summary>
///     Command-line tool for running and benchmarking Terminal.Gui scenarios.
/// </summary>
public static class Program
{
    private static LoggingLevelSwitch LogLevelSwitch { get; } = new ();
    private const string LOGFILE_LOCATION = "logs";
    private static string LogFilePath { get; set; } = string.Empty;

    public static int Main (string [] args)
    {
        Console.OutputEncoding = Encoding.Default;

        // Process command line args
        // If no driver is provided, the default driver is used.
        // Get allowed driver names
        string? [] allowedDrivers = DriverRegistry.GetDriverNames ().ToArray ();

        Option<string> driverOption = new Option<string> ("--driver", "The IDriver to use.")
            .FromAmong (allowedDrivers!);
        driverOption.SetDefaultValue (string.Empty);
        driverOption.AddAlias ("-d");

        Option<bool> disableConfigManagement = new (
                                                    "--disable-cm",
                                                    "Indicates Configuration Management should not be enabled.");
        disableConfigManagement.AddAlias ("-dcm");

        Option<bool> force16Colors = new (
                                          "--force-16-colors",
                                          "Forces the driver to use 16-color mode instead of TrueColor.");
        force16Colors.AddAlias ("-16");

        Option<uint> benchmarkTimeout = new (
                                             "--timeout",
                                             () => Scenario.BenchmarkTimeout,
                                             $"The maximum time in milliseconds to run a benchmark. Default is {Scenario.BenchmarkTimeout}ms.");
        benchmarkTimeout.AddAlias ("-t");

        Option<string> resultsFile = new ("--file", "The file to save benchmark results to.");
        resultsFile.AddAlias ("-f");

        LogFilePath = $"{LOGFILE_LOCATION}/{Assembly.GetExecutingAssembly ().GetName ().Name}";

        Option<string> debugLogLevel = new Option<string> ("--debug-log-level", "The level to use for logging.")
            .FromAmong (Enum.GetNames<LogLevel> ());
        debugLogLevel.SetDefaultValue ("Warning");
        debugLogLevel.AddAlias ("-dl");

        // List command
        Command listCommand = new ("list", "List all available scenarios");

        listCommand.SetHandler (() =>
                                {
                                    ObservableCollection<Scenario> scenarios = Scenario.GetScenarios ();

                                    Console.WriteLine (@$"Available scenarios ({scenarios.Count})");
                                    Console.WriteLine ();

                                    foreach (Scenario s in scenarios)
                                    {
                                        Console.WriteLine (@$"  {s.GetName (),-30} {s.GetDescription ()}");
                                    }
                                });

        // Run command
        Argument<string> scenarioArgument = new (
                                                 "scenario",
                                                 "The name of the Scenario to run.");

        Command runCommand = new ("run", "Run a specific scenario")
        {
            scenarioArgument
        };

        runCommand.AddOption (driverOption);
        runCommand.AddOption (disableConfigManagement);
        runCommand.AddOption (force16Colors);
        runCommand.AddOption (debugLogLevel);

        runCommand.SetHandler (
                               (scenarioName, driver, disableCm, force16, logLevel) =>
                               {
                                   SetupLogging (logLevel);

                                   if (!disableCm)
                                   {
                                       ConfigurationManager.Enable (ConfigLocations.All);
                                   }

                                   Scenario? scenario = FindScenario (scenarioName);

                                   if (scenario is null)
                                   {
                                       Console.Error.WriteLine ($"Scenario '{scenarioName}' not found.");

                                       return;
                                   }

                                   // Pass force16 only if explicitly set (default false means not set)
                                   Runner runner = new (driver, force16 ? true : null);
                                   runner.RunScenario (scenarioName, false);
                               },
                               scenarioArgument,
                               driverOption,
                               disableConfigManagement,
                               force16Colors,
                               debugLogLevel);

        // Benchmark command
        Argument<string?> benchmarkScenarioArgument = new (
                                                           "scenario",
                                                           () => null,
                                                           "The name of the Scenario to benchmark. If not specified, all scenarios are benchmarked.");

        Command benchmarkCommand = new ("benchmark", "Benchmark scenarios")
        {
            benchmarkScenarioArgument
        };

        benchmarkCommand.AddOption (driverOption);
        benchmarkCommand.AddOption (disableConfigManagement);
        benchmarkCommand.AddOption (force16Colors);
        benchmarkCommand.AddOption (benchmarkTimeout);
        benchmarkCommand.AddOption (resultsFile);
        benchmarkCommand.AddOption (debugLogLevel);

        benchmarkCommand.SetHandler (
                                     (scenarioName, driver, disableCm, force16, timeout, file, logLevel) =>
                                     {
                                         SetupLogging (logLevel);
                                         Scenario.BenchmarkTimeout = timeout;

                                         if (!disableCm)
                                         {
                                             ConfigurationManager.Enable (ConfigLocations.All);
                                         }

                                         // Pass force16 only if explicitly set
                                         Runner runner = new (driver, force16 ? true : null);
                                         List<BenchmarkResults> results;

                                         if (string.IsNullOrEmpty (scenarioName))
                                         {
                                             // Benchmark all scenarios
                                             ObservableCollection<Scenario> scenarios = Scenario.GetScenarios ();
                                             results = runner.BenchmarkAllScenarios (scenarios);
                                         }
                                         else
                                         {
                                             // Benchmark single scenario
                                             Scenario? scenario = FindScenario (scenarioName);

                                             if (scenario is null)
                                             {
                                                 Console.Error.WriteLine ($"Scenario '{scenarioName}' not found.");

                                                 return;
                                             }

                                             BenchmarkResults? result = runner.RunScenario (scenarioName, true);
                                             results = result is { } ? [result] : [];
                                         }

                                         if (results.Count == 0)
                                         {
                                             Console.WriteLine (@"No benchmark results collected.");

                                             return;
                                         }

                                         if (!string.IsNullOrEmpty (file))
                                         {
                                             Runner.SaveResultsToFile (results, file);
                                             Console.WriteLine (@$"Results saved to {file}");
                                         }
                                         else
                                         {
                                             // Display in UI
                                             Runner.DisplayResultsUI (results);
                                         }
                                     },
                                     benchmarkScenarioArgument,
                                     driverOption,
                                     disableConfigManagement,
                                     force16Colors,
                                     benchmarkTimeout,
                                     resultsFile,
                                     debugLogLevel);

        RootCommand rootCommand = new ("Terminal.Gui Scenario Runner - Run and benchmark Terminal.Gui scenarios")
        {
            listCommand,
            runCommand,
            benchmarkCommand
        };

        Parser parser = new CommandLineBuilder (rootCommand)
                        .UseDefaults ()
                        .Build ();

        return parser.Invoke (args);
    }

    private static Scenario? FindScenario (string name)
    {
        ObservableCollection<Scenario> scenarios = Scenario.GetScenarios ();

        return scenarios.FirstOrDefault (s => s.GetName ().Equals (name, StringComparison.OrdinalIgnoreCase));
    }

    private static void SetupLogging (string logLevelName)
    {
        var logLevel = Enum.Parse<LogLevel> (logLevelName);
        LogLevelSwitch.MinimumLevel = LogLevelToLogEventLevel (logLevel);

        Log.Logger = new LoggerConfiguration ()
                     .MinimumLevel.ControlledBy (LogLevelSwitch)
                     .Enrich.FromLogContext ()
                     .WriteTo.Debug ()
                     .WriteTo.File (
                                    LogFilePath,
                                    rollingInterval: RollingInterval.Day,
                                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                     .CreateLogger ();

        using ILoggerFactory loggerFactory = LoggerFactory.Create (builder =>
                                                                   {
                                                                       builder
                                                                           .AddSerilog (dispose: true)
                                                                           .SetMinimumLevel (LogLevel.Trace);
                                                                   });

        Logging.Logger = loggerFactory.CreateLogger ("ScenarioRunner");
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
                   _ => LogEventLevel.Fatal
               };
    }
}
