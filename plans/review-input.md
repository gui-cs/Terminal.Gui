# Terminal.Gui v2.0.0 Input Subsystem Review

## P0 - Critical Ship Stoppers

### [P0] Key.Equals includes Handled property but GetHashCode does not
**File:** `/home/user/Terminal.Gui/Terminal.Gui/Input/Keyboard/Key.cs:699,708`
**Issue:** `Key.Equals()` (line 699) compares both `_keyCode` AND `Handled` status, but `GetHashCode()` (line 708) only hashes `_keyCode`. This violates the .NET contract: if two objects are equal, they MUST have the same hash code. When a Key with `Handled=true` is used as a dictionary key in `KeyBindings` (ConcurrentDictionary<Key, KeyBinding>), lookups will fail or produce incorrect results because the hash codes diverge from equality semantics. This is critical because `KeyBindings` uses Keys as dictionary keys.
**Suggested fix:** Remove `other.Handled == Handled` from the Equals method (line 699). The Handled property is event state, not part of the binding identity. Alternatively, include Handled in GetHashCode, but that's wrong because Handled shouldn't affect binding lookup semantics.

### [P0] CommandContextExtensions uses invalid C# extension syntax
**File:** `/home/user/Terminal.Gui/Terminal.Gui/Input/CommandContextExtensions.cs:9`
**Issue:** Line 9 uses `extension (ICommandContext? context) { ... }` which is not valid C# syntax. The extension method declaration is malformed. This code will not compile. The correct syntax should be `public static bool TryGetSource (this ICommandContext? context, out View? source)` as a static method or proper extension method syntax.
**Suggested fix:** Change line 9 from `extension (ICommandContext? context)` to proper static extension method signature. Wrap the TryGetSource method in a proper class-level static method declaration.

### [P0] HotKey property setter updates _hotKey AFTER firing TitleTextFormatter.HotKeyChanged event
**File:** `/home/user/Terminal.Gui/Terminal.Gui/ViewBase/View.Keyboard.cs:97-99`
**Issue:** The BUGBUG comment at line 97 flags that `_hotKey` is set AFTER `TitleTextFormatter.HotKey = value` (line 99). This means when `TextFormatter_HotKeyChanged` fires (which updates `_hotKey`), the old `_hotKey` is still active, causing race conditions. If code checks `_hotKey` during the HotKeyChanged event, it will see the stale value. Also, `AddKeyBindingsForHotKey` at line 91 compares `_hotKey` to `hotKey` (line 125) before the assignment, which is correct, but the event handlers will see inconsistent state.
**Suggested fix:** Move `_hotKey = value` assignment to line 98 BEFORE calling `TitleTextFormatter.HotKey = value` on line 99 to ensure the field is updated before any side effects fire.

## P1 - Critical But Not Ship Stoppers

