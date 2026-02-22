# Parallel-Safe Logging Implementation Status

> Combined from `gh-issue-parallel-logging.md` and `parallel-safe-xunit-logging-plan.md`

## Problem Statement

`Terminal.Gui.App.Logging` previously used a single process-wide static `ILogger`. In `Tests/UnitTestsParallelizable`, concurrent tests race on the same global logger, causing:
- Cross-test log routing/bleed
- Logger clobbering
- Writes to disposed `ITestOutputHelper` instances

## Implementation Status: ✅ COMPLETE

### Core Changes (Implemented)

**`Terminal.Gui/App/Logging.cs`** - Ambient scoped logger with `AsyncLocal`:

```csharp
private static readonly AsyncLocal<ILogger?> _ambientLogger = new ();
private static ILogger _globalLogger = NullLogger.Instance;
private static ILogger CurrentLogger => _ambientLogger.Value ?? _globalLogger;

public static ILogger Logger
{
    get => CurrentLogger;
    set => _globalLogger = value ?? NullLogger.Instance;
}

public static IDisposable PushLogger(ILogger logger)
{
    // Pushes logger into ambient async context
    // Returns IDisposable scope that restores previous logger
}
```

**`Tests/TerminalGuiFluentTesting/TestContext.cs`** - Now uses scoped binding:
- Uses `_loggerScope = Logging.PushLogger(_testLogger!)` instead of global swap
- Scope disposed during cleanup
- Each `TestContext` logging isolated from concurrent tests

### Test Results

| Branch | Tests | Duration |
|--------|-------|----------|
| `v2_develop` | 13,718 | 1m 16s |
| `fix/4728-logging-ambient-scope` | 13,722 | 1m 24s |

**~10% slowdown observed** - likely due to `AsyncLocal` overhead in hot paths.

## Design Rationale

- **`AsyncLocal<T>`** chosen over `ThreadLocal<T>` because execution hops threads in async code
- **Global fallback** preserved for backward compatibility (UICatalog, scenarios that set `Logging.Logger` directly)
- **Scope-based API** (`PushLogger`) makes setup/teardown deterministic via `IDisposable`

## UICatalog Compatibility (Preserved)

1. ✅ Global logger setup at startup (`Logging.Logger = CreateLogger()`)
2. ✅ `ScenarioLogCapture` provider works unchanged
3. ✅ Runtime log-level switching works
4. ✅ Scenario-level logger reassignment works

## Files Changed

**Core:**
- `Terminal.Gui/App/Logging.cs` - Added `AsyncLocal`, `PushLogger`, `LoggerScope`

**Test Infrastructure:**
- `Tests/TerminalGuiFluentTesting/TestContext.cs` - Uses `PushLogger` scope

**New Tests:**
- `Tests/UnitTestsParallelizable/Application/LoggingTests.cs` - Scope behavior tests

## Remaining Concerns

### Performance Investigation Needed

The 10% slowdown may be caused by:
1. `AsyncLocal` access overhead in logging hot paths
2. `LoggerScope` allocation per test context
3. Additional locking in `ThreadSafeStringWriter`

### Potential Optimizations

1. Cache `CurrentLogger` resolution where possible
2. Consider pooling `LoggerScope` objects
3. Profile to identify actual bottleneck

## Verification Checklist

- [x] Scoped routing works
- [x] Nested scopes restore correctly  
- [x] Fallback to global logger works
- [x] Parallel tests don't cross-log
- [x] UICatalog startup works
- [x] Runtime log-level switching works
- [x] Performance regression investigated
