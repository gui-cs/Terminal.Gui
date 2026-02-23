# Plan: Terminal Default Color Detection (Issue #2381)

## Context

PR #4740 introduced `Color.None` — a sentinel (alpha=0) that tells the driver to emit `CSI 39m`/`CSI 49m` (reset fg/bg) instead of explicit RGB, letting the terminal's native colors show through. The Default theme's `Base` and `Runnable` schemes now use `Color.None` for both fg and bg.

**Core Problem:** TG doesn't know what the terminal's actual default colors *are*. This causes:
- Color math (`GetBrighterColor`, `GetDimColor`) to operate on `Color.None`'s sentinel RGB (255,255,255,alpha=0) producing wrong results
- Scheme role derivation (Focus, Active, Highlight, etc.) to produce poor contrast on light-background terminals
- No way to determine if the terminal has a dark or light background

This plan has three phases:
1. **Parts A & C (DONE):** OSC 10/11 queries + environment variable detection
2. **Part D (NEW):** Revamp `GetBrighterColor`/`GetDimColor` for dark/light awareness
3. **Part E (NEW):** Revamp built-in Themes to use `Color.None` smartly

---

## Part A: OSC 10/11 Terminal Color Querying — DONE

All items implemented in commits `8a0cbb062..01bb8c940`.

