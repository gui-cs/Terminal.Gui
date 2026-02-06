# Terminal.Gui Benchmarks

This project contains performance benchmarks for Terminal.Gui using [BenchmarkDotNet](https://benchmarkdotnet.org/).

## Running Benchmarks

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

### Example Output

```
BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4602/23H2/2023Update/SunValley3)
Intel Core i7-9750H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 10.0.1 (10.0.125.52708), X64 RyuJIT AVX2
  DefaultJob : .NET 10.0.1 (10.0.125.52708), X64 RyuJIT AVX2

| Method              | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| SimpleLayout        |   5.234 μs | 0.0421 μs | 0.0394 μs |  1.00 |    0.01 | 0.3586 |   3.03 KB |        1.00 |
| ComplexLayout       |  42.561 μs | 0.8234 μs | 0.7701 μs |  8.13 |    0.17 | 2.8076 |  23.45 KB |        7.74 |
| DeeplyNestedLayout  |  25.123 μs | 0.4892 μs | 0.4577 μs |  4.80 |    0.10 | 1.7090 |  14.28 KB |        4.71 |
```

## Adding New Benchmarks

1. Create a new class in an appropriate subdirectory (e.g., `Layout/`, `Text/`)
2. Add the `[MemoryDiagnoser]` attribute to measure allocations
3. Add `[BenchmarkCategory("CategoryName")]` to group related benchmarks
4. Mark baseline scenarios with `[Benchmark(Baseline = true)]`
5. Use `[GlobalSetup]` and `[GlobalCleanup]` for initialization/cleanup

## Best Practices

- Always run benchmarks in **Release** configuration
- Run multiple iterations for accurate results (default is better than `-j short`)
- Use `[ArgumentsSource]` for parametrized benchmarks
- Include baseline scenarios for comparison
- Document what each benchmark measures

## Continuous Integration

Benchmarks are not run automatically in CI. Run them locally when:
- Making performance-critical changes
- Implementing performance optimizations
- Before releasing a new version

## Resources

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [Performance Analysis Plan](../../plans/dimauto-perf-plan.md)
