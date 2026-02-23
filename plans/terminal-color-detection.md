# Plan: Terminal Default Color Detection (Issue #2381)

> **Note:** This plan should be saved to `./plans/terminal-color-detection.md` in the repo root as the first step of implementation.

## Context

PR #4740 introduced `Color.None` — a sentinel (alpha=0) that tells the driver to emit `CSI 39m`/`CSI 49m` (reset fg/bg) instead of explicit RGB, letting the terminal's native colors show through. The Default theme's `Base` and `Runnable` schemes now use `Color.None` for background.

**Problem:** TG doesn't know what those actual default colors *are*. This causes:
- `ResolveNoneToBlack` (Scheme.cs:530) hardcodes Black when inverting for Focus
- `Highlight` derivation calls `.GetBrighterColor()` on `Color.None`'s RGB (255,255,255) — producing wrong results
- No color math (dim, brighten) can work correctly without real RGB values

This plan adds two capabilities (PowerShell `$PSStyle.Formatting` deferred to a future PR):
1. **OSC 10/11 queries** to discover the terminal's actual default fg/bg colors
2. **`$TERM`/`$COLORTERM`** environment variable integration

---

## Part A: OSC 10/11 Terminal Color Querying

### A.1: Parser Challenge — OSC Response Termination

**The critical issue:** The ANSI response parser (`AnsiResponseParserBase.cs:147-154`) treats ESC as the start of a *new* sequence. But OSC responses can end with ST = `ESC\`, which would cause the parser to break the OSC response in two.

**Solution:** Add an `_inOscSequence` flag to `AnsiResponseParserBase`. When the parser is in `ExpectingEscapeSequence` state and sees `]` (line 128-137), set `_inOscSequence = true`. Then in `InResponse` state (line 147), when `_inOscSequence` is true:
- If ESC is seen, **do not release**. Instead, continue accumulating (ESC is part of ST).
- On the next character: if it's `\`, the ST is complete — call `HandleHeldContent()` to match.
- If the next char after ESC is anything else, it's malformed — release and reset.
- Also handle BEL (`\a`, 0x07) as an alternative OSC terminator: when accumulated content starts with `ESC]` and ends with `\a`, treat as complete.

**Files to modify:**
- `Terminal.Gui/Drivers/AnsiHandling/AnsiResponseParserBase.cs`
  - Add `private bool _inOscSequence;` field
  - In `ExpectingEscapeSequence` case (line 128): when next char is `]`, set `_inOscSequence = true`
  - In `InResponse` case (line 147): if `_inOscSequence && isEscape`, accumulate instead of releasing. Track "expecting ST backslash" state.
  - In `ResetState()` (line 60): reset `_inOscSequence = false`

### A.2: Update AnsiResponseExpectation.Matches() for OSC Format

**File:** `Terminal.Gui/Drivers/AnsiHandling/AnsiResponseExpectation.cs`

The current `Matches()` regex at line 39 only handles CSI: `@"^\[(\d+);"`. Add OSC matching:

```csharp
// After stripping leading ESC:
// Existing CSI: ^\[(\d+);
// New OSC:      ^\](\d+);
Match oscMatch = Regex.Match (s, @"^\](\d+);");
if (oscMatch.Success)
{
    return string.Equals (oscMatch.Groups [1].Value, Value, StringComparison.Ordinal);
}
```

Also update the fallback `Contains` check (line 47) to include `]`.

### A.3: Add OSC 10/11 Definitions and Response Parser to EscSeqUtils

**File:** `Terminal.Gui/Drivers/AnsiHandling/EscSeqUtils/EscSeqUtils.cs`

Add after the existing `#region OSC` section (line 1057):

```csharp
public static readonly AnsiEscapeSequence OSC_QueryForegroundColor = new ()
{
    Request = $"{OSC}10;?{ST}",
    Terminator = ST,  // ESC\
    Value = "10"
};

public static readonly AnsiEscapeSequence OSC_QueryBackgroundColor = new ()
{
    Request = $"{OSC}11;?{ST}",
    Terminator = ST,
    Value = "11"
};

public static bool TryParseOscColorResponse (string? response, out Color? color)
{
    // Parses: ESC]10;rgb:RRRR/GGGG/BBBB ST  (or with \a terminator)
    // Handles both 4-digit (16-bit) and 2-digit (8-bit) per-channel hex
    // Takes high byte of 16-bit values
}
```

### A.4: Add `DefaultAttribute` to IDriver

**File:** `Terminal.Gui/Drivers/IDriver.cs` — in `#region Color Support` (after line 127):

```csharp
/// <summary>
///     Gets the terminal's actual default foreground and background colors,
///     queried via OSC 10/11 at driver startup.
///     <see langword="null"/> if the terminal did not respond.
/// </summary>
Attribute? DefaultAttribute { get; }
```

