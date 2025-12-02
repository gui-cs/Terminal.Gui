#nullable enable
// Example Runner - Demonstrates discovering and running all examples using the example infrastructure

using System.Diagnostics.CodeAnalysis;
using Terminal.Gui.Examples;

[assembly: ExampleMetadata ("Example Runner", "Discovers and runs all examples sequentially")]
[assembly: ExampleCategory ("Infrastructure")]

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
    ExampleContext context = new ()
    {
        KeysToInject = example.DemoKeyStrokes.OrderBy (ks => ks.Order)
                          .SelectMany (ks => ks.KeyStrokes)
                          .ToList (),
        TimeoutMs = 5000,
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

return failCount == 0 ? 0 : 1;
