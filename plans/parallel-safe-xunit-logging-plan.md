# Parallel-Safe xUnit Logging Plan (UnitTestsParallelizable)

## Problem Statement
`Terminal.Gui.App.Logging` currently stores a single process-wide static `ILogger` (`Logging.Logger`).

That works for non-parallel test runs, but in `Tests/UnitTestsParallelizable` multiple tests can run at the same time and each test may try to redirect logs to its own `ITestOutputHelper`. A global logger causes cross-test log routing, logger clobbering, and occasional writes to disposed `ITestOutputHelper` instances.

`Tests/TerminalGuiFluentTesting/TestContext.cs` demonstrates the current hazard: it snapshots `Logging.Logger`, sets it globally, then restores it during cleanup. This is race-prone when tests overlap.

## Goals
1. Make `Logging` support per-test/per-execution-context log routing.
2. Keep existing `Logging.*` call sites in `Terminal.Gui` source unchanged.
3. Enable easy, explicit binding of logs to xUnit `ITestOutputHelper` in parallel tests.
4. Preserve backward compatibility for existing code/tests that set `Logging.Logger` directly.

## Design Direction
Use **ambient logger scope with `AsyncLocal<ILogger?>`** plus a global fallback logger.

Rationale:
- `ThreadLocal<T>` is not sufficient for async code because execution hops threads.
- `AsyncLocal<T>` flows with `ExecutionContext` into `Task.Run` by default and maps better to xUnit async test execution.
- Scope-based API makes setup/teardown deterministic via `IDisposable`.

## Proposed API Changes
### 1) `Terminal.Gui/App/Logging.cs`
Introduce layered logger resolution:
- `private static ILogger _globalLogger = NullLogger.Instance;`
- `private static readonly AsyncLocal<ILogger?> _ambientLogger = new ();`
- `private static ILogger CurrentLogger => _ambientLogger.Value ?? _globalLogger;`

Preserve compatibility:
- Keep public `Logging.Logger` property.
- `get` returns `CurrentLogger` (or `_globalLogger`, based on compatibility choice).
- `set` updates `_globalLogger` (existing behavior for legacy callers).

Add new scope API:
- `public static IDisposable PushLogger (ILogger logger)`
- Optional convenience:
  - `public static IDisposable PushLoggerFactory (Action<ILoggingBuilder> configure, string category = "Terminal.Gui")`

All logging methods (`Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`) should call `CurrentLogger` instead of directly using a single static logger field.

Implementation notes:
- `PushLogger` should support nested scopes and restore previous ambient logger in `Dispose`.
- Scope token should be private nested class/struct implementing `IDisposable`.
- Keep meter/counters untouched.

### 2) Add xUnit output adapter in test utilities
Create a reusable test helper in `Tests/UnitTestsParallelizable` (or shared tests utility project):
- `ITestOutputHelperWriter` / `XunitOutputTextWriter` wrapping `ITestOutputHelper`.
- `ILoggerProvider` + `ILogger` that writes through this writer.
- Internal lock around each write for thread-safe line emission.
- Catch and ignore `InvalidOperationException` from late/disposed output helper writes (common during teardown races).

This replaces ad-hoc `TextWriter` wiring and makes the path explicit for parallel tests.

### 3) Update `Tests/TerminalGuiFluentTesting/TestContext.cs`
Replace global logger swap:
- Remove `_originalLogger` snapshot/restore behavior.
- Add field: `private IDisposable? _loggingScope;`
- In `CommonInit`, build logger as today, then call `Logging.PushLogger(logger)` and store returned scope.
- In cleanup/dispose, dispose `_loggingScope`.

This makes each `TestContext` logging isolated from other concurrently running tests.

### 4) Optional compatibility helper for non-Fluent tests
Add small helper for direct unit tests:
- `using IDisposable _ = UnitTestLogging.BindTo(outputHelper);`

This allows any test class to bind `Logging` to its own `ITestOutputHelper` without custom boilerplate.

## Files Expected to Change
Core:
- `Terminal.Gui/App/Logging.cs`

Test infra:
- `Tests/TerminalGuiFluentTesting/TestContext.cs`
- `Tests/TerminalGuiFluentTesting/TextWriterLogger.cs` (if needed for richer formatting/thread-safety)
- `Tests/TerminalGuiFluentTesting/TextWriterLoggerProvider.cs` (if scope/lifetime adjustments are needed)
- New helper(s), e.g.:
  - `Tests/UnitTestsParallelizable/Helpers/XunitOutputLogger.cs`
  - `Tests/UnitTestsParallelizable/Helpers/UnitTestLogging.cs`

Tests to add/update:
- `Tests/UnitTestsParallelizable/Application/...` new logging concurrency tests
- Possibly update `Tests/UnitTests/Application/MainLoopCoordinatorTests.cs` to assert against scoped/global semantics explicitly

## Verification Plan
1. Unit tests for `Logging` ambient behavior:
- `PushLogger` routes logs within scope.
- Nested scopes restore previous logger.
- Outside scope falls back to global logger.
- Parallel tasks with separate scopes do not cross-log.

2. Concurrency stress test (parallelizable project):
- Start N tasks; each binds unique in-memory sink via `PushLogger`.
- Emit logs concurrently via `Logging.Trace/Debug/...`.
- Assert each sink only contains its own marker.

3. Integration with `TestContext`:
- Run representative `UnitTestsParallelizable` tests that create multiple `TestContext` instances concurrently.
- Validate no logger clobbering and no disposed-output crashes.

4. Regression checks:
- Run existing non-parallel test projects to ensure direct `Logging.Logger = ...` code paths still work.

## Rollout Sequence
1. Implement `Logging` ambient scope API with compatibility-preserving global fallback.
2. Add focused `Logging` unit tests for scope + parallel behavior.
3. Migrate `TestContext` from global swap to scope-based binding.
4. Add reusable xUnit output logger helper for `UnitTestsParallelizable`.
5. Migrate selected high-value tests first; expand as needed.
6. Run full `UnitTestsParallelizable`, then broader test matrix.

## Risks and Mitigations
- Risk: Some background work may suppress `ExecutionContext` flow and lose ambient logger.
  - Mitigation: Keep global fallback; for known worker entry points, set scope inside worker startup.

- Risk: Existing tests rely on reading back `Logging.Logger` identity.
  - Mitigation: Define clear `Logger` property semantics in XML docs and adjust those tests explicitly.

- Risk: `ITestOutputHelper` lifecycle exceptions during teardown.
  - Mitigation: defensive writer/provider catches `InvalidOperationException` and no-ops after test completion.

## Reuse Opportunities from Existing Code
- Reuse `ThreadSafeStringWriter` locking pattern (`Tests/TerminalGuiFluentTesting/ThreadSafeStringWriter.cs`).
- Reuse `TextWriterLogger` / `TextWriterLoggerProvider` pattern to avoid introducing a second logging abstraction.
- Reuse `TestOutputWriter` concept from `Tests/IntegrationTests/TestOutputWriter.cs`, but harden for parallel teardown and cross-thread writes.
