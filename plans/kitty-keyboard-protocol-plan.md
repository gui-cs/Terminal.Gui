# Kitty Keyboard Protocol Plan

## Problem Statement

The ANSI driver currently parses classic ANSI keyboard sequences through `AnsiResponseParser` and `AnsiKeyboardParser`, and it already has a request/response pipeline for terminal capability probes through `AnsiRequestScheduler` and `IDriver.QueueAnsiRequest`. It does not detect or use the kitty keyboard protocol, so modern terminals cannot provide Terminal.Gui with the richer, less ambiguous keyboard stream they already expose.

That is only the first gap. The current input model also does not preserve all keyboard semantics that kitty can report and that Terminal.Gui v1 exposed in some places, especially on Windows:

- key press vs key release
- repeat semantics
- left/right or otherwise distinct modifier keys where available
- standalone modifier-key events
- richer shifted-key disambiguation without collapsing everything into the current `KeyCode` bitmask model

The intent is to fix this in phases, not to stop at a lossy kitty-to-current-`Key` adapter.

## Goal

Build toward a keyboard pipeline where Terminal.Gui can fully plumb kitty keyboard protocol data through the driver, input model, application APIs, and view/event layers.

Phase 1 should still be intentionally narrow:

1. Detect whether the active terminal supports kitty keyboard protocol.
2. Enable kitty keyboard reporting only when support is confirmed.
3. Parse kitty keyboard sequences and map them into the current `Terminal.Gui.Input.Key` model so all existing Terminal.Gui functionality continues to work when the ANSI driver is running under a kitty-capable terminal.
4. Disable the protocol on shutdown.
5. Add deterministic tests around detection, parsing, and startup/shutdown integration.

Later phases should extend the input model instead of treating kitty-only fields as permanently out of scope.

## Relevant Existing Patterns To Reuse

- Capability probe pattern: `Terminal.Gui/Drawing/Sixel/SixelSupportDetector.cs`
- Async terminal query pattern: `Terminal.Gui/Drivers/AnsiHandling/TerminalColorDetector.cs`
- Driver startup wiring: `Terminal.Gui/App/MainLoop/MainLoopCoordinator.cs`
- ANSI request scheduling and collision handling: `Terminal.Gui/Drivers/AnsiHandling/AnsiRequestScheduler.cs`
- ANSI keyboard parser extensibility: `Terminal.Gui/Drivers/AnsiHandling/AnsiKeyboardParser.cs`
- Existing CSI key parsing: `Terminal.Gui/Drivers/AnsiHandling/CsiKeyPattern.cs`
- ANSI driver lifecycle hooks: `Terminal.Gui/Drivers/AnsiDriver/AnsiOutput.cs`
- Current keyboard routing APIs:
  - `Terminal.Gui/Input/Keyboard/Key.cs`
  - `Terminal.Gui/App/Keyboard/IKeyboard.cs`
  - `Terminal.Gui/App/IApplication.cs`
  - `Terminal.Gui/App/Keyboard/ApplicationKeyboard.cs`
- Diagnostics patterns:
  - `Terminal.Gui/App/Tracing/Trace.cs`
  - `Tests/UnitTests/TestHelpers/TestLogger.cs`
- Parser and end-to-end input tests:
  - `Tests/UnitTestsParallelizable/Drivers/AnsiHandling/AnsiKeyboardParserTests.cs`
  - `Tests/UnitTestsParallelizable/Drivers/AnsiDriver/AnsiInputTestableTests.cs`
  - `Tests/UnitTestsParallelizable/Drawing/Sixel/SixelSupportDetectorTests.cs`

## Debugging And Diagnostics Expectations

When debugging keyboard-flow issues in this work, coding agents should prefer instrumented observation over speculative reasoning.

Required approach:

- use `Tracing.Trace` to inspect the live flow through detection, parser, driver, and application keyboard routing
- use `TestLogger` in tests and focused reproductions to capture what the system is actually doing
- add or enable targeted tracing/logging where needed so failures can be diagnosed from observed behavior

Do not rely on:

- mental simulation of the parser or input pipeline as the primary debugging method
- reasoning from code alone when a focused test plus tracing/logging can show the actual flow

