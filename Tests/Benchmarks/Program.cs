using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Terminal.Gui.Benchmarks;

class Program
{
    static void Main (string [] args)
    {
        var config = DefaultConfig.Instance;

        // Uncomment for faster but less accurate intermediate iteration.
        // Final benchmarks should be run with at least the default run length.
        //config = config.AddJob (BenchmarkDotNet.Jobs.Job.ShortRun);

        BenchmarkSwitcher
            .FromAssembly (typeof (Program).Assembly)
            .Run(args, config);
    }
}