**File:** `Terminal.Gui/Drivers/DriverImpl.cs` — implement with backing field + internal setter:

```csharp
private Attribute? _defaultAttribute;
public Attribute? DefaultAttribute => _defaultAttribute;
internal void SetDefaultAttribute (Attribute attr) => _defaultAttribute = attr;
```

### A.5: Create TerminalColorDetector

**New file:** `Terminal.Gui/Drivers/AnsiHandling/TerminalColorDetector.cs`

Follows the `SixelSupportDetector` pattern — chains two async ANSI requests:
1. Send OSC 10 query → parse fg color from response
2. Send OSC 11 query → parse bg color from response
3. Deliver result via callback: `Action<Color?, Color?>`
4. Skip query if `IsLegacyConsole` is true
5. Handle `Abandoned` callbacks gracefully (terminal doesn't support OSC)

### A.6: Trigger Detection at Driver Startup

**File:** `Terminal.Gui/App/MainLoop/MainLoopCoordinator.cs` — in `BuildDriverIfPossible()` (after line 151):

```csharp
// After: _loop.SizeMonitor.Initialize(_driver);
TerminalColorDetector colorDetector = new (_driver);
colorDetector.Detect ((fg, bg) =>
{
    if (fg is { } || bg is { })
    {
        _driver.SetDefaultAttribute (new Attribute (
            fg ?? Color.White,
            bg ?? Color.Black));

        // Make queried colors available for scheme derivation
        Scheme.TerminalDefaultAttribute = _driver.DefaultAttribute;
    }
});
```

### A.7: Use Queried Colors in Scheme Derivation

**File:** `Terminal.Gui/Drawing/Scheme.cs`

Add a static property:

```csharp
internal static Attribute? TerminalDefaultAttribute { get; set; }
```

Replace `ResolveNoneToBlack` (line 530) with `ResolveNone`:

```csharp
private static Color ResolveNone (Color color, bool isForeground = false)
{
    if (color != Color.None) return color;

    if (TerminalDefaultAttribute is { } attr)
        return isForeground ? attr.Foreground : attr.Background;

    return isForeground ? new Color (255, 255, 255) : new Color (0, 0, 0);
}
```

Update call sites:
- Line 277: `ResolveNone(Normal.Background)` (was `ResolveNoneToBlack`)
- Line 290: `ResolveNone(Normal.Background).GetBrighterColor()` — wrap Highlight derivation similarly
- Line 298: `ResolveNone(Normal.Foreground, true).GetDimColor()` — Editable derivation

---

## Part B: PowerShell `$PSStyle.Formatting` Integration

**DEFERRED** — Too complicated and narrow audience for this PR. Can be added in a future PR using `ClipboardProcessRunner.Process()` to spawn `pwsh`.

---

## Part C: `$TERM` / `$COLORTERM` Environment Variable Integration

### C.1: Create TerminalEnvironmentDetector

**New file:** `Terminal.Gui/Drivers/TerminalEnvironment/TerminalEnvironmentDetector.cs`

```csharp
public static class TerminalEnvironmentDetector
{
    public static TerminalColorCapabilities DetectColorCapabilities ()
    {
        // Read: TERM, COLORTERM, TERM_PROGRAM, WT_SESSION
        // Map to capability level:
        //   TERM=dumb           → NoColor
        //   TERM=linux          → Colors16
        //   COLORTERM=truecolor → TrueColor
        //   COLORTERM=24bit     → TrueColor
        //   WT_SESSION present  → TrueColor (Windows Terminal)
        //   *-256color          → Colors256
        //   Default             → TrueColor
    }
}
```

### C.2: TerminalColorCapabilities Record

```csharp
public record TerminalColorCapabilities
{
    public string? Term { get; init; }
    public string? ColorTerm { get; init; }
    public string? TermProgram { get; init; }
    public bool IsWindowsTerminal { get; init; }
    public ColorCapabilityLevel Capability { get; internal set; }
}

public enum ColorCapabilityLevel { NoColor, Colors16, Colors256, TrueColor }
```

### C.3: Integrate into Driver Creation — Run BEFORE OSC Queries

**File:** `Terminal.Gui/App/ApplicationImpl.Driver.cs` — after `Driver.Force16Colors = ...` (line 108):

```csharp
TerminalColorCapabilities caps = TerminalEnvironmentDetector.DetectColorCapabilities ();
Driver.ColorCapabilities = caps;

if (caps.Capability is ColorCapabilityLevel.NoColor or ColorCapabilityLevel.Colors16)
{
    Driver.Force16Colors = true;
}
```

**Important:** Environment detection must run before the OSC 10/11 queries in `MainLoopCoordinator.BuildDriverIfPossible()`. The `TerminalColorDetector` should check `ColorCapabilities` and skip the query if `Capability` is `NoColor` or `Colors16` (these terminals typically don't support OSC).

### C.4: Add to IDriver

```csharp
// In IDriver.cs, Color Support region:
TerminalColorCapabilities? ColorCapabilities { get; }
```

---

## Implementation Sequence

| Phase | Work | Dependencies |
|-------|------|-------------|
| **1** | Parser: OSC support in `AnsiResponseParserBase` + `AnsiResponseExpectation` | None |
| **2** | `EscSeqUtils`: OSC 10/11 definitions + `TryParseOscColorResponse` | None (parallel with 1) |
| **3** | `TerminalEnvironmentDetector` + `ColorCapabilities` | None (parallel with 1, 2) |
| **4** | `TerminalColorDetector` class (gates on `ColorCapabilities`) | Phase 1, 2, 3 |
| **5** | `IDriver.DefaultAttribute` + `DriverImpl` + startup wiring | Phase 4 |
| **6** | `Scheme.ResolveNone` — use queried colors in derivation | Phase 5 |
| **7** | Tests for all of the above | All phases |

## Key Files to Modify

| File | Change |
|------|--------|
| `Terminal.Gui/Drivers/AnsiHandling/AnsiResponseParserBase.cs` | Add `_inOscSequence` flag, OSC accumulation logic |
| `Terminal.Gui/Drivers/AnsiHandling/AnsiResponseExpectation.cs` | Add `]` regex for OSC matching |
| `Terminal.Gui/Drivers/AnsiHandling/EscSeqUtils/EscSeqUtils.cs` | OSC 10/11 definitions, `TryParseOscColorResponse` |
| `Terminal.Gui/Drivers/IDriver.cs` | `DefaultAttribute`, `ColorCapabilities` properties |
| `Terminal.Gui/Drivers/DriverImpl.cs` | Implement new properties |
| `Terminal.Gui/Drawing/Scheme.cs` | `TerminalDefaultAttribute`, rename/expand `ResolveNoneToBlack` |
| `Terminal.Gui/App/MainLoop/MainLoopCoordinator.cs` | Trigger `TerminalColorDetector` |
| `Terminal.Gui/App/ApplicationImpl.Driver.cs` | Trigger `TerminalEnvironmentDetector` |

## Existing Utilities to Reuse

| File | Utility |
|------|---------|
| `Terminal.Gui/Drivers/AnsiHandling/AnsiRequestScheduler.cs` | Request throttling + abandoned timeout (1s) |
| `Terminal.Gui/Drivers/Sixel/SixelSupportDetector.cs` | Pattern for chained async ANSI queries with callbacks |

## New Files

| File | Purpose |
|------|---------|
| `Terminal.Gui/Drivers/AnsiHandling/TerminalColorDetector.cs` | OSC 10/11 async query |
| `Terminal.Gui/Drivers/TerminalEnvironment/TerminalEnvironmentDetector.cs` | `$TERM`/`$COLORTERM` parsing |
| `Terminal.Gui/Drivers/TerminalEnvironment/TerminalColorCapabilities.cs` | Capability data record |

## Edge Cases

1. **Terminal doesn't respond to OSC** — `Abandoned` callback fires (1s timeout from `AnsiRequestScheduler`). `DefaultAttribute` stays null. `ResolveNone` falls back to White/Black.
2. **Terminal responds with BEL instead of ST** — Parser handles both: BEL is a single char (doesn't trigger ESC release), ST is handled via the new `_inOscSequence` flag.
3. **Legacy console (conhost)** — `TerminalColorDetector.Detect()` checks `IsLegacyConsole` and skips query entirely.
4. **tmux/screen** — May intercept OSC queries. Handled by the same `Abandoned` timeout.
5. **Race condition (async response)** — First frame renders with fallback colors. When OSC response arrives, `Scheme.TerminalDefaultAttribute` is updated. Next natural `Draw` cycle picks up the correct colors (no forced redraw).
6. **Color.None in GetBrighterColor/GetDimColor** — The `ResolveNone` call wraps these so they never operate on `Color.None`'s sentinel RGB.
7. **`TERM=dumb`** — `Force16Colors` is set. OSC queries are **skipped** (gated on `ColorCapabilities`).

## Verification

1. **Unit tests** (in `Tests/UnitTestsParallelizable`):
   - `TryParseOscColorResponse` with valid 16-bit, 8-bit, and malformed inputs
   - `AnsiResponseExpectation.Matches()` with OSC response strings
   - Parser integration: feed OSC response bytes through `AnsiResponseParser.ProcessInput()` and verify callback fires
   - `ResolveNone` with and without `TerminalDefaultAttribute` set
   - `TerminalEnvironmentDetector` with mocked env vars

2. **Manual testing**:
   - Run UICatalog in Windows Terminal → verify `DefaultAttribute` is populated
   - Run UICatalog in VS Code terminal → verify OSC works
   - Run with `TERM=dumb` → verify `Force16Colors` is set
   - Check Focus/Highlight roles look correct with transparent backgrounds