This is especially important for kitty support because:

- parser ordering matters
- async driver startup behavior matters
- enable/disable sequencing matters
- lossy compatibility mapping can hide where information was dropped

## Testing Constraint For Trace

`Tracing.Trace` is a debugging and diagnosis tool, not a behavioral contract for tests.

Tests must not validate behavior by asserting on `Trace` output because:

- `Trace` is not available in `RELEASE` builds
- all tests in this area must pass in both `DEBUG` and `RELEASE`

Implication:

- use `Trace` and `TestLogger` to diagnose failures and understand flow while developing
- validate behavior in tests using observable outcomes such as parsed keys, raised events, state transitions, and written ANSI sequences
- if additional diagnostics are needed in tests, keep them supplemental and non-assertive unless they are available in both build configurations

## Design Direction

Use a phased design instead of a one-off parser enhancement.

### Phase 1: Compatibility plumbing

Deliver kitty protocol support in the ANSI driver using the current `Key` model so existing application behavior and tests continue to work.

This phase is about replacing legacy ANSI ambiguity with kitty input where available, not yet exposing every kitty event dimension to applications.

### Phase 2: Rich keyboard event model

Revise `Key` and the keyboard event pipeline so Terminal.Gui can preserve richer semantics instead of collapsing them into `KeyCode`:

- event type: press, release, repeat
- standalone modifier key transitions
- distinct modifier keys where available
- richer source metadata from ANSI/kitty and other drivers

This likely implies coordinated changes across:

- `Terminal.Gui.Input.Key`
- driver `KeyDown`-only event contracts
- `IKeyboard`
- `IApplication`
- legacy `Application` shims
- view keyboard events and test helpers

### Phase 3: Full end-to-end adoption

Once the richer model exists, update the kitty parser and driver plumbing to populate it fully, then bring other drivers into parity where feasible.

This sequencing keeps phase 1 shippable while avoiding the false assumption that lossy mapping is the intended end state.

## Phase 1 Scope

Phase 1 is successful when:

- ANSI driver startup probes for kitty support.
- kitty keyboard mode is enabled only after positive detection.
- kitty CSI `u` sequences are parsed before generic CSI key patterns.
- parsed input is translated into today’s `Key` abstraction without regressing current behavior.
- existing Terminal.Gui features continue to work with input arriving via kitty protocol.
- shutdown restores terminal keyboard mode.

Phase 1 explicitly does not require new public keyboard APIs yet, but it should avoid painting later phases into a corner.

## Progress Status

### Phase 1 summary

Status: In progress

Completed:

- kitty protocol query, enable, and disable constants were added to `EscSeqUtils`
- a dedicated `KittyKeyboardProtocolDetector` was added with structured results
- ANSI driver state now persists detected and enabled kitty protocol flags
- startup wiring now probes for kitty support, enables kitty mode only after a positive response, and records the enabled state
- `AnsiOutput` now owns kitty enable/disable emission and restores keyboard mode during dispose
- `AnsiKeyboardParser` now checks a dedicated `KittyKeyboardPattern` before broader CSI patterns
- phase-1 kitty CSI `u` parsing now maps printable keys, modifiers, and a first compatibility subset of function keys into the current `Key` model
- focused detector, parser, input, lifecycle, and startup tests were added
- targeted lifecycle traces were added for kitty probe, response, enable, skip, and disable flow in `DEBUG`

Remaining for phase 1:

- broaden kitty compatibility mapping beyond the current first subset of kitty special-key codes
- run the non-parallel unit test/build validation that should accompany the full phase-1 completion
- update the PR body as follow-on work lands so it stays aligned with actual scope

## Phase 1 Implementation Steps

### 1. Add kitty protocol constants and response metadata

Status: Completed

Extend `Terminal.Gui/Drivers/AnsiHandling/EscSeqUtils/EscSeqUtils.cs` with:

- kitty progressive enhancement query request
- kitty progressive enhancement enable request
- kitty progressive enhancement disable request
- value and terminator metadata needed so `AnsiRequestScheduler` can distinguish the response

Notes:

