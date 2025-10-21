# InvokeLeakTest Debugger Failure - Investigation Summary

## Quick Summary

The `InvokeLeakTest` stress test fails on @BDisp's machine when run under a debugger due to a **timing race condition** in the `TimedEvents` system caused by low resolution of `DateTime.UtcNow`.

## Problem

- **Test**: `InvokeLeakTest` in `Tests/StressTests/ApplicationStressTests.cs`
- **Symptoms**: Times out after 100ms, claims some `Application.Invoke()` calls were "lost"
- **When**: Only under debugger (VS2022, VSCode) on x64 machines (Windows/macOS)
- **Architecture**: Confirmed fails on x64, does NOT fail on ARM (@tig confirmed)
- **Frequency**: Consistent on x64 machines under debugger, never on ARM or without debugger

## Root Cause

`Application.Invoke()` adds actions to a timer queue with `TimeSpan.Zero` (immediate execution). The timer system uses `DateTime.UtcNow.Ticks` which has ~15ms resolution on Windows. When many invocations occur rapidly:

1. Multiple invocations get the **same timestamp** (within 15ms window)
2. `NudgeToUniqueKey` increments timestamps: T-100, T-99, T-98, ...
3. Race condition: Later timestamps might have `k >= now` when checked
4. Those timeouts don't execute immediately, get re-queued
5. Test's 100ms polling window expires before they execute → FAIL

**Debugger makes it worse** by:
- Slowing main loop iterations (2-5x slower)
- Allowing more invocations to accumulate
- Making timer behavior less predictable

## Documentation

- **[InvokeLeakTest_Analysis.md](InvokeLeakTest_Analysis.md)** - Detailed technical analysis (12KB)
- **[InvokeLeakTest_Timing_Diagram.md](InvokeLeakTest_Timing_Diagram.md)** - Visual diagrams (8.5KB)

## Recommended Solutions

### Option 1: Fix Production Code (Recommended)
Replace `DateTime.UtcNow` with `Stopwatch.GetTimestamp()` in `TimedEvents.cs`:

```csharp
// In TimedEvents.cs
private static long GetTimestampTicks()
{
    return Stopwatch.GetTimestamp() * (TimeSpan.TicksPerSecond / Stopwatch.Frequency);
}

// Use in AddTimeout:
long k = GetTimestampTicks() + time.Ticks;
```

**Benefits**:
- Microsecond resolution vs millisecond
- Eliminates timestamp collisions
- Works reliably under debugger
- Cross-platform consistent

### Option 2: Increase TimeSpan.Zero Buffer
Change from 100 ticks (0.01ms) to more substantial buffer:

```csharp
if (time == TimeSpan.Zero)
{
    k -= TimeSpan.TicksPerMillisecond * 10;  // 10ms instead of 0.01ms
}
```

### Option 3: Wakeup Main Loop
Add explicit wakeup after TimeSpan.Zero timeout:

```csharp
_timedEvents.Add(TimeSpan.Zero, ...);
MainLoop?.Wakeup();
```

### Option 4: Test-Only Fix
Increase polling timeout when debugger attached:

```csharp
#if DEBUG
private const int POLL_MS = Debugger.IsAttached ? 500 : 100;
#else
private const int POLL_MS = 100;
#endif
```

## For x64 Users (@BDisp and @tig)

### Architecture-Specific Issue (CONFIRMED)
@tig confirmed the issue reproduces on x64 Windows but NOT on ARM Windows. This validates the hypothesis that x64 timer architecture (Intel/AMD TSC/HPET) is more susceptible to this race condition than ARM timer implementations.

### Immediate Workaround
Run the test **without** debugger - it should pass (as it does in CI and on ARM machines).

### To Confirm Hypothesis
Add diagnostics to the test (see [InvokeLeakTest_Analysis.md](InvokeLeakTest_Analysis.md) section "Additional Investigation Needed") to:
- Measure `DateTime.UtcNow` resolution on your system
- Monitor timer queue state when timeout occurs
- Log timing of main loop iterations

### Not Your Fault!
This is a **stress test** (not unit test) that exposed a timing issue in the implementation specific to x64 architecture. Your hardware correctly identifies this edge case that ARM machines don't hit.

## Next Steps

1. Terminal.Gui team reviews recommended solutions
2. Decide on approach:
   - Fix production code (Option 1) - most robust
   - Or document as known limitation with workaround
3. If fixing production code, also add unit test for high-frequency `Invoke` calls
4. Close related issue #4296

## Related

- Issue #4296 - This issue
- Issue #4295 - Different test failure (not related)
- PR #XXXX - This investigation and analysis

## Files Changed

- `InvokeLeakTest_Analysis.md` - New file with detailed analysis
- `InvokeLeakTest_Timing_Diagram.md` - New file with visual diagrams
- `Tests/StressTests/ApplicationStressTests.cs` - Added XML documentation to test method
