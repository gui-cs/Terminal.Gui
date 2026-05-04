### [P0] CWP Violation: SessionBegun event raised BEFORE state changes in Begin()

**File:** /home/user/Terminal.Gui/Terminal.Gui/App/ApplicationImpl.Run.cs:154
**Issue:** The `SessionBegun` event is invoked BEFORE `SetIsRunning(true)` and `SetIsModal(true)` are called on lines 155-156. Per the CWP pattern (`.claude/rules/cwp-pattern.md`), work must happen BEFORE the notification. Event subscribers see inconsistent state where the runnable is not yet IsRunning or IsModal, violating the contract that the state change is complete before notification.
**Suggested fix:** Move the `SessionBegun?.Invoke()` call to AFTER lines 155-156, so the state is fully updated before subscribers are notified. Alternatively, call it after the lock is released but immediately before the event-firing section (line 164+).

### [P0] Race condition in Invoke() thread-safety check

**File:** /home/user/Terminal.Gui/Terminal.Gui/App/ApplicationImpl.Run.cs:59 and 79
**Issue:** The condition `MainThreadId == Thread.CurrentThread.ManagedThreadId` checks `MainThreadId` (which can be null) and `TopRunnableView` without synchronization. Between the check and the immediate execution, another thread could dispose the application, set `MainThreadId = null`, or pop the `TopRunnableView`. This allows execution off the main thread despite the intent of the check, causing race conditions when `action` modifies shared UI state.
**Suggested fix:** Store `MainThreadId` in a local before the check, or require the caller to marshal off-thread calls through the timeout queue (i.e., remove the "already on main thread" fast path entirely and always use `TimedEvents.Add`).

### [P1] SessionToken.Result not set until End() completes, but extracted in IsRunningChanging

**File:** /home/user/Terminal.Gui/Terminal.Gui/App/ApplicationImpl.Run.cs:392 and IRunnable.cs:41-50
**Issue:** The documentation in `IRunnable.cs:41-50` states "Implementations should set this in the `RaiseIsRunningChanging` method" to extract result data, but the field is public on `SessionToken`. If an event subscriber reads `token.Result` during `End()` in the `IsRunningChanging` event (line 342), it sees the old/stale result. `SessionToken.Result` is only set after the event completes (line 392), and the documented pattern requires extracting from `runnable.Result` into `token.Result` via external code during the event, not relying on `End()` to do it. This is confusing and error-prone.
**Suggested fix:** Clarify the contract: either `token.Result` must be set BEFORE raising `IsRunningChanging`, or the documentation must mandate that subscribers extract into `runnable.Result` (not `token.Result`), since `SessionToken.Result` is an implementation detail. Consider making `SessionToken.Result` internal to avoid this confusion.

### [P1] Invoke() does not handle null MainThreadId or disposed state

**File:** /home/user/Terminal.Gui/Terminal.Gui/App/ApplicationImpl.Run.cs:56-93
**Issue:** If `Invoke()` is called after `Dispose()` completes (when `MainThreadId` is null and `TimedEvents` may be stopped), the code attempts to add a timeout to a stopped/null event queue. The timeout may be dropped silently or throw. No error is raised to the caller, leaving them unaware the action was never queued.
**Suggested fix:** Check `if (!Initialized)` at the start of `Invoke()` and throw `NotInitializedException`, or document that `Invoke()` is undefined after `Dispose()`.

### [P1] Missing nullability annotation on IRunnable in SessionStack

**File:** /home/user/Terminal.Gui/Terminal.Gui/App/Runnable/IRunnable.cs and ApplicationImpl.Run.cs:138
**Issue:** `SessionStack` holds `SessionToken` which contains `IRunnable?`. Code accesses `previousToken.Runnable` without null-check (line 138: `previousToken.Runnable is { }`), but `Runnable` is nullable. While the pattern guards with `is { }`, the public API surface doesn't clearly document when `Runnable` is null (only `End()` sets it to null). Callers holding a `SessionToken` from `SessionStack` could be surprised to find `Runnable` is null if the session ended concurrently.
**Suggested fix:** Add XML doc to `SessionToken.Runnable` noting it is set to null by `End()`, and may be null if the session has completed. Or add a property `IsEnded => Runnable is null`.

### [P1] TopRunnable can become stale/inconsistent with SessionStack

**File:** /home/user/Terminal.Gui/Terminal.Gui/App/ApplicationImpl.Run.cs:151, 380, 384
**Issue:** `TopRunnable` is updated within the lock (line 151) and then nulled outside it (line 380), then set again (line 384). Between line 380-384, `TopRunnable` is null even though a runnable is still on the stack. If another thread calls `RequestStop()` (which checks `TopRunnable`), or if an event handler in line 385 queries `TopRunnable`, they see null. After line 384 it's restored, but the window of inconsistency is observable.
**Suggested fix:** Update `TopRunnable` only within the critical section (lock), or document this as a known brief inconsistency and ensure code that queries `TopRunnable` outside the lock is defensive.

### [P1] Possible null dereference in Begin() when previousTop is updated

**File:** /home/user/Terminal.Gui/Terminal.Gui/App/ApplicationImpl.Run.cs:159, 165
**Issue:** Line 159 calls `previousTop?.SetIsModal(false)` only if `previousTop != null`, but line 165 unconditionally calls `previousTop?.RaiseIsModalChangedEvent(false)`. If `previousTop` transitions from non-null in the lock to null due to concurrent `End()`, line 165 does not execute the event. However, the real issue is that `SetIsModal(false)` (line 159, inside lock) is called but `RaiseIsModalChangedEvent(false)` (line 165, outside lock) is not, breaking the CWP contract that both the state change and the event always occur together.
**Suggested fix:** Always fire the event if SetIsModal was called: move the event firing inside a conditional that matches the SetIsModal condition, or ensure SetIsModal and RaiseIsModalChangedEvent are paired unconditionally.

