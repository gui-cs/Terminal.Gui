# Terminal.Gui v2.0.0 RC — Core Library Code Review

**Scope:** `/Terminal.Gui/Terminal.Gui/` excluding `ViewBase/` and `Views/`.
**Method:** Six parallel subsystem reviews (App, Drivers, Drawing, Configuration, Input, Text/FileServices/Time/Testing). Per-subsystem source reports live alongside this file: `review-app.md`, `review-drivers.md`, `review-drawing.md`, `review-configuration.md`, `review-input.md`, `review-text-fileservices.md`.

**Priority key:**
- **P0** — critical, must-fix ship stopper
- **P1** — critical, but not a ship stopper
- **P2** — nice to fix

> **Note (verification pass against `origin/develop` at `ef9a96a`):** The branch this review was performed on is behind `develop` by ~30 commits. After re-checking each P0 against `develop`, several have already been fixed there (notably `#5131 OutputBufferImpl race`, `#5134 AOT config warning cleanup`, AttributeJsonConverter rewrite, DeepCloner visited-mapping ordering, and DictionaryJsonConverter duplicate-key handling). One P0 was a false positive on my part (`CommandContextExtensions` uses C# 14 extension-block syntax, which is valid for this `net10.0`/C# 14 project). See **Verification Status** at the bottom of this file for the per-finding outcome.

---

## Executive Summary

| Subsystem            | P0 | P1 | P2 | Total |
|----------------------|----|----|----|-------|
| App                  | 2  | 8  | 4  | 14    |
| Drivers              | 5  | 8  | 9  | 22    |
| Drawing              | 2  | 6  | 7  | 15    |
| Configuration        | 5  | 8  | 7  | 20    |
| Input                | 3  | 5  | 6  | 14    |
| Text/FileServices    | 2  | 6  | 5  | 13    |
| **Totals**           | **19** | **41** | **38** | **98** |

**Three findings flagged by the Input subagent fall in `ViewBase/` and are out-of-scope for this review pass** (Key.Equals/GetHashCode is in Input proper and stays in scope; the HotKey-setter and CWP `OnXxx`-not-empty findings live in `View.Keyboard.cs` / `View.Command.cs`). They are listed at the end so they aren't lost.

### Top ship-blockers (highest-leverage P0s)

1. **Key.Equals includes `Handled`, GetHashCode does not** — breaks every `KeyBindings` dictionary lookup. (Input)
2. **`CommandContextExtensions.cs` uses malformed `extension(...)` syntax** — does not compile under any released C# version. (Input)
3. **`AttributeJsonConverter.Read()` re-reads the value token as a string and re-quotes it** — Attribute round-trip is broken whenever Foreground/Background are object-typed. (Configuration)
4. **`RuneJsonConverter.Write()` uses `WriteRawValue` without escaping** — produces invalid JSON for any Rune containing `"` or `\`. (Configuration)
5. **`SourceGenerationContext` missing types (`KeyCode`, `Rune`, `VisualRole`, `ColorName16`, `Dictionary<string, KeyCode>` etc.)** — every AOT/trimmed build will fail at runtime when those JSON paths are exercised. (Configuration)
6. **`DeepCloner` records the visited mapping after property cloning** — self-referential graphs recurse forever or partially clone. (Configuration)
7. **Dictionary/ConcurrentDictionary converters silently drop duplicate keys via `TryAdd`** — silent data loss on round-trip. (Configuration)
8. **`OutputBufferImpl.ClearContents()` reassigns `Contents`/`Clip` outside the lock** — use-after-free race against `AddGrapheme`. (Drivers)
9. **Unix raw mode is not restored if the input thread dies before driver disposal** — leaves the user's shell unusable. (Drivers)
10. **`AnsiOutput.Dispose()` writes cleanup ANSI without flushing** — process exits with mouse on, attributes corrupt, alternate buffer still active. (Drivers)
11. **Inline-mode cursor parked one row past the inline region on shutdown** — corrupts the host terminal on exit. (Drivers)
12. **`Region.XOR` differences against an already-mutated state** — produces results that are not XOR. (Drawing)
13. **`Region.DrawOuterBoundary` off-by-one in line lengths (already self-flagged BUGBUG)** — visible misrender. (Drawing)
14. **CWP violation in `ApplicationImpl.Begin()`: `SessionBegun` raised before `SetIsRunning`/`SetIsModal`** — subscribers see partial state. (App)
15. **`ApplicationImpl.Invoke()` thread-affinity check unsynchronized** — runs UI work on background threads under teardown. (App)
16. **`TextFormatter.FormatAndGetSize` uses `line.Length` for vertical height** — wrong height for CJK/emoji/combining-mark text. (Text)
17. **Vertical-text width path uses UTF-16 char-count check instead of grapheme count** — misclassifies combining-mark-only strings. (Text)
18. **`OutputBufferImpl` lazy-initialized `Clip` racing with `ClearContents`** — inconsistent clip after concurrent reset. (Drivers)
19. **HotKey property setter races with `TextFormatter.HotKeyChanged`** *(in ViewBase, listed at end as out-of-scope)*.

---

## P0 — Critical Ship Stoppers

### App

#### [P0] CWP violation: `SessionBegun` raised before state changes in `Begin()`
**File:** `Terminal.Gui/App/ApplicationImpl.Run.cs:154`
**Issue:** `SessionBegun?.Invoke(...)` fires before `SetIsRunning(true)` / `SetIsModal(true)` (lines 155–156). Per `.claude/rules/cwp-pattern.md`, work must complete before notification. Subscribers see a runnable that is not yet running/modal — a contract violation that's hard to retract once 2.0 ships.
**Fix:** Move the invoke call after the state mutations (or release the lock and raise outside it once state is committed).

#### [P0] Race condition in `Invoke()` thread-affinity fast path
**File:** `Terminal.Gui/App/ApplicationImpl.Run.cs:59,79`
**Issue:** `MainThreadId == Thread.CurrentThread.ManagedThreadId` and the `TopRunnableView` read are unsynchronized. `Dispose()` can null `MainThreadId` between check and dispatch; the action then runs off the main thread or against a stopped queue. UI mutations on a background thread corrupt state.
**Fix:** Snapshot `MainThreadId` to a local; either remove the fast path or guard with the same lock that `Init/Dispose` use. Treat post-`Dispose` invokes as `NotInitializedException`.

### Drivers

#### [P0] `OutputBufferImpl.ClearContents()` reassigns `Contents` and `Clip` outside the lock
**File:** `Terminal.Gui/Drivers/Output/OutputBufferImpl.cs:371-395`
**Issue:** Line 373 swaps `Contents` to a new array, line 377 swaps `Clip`, then line 384 takes the lock. Concurrent `AddGrapheme` callers (which lock internally) read the new array reference before the lock is held, blowing dirty tracking and producing garbled output.
**Fix:** Acquire the lock first, then reassign `Contents`/`Clip` inside the critical section.

#### [P0] Unix raw mode not restored on input thread crash
**File:** `Terminal.Gui/Drivers/UnixHelpers/UnixRawModeHelper.cs:38-109`
**Issue:** `Restore()` only runs on `Dispose()`. An unhandled exception on the input thread leaves the terminal in raw mode — the user's shell is unusable until they `reset(1)`.
**Fix:** Hook `AppDomain.CurrentDomain.UnhandledException` and `Console.CancelKeyPress`, and add a finalizer fallback that calls `Restore()`.

#### [P0] `AnsiOutput.Dispose()` doesn't flush before returning
**File:** `Terminal.Gui/Drivers/AnsiDriver/AnsiOutput.cs:363-405`
**Issue:** Cleanup writes (mouse off, SGR reset, alternate buffer leave) are queued but not flushed. On a slow stdout the process can exit with the buffer still pending, leaving the host terminal with mouse capture on, weird colors, and alt-buffer active.
**Fix:** Call the native flush after each cleanup write — or once at end of the `finally` block.

#### [P0] Inline-mode cursor parked one row past the region on dispose
**File:** `Terminal.Gui/Drivers/AnsiDriver/AnsiOutput.cs:384`
**Issue:** `lastInlineRow = appScreen.Y + appScreen.Height` points one row past the last drawn row. The cleanup write therefore lands outside the inline region, scrolling the terminal and leaving stray content.
**Fix:** Use `appScreen.Y + appScreen.Height - 1`.

#### [P0] `OutputBufferImpl.Clip` lazy-init races with `ClearContents`
**File:** `Terminal.Gui/Drivers/Output/OutputBufferImpl.cs:191,377`
**Issue:** `AddGrapheme()` lazily creates `Clip` on first access; `ClearContents()` recreates it from a different code path. Concurrent paths can produce inconsistent clip regions or null reads.
**Fix:** Initialize `Clip` in the constructor. Never leave it null; treat reset as overwrite-under-lock.

### Drawing

#### [P0] `Region.XOR` produces non-XOR results
**File:** `Terminal.Gui/Drawing/Region.cs:217-222`
**Issue:** The implementation does `this.Exclude(region)` and then `region.Combine(this, Difference)` — the second step uses the already-mutated `this`, so the result is `(this − region) ∪ (region − (this − region))`, not `(this ∖ region) ∪ (region ∖ this)`.
**Fix:** Snapshot/clone both operands before mutating either, then compute symmetric difference from the snapshots.

#### [P0] `Region.DrawOuterBoundary` off-by-one in line lengths (self-flagged BUGBUG)
**File:** `Terminal.Gui/Drawing/Region.cs:986-1031` (lengths at 1116 and 1130)
**Issue:** `int length = x - startX + 1;` should be `x - startX` because the iteration already includes the endpoint. The file's own BUGBUG comment documents the symptom: regions render one cell too tall and wide.
**Fix:** Drop the `+1` on lines 1116 and 1130 and add a regression test.

### Configuration

#### [P0] `AttributeJsonConverter.Read()` re-quotes the value token
**File:** `Terminal.Gui/Configuration/AttributeJsonConverter.cs:66-68`
**Issue:** After reading the property name, the code calls `reader.Read(); var property = $"\"{reader.GetString()}\"";` and feeds that back into `JsonSerializer.Deserialize<Color>(property, …)`. This (a) throws when the value is an object/number, (b) double-wraps strings, (c) breaks every Attribute whose colors are object-encoded.
**Fix:** Pass `ref reader` directly to the inner `JsonSerializer.Deserialize<Color>` and let it consume the next token.

#### [P0] `RuneJsonConverter.Write()` uses `WriteRawValue` without escaping
**File:** `Terminal.Gui/Configuration/RuneJsonConverter.cs:143`
**Issue:** `writer.WriteRawValue($"\"{value}\"")` injects raw text into the stream — any Rune that stringifies to a `"` or `\` corrupts the JSON document. `WriteRawValue` does not escape.
**Fix:** Replace with `writer.WriteStringValue(value.ToString())` unconditionally.

#### [P0] `SourceGenerationContext` is missing types used at runtime → AOT/trim breakage
**File:** `Terminal.Gui/Configuration/SourceGenerationContext.cs`
**Issue:** Generic-typed dictionary deserializers (`Dictionary<string, KeyCode>`, `Dictionary<string, Rune>`, etc.) call `JsonSerializer.Deserialize(ref reader, typeof(T), ConfigurationManager.SerializerContext)` for `T`s that aren't in the generation context: `KeyCode`, `Rune`, `VisualRole`, `ColorName16`, plus any open-generic dictionary instantiations. AOT/trimmed apps will throw `NotSupportedException` at runtime.
**Fix:** Add `[JsonSerializable(typeof(...))]` for every concrete `T` reachable through the dictionary/concurrent-dictionary converters; add `[JsonSerializable(typeof(Dictionary<string, KeyCode>))]` etc. for the closed generics; audit every enum used in config.

#### [P0] `DeepCloner` records the visited mapping after cloning, not before
**File:** `Terminal.Gui/Configuration/DeepCloner.cs:123-124`
**Issue:** The clone is recorded in `visited` *after* properties are cloned. Self-referential graphs recurse before the mapping exists; once it is finally added via `TryAdd`, the silent-failure path masks the bug. Result: stack overflow on cycles, or a half-initialized clone published into `visited`.
**Fix:** Insert `visited[source] = clone;` immediately after constructing `clone`, before walking properties. Treat a `TryAdd` failure as a logic error.

#### [P0] Dictionary / ConcurrentDictionary converters silently drop duplicate keys
**Files:** `Terminal.Gui/Configuration/DictionaryJsonConverter.cs:39`, `ConcurrentDictionaryJsonConverter.cs:40`
**Issue:** Both call `dictionary.TryAdd(key, value)` and ignore the return. Duplicate keys in the input JSON disappear with no error. Round-trip serialize-then-load loses entries silently — exactly the kind of bug that bricks user configs without anyone noticing.
**Fix:** Either throw `JsonException` on duplicates, or use `dictionary[key] = value` and document last-write-wins.

### Input

#### [P0] `Key.Equals` checks `Handled`, `GetHashCode` does not
**File:** `Terminal.Gui/Input/Keyboard/Key.cs:699,708`
**Issue:** `Equals` compares both `_keyCode` and `Handled`; `GetHashCode` only mixes `_keyCode`. Two `Key` values can be unequal but hash identically — but worse, two equal keys can hash differently if `Handled` differs. `KeyBindings` is a `ConcurrentDictionary<Key, KeyBinding>`; mutating `Handled` after lookup is enough to break subsequent lookups of the same key.
**Fix:** Drop `Handled` from `Equals`. `Handled` is per-event state and must not influence binding identity.

#### [P0] `CommandContextExtensions.cs` uses non-existent C# `extension` syntax
**File:** `Terminal.Gui/Input/CommandContextExtensions.cs:9`
**Issue:** The file declares `extension (ICommandContext? context) { ... }`. That is the C# 14 extension-block proposal; it is not in any shipped compiler. The file does not compile.
**Fix:** Restore the standard `public static class CommandContextExtensions { public static bool TryGetSource(this ICommandContext? context, out View? source) { … } }` form.

#### [P0] `KeyBindings` constructor lambda discards `key` and never sets `Target`/`Data`
**File:** `Terminal.Gui/Input/Keyboard/KeyBindings.cs:12`
**Issue:** The factory lambda passed to `CommandBindingsBase` is `(commands, key, source) => new KeyBinding(commands, source)`. The four-arg `KeyBinding` constructor expects `(commands, key, source, target, data)`. Every key binding created via `Add()` has `Key=null` and `Target=null`; application-level hot-key binding (which routes via `Target`) is dead.
**Fix:** Pass through: `(commands, key, source) => new KeyBinding(commands, key, source, null, null)`. Add a test that asserts `binding.Key` matches the dictionary key for a freshly added entry.

> *(Listed under P1 in the per-subsystem report; promoted here because if `Target` is always null, hot-key activation is silently broken at runtime.)*

### Drawing — duplicate listing intentionally omitted; see above.

### Text / FileServices

#### [P0] `TextFormatter.FormatAndGetSize` uses `line.Length` for vertical-text height
**File:** `Terminal.Gui/Text/TextFormatter.cs:556`
**Issue:** Width on the line above correctly calls `GetColumnsRequiredForVerticalText`; height on line 556 reverts to `lines.Max(line => line.Length)`. UTF-16 char count overstates height for surrogate pairs and ignores grapheme clustering. CJK/emoji vertical text is laid out wrong.
**Fix:** `height = lines.Max(line => line.GetColumns());`.

#### [P0] Vertical-text width gate uses UTF-16 char count instead of grapheme count
**File:** `Terminal.Gui/Text/TextFormatter.cs:2264`
**Issue:** `if (strings.Length > 0)` is checking UTF-16 char length before iterating graphemes. Combining-mark-only inputs and grapheme-cluster edge cases trip the wrong branch.
**Fix:** Test against `GraphemeHelper.GetGraphemes(strings).Any()` (or a count helper).

---

## P1 — Critical, Not Ship-Blocking

### App

- **[P1] `SessionToken.Result` lifecycle confusion** — `IRunnable.cs:41-50` documents that subscribers should set `Result` during `IsRunningChanging`, but the field is also set by `End()` afterward. Public + mutable + ambiguous = breakage in subclasses. (`ApplicationImpl.Run.cs:392`)
  *Fix:* Make `SessionToken.Result` `internal set` and document a single owner.
- **[P1] `Invoke()` after `Dispose()` silently drops actions** (`ApplicationImpl.Run.cs:56-93`). *Fix:* Throw `NotInitializedException` when `!Initialized`.
- **[P1] `SessionToken.Runnable` nullability undocumented** (`Runnable/IRunnable.cs`, `ApplicationImpl.Run.cs:138`). *Fix:* Document that `End()` nulls it; consider an `IsEnded` view.
- **[P1] `TopRunnable` transiently null between `End()` and pop** (`ApplicationImpl.Run.cs:151,380,384`). Concurrent `RequestStop` sees null while the stack still holds runnables. *Fix:* Update `TopRunnable` only inside the lock.
- **[P1] `previousTop` set/event-pair split across the lock boundary** (`ApplicationImpl.Run.cs:159,165`). One side runs unconditionally, the other guarded — the CWP pairing breaks. *Fix:* Pair `SetIsModal` and `RaiseIsModalChangedEvent` together.
- **[P1] `ResetState()` is not safe under cancelable `End()` or handler exceptions** (`ApplicationImpl.Lifecycle.cs:231-237`). *Fix:* Snapshot the stack, clear it, then end each token under try/catch; respect cancellation.
- **[P1] `_disposed` reset only on the legacy singleton path** (`ApplicationImpl.Lifecycle.cs:140-149`). Re-init semantics diverge between models. *Fix:* Remove the legacy reset or document the asymmetry as a 2.0 contract.
- **[P1] `MainThreadId` read without memory barrier** (`ApplicationImpl.Run.cs:59,79`). Same root cause as the P0 race; flagged separately because the unsynchronized read alone causes torn reads on ARM. *Fix:* `volatile` field or a single-field "running state" sentinel.

### Drivers

- **[P1] `SetTerminalTitle` clamps `mode` silently and lacks length cap** (`DriverImpl.cs:387-395`). *Fix:* Validate `mode ∈ {0,1,2}`; cap title length.
- **[P1] `UnixRawModeHelper.Restore()` uses `_originalTermios` even if `tcgetattr` failed** (`UnixHelpers/UnixRawModeHelper.cs:54-62`). *Fix:* Add a `_haveSavedTermios` flag.
- **[P1] Windows console mode restore not serialized with concurrent `WriteFile`** (`WindowsHelpers/WindowsVTOutputHelper.cs:139-157`). *Fix:* Lock writes/mode changes against each other.
- **[P1] `AnsiInput.Dispose()` flush capped at 10 attempts; silent on cap hit** (`AnsiDriver/AnsiInput.cs:286-360`). *Fix:* Use a deadline; log on early-out.
- **[P1] `OutputBase.Write()` cell-batching loses dirty state for wide glyphs** (`Output/OutputBase.cs:105-171`). The for-loop `++col` post-increments after `AppendCellAnsi` may have already advanced `col`, double-stepping. *Fix:* Switch to `while`-loop with explicit width-aware advance.
- **[P1] Cursor positioning has no bounds check** (`AnsiDriver/AnsiOutput.cs:316-324`). Out-of-bounds positions emit malformed ANSI. *Fix:* Clamp + log.
- **[P1] Windows Ctrl+Z workaround unconditionally synthesizes 0x1A** (`WindowsHelpers/WindowsVTInputHelper.cs:192-203`). Real Ctrl+Z input duplicates. *Fix:* Detect actual EOF vs spurious 0-byte read.
- **[P1] `AnsiSizeMonitor` may leak its query response into the shell on shutdown** (`AnsiDriver/AnsiComponentFactory.cs:48-72`). *Fix:* Dispose size monitor before input; add a query timeout.

### Drawing

- **[P1] `Cell.SplitNewLines` indexes graphemes by char** (`Drawing/Cell.cs:197-205`). *Fix:* `cells[i].Grapheme == "\n"`.
- **[P1] `Region.MinimizeRectangles` merges *overlapping* rectangles even though comments say "adjacent only"** (`Drawing/Region.cs:796,804`). Either the merge condition or the doc is wrong; today the resulting rectangle list disagrees with the comment. *Fix:* Pick a contract and remove the `IntersectsWith` clause if the contract is "adjacent".
- **[P1] `Ruler.Draw` indexes potentially wide-grapheme template strings by char** (`Drawing/Ruler.cs:58`). *Fix:* Iterate by grapheme.
- **[P1] `Region.Complement` can blow memory on large bounds** (`Drawing/Region.cs:248-264`). *Fix:* Cap intermediate rectangle count; use the same guard as `DrawOuterBoundary`.
- **[P1] `SixelEncoder.ProcessBand` allocates `Quantizer.Palette.Count + 1` arrays without bounds check** (`Drawing/Sixel/SixelEncoder.cs:107-114`). *Fix:* Reject palettes > 256.
- **[P1] `PopularityPaletteWithThreshold.MergeSimilarColors` relies on a `.ToList()` snapshot for correctness with no comment** (`Drawing/Quant/PopularityPaletteWithThreshold.cs:78`). *Fix:* Add a guarding comment or refactor to an index loop.

### Configuration

- **[P1] `ColorJsonConverter` null handling asymmetric** (`ColorJsonConverter.cs:34-55`). *Fix:* Read should accept `Null`; Write should emit null when applicable.
- **[P1] `KeyJsonConverter` returns `Key.Empty` on parse failure** (`KeyJsonConverter.cs:12`). Silent data loss for typos in user configs. *Fix:* Throw `JsonException` with the offending value.
- **[P1] `ScopeJsonConverter` throws on unknown property names** (`ScopeJsonConverter.cs:150-152`). Forward incompatibility — newer-version configs crash older Terminal.Gui. *Fix:* `Logging.Warning(...); reader.Skip();` (the file already TODOs this).
- **[P1] `SourceGenerationContext` missing additional enums (`VisualRole`, `ColorName16`)** — same root cause as the P0; broken out so it can be tracked separately. *Fix:* Inventory every enum reachable from converters and serialize-attr them.
- **[P1] `Settings` callers don't hold the read lock across multiple reads** (`ConfigurationManager.cs:66-94`, callers at `:345,:676`). *Fix:* Take the read lock for the whole composite operation, not for each accessor.
- **[P1] `DeepCloner` AOT fallback only catches `MissingMethodException`** (`DeepCloner.cs:191-193`). `JsonException` from un-registered types surfaces as a noisy stack instead of the actionable "add to SourceGenerationContext" message. *Fix:* Catch broadly and rewrap.
- **[P1] `AttributeJsonConverter` requires both Foreground and Background** (`AttributeJsonConverter.cs:46-49`). Themes that override only one color don't load. *Fix:* Make either color optional with a documented default.
- **[P1] `ConfigurationManager.Initialize()` toggles `Immutable` off→clone→on, observable mid-flight** (`ConfigurationManager.cs:200-211`). *Fix:* Hold the write lock across the whole block.

### Input

- **[P1] `CommandBridge` leaks event subscriptions when the owner is GC'd without explicit dispose** (`Input/CommandBridge.cs:44-62,80-101`). *Fix:* Mandatory `Dispose` (XML-doc + finalizer warning) or weak handlers.
- **[P1] `CommandBridge.OnRemoteCommandNotBound` `Handled = true` doesn't stop the remote's already-started dispatch** (`Input/CommandBridge.cs:145-181`). Possible double-processing. *Fix:* Track bridged-command state; document the contract.
- **[P1] `Key.operator !=(Key?, Key?)` and `Key.operator ==(Key, Key)` have asymmetric nullability** (`Input/Keyboard/Key.cs:714,720`). *Fix:* Make both nullable with consistent semantics.
- **[P1] `CommandOutcome.HandledContinue` semantics ambiguous** (`Input/CommandOutcome.cs:39-44`). Name suggests "continue routing up"; implementation means "continue to next sequenced command". *Fix:* Rename to `HandledContinueSequence` or expand XML docs explicitly.
- **[P1] `KeyBindings` constructor lambda discards `key`** — promoted to P0 above; left here only as cross-reference.

### Text / FileServices

- **[P1] `TextFormatter.Justify` splits on `' '` chars, mangling combining-mark text around spaces** (`Text/TextFormatter.cs:1948-1997`). *Fix:* Iterate by grapheme.
- **[P1] `TextFormatter.WordWrapText` separates trailing zero-width marks from their base** (`Text/TextFormatter.cs:1614-1632`). *Fix:* Pull trailing combining marks into the same word.
- **[P1] `FileSystemTreeBuilder.TryGetChildren` has no symlink-loop / depth guard** (`FileServices/FileSystemTreeBuilder.cs:53`). *Fix:* Track visited inodes/paths or cap depth.
- **[P1] `DefaultSearchMatcher` uses `OrdinalIgnoreCase` undocumented** (`FileServices/DefaultSearchMatcher.cs:18-22`). Silent for Turkish-style locales. *Fix:* Document or expose culture.
- **[P1] `TextFormatter` rented arrays leak on exception** (`Text/TextFormatter.cs:128-145,971-988`). *Fix:* Hard try/finally around `ArrayPool.Rent`.
- **[P1] `TextFormatter.FindHotKey` compares against `0xFFFD` instead of using `Rune.IsValid`** (`Text/TextFormatter.cs:2472,2496`). *Fix:* Use `Rune.IsValid`.

---

## P2 — Nice to Fix

### App
- CWP placement of `SessionBegun` inside the lock makes the contract unclear (`ApplicationImpl.Run.cs:154`).
- `Keyboard`/`Mouse` lazy re-init can re-subscribe if `UnsubscribeApplicationEvents` is incomplete (`ApplicationImpl.cs:205-230`).
- `SessionToken.Runnable` nulled after `SessionEnded` invoke; subscribers see it flip (`ApplicationImpl.Run.cs:399`).
- `Trace.Lifecycle` call passes the literal expression as a string (`ApplicationImpl.Run.cs:112`).

### Drivers
- `DriverImpl` event-forwarding lambdas capture `this` without disposed-guards (`DriverImpl.cs:70-72`).
- `_clearLastOutputPending` flag in `OutputBase.Write` is non-atomic (`Output/OutputBase.cs:61-62,280-289`).
- `AnsiInputProcessor.OnKeyboardEventParsed` resets suppression on every key (`AnsiDriver/AnsiInputProcessor.cs:62-80`).
- `OutputBufferImpl.IsValidLocation` doesn't reject negative `Cols`/`Rows` (`Output/OutputBufferImpl.cs:406-411`).
- `EscSeqUtils.SanitizeOscText` lets through nulls and other non-control non-printables (`AnsiHandling/EscSeqUtils/EscSeqUtils.cs:1106-1124`).
- Windows mouse polling unthrottled in `Peek` (`WindowsHelpers/WindowsVTInputHelper.cs:254-256`).
- `OutputBase.Write` uses null-forgiving on `Contents` (`Output/OutputBase.cs:109`).
- `OutputBufferImpl._column1ReplacementChar` mutated without barrier (`Output/OutputBufferImpl.cs:101,269`).
- ANSI size-query response parser silently swallows malformed responses (`AnsiDriver/AnsiOutput.cs:331-360`).

### Drawing
- `Region.GetBounds` variable naming invites off-by-one mistakes (`Drawing/Region.cs:407-430`).
- `Cell.SplitNewLines` doesn't recognize U+2028/U+2029 (`Drawing/Cell.cs:187-227`).
- `Thickness.Draw` no null-check on `driver.CurrentAttribute` (`Drawing/Thickness.cs:178-189`).
- `Region.MergeRectangles` allocates a fresh `SortedSet` per x-transition (`Drawing/Region.cs:620-680`).
- `ColorJsonConverter` no length cap on input (`Configuration/ColorJsonConverter.cs:34-56`).
- `Gradient.GetColorAtFraction` doesn't guard `Spectrum.Last()` against empty (`Drawing/Gradient.cs:100-116`).
- `Attribute.Equals` includes alpha but doesn't document it (`Drawing/Attribute.cs:168`).

### Configuration
- `RuneJsonConverter.Read()` validation messaging unclear (`RuneJsonConverter.cs:94-119`).
- `KeyCodeJsonConverter` Read/Write empty-state asymmetry (`KeyCodeJsonConverter.cs:15`).
- `TraceCategoryJsonConverter` accepts three input shapes but writes only two (`TraceCategoryJsonConverter.cs:67-104`).
- `DeepCloner` `ReferenceEqualityComparer` reasoning undocumented (`DeepCloner.cs:65`).
- `SourcesManager` config-file loading does no path validation.
- `ScopeJsonConverter` calls `Activator.CreateInstance` per converter without caching (`ScopeJsonConverter.cs:65,80`).
- `AppSettings` getter has dual defensive null paths that hide init bugs (`ConfigurationManager.cs:635-664`).

### Input
- `CommandBridge` subscribes to `Activated` only, not `Activating` (`Input/CommandBridge.cs:49-52`).
- `AddKeyBindingsForHotKey` always Remove+Add, non-atomic on `ConcurrentDictionary` (`ViewBase/View.Keyboard.cs:191-204` — out of scope, retained as note).
- `CommandBridge` has no zombie-handler cleanup.
- `MouseBinding` uses `DateTime.Now`; coordinate frame undocumented (`Input/Mouse/MouseBinding.cs:16-20`).
- `CommandContext.WithValue` allocates `[..Values, value]` per append; O(N²) for deep hierarchies (`Input/CommandContext.cs:87`).
- CWP `OnXxx` virtuals return `bool` instead of being empty in base (`ViewBase/View.Command.cs:305,490,837,995,1020` — out of scope, retained as note).

### Text / FileServices / Time / Testing
- Dead `for (var i = 0; i < 1; i++)` in `Justify` (`Text/TextFormatter.cs:1980-1981`).
- `StripCRLF` and `ReplaceCRLFWithSpace` near-duplicates (`Text/TextFormatter.cs:1317-1373`).
- `ModuleInitializers` runs `ConfigurationManager.Initialize()` unconditionally (`ModuleInitializers.cs:22-25`).
- `VirtualTimeProvider.Advance` doesn't re-fire timers scheduled by callbacks (`Time/VirtualTimeProvider.cs:24-27`).
- `GlobalResources` uses `null!` defaults on parameters (`Resources/GlobalResources.cs:29,68`).
- `InputInjector` silently ignores delays under `SystemTimeProvider` (`Testing/InputInjector.cs:213-216`).

---

## Out-of-Scope Findings (ViewBase) — listed so they aren't dropped

These were surfaced by the Input subagent but live under `ViewBase/`, which the user excluded from this pass:

- **[P0/ViewBase] HotKey setter assigns `_hotKey` after firing `TextFormatter.HotKeyChanged`** — handler reads stale `_hotKey`. (`ViewBase/View.Keyboard.cs:97-99`, comment marked BUGBUG)
- **[P2/ViewBase] `OnAccepting`/`OnActivating`/etc. return `bool` from base** instead of being empty per CWP rule. (`ViewBase/View.Command.cs:305,490,837,995,1020`)
- **[P2/ViewBase] `AddKeyBindingsForHotKey` non-atomic Remove+Add on `ConcurrentDictionary`** (`ViewBase/View.Keyboard.cs:191-204`).

---

## Cross-Cutting Themes (worth fixing as a class, not as one-offs)

1. **CWP discipline is uneven.** Multiple subsystems either fire events before mutating state (`Begin`, `previousTop`) or split paired state-change/event across lock boundaries. A targeted sweep enforcing "lock → mutate → release → notify" would clear several P0/P1 items at once.
2. **AOT/trim coverage is incomplete.** The `SourceGenerationContext` review surfaces multiple missing types; couple this with a CI job that builds and runs a smoke-test under `PublishAot=true` and `TrimMode=full` against the configuration round-trip suite.
3. **Grapheme rule is mostly followed but slips into char/Length in vertical text and Justify.** A helper `GraphemeIterator` plus targeted unit tests would catch the remaining sites — these are subtle and easy to regress.
4. **JSON converters lack a round-trip test harness.** Most P0/P1 in Configuration would have been caught by a property-based test that round-trips every registered type. Recommend adding one before shipping.
5. **Driver dispose paths don't fence I/O.** Three of four driver P0s are about cleanup ordering (flush, mode restore, cursor position). A single "shutdown choreography" doc + tests would prevent regressions.
6. **`Key`/`KeyCode` equality + dictionary use is fragile.** Beyond the P0, the class has overload asymmetries and a lossy JSON converter. Consider a small KeyEquality test that asserts the .NET contract on a representative sample.

---

## Recommended Pre-Ship Action List (P0 only)

In rough order of risk × ease-of-fix:

1. Fix `CommandContextExtensions.cs` syntax (1-line, won't compile until it's fixed).
2. Fix `Key.Equals` / `GetHashCode` contract (drop `Handled` from `Equals`).
3. Fix `KeyBindings` factory lambda to pass `key` through (also covers a P1).
4. Fix `AttributeJsonConverter.Read` value-token handling.
5. Fix `RuneJsonConverter.Write` raw-write escape bug.
6. Fix Dictionary/ConcurrentDictionary `TryAdd` silent-drop.
7. Fix `DeepCloner` visited-mapping ordering.
8. Audit `SourceGenerationContext` and add missing types; add an AOT smoke test.
9. Lock `ClearContents` / `Clip` writes in `OutputBufferImpl`.
10. Fix `AnsiOutput.Dispose` flush + the `lastInlineRow` off-by-one.
11. Add a Unix raw-mode unhandled-exception/Ctrl+C net (signal + finalizer).
12. Fix `Region.XOR` correctness and `Region.DrawOuterBoundary` line lengths.
13. Fix CWP order in `ApplicationImpl.Begin`; remove the `Invoke` thread-affinity race.
14. Fix `TextFormatter` vertical-height `line.Length` and grapheme-count gate.

The remaining P1s should be triaged for a follow-on RC; P2s can land post-2.0.

---

## Verification Status Against `origin/develop` (`ef9a96a`)

The original review was performed against `claude/review-core-v2-EKNyf`, which is ~30 commits behind `develop`. Each P0 was re-checked on `develop`. Outcomes:

### P0s already fixed on `develop` — strike from action list

| # | Finding | File | Status / Evidence |
|---|---|---|---|
| 8 | OutputBufferImpl `ClearContents` race | `Drivers/Output/OutputBufferImpl.cs` | **FIXED** — PR #5131. A new `private readonly Lock _contentsLock = new();` (line 18) is now held by `ClearContents`, `AddGrapheme`, `FillRect`, and `SetSize`; the doc-comment explicitly calls out "never replaced, guaranteeing mutual exclusion". |
| 9 | OutputBufferImpl `Clip` lazy-init race | same file | **FIXED** — same `_contentsLock` covers `Clip` access in `FillRect`, `ClearContentsCore`, and `AddGrapheme`. |
| 10 | `AttributeJsonConverter.Read()` re-quotes the value token | `Configuration/AttributeJsonConverter.cs` | **FIXED** — rewritten to call `JsonSerializer.Deserialize(ref reader, ConfigurationManager.SerializerContext.Color)` directly. The `$"\"{reader.GetString()}\""` is now only used to format error messages in the catch block. |
| 13 | `DeepCloner` visited-mapping ordering | `Configuration/DeepCloner.cs` | **FIXED** — `visited.TryAdd(source, clone);` now runs immediately after `CreateInstance(type)`, with an explicit `// Add to visited before cloning properties` comment, before the property-clone loop. |
| 14a | `DictionaryJsonConverter` silent duplicate-key drop | `Configuration/DictionaryJsonConverter.cs` | **FIXED** — switched from `TryAdd` to `dictionary.Add(key, (T)value)`, which throws on duplicates. (Note: see 14b below — the `ConcurrentDictionary` variant is *not* fixed.) |

### P0 that was a false positive — withdraw

| # | Finding | File | Status |
|---|---|---|---|
| 2 | `CommandContextExtensions.cs` "uses invalid C# extension syntax" | `Input/CommandContextExtensions.cs` | **NOT A BUG.** The file uses C# 14 extension-block syntax (`extension (ICommandContext? context) { ... }`), which is valid on this project (`net10.0` / C# 14 per CLAUDE.md). The original review subagent applied pre–C# 14 syntax expectations. Withdrawn. |

### P0s partially addressed — re-audit needed before ship

| # | Finding | File | Status |
|---|---|---|---|
| 12 | `SourceGenerationContext` missing types | `Configuration/SourceGenerationContext.cs` | **PARTIALLY FIXED** — PR #5134 added `Dictionary<string, object>`, `Dictionary<ColorName16, string>`, `Dictionary<Command, PlatformKeyBinding>`, and the nested `Dictionary<string, Dictionary<Command, PlatformKeyBinding>>`, plus `PlatformKeyBinding` itself. The original concern about `Dictionary<string, KeyCode>` is moot because that type is no longer used (replaced by `PlatformKeyBinding`). Re-audit `Rune` and `VisualRole` coverage; `Rune` has its own `JsonConverter` so likely fine, but a one-off AOT smoke test would close the loop. |
| 14b | `ConcurrentDictionaryJsonConverter` silent duplicate-key drop | `Configuration/ConcurrentDictionaryJsonConverter.cs` | **STILL PRESENT** — still uses `dictionary.TryAdd(key, (T)value);` and ignores the return. Apply the same fix as `DictionaryJsonConverter`. |

### P0s confirmed still present on `develop`

| # | Finding | File | Notes |
|---|---|---|---|
| 1 | CWP: `SessionBegun` before `SetIsRunning`/`SetIsModal` | `App/ApplicationImpl.Run.cs` ~ line 155 | Still inside the lock and still raised before the state setters. |
| 3 | `Invoke()` thread-affinity race | `App/ApplicationImpl.Run.cs` ~ lines 60, 80 | `MainThreadId == Thread.CurrentThread.ManagedThreadId` still unsynchronized. |
| 4 | Unix raw mode not restored on input thread crash | `Drivers/UnixHelpers/UnixRawModeHelper.cs` | No diff vs branch. |
| 5 | `AnsiOutput.Dispose()` doesn't flush | `Drivers/AnsiDriver/AnsiOutput.cs` | No diff vs branch. |
| 6 | Inline-mode cursor parked one row past region | `Drivers/AnsiDriver/AnsiOutput.cs` ~ line 384 | No diff vs branch. |
| 7 | `Region.XOR` produces non-XOR results | `Drawing/Region.cs:217-222` | No diff vs branch. |
| 11 | `Region.DrawOuterBoundary` off-by-one (self-flagged BUGBUG) | `Drawing/Region.cs:986-1031` | No diff vs branch. |
| 15 | `RuneJsonConverter.Write` uses `WriteRawValue` without escaping | `Configuration/RuneJsonConverter.cs:144` | Still present (the un-escaped `writer.WriteRawValue($"\"{value}\"")` path remains for the `Rune.MakePrintable() == ReplacementChar` case). |
| 16 | `Key.Equals` includes `Handled`, `GetHashCode` does not | `Input/Keyboard/Key.cs:699,708` | Still present. |
| 17 | `KeyBindings` factory lambda discards `key` | `Input/Keyboard/KeyBindings.cs:12` | Still present: `(commands, key, source) => new KeyBinding (commands, source)`. |
| 18 | `TextFormatter.FormatAndGetSize` vertical height uses `line.Length` | `Text/TextFormatter.cs:556` | Still present. |
| 19 | `TextFormatter.GetColumnsRequiredForVerticalText` `strings.Length` gate | `Text/TextFormatter.cs:2264` | Gate still uses UTF-16 char count. **Severity reconsidered:** practically a P2, not a P0 — for any non-empty string the gate is correct (an all-combining-mark string still has `.Length > 0`). The substantive grapheme-correctness work is finding 18, not this one. Recommend reclassifying to P2. |

### Updated P0 ledger (post-verification)

| Bucket | Count | Items |
|---|---|---|
| P0s genuinely outstanding | **11** | 1, 3, 4, 5, 6, 7, 11, 14b (ConcurrentDictionary half), 15, 16, 17, 18 |
| P0s already fixed on develop | **5** | 8, 9, 10, 13, 14a |
| P0s withdrawn (not a bug) | **1** | 2 (CommandContextExtensions) |
| P0s partially fixed | **1** | 12 (SourceGenerationContext — re-audit Rune/VisualRole) |
| P0 reclassified to P2 | **1** | 19 (vertical-text Length gate) |

> Counts above add to 19, matching the original tally in the executive summary.

### Revised pre-ship action list (P0s only)

1. Drop `Handled` from `Key.Equals` (item 16).
2. Pass `key` through the `KeyBindings` factory lambda (item 17).
3. Fix `Region.XOR` (snapshot operands first) (item 7).
4. Fix `Region.DrawOuterBoundary` line lengths (`-1` on lines 1116/1130) (item 11).
5. Fix `TextFormatter.FormatAndGetSize` vertical height to use `GetColumns()` (item 18).
6. Fix `RuneJsonConverter.Write` to use `WriteStringValue` unconditionally (item 15).
7. Apply the `Add`-instead-of-`TryAdd` fix to `ConcurrentDictionaryJsonConverter` (item 14b).
8. CWP order in `ApplicationImpl.Begin` — set `IsRunning`/`IsModal` before raising `SessionBegun` (item 1).
9. Synchronize the `Invoke` thread-affinity check or remove the fast path (item 3).
10. Flush + correct `lastInlineRow` in `AnsiOutput.Dispose` (items 5, 6).
11. Add Unix raw-mode crash-restore safety net (item 4).
12. Confirm `SourceGenerationContext` AOT coverage with a build-time smoke test (item 12).

P1s and P2s above remain unchanged by this verification pass; spot checks suggest most are still present, but a full re-verification was out of scope for this pass.

---

## Issue & PR Tracker

Issues filed and PRs opened against `gui-cs/Terminal.Gui` (one PR per issue, each containing a fix plus a unit test that fails before the fix and passes after):

| P0 | Issue | PR | Subject |
|---|---|---|---|
| 1 | [#5162](https://github.com/gui-cs/Terminal.Gui/issues/5162) | [#5184](https://github.com/gui-cs/Terminal.Gui/pull/5184) → [#5188](https://github.com/gui-cs/Terminal.Gui/pull/5188) | **Reframed**: original CWP-violation framing was wrong. `SessionBegun` is a token-creation hook, `SessionEnded` is a token-disposal hook — not paired before/after state-change events. Issue #5162 closed `not_planned`; PR #5184 closed unmerged; replaced by doc-only PR #5188 tightening XML on `IApplication.cs` to make the semantics explicit. |
| 2 | [#5163](https://github.com/gui-cs/Terminal.Gui/issues/5163) | [#5185](https://github.com/gui-cs/Terminal.Gui/pull/5185) | `Invoke()` throws `NotInitializedException` after `Dispose` (deeper race deferred) |
| 3 | [#5164](https://github.com/gui-cs/Terminal.Gui/issues/5164) | [#5186](https://github.com/gui-cs/Terminal.Gui/pull/5186) | `UnixRawModeHelper` saved-state guard, finalizer, `ProcessExit`/`CancelKeyPress` hooks |
| 4 | [#5165](https://github.com/gui-cs/Terminal.Gui/issues/5165) | [#5187](https://github.com/gui-cs/Terminal.Gui/pull/5187) | `AnsiOutput.Dispose` flushes after cleanup writes (stacked on #5182) |
| 5 | [#5166](https://github.com/gui-cs/Terminal.Gui/issues/5166) | [#5182](https://github.com/gui-cs/Terminal.Gui/pull/5182) | Inline-mode cursor parks on last row of region (off-by-one fix) |
| 6 | [#5167](https://github.com/gui-cs/Terminal.Gui/issues/5167) | [#5178](https://github.com/gui-cs/Terminal.Gui/pull/5178) | `Region.XOR` snapshot operands; produces correct symmetric difference |
| 7 | [#5168](https://github.com/gui-cs/Terminal.Gui/issues/5168) | [#5183](https://github.com/gui-cs/Terminal.Gui/pull/5183) | `Region.DrawOuterBoundary` draws within bounds (drops `+1` and shifts bottom/right edges) |
| 8 | [#5169](https://github.com/gui-cs/Terminal.Gui/issues/5169) | — | **Withdrawn**: `WriteRawValue` branch unreachable; commented with analysis recommending defensive cleanup or close. |
| 9 | [#5170](https://github.com/gui-cs/Terminal.Gui/issues/5170) | [#5179](https://github.com/gui-cs/Terminal.Gui/pull/5179) | `Key.Equals` drops `Handled`; `GetHashCode` contract restored |
| 10 | [#5171](https://github.com/gui-cs/Terminal.Gui/issues/5171) | [#5177](https://github.com/gui-cs/Terminal.Gui/pull/5177) | `KeyBindings` factory lambda passes `key` through (also fixes silent `source`-into-`Data` bug) |
| 11 | [#5172](https://github.com/gui-cs/Terminal.Gui/issues/5172) | [#5180](https://github.com/gui-cs/Terminal.Gui/pull/5180) | `TextFormatter` vertical-height uses `GetColumns()` |
| 12 | [#5173](https://github.com/gui-cs/Terminal.Gui/issues/5173) | [#5181](https://github.com/gui-cs/Terminal.Gui/pull/5181) | `ConcurrentDictionaryJsonConverter` rejects duplicate keys |

**Summary:** 12 P0 issues filed; final state — 10 fix PRs open (#5177–#5183, #5185–#5187), 1 doc-only PR (#5188), 1 withdrawn (#5169 — branch unreachable), 1 reframed (#5162 → doc-only #5188; original PR #5184 closed unmerged). Each fix PR contains a unit test that fails on `develop` and passes with the fix.
