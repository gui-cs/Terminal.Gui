# Fix: ANSI Driver Screen Size Stuck at 80×25

## Status

**Primary bug is fixed** (commit `30ae31719`, merged into HEAD).  
All 14,753 parallelizable unit tests pass.

Remaining work: code-review issues in the fix + missing `AnsiSizeMonitor` behavioral tests.

---

## Problem Statement

When running UICatalog with the ANSI driver on Windows (or any platform), the screen
size is permanently stuck at 80×25. Terminal resize events are never reported.

## Root Cause Analysis

### The Call Chain

```
ApplicationMainLoop.IterationImpl()
  └─ SizeMonitor.Poll()                        ← called every iteration
       └─ AnsiOutput.GetSize()                 ← returns _consoleSize ALWAYS
            └─ _consoleSize == new Size(80,25) ← set in constructor, NEVER changes
```

### Why It Breaks

`AnsiComponentFactory.CreateSizeMonitor()` only used `AnsiSizeMonitor` when
`Driver.SizeDetection == SizeDetectionMode.AnsiQuery`. The default was
`SizeDetectionMode.Polling`, which fell back to `SizeMonitorImpl(ansiOutput)`.

`SizeMonitorImpl.Poll()` calls `IOutput.GetSize()`. For `NetOutput` and
`WindowsOutput` this queries a live OS API. For `AnsiOutput`, `GetSize()` returned
the cached `_consoleSize` field — initialized to `(80,25)` and only updated by the
ANSI-query path. Result: `SizeMonitorImpl` always saw the same size; `SizeChanged`
was never raised.

---

## What Was Fixed (commit 30ae31719)

### 1 — `SizeDetectionMode` default swapped

`AnsiQuery` is now the default (value 0); `Polling` is opt-in. This means the
ANSI driver now uses `AnsiSizeMonitor` by default.

### 2 — `AnsiComponentFactory.CreateSizeMonitor()` restructured

- `AnsiOutput` + `AnsiQuery` (default) → `AnsiSizeMonitor` ✅
- `AnsiOutput` + `Polling` → injects `NativeSizeQuery` delegate into `AnsiOutput`,
  then returns `SizeMonitorImpl` (delegate calls `Console.WindowWidth/Height` on
  Windows or `ioctl(TIOCGWINSZ)` on Unix) ✅
- Non-`AnsiOutput` → `SizeMonitorImpl` (unchanged) ✅

The platform-specific code lives in `AnsiComponentFactory.CreateNativeSizeQuery()`
— NOT in `AnsiOutput` — keeping `AnsiOutput` platform-agnostic.

### 3 — `AnsiOutput.NativeSizeQuery` property added

`GetSize()` now calls the delegate (if set) to get a live OS size, then caches
and returns it. When `null` (the `AnsiQuery` default), returns the cached constant
as before.

### 4 — `SizeMonitorImpl` constructor fixed

Previously used a primary constructor with `_lastSize = Size.Empty`. Now explicitly
captures the initial size from `consoleOut.GetSize()` at construction, so the
first `Poll()` only fires if the size has genuinely changed.

### 5 — Tests added / updated

- **New:** `AnsiComponentFactorySizeMonitorTests.cs` — 6 factory-level tests
- **Updated:** `SizeMonitorTests.cs` — 3 tests for `SizeMonitorImpl` initial-size behaviour

---

## CSI 18t Compatibility

CSI 18t (xterm window manipulation) is supported by every terminal that the
ANSI driver requires:

| Environment | CSI 18t supported? | Notes |
|---|---|---|
| **Windows Terminal** (wt.exe) | ✅ Yes | |
| **Windows Console Host** (conhost.exe, Win10+) | ✅ Yes | VT mode is enabled by the driver first |
| **xterm** | ✅ Yes | Reference implementation |
| **VTE terminals** (GNOME Terminal, etc.) | ✅ Yes | |
| **macOS Terminal.app** | ✅ Yes | |
| **iTerm2** | ✅ Yes | |
| **SSH with xterm/modern client terminal** | ✅ Yes | SSH is transparent; the client terminal matters |
| **tmux / screen** | ⚠️ Varies | Forwards or intercepts depending on config |
| **TERM=vt100 or TERM=dumb** | ❌ No | No xterm extensions; ANSI driver can't function here |

