# Bug: `Logging.Logger` is process-wide and breaks per-test `ITestOutputHelper` logging in `UnitTestsParallelizable`

## Summary
`Terminal.Gui.App.Logging` currently uses a single process-wide static `ILogger` (`Logging.Logger`).

This causes test interference in `Tests/UnitTestsParallelizable` when tests run concurrently and each test wants logs routed to its own `ITestOutputHelper`. One test can overwrite the logger for all other tests, causing cross-test log bleed and occasional writes to a disposed output helper.

## Affected Areas
- Core logging infrastructure: `Terminal.Gui/App/Logging.cs`
- Parallel test harness usage: `Tests/TerminalGuiFluentTesting/TestContext.cs`
- Target project: `Tests/UnitTestsParallelizable`

## Current Behavior
- `Logging` methods (`Trace`, `Debug`, etc.) resolve through a single static logger.
- `TestContext` snapshots and replaces `Logging.Logger`, then restores it on cleanup.
- In parallel execution, concurrent tests race on the same global logger.

## Expected Behavior
- Tests running in parallel should be able to bind `Logging` to their own sink (`ITestOutputHelper`) without affecting other tests.
- Log routing should be thread-safe and async-safe.
- Existing usages that intentionally set a global logger should continue to work.

## Evidence in Repo
- Global logger field: `Terminal.Gui/App/Logging.cs`
- Global swap pattern in tests: `Tests/TerminalGuiFluentTesting/TestContext.cs:205`, `Tests/TerminalGuiFluentTesting/TestContext.cs:212`, `Tests/TerminalGuiFluentTesting/TestContext.cs:545`
- Parallel enabled in target suite: `Tests/UnitTestsParallelizable/xunit.runner.json` (`parallelizeAssembly: true`, `parallelizeTestCollections: true`)

## UICatalog Compatibility Requirements (must not regress)
`UICatalog` has concrete logging behavior that this change must preserve:

1. Global logger setup at startup
- `Examples/UICatalog/UICatalog.cs:197` sets `Logging.Logger = CreateLogger();`
- This must remain a supported/global fallback path.

2. Scenario log capture pipeline
- `Examples/UICatalog/UICatalog.cs` adds `ScenarioLogCapture` as an `ILoggerProvider`.
- `Examples/UICatalog/Runner.cs:47` calls `UICatalog.LogCapture.MarkScenarioStart()` before scenario run.
- `Examples/UICatalog/UICatalogRunnable.cs` checks `UICatalog.LogCapture.HasErrors` and displays scenario logs.
- This capture and retrieval behavior must continue to work unchanged.

3. Runtime log-level switching
- `Examples/UICatalog/UICatalogRunnable.cs` updates `UICatalog.LogLevelSwitch.MinimumLevel` from the Logging menu.
- Any `Logging` changes must not break this control path.

4. Scenario-level logger reassignment behavior
- Some scenarios (e.g. `Examples/UICatalog/Scenarios/Menus.cs:23`) reassign `Logging.Logger`.
- Backward-compatible semantics for global assignment are required.

## Proposed Solution
Adopt ambient scoped logger resolution with `AsyncLocal` + global fallback.

### 1) Upgrade `Terminal.Gui/App/Logging.cs`
- Introduce:
  - `private static ILogger _globalLogger = NullLogger.Instance;`
  - `private static readonly AsyncLocal<ILogger?> _ambientLogger = new ();`
  - `private static ILogger CurrentLogger => _ambientLogger.Value ?? _globalLogger;`
- Keep `Logging.Logger` public property for compatibility:
  - `set` updates `_globalLogger`
  - `get` returns resolved current logger (or global, finalize during implementation)
- Add scope API:
  - `public static IDisposable PushLogger (ILogger logger)`
- Update `Trace/Debug/Information/Warning/Error/Critical` to use `CurrentLogger`.

### 2) Migrate test harness to scoped binding
- In `Tests/TerminalGuiFluentTesting/TestContext.cs`:
  - stop snapshot/restore of global `Logging.Logger`
  - use `IDisposable` scope from `Logging.PushLogger(...)`
  - dispose scope during cleanup

### 3) Add reusable xUnit output logger helper for parallel tests
- Add helper(s) in test utilities for binding `Logging` to `ITestOutputHelper` via scope.
- Ensure thread-safe writes and graceful handling of late writes during teardown.

## Verification Plan
1. Add unit tests for `Logging` scope behavior:
- scoped routing works
- nested scopes restore correctly
- fallback to global logger works

2. Add parallel stress tests in `UnitTestsParallelizable`:
- concurrent tasks/tests bind unique sinks and do not cross-log

3. Validate UICatalog compatibility:
- startup global logger assignment still works
- `ScenarioLogCapture.MarkScenarioStart/GetScenarioLogs/HasErrors` behavior unchanged
- runtime log-level switching still affects output

## Acceptance Criteria
- `UnitTestsParallelizable` can route Terminal.Gui logs to per-test `ITestOutputHelper` without cross-test contamination.
- No regressions in existing non-parallel tests that use `Logging.Logger` directly.
- UICatalog logging and scenario diagnostics behavior remains intact.

## Notes
A design/implementation plan is in: `plans/parallel-safe-xunit-logging-plan.md`.
