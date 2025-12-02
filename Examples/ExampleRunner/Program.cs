#nullable enable
// Example Runner - Demonstrates discovering and running all examples using the example infrastructure

using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.Examples;
using ILogger = Microsoft.Extensions.Logging.ILogger;

// Configure Serilog to write to Debug output and Console
Log.Logger = new LoggerConfiguration ()
    .MinimumLevel.Is (LogEventLevel.Verbose)
                                  .WriteTo.Debug ()
                                  .CreateLogger ();

ILogger logger = LoggerFactory.Create (builder =>
                                       {
                                           builder
                                               .AddSerilog (dispose: true) // Integrate Serilog with ILogger
                                               .SetMinimumLevel (LogLevel.Trace); // Set minimum log level
                                       }).CreateLogger ("ExampleRunner Logging");
Logging.Logger = logger;

Logging.Debug ("Logging enabled - writing to Debug output\n");

// Parse command line arguments
bool useFakeDriver = args.Contains ("--fake-driver") || args.Contains ("-f");
int timeout = 30000; // Default timeout in milliseconds

for (var i = 0; i < args.Length; i++)
{
    if ((args [i] == "--timeout" || args [i] == "-t") && i + 1 < args.Length)
    {
        if (int.TryParse (args [i + 1], out int parsedTimeout))
        {
            timeout = parsedTimeout;
        }
    }
}

// Configure ForceDriver via ConfigurationManager if requested
if (useFakeDriver)
{
    Console.WriteLine ("Using FakeDriver (forced via ConfigurationManager)\n");
    ConfigurationManager.RuntimeConfig = """{ "ForceDriver": "FakeDriver" }""";
    ConfigurationManager.Enable (ConfigLocations.All);
}

// Discover examples from the Examples directory
string? assemblyDir = Path.GetDirectoryName (System.Reflection.Assembly.GetExecutingAssembly ().Location);

if (assemblyDir is null)
{
    Console.WriteLine ("Error: Could not determine assembly directory");

    return 1;
}

// Go up to find the Examples directory - from bin/Debug/net8.0 to Examples
string examplesDir = Path.GetFullPath (Path.Combine (assemblyDir, "..", "..", "..", ".."));

if (!Directory.Exists (examplesDir))
{
    Console.WriteLine ($"Error: Examples directory not found: {examplesDir}");

    return 1;
}

Console.WriteLine ($"Searching for examples in: {examplesDir}\n");

// Discover all examples - look specifically in each example's bin directory
List<ExampleInfo> examples = [];
HashSet<string> seen = [];

foreach (string dir in Directory.GetDirectories (examplesDir))
{
    string binDir = Path.Combine (dir, "bin", "Debug", "net8.0");

    if (!Directory.Exists (binDir))
    {
        continue;
    }

    foreach (ExampleInfo example in ExampleDiscovery.DiscoverFromDirectory (binDir, "*.dll", SearchOption.TopDirectoryOnly))
    {
        // Don't include this runner in the list and avoid duplicates
        if (example.Name != "Example Runner" && seen.Add (example.Name))
        {
            examples.Add (example);
        }
    }
}

Console.WriteLine ($"Discovered {examples.Count} examples\n");

// Run all examples sequentially
var successCount = 0;
var failCount = 0;

foreach (ExampleInfo example in examples)
{
    Console.Write ($"Running: {example.Name,-40} ");

    // Create context for running the example
    // Note: When running with example mode, the demo keys from attributes will be used
    // We don't need to inject additional keys via the context
    ExampleContext context = new ()
    {
        DriverName = useFakeDriver ? "FakeDriver" : null,
        KeysToInject = [], // Empty - let example mode handle keys from attributes
        TimeoutMs = timeout,
        Mode = ExecutionMode.InProcess
    };

    try
    {
        ExampleResult result = ExampleRunner.Run (example, context);

        if (result.Success)
        {
            Console.WriteLine ($"✓ Success");
            successCount++;
        }
        else if (result.TimedOut)
        {
            Console.WriteLine ($"✗ Timeout");
            failCount++;
        }
        else
        {
            Console.WriteLine ($"✗ Failed: {result.ErrorMessage ?? "Unknown"}");
            failCount++;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine ($"✗ Exception: {ex.Message}");
        failCount++;
    }
}

Console.WriteLine ($"\n=== Summary: {successCount} passed, {failCount} failed ===");

if (useFakeDriver)
{
    Console.WriteLine ("\nNote: Tests run with FakeDriver. Some examples may timeout if they don't respond to Esc key.");
}

// Flush logs before exiting
Log.CloseAndFlush ();

return failCount == 0 ? 0 : 1;
