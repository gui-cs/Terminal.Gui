using BenchmarkDotNet.Attributes;
using Terminal.Gui.Configuration;

namespace Terminal.Gui.Benchmarks.Configuration;

/// <summary>
///     Measures the cold-start cost of loading the embedded library configuration:
///     <c>ConfigurationManager.Disable (true)</c> → <c>Enable (ConfigLocations.LibraryResources)</c> → <c>Apply ()</c>.
///     This is the app-startup hot path.
/// </summary>
/// <remarks>
///     <para>
///         Run:
///         <code>dotnet run --project Tests/Benchmarks -c Release -- --filter '*ConfigurationManagerLoad*'</code>
///     </para>
/// </remarks>
[MemoryDiagnoser]
[BenchmarkCategory ("Configuration")]
public class ConfigurationManagerLoadBenchmark
{
    /// <summary>Resets ConfigurationManager to a clean state before each iteration.</summary>
    [IterationSetup]
    public void IterationSetup ()
    {
        ConfigurationManager.Disable (true);
    }

    /// <summary>
    ///     Loads the embedded library configuration from scratch and applies it.
    ///     Captures the full deserialize + merge + apply path.
    /// </summary>
    [Benchmark]
    public void LoadAndApply ()
    {
        ConfigurationManager.Enable (ConfigLocations.LibraryResources);
        ConfigurationManager.Apply ();
    }

    /// <summary>Ensures ConfigurationManager is disabled after all iterations.</summary>
    [GlobalCleanup]
    public void Cleanup ()
    {
        ConfigurationManager.Disable (true);
    }
}
