# Terminal.Gui Benchmarks

This project contains performance benchmarks and memory profilers for Terminal.Gui.

## Memory Profilers

Fast, in-process memory measurement using `GC.GetAllocatedBytesForCurrentThread()`. Results are printed as markdown tables.

### View Memory (`memory`)

Measures memory allocated when instantiating each concrete `View` subclass. Discovers all types via reflection (same technique as `TestsAllViews`). Tracks per-view footprint for [#4696](https://github.com/gui-cs/Terminal.Gui/issues/4696).

```bash
# Print to console (note the -- separator before the command)
dotnet run --project Tests/Benchmarks -c Release -- memory

# Export for comparison
dotnet run --project Tests/Benchmarks -c Release -- memory > after.md
```

### Scenario Memory (`scenarios`)

Runs each UICatalog `Scenario` for one main-loop iteration and reports total memory allocated. Uses `StopAfterFirstIteration` for fast exit plus `Scenario.StartBenchmark()` timeout as a safety net.

```bash
# Print to console (note the -- separator before the command)
dotnet run --project Tests/Benchmarks -c Release -- scenarios

# Export for comparison
dotnet run --project Tests/Benchmarks -c Release -- scenarios > scenarios.md
```

## BenchmarkDotNet Benchmarks

Formal performance benchmarks using [BenchmarkDotNet](https://benchmarkdotnet.org/).

### Run All Benchmarks

```bash
cd Tests/Benchmarks
dotnet run -c Release
```

### Run Specific Benchmark Category

```bash
# Run only DimAuto benchmarks
dotnet run -c Release -- --filter '*DimAuto*'

# Run only TextFormatter benchmarks
dotnet run -c Release -- --filter '*TextFormatter*'
```

### Run Specific Benchmark Method

```bash
# Run only the ComplexLayout benchmark
dotnet run -c Release -- --filter '*DimAutoBenchmark.ComplexLayout*'
```

### Quick Run (Shorter but Less Accurate)

For faster iteration during development:

```bash
dotnet run -c Release -- --filter '*DimAuto*' -j short
```

### List Available Benchmarks

```bash
dotnet run -c Release -- --list flat
```

## DimAuto Benchmarks

The `DimAutoBenchmark` class tests layout performance with `Dim.Auto()` in various scenarios:

- **SimpleLayout**: Baseline with 3 subviews using basic positioning
- **ComplexLayout**: 20 subviews with mixed Pos/Dim types (tests iteration overhead)
- **DeeplyNestedLayout**: 5 levels of nested views with DimAuto (tests recursive performance)

## Adding New Benchmarks

1. Create a new class in an appropriate subdirectory (e.g., `Layout/`, `Text/`, `ViewBase/`)
2. For BenchmarkDotNet: add `[MemoryDiagnoser]`, `[BenchmarkCategory]`, `[Benchmark(Baseline = true)]`
3. For memory profilers: add a `public static void Run()` method and route it from `Program.cs`
4. Use `[GlobalSetup]`/`[GlobalCleanup]` for `Application.Init`/`Shutdown`

## Best Practices

- Always run benchmarks in **Release** configuration
- Use the `memory` / `scenarios` commands for quick allocation checks
- Use BenchmarkDotNet for formal timing benchmarks with statistical rigor
- Document what each benchmark measures

## Continuous Integration

Benchmarks are not run automatically in CI. Run them locally when:
- Making performance-critical changes
- Implementing performance optimizations
- Before releasing a new version

## Resources

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [Issue #4696 — View memory footprint](https://github.com/gui-cs/Terminal.Gui/issues/4696)