### [P1] ResetState() calls End() on all sessions, but can deadlock on IsRunningChanging events

**File:** /home/user/Terminal.Gui/Terminal.Gui/App/ApplicationImpl.Lifecycle.cs:231-237
**Issue:** `ResetState()` iterates `SessionStack.Reverse()` and calls `End()` on each token. If an `IsRunningChanging` event handler (raised inside `End()`) throws an exception or tries to interact with the application, the loop continues without proper cleanup. If a handler calls `Begin()` on another runnable, the stack is mutated during iteration, causing undefined behavior. Additionally, if `End()` is cancelled (returns early on line 345 of Run.cs), the loop doesn't detect it and `Initialized` is set to false while sessions are still active.
**Suggested fix:** Before calling `End()`, copy the stack, then clear it. In the loop, only call `End()` if the runnable is still on the stack (handle cancellation), or wrap in try-catch to ensure cleanup proceeds even if handlers fail.

### [P1] Disposed flag asymmetry between legacy and instance-based models

**File:** /home/user/Terminal.Gui/Terminal.Gui/App/ApplicationImpl.Lifecycle.cs:140-149
**Issue:** In `Dispose()`, the singleton instance (legacy model) resets `_disposed = false` to allow re-init, but instance-based apps set `_disposed = true` and are never re-initializable. This means a legacy app can call `Init() -> Dispose() -> Init()`, but if someone calls `Application.Instance.Init()` after manual disposal, behavior is inconsistent. Additionally, if `_disposed` is true and `Dispose()` is called again, it returns early (line 125), leaving cleanup incomplete.
**Suggested fix:** Consider removing the `_disposed` flag reset for the legacy model, or document clearly that re-initialization is a legacy-only pattern and instance-based apps must create new instances.

### [P1] Missing thread safety in Invoke() check for MainThreadId

**File:** /home/user/Terminal.Gui/Terminal.Gui/App/ApplicationImpl.Run.cs:59, 79
**Issue:** The check `MainThreadId == Thread.CurrentThread.ManagedThreadId` is not synchronized. `MainThreadId` is set in `Init()` and cleared in `ResetState()`, but `Invoke()` can be called concurrently. If `Dispose()` clears `MainThreadId` to null between the property read and the comparison, the condition may evaluate unexpectedly. Also, `TopRunnableView` is not cached, so it could change between the check and the invocation.
**Suggested fix:** Cache both `MainThreadId` and `TopRunnableView` in locals at the start of the method, or use a single atomic volatile field for the "is initialized and running" state.

### [P2] CWP pattern confusion in Begin(): SessionBegun raised inside lock but event contract is unclear

**File:** /home/user/Terminal.Gui/Terminal.Gui/App/ApplicationImpl.Run.cs:154
**Issue:** The event is raised inside the `_sessionStackLock` (line 154), but the comment says "Update cached state atomically" and the state variables (`SetIsRunning`, `SetIsModal`) are set on lines 155-156, also inside the lock. The naming and placement suggest the event should fire after state update, not before. The inconsistency with the CWP pattern (work before notification) is the root cause.
**Suggested fix:** Restructure to: (1) SetIsRunning/SetIsModal inside lock, (2) release lock, (3) raise event outside lock. Or if event must fire inside lock, rename the section to clarify.

### [P2] Keyboard/Mouse lazy initialization can cause re-subscription on re-init

**File:** /home/user/Terminal.Gui/Terminal.Gui/App/ApplicationImpl.cs:205-230
**Issue:** `Keyboard` and `Mouse` properties are lazy-initialized via `??=` and subscribed to `Application` static events in their constructors. If `Dispose()` clears `_keyboard = null` and `_mouse = null` (line 292, 298), and then `Init()` is called again on the same instance, the properties are re-created and re-subscribe to the static events. If `UnsubscribeApplicationEvents()` failed or was incomplete, duplicate subscribers may accumulate.
**Suggested fix:** Ensure `UnsubscribeApplicationEvents()` is called in `ResetState()` BEFORE clearing `_keyboard` and `_mouse`, or add a `_isSubscribed` flag to prevent double-subscription.

### [P2] SessionToken.Runnable is set to null after End() completes, but subscribers may hold a reference

**File:** /home/user/Terminal.Gui/Terminal.Gui/App/ApplicationImpl.Run.cs:399
**Issue:** Line 399 sets `token.Runnable = null` after `SessionEnded` is invoked. Subscribers to `SessionEnded` (line 400) receive a `SessionToken` with a non-null `Runnable`. But the field is public and mutable, so subscribers could inadvertently modify or null it. After `SessionEnded` returns, the `Runnable` is guaranteed null, but subscribers holding a reference to `token` may see this change unexpectedly.
**Suggested fix:** Set `token.Runnable = null` BEFORE invoking `SessionEnded`, not after, so subscribers see consistent null state. Or make `SessionToken.Runnable` internal (not public).

### [P2] Unused parameter in Begin() Trace call

**File:** /home/user/Terminal.Gui/Terminal.Gui/App/ApplicationImpl.Run.cs:112
**Issue:** `Trace.Lifecycle()` call on line 112 passes a string literal `"(token.Runnable as Runnable)?.ToIdentifyingString ()"` instead of evaluating the expression. The string is always the same, not the actual runnable identifier. This makes tracing output useless.
**Suggested fix:** Change to `Trace.Lifecycle(MainThreadId.ToString(), "Begin", $(token.Runnable as Runnable)?.ToIdentifyingString () ?? "unknown");`.

