#nullable enable
using System.Diagnostics.CodeAnalysis;
using Terminal.Gui.Examples;
using Xunit.Abstractions;

namespace UnitTests.Parallelizable.Examples;

/// <summary>
///     Tests for the example discovery and execution infrastructure.
/// </summary>
public class ExampleTests
{
    private readonly ITestOutputHelper _output;

    public ExampleTests (ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    ///     Discovers all examples by looking for assemblies with ExampleMetadata attributes.
    /// </summary>
    /// <returns>Test data for all discovered examples.</returns>
    [RequiresUnreferencedCode ("Calls ExampleDiscovery.DiscoverFromDirectory")]
    [RequiresDynamicCode ("Calls ExampleDiscovery.DiscoverFromDirectory")]
    public static IEnumerable<object []> AllExamples ()
    {
        // Navigate from test assembly location to repository root, then to Examples directory
        // Test output is typically at: Tests/UnitTestsParallelizable/bin/Debug/net8.0/
        // Examples are at: Examples/
        string examplesDir = Path.GetFullPath (Path.Combine (AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Examples"));

        if (!Directory.Exists (examplesDir))
        {
            return [];
        }

        List<ExampleInfo> examples = ExampleDiscovery.DiscoverFromDirectory (examplesDir).ToList ();

        if (examples.Count == 0)
        {
            return [];
        }

        return examples.Select (e => new object [] { e });
    }

    [Theory]
    [MemberData (nameof (AllExamples))]
    public void Example_Has_Metadata (ExampleInfo example)
    {
        Assert.NotNull (example);
        Assert.False (string.IsNullOrWhiteSpace (example.Name), "Example name should not be empty");
        Assert.False (string.IsNullOrWhiteSpace (example.Description), "Example description should not be empty");
        Assert.True (File.Exists (example.AssemblyPath), $"Example assembly should exist: {example.AssemblyPath}");

        _output.WriteLine ($"Example: {example.Name}");
        _output.WriteLine ($"  Description: {example.Description}");
        _output.WriteLine ($"  Categories: {string.Join (", ", example.Categories)}");
        _output.WriteLine ($"  Assembly: {example.AssemblyPath}");
    }

    [Theory]
    [MemberData (nameof (AllExamples))]
    public void All_Examples_Quit_And_Init_Shutdown_Properly_OutOfProcess (ExampleInfo example)
    {
        _output.WriteLine ($"Running example '{example.Name}' out-of-process");

        ExampleContext context = new ()
        {
            DriverName = "FakeDriver",
            KeysToInject = new () { "Esc" },
            TimeoutMs = 5000,
            CollectMetrics = false,
            Mode = ExecutionMode.OutOfProcess
        };

        ExampleResult result = ExampleRunner.Run (example, context);

        if (!result.Success)
        {
            _output.WriteLine ($"Example failed: {result.ErrorMessage}");

            if (!string.IsNullOrEmpty (result.StandardOutput))
            {
                _output.WriteLine ($"Standard Output:\n{result.StandardOutput}");
            }

            if (!string.IsNullOrEmpty (result.StandardError))
            {
                _output.WriteLine ($"Standard Error:\n{result.StandardError}");
            }
        }

        Assert.True (result.Success, $"Example '{example.Name}' should complete successfully");
        Assert.False (result.TimedOut, $"Example '{example.Name}' should not timeout");
        Assert.Equal (0, result.ExitCode);
    }

    [Theory]
    [MemberData (nameof (AllExamples))]
    public void All_Examples_Quit_And_Init_Shutdown_Properly_InProcess (ExampleInfo example)
    {
        _output.WriteLine ($"Running example '{example.Name}' in-process");

        // Force a complete reset to ensure clean state
        Application.ResetState (true);

        ExampleContext context = new ()
        {
            DriverName = "FakeDriver",
            KeysToInject = new () { "Esc" },
            TimeoutMs = 5000,
            CollectMetrics = false,
            Mode = ExecutionMode.InProcess
        };

        ExampleResult result = ExampleRunner.Run (example, context);

        if (!result.Success)
        {
            _output.WriteLine ($"Example failed: {result.ErrorMessage}");
        }

        // Reset state after in-process execution
        Application.ResetState (true);

        Assert.True (result.Success, $"Example '{example.Name}' should complete successfully");
        Assert.False (result.TimedOut, $"Example '{example.Name}' should not timeout");
    }

    [Fact]
    public void ExampleContext_Serialization_Works ()
    {
        ExampleContext context = new ()
        {
            DriverName = "FakeDriver",
            KeysToInject = new () { "Esc", "Enter" },
            TimeoutMs = 5000,
            MaxIterations = 100,
            CollectMetrics = true,
            Mode = ExecutionMode.InProcess
        };

        string json = context.ToJson ();
        Assert.False (string.IsNullOrWhiteSpace (json));

        ExampleContext? deserialized = ExampleContext.FromJson (json);
        Assert.NotNull (deserialized);
        Assert.Equal (context.DriverName, deserialized.DriverName);
        Assert.Equal (context.TimeoutMs, deserialized.TimeoutMs);
        Assert.Equal (context.MaxIterations, deserialized.MaxIterations);
        Assert.Equal (context.CollectMetrics, deserialized.CollectMetrics);
        Assert.Equal (context.Mode, deserialized.Mode);
        Assert.Equal (context.KeysToInject.Count, deserialized.KeysToInject.Count);
    }
}
