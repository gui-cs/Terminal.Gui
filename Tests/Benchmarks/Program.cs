using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Terminal.Gui.Benchmarks.ViewBase;

namespace Terminal.Gui.Benchmarks;

internal class Program
{
    private static void Main (string [] args)
    {
        // Fast memory profilers: no BenchmarkDotNet overhead
        if (args.Length > 0)
        {
            switch (args [0].ToLowerInvariant ())
            {
                case "memory":
                    ViewMemoryBenchmark.Run ();

                    return;

                case "scenarios":
                case "scenario":
                    ScenarioMemoryBenchmark.Run ();

                    return;
            }
        }

        IConfig config = DefaultConfig.Instance;

        // Uncomment for faster but less accurate intermediate iteration.
        // Final benchmarks should be run with at least the default run length.
        //config = config.AddJob (BenchmarkDotNet.Jobs.Job.ShortRun);

        BenchmarkSwitcher.FromAssembly (typeof (Program).Assembly).Run (args, config);
    }
}