### A.1: Parser — OSC Response Termination — DONE
- Added `_inOscSequence` flag to `AnsiResponseParserBase.cs`
- Handles both ST (`ESC\`) and BEL (`\a`) as OSC terminators
- ESC inside OSC sequences is accumulated rather than treated as new sequence start

### A.2: AnsiResponseExpectation.Matches() — DONE
- Added OSC `]` regex matching alongside existing CSI `[` matching
- Updated fallback `Contains` check

### A.3: OSC 10/11 Definitions in EscSeqUtils — DONE
- `OSC_QueryForegroundColor` and `OSC_QueryBackgroundColor` definitions
- `TryParseOscColorResponse()` parser handling both 4-digit (16-bit) and 2-digit (8-bit) hex per channel

### A.4: `DefaultAttribute` on IDriver — DONE
- `IDriver.DefaultAttribute` property (nullable)
- `DriverImpl.SetDefaultAttribute()` internal setter

### A.5: TerminalColorDetector — DONE
- New file: `Terminal.Gui/Drivers/AnsiHandling/TerminalColorDetector.cs`
- Chains OSC 10 → OSC 11 queries with abandoned fallback
- Skips on `IsLegacyConsole`

### A.6: Startup Wiring — DONE
- `MainLoopCoordinator.BuildDriverIfPossible()` triggers `TerminalColorDetector`
- Gated on `ColorCapabilities` (only for Colors256/TrueColor)

### A.7: Scheme.ResolveNone — DONE
- `ResolveNone(color, defaultTerminalColors, isForeground)` replaces old `ResolveNoneToBlack`
- `GetAttributeForRole()` now accepts `Attribute? defaultTerminalColors` parameter
- `View.GetAttributeForRole()` passes `App?.Driver?.DefaultAttribute` through

---

## Part B: PowerShell `$PSStyle.Formatting` Integration

**DEFERRED** — Too complicated and narrow audience for this PR.

---

## Part C: `$TERM` / `$COLORTERM` Environment Variable Integration — DONE

### C.1: TerminalEnvironmentDetector — DONE
- New file: `Terminal.Gui/Drivers/TerminalEnvironment/TerminalEnvironmentDetector.cs`
- Reads `TERM`, `COLORTERM`, `TERM_PROGRAM`, `WT_SESSION`, `NO_COLOR`
- Maps to `ColorCapabilityLevel` enum

### C.2: TerminalColorCapabilities — DONE
- New file: `Terminal.Gui/Drivers/TerminalEnvironment/TerminalColorCapabilities.cs`
- New file: `Terminal.Gui/Drivers/TerminalEnvironment/ColorCapabilityLevel.cs`
- `NoColor`, `Colors16`, `Colors256`, `TrueColor` levels

### C.3: Integration in ApplicationImpl.Driver.cs — DONE
- Runs after driver creation, before OSC queries
- Sets `Force16Colors = true` for `NoColor`/`Colors16`

### C.4: ColorCapabilities on IDriver — DONE
- `IDriver.ColorCapabilities` property
- `DriverImpl.SetColorCapabilities()` internal setter

---

## Part D: Revamp `GetBrighterColor` / `GetDimColor` for Dark/Light Awareness — DONE

### Problem Statement

`GetBrighterColor` and `GetDimColor` (in `Color.cs:297` and `Color.cs:344`) operate purely on the input color's own lightness, with no awareness of the surrounding context (dark vs light background). This produces visually broken results in the `Scheme` derivation algorithm when `Color.None` resolves to a light background color.

**Specific issues:**

1. **`GetDimColor` always darkens.** On a light background (e.g., resolved `Color.None` → white), "dimming" should make colors *lighter* (closer to the background), not darker. Currently it unconditionally reduces lightness, which on a light bg produces dark colors that are *more* prominent, the opposite of "dim."

2. **`GetBrighterColor` is context-unaware for contrast.** It does adjust direction based on the color's own lightness (brightens dark, darkens light), but when used for Focus/Active/Highlight derivation, it doesn't know whether the *result* needs to contrast against a dark or light background.

3. **`GetDimColor` returns `DarkGray` for very dark inputs (L ≤ 0.1).** This hardcoded fallback is wrong on light backgrounds where the dim color should be a light gray.

4. **The Scheme derivation algorithm** (`Scheme.cs:277-312`) applies `GetBrighterColor`/`GetDimColor` after `ResolveNone`, but the resolved color values don't carry enough context about whether we're operating in a "dark mode" or "light mode" environment.

### Proposed Solution

Add an overload (or optional parameter) to `GetBrighterColor` and `GetDimColor` that accepts the terminal's background color, enabling direction-aware adjustments.

### D.1: Add `isDarkBackground` context to `GetDimColor` — DONE

Added optional `bool? isDarkBackground` parameter. When `true` (or `null`/default), reduces lightness. When `false`, increases lightness (washes out toward light bg). Extreme-value fallback now returns `Gray` for light backgrounds instead of always `DarkGray`. Thresholds use `hsl.L` directly (ColorHelper's 0-100 HSL scale).

### D.2: Add `isDarkBackground` context to `GetBrighterColor` — DONE

Added optional `bool? isDarkBackground` parameter. When `true`, increases lightness (brighter on dark bg). When `false`, decreases lightness (darker/more visible on light bg). When `null`, auto-detects from color's own lightness (backward compatible).

### D.3: Add `IsDarkColor()` helper to `Color` — DONE

Returns `true` if `hsl.L < 50` (using ColorHelper's 0-100 HSL L scale). Used by `Scheme.GetAttributeForRoleCore` to determine the `isDarkBackground` parameter.

### D.4: Update `Scheme.GetAttributeForRoleCore` to pass background context — DONE

Refactored from switch expression to switch statement to enable local variables. Each role that uses color math (Active, Highlight, Editable, ReadOnly, Disabled) now:
1. Resolves the background color via `ResolveNone`
2. Determines `isDark` via `IsDarkColor()`
3. Passes `isDark` to `GetBrighterColor`/`GetDimColor`

**Bug fix:** Fixed a stack cleanup bug in `GetAttributeForRoleCore` where explicitly-set roles were not removed from the recursion-guard `HashSet` on early return (line 271). This caused the old switch expression's redundant calls to `GetAttributeForRoleCore` for the same role to incorrectly return `Normal` instead of the explicit value. The refactoring to local variables also avoids triggering this bug.

### D.5: Backward Compatibility — DONE

- Default parameter `isDarkBackground = null` preserves behavior for all external callers (ShadowView, ColorPicker, TextField, UICatalog scenarios)
- Only the Scheme derivation code passes explicit `isDarkBackground` values
- Updated `DeepClonerTests.Scheme_All_Set_ReturnsEqualValue` to account for corrected derivation behavior

### Files Modified

| File | Change |
|------|--------|
| `Terminal.Gui/Drawing/Color/Color.cs` | Added `isDarkBackground` param to `GetBrighterColor`/`GetDimColor`; added `IsDarkColor()` |
| `Terminal.Gui/Drawing/Scheme.cs` | Refactored derivation to switch statement; pass background context; fixed stack cleanup bug |
| `Tests/UnitTestsParallelizable/Drawing/Color/ColorClassTests.DarkLightAwareness.cs` | **NEW** — 16 tests for `IsDarkColor`, `GetBrighterColor`, `GetDimColor` with background context |
| `Tests/UnitTestsParallelizable/Drawing/SchemeTests.ColorNoneDerivationTests.cs` | Added 5 dark/light awareness derivation tests |
| `Tests/UnitTestsParallelizable/Configuration/DeepClonerTests.cs` | Fixed HotActive test value to account for corrected derivation |

---

## Part E: Revamp Themes to Use `Color.None` Smartly — NEW

### Problem Statement

The Default (hardcoded) theme uses `Color.None` for both fg and bg in `Base` and `Runnable`, letting the terminal's native colors show through. However, none of the config.json themes take advantage of `Color.None`. Some themes are natural candidates for transparency, while others are not.

### Design Principles

1. **`Color.None` for background** means the terminal's own background shows through. Good for themes that want to blend with the user's terminal setup.
2. **`Color.None` for foreground** means the terminal's default text color shows through. Good for themes that don't enforce a specific text color.
3. **Monochrome themes** (Green Phosphor, Amber Phosphor) should use `Color.None` for background but **NOT** for foreground — their identity *is* the specific fg color.
4. **Fully styled themes** (TurboPascal 5, Anders, 8-Bit, Hot Dog Stand) should NOT use `Color.None` — their specific color choices are the whole point.
5. **Dark/Light themes** should use `Color.None` only for the Runnable and Base scheme backgrounds, since they are designed to float over the terminal bg. Dialog, Menu, Error need explicit bg for visual separation.

### Theme-by-Theme Plan

#### E.1: `Terminal.Gui/Resources/config.json`

| Theme | Change | Rationale |
|-------|--------|-----------|
| **Dark** | `Runnable.Normal.Background` → `"None"`, `Base.Normal.Background` → `"None"` | Dark theme should blend with dark terminal bg. Dialog/Menu/Error keep explicit dark colors for layering. |
| **Light** | `Runnable.Normal.Background` → `"None"`, `Base.Normal.Background` → `"None"` | Light theme should blend with light terminal bg. Dialog/Menu/Error keep explicit light colors. |
| **Green Phosphor** | `Runnable.Normal.Background` → `"None"`, `Base.Normal.Background` → `"None"` | Green fg on terminal bg. Fg stays `GreenPhosphor`. |
| **Amber Phosphor** | `Runnable.Normal.Background` → `"None"`, `Base.Normal.Background` → `"None"` | Amber fg on terminal bg. Fg stays `AmberPhosphor`. |
| **TurboPascal 5** | No change | Retro theme with intentional Blue bg — `Color.None` would break the aesthetic. |
| **Anders** | No change | Dark-blue base bg is part of the identity. |
| **8-Bit** | No change | B&W theme with fully explicit colors; `Color.None` adds no value since it already uses Black/White. |

**Detail for Dark theme:**
```json
"Base": {
  "Normal": {
    "Foreground": "LightGray",
    "Background": "None",      // was "Black"
    "Style": "None"
  },
  // Focus, HotNormal, etc. keep explicit colors for contrast
  ...
}
```

For roles that derive from `Normal.Background` (Focus swaps fg/bg, Active dims bg, etc.), the derivation algorithm will use `ResolveNone` → `DefaultAttribute` → terminal's actual bg. With Part D's dark/light awareness, the derived colors will be correct.

**Detail for Green Phosphor:**
```json
"Runnable": {
  "Normal": {
    "Foreground": "GreenPhosphor",   // KEEP - this IS the theme
    "Background": "None",            // was "Black" — let terminal bg shine through
    "Style": "None"
  }
},
"Base": {
  "Normal": {
    "Foreground": "GreenPhosphor",   // KEEP
    "Background": "None",            // was "Black"
    "Style": "None"
  },
  // Active, Highlight, Editable keep their explicit overrides
  ...
}
```

Note: Green/Amber Phosphor's `Dialog` and `Menu` schemes use *inverted* colors (Black on GreenPhosphor) — these should NOT use `Color.None` since the inversion is intentional.

#### E.2: `Examples/UICatalog/Resources/config.json`

| Theme | Change | Rationale |
|-------|--------|-----------|
| **Hot Dog Stand** | No change | Fully explicit novelty theme. |
| **UI Catalog** | No change | Fully explicit demo theme. |

### Cascade Effects

When `Normal.Background` is `Color.None` in a config theme, the derivation algorithm produces:
- **Focus**: fg = ResolveNone(bg) → terminal bg color; bg = ResolveNone(fg) → terminal fg color (or explicit fg)
- **Active**: derived from Focus with `GetBrighterColor`/`GetDimColor` — Part D ensures correct direction
- **Highlight**: fg = ResolveNone(Normal.bg).GetBrighterColor() — correct with Part D
- **Editable**: bg = ResolveNone(Normal.fg).GetDimColor() — correct with Part D

For roles that have explicit overrides in the config (e.g., Dark theme defines explicit Focus, Active, Highlight, etc.), the `Color.None` in `Normal` only affects roles that are NOT explicitly overridden. Since the Dark and Light themes already override most roles explicitly, the immediate visual impact is limited to the main background area — which is exactly what we want.

For Green/Amber Phosphor, the roles that are NOT explicitly overridden (Focus, HotNormal, HotFocus, Disabled, HotActive) will derive from the `Color.None` bg. This should work well because:
- Focus: terminal bg on GreenPhosphor (readable on both dark and light terminals)
- Disabled: GreenPhosphor dimmed (via Part D, direction-aware)

### Files to Modify

| File | Change |
|------|--------|
| `Terminal.Gui/Resources/config.json` | Update Dark, Light, Green Phosphor, Amber Phosphor schemes |

### Verification

1. **Manual testing** — Run UICatalog with each modified theme on:
   - Dark terminal background (e.g., Windows Terminal dark theme)
   - Light terminal background (e.g., Windows Terminal light theme)
   - Verify readable text, proper focus/highlight contrast, correct Dialog/Menu layering
2. **Unit tests** — Existing `SchemeTests.ColorNoneDerivationTests` cover derivation correctness

---

## Implementation Sequence (Updated)

| Phase | Work | Status |
|-------|------|--------|
| **1** | Parser: OSC support in `AnsiResponseParserBase` + `AnsiResponseExpectation` | **DONE** |
| **2** | `EscSeqUtils`: OSC 10/11 definitions + `TryParseOscColorResponse` | **DONE** |
| **3** | `TerminalEnvironmentDetector` + `ColorCapabilities` | **DONE** |
| **4** | `TerminalColorDetector` class (gates on `ColorCapabilities`) | **DONE** |
| **5** | `IDriver.DefaultAttribute` + `DriverImpl` + startup wiring | **DONE** |
| **6** | `Scheme.ResolveNone` — use queried colors in derivation | **DONE** |
| **7** | Tests for A.1–A.7 and C.1–C.4 | **DONE** |
| **8** | `GetBrighterColor`/`GetDimColor` dark/light awareness (Part D) | **DONE** |
| **9** | Theme config.json revamp with `Color.None` (Part E) | TODO — depends on Phase 8 |
| **10** | Tests for Part E | TODO |

## Edge Cases

1. **Terminal doesn't respond to OSC** — `Abandoned` callback fires (1s timeout). `DefaultAttribute` stays null. `ResolveNone` falls back to White fg / Black bg. Part D's `isDarkBackground` defaults to `null` (auto-detect from color's own lightness).
2. **Terminal responds with BEL instead of ST** — Parser handles both terminators. ✅
3. **Legacy console (conhost)** — `TerminalColorDetector` skips query. Falls back to White/Black. ✅
4. **tmux/screen** — May intercept OSC queries. Handled by `Abandoned` timeout. ✅
5. **Race condition (async response)** — First frame uses fallback colors; next draw cycle picks up queried colors. ✅
6. **Light terminal background** — Part D ensures `GetDimColor` washes out (increases L) instead of darkening. Part E's `Color.None` bg resolves to the light color, and derivation adjusts accordingly.
7. **Green/Amber Phosphor on light bg** — fg stays the specific phosphor color. bg is `Color.None` → light terminal bg. Focus becomes terminal-bg-on-phosphor-fg. `GetDimColor` with `isDarkBackground=false` produces appropriately washed-out variants.
8. **`TERM=dumb`** — `Force16Colors` is set. OSC queries are skipped. Themes with `Color.None` still resolve via fallback (White/Black). ✅
9. **Config themes with explicit overrides** — When Dark/Light themes define explicit Focus/Active/etc. colors, `Color.None` in Normal only affects the base background. Explicit overrides are preserved as-is by the derivation algorithm.