- The kitty protocol uses CSI `?u` for progressive enhancement query/response and CSI `>flagsu` to set enhancement flags.
- Keep the enabled flag set explicit in code rather than scattering string literals.
- Phase 1 should choose flags that are compatible with current Terminal.Gui behavior while still giving the parser the disambiguated stream it needs.

### 2. Introduce a focused detector object

Status: Completed

Add `Terminal.Gui/Drivers/AnsiHandling/KittyKeyboardProtocolDetector.cs`, following the style of `TerminalColorDetector` and `SixelSupportDetector`.

Responsibilities:

- skip probing for legacy consoles
- send the kitty query through `IDriver.QueueAnsiRequest`
- parse the response and determine support
- return a structured result rather than a bare `bool`

Suggested result shape:

- `IsSupported`
- `SupportedFlags`
- `EnabledFlags`

Even if phase 1 enables only a subset of flags, retain the reported capability so later phases can build on it cleanly.

### 3. Persist detected capability on the driver side

Status: Completed

Add ANSI-driver-visible state rather than burying kitty support inside the detector callback.

Recommended direction:

- add internal driver-level state for whether kitty keyboard is supported and enabled
- if useful, also retain the negotiated flag set

This is needed so:

- startup detection can inform output/input behavior
- tests can assert enable/disable behavior deterministically
- later phases have somewhere to hang richer keyboard capabilities without rediscovering them

### 4. Wire detection into startup after the driver exists

Status: Completed

Extend `Terminal.Gui/App/MainLoop/MainLoopCoordinator.cs` after driver construction, near existing terminal capability detection.

Sequence:

1. build driver
2. initialize size monitor
3. run kitty protocol detection
4. if supported, enable kitty keyboard mode
5. mark driver state enabled only after the enable sequence is emitted

Constraints:

- detection must remain asynchronous and non-blocking
- the driver must not enter kitty mode on unsupported terminals

### 5. Keep enable/disable in the ANSI output lifecycle

Status: Completed

Keep terminal mode mutation in the ANSI driver lifecycle, not in the detector.

Recommended split:

- detector queries and interprets
- `AnsiOutput` emits enable and disable sequences

Concrete changes:

- add methods on `AnsiOutput` such as `EnableKittyKeyboard (...)` and `DisableKittyKeyboard ()`
- call enable from the startup callback
- call disable from `AnsiOutput.Dispose ()` only if kitty mode was enabled

### 6. Extend the keyboard parser with kitty CSI `u` support

Status: Partially completed

Add a new parser pattern, e.g. `Terminal.Gui/Drivers/AnsiHandling/KittyKeyboardPattern.cs`, and register it in `AnsiKeyboardParser`.

It should parse kitty keyboard CSI `u` sequences before generic fallback patterns.

The pattern should handle at least:

- printable Unicode keys encoded as codepoints
- functional and navigation keys encoded through kitty key codes
- modifier decoding for the current `Key` model
- shifted printable keys without relying on legacy ESC-prefix ambiguity

Implementation detail:

- keep kitty parsing isolated in a dedicated `AnsiKeyboardParserPattern` subclass instead of expanding `CsiKeyPattern`

### 7. Use a deliberate compatibility mapping into today’s `Key`

Status: Partially completed

Phase 1 should map kitty events into current `Terminal.Gui.Input.Key` as a compatibility layer, not as the final architecture.

Map in phase 1:

- printable Unicode keys into `Key`
- modifiers into existing `WithShift`, `WithAlt`, `WithCtrl`
- navigation and function keys through a dedicated lookup table
- enough shifted-key disambiguation to preserve current behavior under the ANSI driver

Do not treat the following as permanently unsupported. Treat them as deferred to later phases:

- key release vs key press
- repeat event distinctions
- standalone modifier-key events
- distinct left/right modifier keys
- kitty event metadata that cannot fit the current `Key` shape

The parser should document which kitty fields are dropped in phase 1 and point back to the later rich-model phase so the limitation is explicit and intentional.

### 8. Make parser ordering explicit

Status: Completed

Update `AnsiKeyboardParser` so kitty sequences are checked before broader CSI patterns.

Reason:

- kitty uses CSI sequences too
- ordering mistakes will cause partial or incorrect matches

### 9. Add end-to-end injection support only if needed