### [P1] CommandBridge does not unsubscribe from remote view events if owner view is garbage collected
**File:** `/home/user/Terminal.Gui/Terminal.Gui/Input/CommandBridge.cs:44-62,80-101`
**Issue:** CommandBridge subscribes to `remote.Accepted`, `remote.Activated`, and `remote.CommandNotBound` events (lines 46, 51, 61). These subscriptions hold a strong reference to CommandBridge through the event handler delegates. If the `_owner` view is garbage collected but CommandBridge is not disposed (because it's not explicitly held anywhere), the event subscriptions remain on the `remote` view indefinitely, preventing the remote view's cleanup. The bridge becomes a zombie subscriber that can't be cleaned up by GC. Callers must explicitly call `Dispose()` or the bridge leaks event subscriptions.
**Suggested fix:** Implement a finalizer or provide clearer documentation that callers must call `Dispose()` on CommandBridge instances. Consider using WeakEventManager pattern or storing bridges in a weak collection that auto-cleans.

### [P1] KeyBindings constructor passes incorrect lambda to CommandBindingsBase - misses source/target parameters
**File:** `/home/user/Terminal.Gui/Terminal.Gui/Input/Keyboard/KeyBindings.cs:12`
**Issue:** Line 12 passes `(commands, key, source) => new KeyBinding(commands, source)` but the KeyBinding constructor at `/home/user/Terminal.Gui/Terminal.Gui/Input/Keyboard/KeyBinding.cs:44` accepts `(commands, key, source, target, data)`. The lambda receives `key` but ignores it, and doesn't provide `target` or `data`. This means all KeyBindings created via `CommandBindingsBase.Add()` will have `Key=null` and `Target=null`, losing critical binding metadata. Application-level hotkey bindings (which use `Target` to specify which view to activate) will break.
**Suggested fix:** Update the lambda to: `(commands, key, source) => new KeyBinding(commands, key, source, null, null)` to preserve the key.

### [P1] CommandBridge marks event as handled but this doesn't stop remote view's own dispatch
**File:** `/home/user/Terminal.Gui/Terminal.Gui/Input/CommandBridge.cs:145-181`
**Issue:** In `OnRemoteCommandNotBound`, when a command is bridged (line 177), the bridge sets `e.Handled = true` (line 180) to prevent the remote's own dispatch/bubbling. However, the remote view may have already begun its DefaultCommandNotBoundHandler logic before the bridge event fires. If the bridged command is invoked on the owner and then bubbles back to the remote, double-processing can occur. Additionally, if the owner's dispatch consumes the command, the remote won't know and may continue processing.
**Suggested fix:** Add trace/logging to detect when a bridged command creates a feedback loop. Consider tracking bridged command state to prevent re-entry.

### [P1] Key.operator!= null-coalesces incorrectly for nullable Key operands
**File:** `/home/user/Terminal.Gui/Terminal.Gui/Input/Keyboard/Key.cs:720`
**Issue:** The `!=` operator at line 720 has signature `public static bool operator != (Key? a, Key? b)` but one operand could be null while the other isn't. The logic `a is null ? b is { } : !a.Equals(b)` correctly handles null, but this inconsistency with `==` operator (line 714, which takes non-nullable Key) is confusing and error-prone. The two operators have asymmetric signatures.
**Suggested fix:** Make both `==` and `!=` operators accept nullable operands with consistent logic, or document the asymmetry clearly in XML docs.

### [P1] Disconnect between CommandOutcome enum semantics and CommandImplementation return type
**File:** `/home/user/Terminal.Gui/Terminal.Gui/Input/CommandOutcome.cs:39-44`
**Issue:** `CommandOutcome.HandledContinue` maps to `bool? false` in `ToBool()`, suggesting "handled but allow routing to continue." However, in `View.Command.cs` lines 121-128, when `InvokeCommands` receives `false`, it continues to the next command in the sequence, NOT to routing-up the hierarchy. This creates semantic confusion: does `HandledContinue` mean "continue routing up" or "continue with next command"? The current implementation treats it as the latter, but the name implies the former.
**Suggested fix:** Clarify XML docs for `HandledContinue` to explicitly state it prevents hierarchy bubbling but allows sequential command processing. Consider renaming to `HandledContinueSequence` to avoid confusion.

## P2 - Nice To Fix

### [P2] CommandBridge subscribes to wrong events for Activate bridging
**File:** `/home/user/Terminal.Gui/Terminal.Gui/Input/CommandBridge.cs:49-52`
**Issue:** The bridge subscribes to `remote.Activated` (a notification event that cannot be cancelled) but should also subscribe to `remote.Activating` (the cancelable event) if the intent is to bridge Activate commands that might be cancelled. The current code bridges the result of activation, not the activation request itself. This is architecturally correct for notifications but might miss early-exit cases if Activating is cancelled on the remote before Activated fires.
**Suggested fix:** Add comment clarifying why only `Activated` is observed (not `Activating`), since Activated is fired after the remote view completes its state change. Document that bridge reflects post-change state.

### [P2] AddKeyBindingsForHotKey removes and re-adds bindings unconditionally
**File:** `/home/user/Terminal.Gui/Terminal.Gui/ViewBase/View.Keyboard.cs:191-204`
**Issue:** Lines 191-194 call `Remove()` then `Add()` even when the key hasn't changed. This creates unnecessary churn and could be optimized to check equality first. Also, the Remove/Add pattern is not atomic in ConcurrentDictionary, so a concurrent reader might see a brief window where the binding doesn't exist.
**Suggested fix:** Add a guard to skip Remove/Add if the new binding is identical to the old. Consider using `AddOrUpdate` for atomicity.

### [P2] CommandBridge has no timeout or cleanup for zombie handlers
**File:** `/home/user/Terminal.Gui/Terminal.Gui/Input/CommandBridge.cs`
**Issue:** If Dispose() is never called and the owner is collected, the bridge remains subscribed to the remote view's events indefinitely. There's no automatic cleanup, aging out, or weak event mechanism to prevent accumulation of zombie handlers on long-lived views (e.g., Application).
**Suggested fix:** Document mandatory Dispose() pattern in XML docs. Consider implementing IAsyncDisposable or a weak handler registration system.

### [P2] Mouse coordinate frame is inconsistent in MouseBinding
**File:** `/home/user/Terminal.Gui/Terminal.Gui/Input/Mouse/MouseBinding.cs:16-20`
**Issue:** The constructor sets `Timestamp = DateTime.Now` which captures wall-clock time, not a stable monotonic clock. If system time is adjusted backward, timing-sensitive code could misbehave. More importantly, there's no documentation about whether `Mouse.Position` in the binding is in screen coordinates or view-relative coordinates, and MouseBinding doesn't normalize it.
**Suggested fix:** Document the coordinate frame clearly. Use `DateTime.UtcNow` for consistency. Consider adding a comment explaining whether Position is screen-relative or view-relative.

### [P2] CommandContext.WithValue allocates new list on each call for deep hierarchies
**File:** `/home/user/Terminal.Gui/Terminal.Gui/Input/CommandContext.cs:87`
**Issue:** The spread expression `[..Values, value]` creates a new list on every append. For deep view hierarchies (10+ levels), this causes O(N²) allocations during value propagation. While acceptable for typical 3–5 level hierarchies, it's wasteful and could cause GC pressure in complex UIs.
**Suggested fix:** Document the O(N²) cost in the XML comment (already done at line 81-83). Consider offering a mutable builder variant for hot paths, or use an immutable linked list for truly deep hierarchies. Current implementation is acceptable but worth monitoring.

### [P2] Key CWP pattern violation: OnXxx methods not empty in base
**File:** `/home/user/Terminal.Gui/Terminal.Gui/ViewBase/View.Command.cs:305,490,837,995,1020`
**Issue:** The default command handlers (`OnCommandNotBound`, `OnAccepting`, `OnActivating`, etc.) return `false` in base class. Per the CWP pattern in `.claude/rules/cwp-pattern.md`, virtual methods should be empty in the base class to allow subclass overrides. Returning `false` in the base class means subclass overrides must either return `true` (to cancel) or `false` (to allow), with no way to chain to the base behavior.
**Suggested fix:** Change all `OnXxx` virtual methods to return no value and be empty (`protected virtual void OnAccepting(CommandEventArgs args) { }`), moving the return logic into `RaiseXxx` methods. This aligns with CWP pattern and allows better subclass composition.

---

**Summary:** The Input subsystem is well-architected overall, with strong command routing and event propagation logic. However, three P0 issues must be fixed before shipping: the Key equality/hash mismatch (breaks dictionary lookups), the malformed extension method syntax (code won't compile), and the HotKey state update race condition. Several P1 issues around KeyBindings initialization and CommandBridge lifecycle cleanup should be addressed to prevent silent failures and memory leaks.