Where CSI 18t fails (and the ANSI driver itself can't function), the
`AnsiRequestScheduler` 1-second stale timeout evicts the request cleanly —
no hang, just stuck at 80×25 (same as before the fix).

---

## Remaining Work

### Fix A — `catch (IOException)` in `CreateNativeSizeQuery` Windows path (bug)

**File:** `Terminal.Gui/Drivers/AnsiDriver/AnsiComponentFactory.cs` line 92

`Console.WindowWidth` does NOT throw `IOException`. It throws:
- `System.InvalidOperationException` (when attached to a non-interactive shell)
- `System.IO.IOException` (rare, e.g., broken pipe on some configurations)
- Other platform exceptions

The catch block must be `catch (Exception ex)` to be robust.

```csharp
// Before (only catches IOException, misses InvalidOperationException etc.):
catch (IOException ex)
{
    Logging.Trace (...);
    return null;
}

// After:
catch (Exception ex)
{
    Trace.Lifecycle (nameof (AnsiComponentFactory), "NativeSizeQuery", $"Console size query failed: {ex.Message}");
    return null;
}
```

### Fix B — `Logging.Trace` → `Trace.Lifecycle` (consistency)

**File:** `Terminal.Gui/Drivers/AnsiDriver/AnsiComponentFactory.cs` line 94

The codebase uses `Trace.Lifecycle(component, phase, message)` throughout
`AnsiOutput`, `AnsiSizeMonitor`, etc. The fix commit used `Logging.Trace(...)` 
(old pattern). Change to `Trace.Lifecycle(...)`.

### Fix C — Add `Trace.Lifecycle` instrumentation (user requirement)

The user asked: "Instead of reasoning over code use TestLogging and trace to
actually trace execution." Trace points are needed so tests can assert the
execution path via `ListBackend`.

| File | Location | What to trace |
|---|---|---|
| `AnsiSizeMonitor` | `Initialize()` | Driver set up, initial query sent |
| `AnsiSizeMonitor` | `SendSizeQuery()` | Query dispatched or throttled |
| `AnsiSizeMonitor` | `HandleSizeResponse()` | Raw response, parse success/fail |
| `AnsiSizeMonitor` | `CheckSizeChanged()` | Size unchanged or new size |
| `SizeMonitorImpl` | `Poll()` | Size checked, old→new or unchanged |
| `DriverImpl` | `OnSizeMonitorOnSizeChanged()` | Event received |
| `DriverImpl` | `SetScreenSize()` | New size applied |

Replace existing commented-out `//Logging.Trace` stubs with proper `Trace.Lifecycle(...)` calls.

### Tests — New `AnsiSizeMonitorTests.cs`

**File:** `Tests/UnitTestsParallelizable/Drivers/AnsiDriver/AnsiSizeMonitorTests.cs` (new)

| Test | What it verifies |
|---|---|
| `HandleSizeResponse_Updates_Size_And_Raises_Event` | Parsing `"[8;30;100t"` fires `SizeChanged(100×30)` |
| `HandleSizeResponse_NoChange_Does_Not_Raise_Event` | Same size → event not raised |
| `Poll_Sends_Query_When_Not_Throttled` | `Poll()` after 500 ms queues a new ANSI request |
| `Poll_Does_Not_Send_Query_When_Throttled` | Second `Poll()` within 500 ms does not queue |
| `Initialize_Sends_Initial_Query` | `Initialize(driver)` queues the first CSI 18t request |
| `SizeChange_Propagates_To_Driver_SizeChanged` | Full chain: monitor → `DriverImpl.OnSizeMonitorOnSizeChanged` → `IDriver.SizeChanged` |
| `Traces_Execution_Via_ListBackend` | Enables `TraceCategory.Lifecycle` with `ListBackend`, triggers a size change, asserts trace entries contain expected phase strings |

---

## Files to Change (Remaining)

| File | Change |
|---|---|
| `Terminal.Gui/Drivers/AnsiDriver/AnsiComponentFactory.cs` | Fix A + Fix B |
| `Terminal.Gui/Drivers/AnsiDriver/AnsiSizeMonitor.cs` | Fix C — add `Trace.Lifecycle` |
| `Terminal.Gui/Drivers/SizeMonitorImpl.cs` | Fix C — add `Trace.Lifecycle` |
| `Terminal.Gui/Drivers/DriverImpl.cs` | Fix C — add `Trace.Lifecycle` |
| `Tests/.../AnsiDriver/AnsiSizeMonitorTests.cs` | New test file |

---

## Verification

```bash
dotnet build --no-restore
dotnet test --project Tests/UnitTestsParallelizable --no-build
```

Manual: run UICatalog with `--driver=ansi` and resize the terminal window; the UI
should reflow correctly instead of staying frozen at 80×25.


When running UICatalog with the ANSI driver on Windows (or any platform), the screen
size is permanently stuck at 80×25. Terminal resize events are never reported.

## Root Cause Analysis

### The Call Chain

```
ApplicationMainLoop.IterationImpl()
  └─ SizeMonitor.Poll()                        ← called every iteration
       └─ AnsiOutput.GetSize()                 ← returns _consoleSize ALWAYS
            └─ _consoleSize == new Size(80,25) ← set in constructor, NEVER changes
```

### Why It Breaks

`ComponentFactoryImpl<T>.CreateSizeMonitor()` (the base-class default) creates
`SizeMonitorImpl(consoleOutput)`. `AnsiComponentFactory` overrides this but only
uses `AnsiSizeMonitor` when `Driver.SizeDetection == SizeDetectionMode.AnsiQuery`.
When `SizeDetection == Polling` (the **default**), it falls back to
`SizeMonitorImpl(ansiOutput)`.

`SizeMonitorImpl.Poll()` calls `IOutput.GetSize()`. For `NetOutput` and
`WindowsOutput` this queries a live OS API. For `AnsiOutput`, `GetSize()` returns
the cached `_consoleSize` field — which is initialized to `(80,25)` and is only
updated by `HandleSizeQueryResponse()` (the ANSI-query path).

Result: `SizeMonitorImpl` always sees the same size; `SizeChanged` is never raised.

### Why AnsiOutput.GetSize() Is Correct By Design

`AnsiOutput` is intentionally OS-agnostic. It must not call Win32 P/Invoke or
Unix `ioctl` — those would be Windows/Unix dependencies.

### Is CSI 18t "Universally Supported"? — Essentially Yes, For the ANSI Driver's Environment

The claim that CSI 18t is "universally supported" is **accurate within the ANSI
driver's required operating environment**:

| Environment | CSI 18t supported? | Notes |
|---|---|---|
| **Windows Terminal** (wt.exe) | ✅ Yes | |
| **Windows Console Host** (conhost.exe, Win10+) | ✅ Yes | VT mode is enabled by the driver (`ENABLE_VIRTUAL_TERMINAL_PROCESSING`/`INPUT`); modern conhost supports CSI 18t |
| **xterm** | ✅ Yes | Reference implementation |
| **VTE terminals** (GNOME Terminal, etc.) | ✅ Yes | |
| **macOS Terminal.app** | ✅ Yes | |
| **iTerm2** | ✅ Yes | |
| **tmux / screen** | ⚠️ Varies | Forwards or intercepts depending on config |
| **SSH (any client with xterm/VTE terminal)** | ✅ Yes | SSH is transparent; the *client terminal* is what matters |
| **TERM=vt100 or TERM=dumb** | ❌ No | These advertise no xterm extensions; the ANSI driver itself can't function here |

**Conclusion:** The ANSI driver enables VT processing mode before operating
(via `WindowsVTOutputHelper` on Windows and raw-mode on Unix). Within that VT
context — which is a prerequisite for the ANSI driver to work at all — CSI 18t
is supported. Terminals that don't support it also don't support enough VT for
the driver to function.

**What happens when CSI 18t is not answered?** The `AnsiRequestScheduler` has a
`_staleTimeout` of 1 second; if no response arrives, the request is evicted
(calls `Abandoned`, clearing `_expectingResponse`). The next `Poll()` 500 ms
later sends a new query. This loops harmlessly, keeping the size at 80×25 — the
same outcome as the broken `SizeMonitorImpl` path, so no regression.

**On Windows with VT mode:** `WindowsVTInputHelper` enables
`ENABLE_VIRTUAL_TERMINAL_INPUT`, converting keyboard/mouse to ANSI sequences.
Window resize events are NOT automatically sent as VT sequences through stdin in
this mode; they come as `WINDOW_BUFFER_SIZE_EVENT` records via Win32 API. The
ANSI driver has no access to those (by design). CSI 18t polling via
`AnsiSizeMonitor` is therefore the only resize detection mechanism available for
the ANSI driver without adding Win32 dependencies — and it works correctly on
both Windows Terminal and modern conhost.

### The Correct Monitor for AnsiOutput

`AnsiSizeMonitor` is already the correct implementation. It:
1. Sends `CSI 18t` via `QueueAnsiRequest` on every Poll (throttled to 500 ms)
2. Parses the `ESC [ 8 ; height ; width t` response asynchronously
3. Calls `AnsiOutput.HandleSizeQueryResponse()` to update `_consoleSize`
4. Then calls `CheckSizeChanged()` which compares `_output.GetSize()` against
   `_lastSize` and raises `SizeChanged` if different

`SizeMonitorImpl` is only correct for outputs whose `GetSize()` calls a live OS
API on every invocation (NetOutput, WindowsOutput).

---

## Proposed Fix

### 1 — `AnsiComponentFactory.CreateSizeMonitor()` — always return `AnsiSizeMonitor`

`AnsiSizeMonitor` must be used unconditionally for `AnsiOutput`. The `Polling`
setting of `Driver.SizeDetection` is not meaningful for the ANSI driver; the ANSI
escape-sequence query IS the polling mechanism for this driver.

```csharp
public override ISizeMonitor CreateSizeMonitor(IOutput consoleOutput, IOutputBuffer outputBuffer)
{
    if (_injectedSizeMonitor is { }) return _injectedSizeMonitor;

    // AnsiOutput.GetSize() returns a cached constant; SizeMonitorImpl would never
    // detect a change. AnsiSizeMonitor is the only correct monitor for AnsiOutput.
    // Driver.SizeDetection == Polling is silently treated as AnsiQuery here
    // because native-API polling is incompatible with AnsiOutput by design.
    if (consoleOutput is AnsiOutput ansiOutput)
    {
        Trace.Lifecycle(...);
        return new AnsiSizeMonitor(ansiOutput, queueAnsiRequest: null);
    }

    return new SizeMonitorImpl(consoleOutput); // fallback, non-AnsiOutput
}
```

Update the doc comment on the `sizeMonitor` constructor parameter (currently says
"chosen based on `Driver.SizeDetection`") to reflect the corrected logic.

### 2 — `SizeDetectionMode` XML doc update

Add a note that `Polling` does not apply to the ANSI driver, which always uses
the `AnsiQuery` mechanism.

### 3 — Add `Trace.Lifecycle` instrumentation

Enable the size-change notification chain to be traced in tests and production
using `TraceCategory.Lifecycle` + `ListBackend`.

| Location | What to trace |
|---|---|
| `AnsiSizeMonitor.Initialize()` | driver set up, initial query sent |
| `AnsiSizeMonitor.SendSizeQuery()` | query dispatched, throttle skipped |
| `AnsiSizeMonitor.HandleSizeResponse()` | raw response text, parse success/fail |
| `AnsiSizeMonitor.CheckSizeChanged()` | old size → new size, or no-change |
| `SizeMonitorImpl.Poll()` | size checked, old→new or no-change |
| `DriverImpl.OnSizeMonitorOnSizeChanged()` | event received from monitor |
| `DriverImpl.SetScreenSize()` | new width×height applied |

Replace the commented-out `Logging.Trace` calls that already exist in these
methods with proper `Trace.Lifecycle(...)` calls.

### 4 — Tests (`Tests/UnitTestsParallelizable/Drivers/`)

New file: `AnsiDriver/AnsiSizeMonitorTests.cs`

| Test | What it verifies |
|---|---|
| `AnsiComponentFactory_CreateSizeMonitor_Returns_AnsiSizeMonitor` | factory always returns `AnsiSizeMonitor` for `AnsiOutput`, regardless of `Driver.SizeDetection` |
| `AnsiSizeMonitor_HandleSizeResponse_Updates_Size_And_Raises_Event` | parsing `"[8;30;100t"` fires `SizeChanged(100×30)` |
| `AnsiSizeMonitor_HandleSizeResponse_NoChange_Does_Not_Raise_Event` | same size → event not raised |
| `AnsiSizeMonitor_Poll_Sends_Query_When_Not_Throttled` | `Poll()` after throttle window queues a new ANSI request |
| `AnsiSizeMonitor_Poll_Does_Not_Send_Query_When_Throttled` | second `Poll()` within 500 ms does not queue another request |
| `AnsiSizeMonitor_Initialize_Sends_Initial_Query` | `Initialize(driver)` queues the first CSI 18t request |
| `AnsiSizeMonitor_SizeChange_Propagates_To_Driver_SizeChanged` | full chain: monitor → `DriverImpl.OnSizeMonitorOnSizeChanged` → `IDriver.SizeChanged` |
| `AnsiSizeMonitor_Traces_Execution_Via_ListBackend` | enables `TraceCategory.Lifecycle` with a `ListBackend`, triggers a size change, asserts trace entries contain expected phase strings |

Update `SizeMonitorTests.cs`:

| Test | Change |
|---|---|
| Existing `SizeMonitorImpl` tests | No change; they already use `Mock<IOutput>` correctly |
| `SizeMonitorImpl_Does_Not_Fire_For_AnsiOutput_Returning_Constant` | **new** — documents and asserts that `SizeMonitorImpl` with a constant-returning `IOutput` never fires `SizeChanged`, confirming why the refactor was necessary |

---

## Files Changed

| File | Change |
|---|---|
| `Terminal.Gui/Drivers/AnsiDriver/AnsiComponentFactory.cs` | Always use `AnsiSizeMonitor` for `AnsiOutput`; update constructor doc |
| `Terminal.Gui/Drivers/AnsiDriver/AnsiSizeMonitor.cs` | Add `Trace.Lifecycle` throughout; uncomment/replace old logging stubs |
| `Terminal.Gui/Drivers/SizeMonitorImpl.cs` | Add `Trace.Lifecycle` for poll events; remove stale `using Microsoft.Extensions.Logging` |
| `Terminal.Gui/Drivers/DriverImpl.cs` | Add `Trace.Lifecycle` in `OnSizeMonitorOnSizeChanged` and `SetScreenSize` |
| `Terminal.Gui/Drivers/SizeDetectionMode.cs` | XML doc: clarify `Polling` does not apply to the ANSI driver |
| `Tests/.../AnsiDriver/AnsiSizeMonitorTests.cs` | **New** — full test suite listed above |
| `Tests/.../Drivers/SizeMonitorTests.cs` | Add regression test documenting the broken-SizeMonitorImpl case |

---

## Verification

```bash
dotnet build --no-restore
dotnet test --project Tests/UnitTestsParallelizable --no-build --filter "ClassName~AnsiSizeMonitor"
dotnet test --project Tests/UnitTestsParallelizable --no-build --filter "ClassName~SizeMonitor"
dotnet test --project Tests/UnitTestsParallelizable --no-build  # full suite
```

Manual: run UICatalog with `--driver=ansi` and resize the terminal window; the UI
should reflow correctly instead of staying frozen at 80×25.