Status: Completed for phase 1

`AnsiInputProcessor.InjectKeyDownEvent ()` currently uses `AnsiKeyboardEncoder.Encode (key)`, which emits legacy ANSI sequences for tests.

Do not make kitty encoding part of phase 1 unless tests actually require it.

Reason:

- runtime parsing is the essential feature
- injection encoding can be added later behind a dedicated kitty-aware path if phase 2 or phase 3 needs richer event simulation

### 10. Add tests in three layers

Status: Mostly completed

Completed:

- parser tests added for representative kitty printable, modifier, function-key, and malformed inputs
- detector tests added for supported, unsupported, abandoned, and legacy-console cases
- ANSI lifecycle tests added for enable/disable behavior
- startup wiring tests added for positive detection and legacy-console skip behavior
- integration-style `AnsiInputProcessor` test added for kitty sequence parsing

Still useful before closing phase 1:

- broader parser coverage for additional kitty navigation/function-code variants

Parser tests:

- extend `Tests/UnitTestsParallelizable/Drivers/AnsiHandling/AnsiKeyboardParserTests.cs`
- add representative kitty CSI `u` inputs for:
  - printable characters
  - Ctrl/Alt/Shift modifiers
  - cursor keys
  - function keys
  - malformed sequences

Detector tests:

- add `Tests/UnitTestsParallelizable/Drivers/AnsiHandling/KittyKeyboardProtocolDetectorTests.cs`
- mirror `SixelSupportDetectorTests`
- verify:
  - query request is queued
  - supported response sets `IsSupported`
  - unsupported or abandoned queries do not enable the protocol

ANSI lifecycle tests:

- add or extend tests under `Tests/UnitTestsParallelizable/Drivers/AnsiDriver/`
- verify:
  - startup detection causes enable sequence to be written only after a positive response
  - dispose writes disable sequence when enabled
  - no disable sequence is written if enable never happened

Optional integration-style test:

- in `AnsiInputTestableTests`, inject a kitty CSI `u` sequence character-by-character and assert that `AnsiInputProcessor` raises the expected current-model `Key`

Diagnostic guidance while building these tests:

- prefer turning on targeted `Trace` categories and `TestLogger` capture to inspect flow before changing parser or routing logic
- do not assert on trace output as the test oracle
- once diagnosis is complete, keep assertions focused on behavior that is present in both `DEBUG` and `RELEASE`

## Later Phase Work Items

These are not phase 1 deliverables, but they should shape phase 1 design choices.

### Phase 2: Rich key model and app pipeline

Investigate redesigning `Terminal.Gui.Input.Key` so it can represent:

- physical or logical key identity separately from produced text
- event kind: down, up, repeat
- modifier state and modifier-key identity
- optionally source protocol metadata when useful for debugging/tests

Likely follow-on changes:

- extend `IDriver` to raise more than `KeyDown`
- add `KeyUp` support back through `IKeyboard` and `IApplication`
- decide whether repeat is a distinct event or metadata on key-down
- update `ApplicationKeyboard`, `ApplicationImpl`, and legacy `Application` shims
- update view/event surfaces and test helpers
- add compatibility adapters so existing `KeyDown`-based code keeps working during transition

### Phase 3: Full kitty fidelity

After the richer model exists:

- update `KittyKeyboardPattern` to populate key-down, key-up, and repeat semantics
- preserve standalone modifier-key events
- preserve distinct modifier keys where kitty exposes them
- evaluate whether injection APIs need kitty-aware encoding for tests
- decide whether non-ANSI drivers should expose the same richer model when the platform supports it

## Proposed File Changes

### Phase 1 likely new files

- `Terminal.Gui/Drivers/AnsiHandling/KittyKeyboardProtocolDetector.cs`
- `Terminal.Gui/Drivers/AnsiHandling/KittyKeyboardPattern.cs`
- `Tests/UnitTestsParallelizable/Drivers/AnsiHandling/KittyKeyboardProtocolDetectorTests.cs`

### Phase 1 likely modified files

- `Terminal.Gui/Drivers/AnsiHandling/EscSeqUtils/EscSeqUtils.cs`
- `Terminal.Gui/Drivers/AnsiHandling/AnsiKeyboardParser.cs`
- `Terminal.Gui/Drivers/AnsiDriver/AnsiOutput.cs`
- `Terminal.Gui/App/MainLoop/MainLoopCoordinator.cs`
- `Terminal.Gui/Drivers/DriverImpl.cs`
- `Tests/UnitTestsParallelizable/Drivers/AnsiHandling/AnsiKeyboardParserTests.cs`
- `Tests/UnitTestsParallelizable/Drivers/AnsiDriver/AnsiInputTestableTests.cs`

### Later phases likely modified files

- `Terminal.Gui/Input/Keyboard/Key.cs`
- `Terminal.Gui/Drivers/IDriver.cs`
- `Terminal.Gui/App/Keyboard/IKeyboard.cs`
- `Terminal.Gui/App/Keyboard/ApplicationKeyboard.cs`
- `Terminal.Gui/App/IApplication.cs`
- `Terminal.Gui/App/ApplicationImpl.Driver.cs`
- `Terminal.Gui/App/Legacy/Application.Keyboard.cs`
- view keyboard event surfaces and related test helpers

## Open Design Choices

### Which kitty flags to enable in phase 1

Recommendation:

- choose the smallest explicit enhancement flag set that yields reliable current-feature compatibility under ANSI

Reason:

- it reduces parser surface area
- it minimizes mismatch with the current `Key` abstraction
- it keeps later richer-event work additive

### Where to hold richer capability state

Recommendation:

- keep phase 1 capability state internal, but model it as more than a bare boolean

Reason:

- later phases will need to distinguish supported vs enabled vs fully consumed features

### Whether to start phase 2 by extending `Key` or adding a new event payload

Recommendation:

- defer the exact API shape until phase 1 lands, but design phase 1 code so the parser can later return richer intermediate data before it is collapsed into `Key`

Reason:

- that makes the later migration much easier
- it avoids entangling parser logic with the current `Key` limitations

## Verification Steps

For phase 1:

1. Run focused parser and detector tests.
2. Run ANSI driver tests that cover injected input and lifecycle behavior.
3. Run the parallelizable test project if the focused tests pass.
4. When diagnosing failures, use targeted tracing/logging to inspect the actual flow before changing code.

Commands:

```powershell
dotnet test --project Tests/UnitTestsParallelizable --no-build --filter "FullyQualifiedName~AnsiKeyboardParserTests|FullyQualifiedName~KittyKeyboardProtocolDetectorTests|FullyQualifiedName~AnsiInputTestableTests"
dotnet test --project Tests/UnitTestsParallelizable --no-build
```

If startup wiring changes broadly, also run:

```powershell
dotnet test --project Tests/UnitTests --no-build --filter "FullyQualifiedName~MainLoop"
```

For later phases, add dedicated verification for:

- `KeyUp` routing
- modifier-key identity and transitions
- repeat semantics
- compatibility behavior for existing `KeyDown` handlers

Across all phases:

- use tracing/logging to diagnose
- use release-safe observable behavior to assert correctness

## Risks To Watch

- Request collision with other `c`, `t`, or `u`-terminated ANSI requests if kitty query metadata is not distinct enough.
- Parser ambiguity if kitty CSI `u` matching is ordered after broad CSI patterns.
- Enabling kitty mode before support is confirmed, which could break input on non-supporting terminals.
- Baking lossy current-`Key` assumptions so deeply into the kitty parser that phase 2 becomes a rewrite instead of an extension.
- Failing to disable the protocol on shutdown, leaving the shell in an altered keyboard mode.
- Designing phase 2 in a way that breaks existing `KeyDown` consumers without a compatibility path.

## Recommended Delivery Order

1. Land phase 1 capability detection, parser support, startup/shutdown wiring, and compatibility tests.
2. Introduce a richer internal keyboard event shape if needed to decouple parser fidelity from the current `Key` API.
3. Revise `Key`, `IKeyboard`, `IApplication`, and related routing to support key-up/repeat/distinct modifiers.
4. Re-plumb the kitty parser and ANSI driver to populate the richer model end to end.
5. Expand tests to cover full-fidelity behavior and cross-driver compatibility.
