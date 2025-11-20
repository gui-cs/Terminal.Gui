# IRunnable Architecture Proposal

**Status**: Proposal  

**Version**: 1.6 (Property-Based Architecture)

**Date**: 2025-01-20

## Summary

This proposal recommends decoupling Terminal.Gui's "Runnable" concept from `Toplevel` and `ViewArrangement.Overlapped`, elevating it to a first-class interface-based abstraction. 

**Key Insight**: Analysis of the codebase reveals that **all runnable sessions are effectively modal** - they block in `Application.Run()` until stopped and capture input. The distinction between "modal" and "non-modal" in the current design is artificial:

- The `Modal` property only affects input propagation and Z-order, not the fundamental run loop behavior
- All `Toplevel`s block in `Run()` - there's no "background" runnable concept
- Non-modal `Toplevel`s (like `WizardAsView`) are just embedded views with `Modal = false`, not true sessions
- Overlapped windows are managed by `ViewArrangement.Overlapped`, not runnability

By introducing `IRunnable`, we create a clean separation where:

- **Runnable** = Can be run as a **UI**-blocking session with `Application.Run()` and returns a result
- **Overlapped** = `ViewArrangement.Overlapped` for window management (orthogonal to runnability)
- **Embedded** = Just views, not runnable at all

## Terminology

This proposal introduces new terminology to clarify the architecture:

| Term | Definition |
|------|------------|
| **`IRunnable`** | Base interface for Views capable of running as an independent session with `Application.Run()` without returning a result. Replaces `Toplevel` as the contract for runnable views. When an `IRunnable` is passed to `IApplication.Run`, `Run` blocks until the `IRunnable` `Stops`. |
| **`IRunnable<TResult>`** | Generic interface derived from `IRunnable` that can return a typed result. |
| **`Runnable`** | Optional base class that implements `IRunnable` and derives from `View`, providing default lifecycle behavior. Views can derive from this or implement `IRunnable` directly. |
| **`TResult`** | Type parameter specifying the type of result data returned when the runnable completes (e.g., `int` for button index, `string` for file path, enum, or other complex type). `Result` is `null` if the runnable stopped without the user explicitly accepting it (ESC pressed, window closed, etc.). |
| **`Result`** | Property on `IRunnable<TResult>` that holds the typed result data. Should be set in `IsRunningChanging` handler (when `newValue = false`) **before** the runnable is popped from `RunnableSessionStack`. This allows subscribers to inspect results and optionally cancel the stop. Available after `IApplication.Run` returns. `null` indicates cancellation/non-acceptance. |
| **RunnableSession** | A running instance of an `IRunnable`. Managed by `IApplication` via `Begin()`, `Run()`, `RequestStop()`, and `End()` methods. Represented by a `RunnableSessionToken` on the `RunnableSessionStack`. |
| **`RunnableSessionToken`** | Object returned by `Begin()` that represents a running session. Wraps an `IRunnable` instance (via a `Runnable` property) and is stored in `RunnableSessionStack`. Disposed when session ends. |
| **`RunnableSessionStack`** | A stack of `RunnableSessionToken` instances, each wrapping an `IRunnable`. Tracks all running runnables in the application. Literally a `ConcurrentStack<IRunnable>`. Replaces `SessionStack` (formerly `Toplevels`). |
| **`IsRunning`** | Boolean property on `IRunnable` indicating whether the runnable is currently on the `RunnableSessionStack` (i.e., `RunnableSessionStack.Any(token => token.Runnable == this)`). Read-only, derived from stack state. Runnables are added during `IApplication.Begin` and removed in `IApplication.End`. Replaces `Toplevel.Running`. |
| **`IsRunningChanging`** | Cancellable event raised **before** an `IRunnable` is added to or removed from `RunnableSessionStack`. When transitioning to `IsRunning = true`, can be canceled to prevent starting. When transitioning to `IsRunning = false`, allows code to prevent closure (e.g., prompt to save changes) AND is the ideal place to extract `Result` before the runnable is removed from the stack. Event args (`CancelEventArgs<bool>`) provide the new state in `NewValue`. Replaces `Toplevel.Closing` and partially `Toplevel.Activate`. |
| **`IsRunningChanged`** | Non-cancellable event raised **after** a runnable has been added to or removed from `RunnableSessionStack`. Fired after `IsRunning` has changed to the new value (true = started, false = stopped). For post-state-change logic (e.g., setting focus after start, cleanup after stop). Replaces `Toplevel.Activated` and `Toplevel.Closed`. |
| **`IsInitialized`**  (`View` property) | Boolean property (on `View`) indicating whether a view has completed two-phase initialization (`View.BeginInit/View.EndInit`). From .NET's `ISupportInitialize` pattern. If the `IRunnable.IsInitialized == false`, `BeginInit` is called from `IApplication.Begin` after `IsRunning` has changed to `true`.  `EndInit` is called immediately after `BeginInit`. |
| **`Initialized`**  (`View` event) | Non-cancellable event raised as `View.EndInit()` completes. |
| **`TopRunnable`** (`IApplication` property) | The `IRunnable` that is on the top of the `RunnableSessionStack` stack. By definition and per-implementation, this `IRunnable` is capturing all mouse and keyboard input and is thus "Modal". Note: any other `IRunnable` instances on `RunnableSessionStack` continue to be laid out, drawn, and receive iteration events; they just don't get any user input. **Renamed from `Current`** to better reflect its purpose as the top runnable in the stack. Synonymous with the runnable having `IsModal = true`. |
| **`IsModal`** | Boolean property on `IRunnable` indicating whether the `IRunnable` is at the top of the `RunnableSessionStack` (i.e., `this == app.TopRunnable` or `app.RunnableSessionStack.Peek().Runnable == this`). The `IRunnable` at the top of the stack gets all mouse/keyboard input and thus is running "modally". Read-only, derived from stack state. `IsModal` represents the concept from the end-user's perspective. |
| **`IsModalChanging`** | Cancellable event raised **before** an `IRunnable` transitions to/from the top of the `RunnableSessionStack`. When becoming modal (`newValue = true`), can be canceled to prevent activation. Event args (`CancelEventArgs<bool>`) provide the new state. Replaces `Toplevel.Activate` and `Toplevel.Deactivate`. |
| **`IsModalChanged`** | Non-cancellable event raised **after** an `IRunnable` has transitioned to/from the top of the `RunnableSessionStack`. Fired after `IsModal` has changed to the new value (true = became modal, false = no longer modal). For post-activation logic (e.g., setting focus, updating UI state). Replaces `Toplevel.Activated` and `Toplevel.Deactivated`. |
| **`End`** (`IApplication` method) | Ends a running `IRunnable` instance by removing its `RunnableSessionToken` from the `RunnableSessionStack`. `IsRunningChanging` with `newValue = false` is raised **before** the token is popped from the stack (allowing result extraction and cancellation). `IsRunningChanged` is raised **after** the `Pop` operation. Then, `RunnableSessionStack.Peek()` is called to see if another `IRunnable` instance can transition to `IApplication.TopRunnable`/`IRunnable.IsModal = true`. |
| **`ViewArrangement.Overlapped`** | Layout mode for windows that can overlap with Z-order management. Orthogonal to runnability - overlapped windows can be embedded views (not runnable) or runnable sessions. |

**Key Architectural Changes:**
- **Simplified**: One interface `IRunnable` replaces both `Toplevel` and the artificial `Modal` property distinction
- **All sessions block**: No concept of "non-modal runnable" - if it's runnable, `Run()` blocks until `RequestStop()`
- **Type-safe results**: Generic `TResult` parameter provides compile-time type safety
- **Decoupled from layout**: Being runnable is independent of `ViewArrangement.Overlapped`
- **Consistent patterns**: All lifecycle events follow Terminal.Gui's Cancellable Work Pattern
- **Result extraction in `Stopping`**: `OnStopping()` is the correct place to extract `Result` before disposal

## Implementation Status

### Completed Work

- [x] **2025-11-20**: Renamed `IApplication.Current` to `IApplication.TopRunnable` to better reflect its role as the top runnable in the session stack
  - Updated interface definition in `IApplication.cs`
  - Updated implementation in `ApplicationImpl.cs`
  - Updated static property in `Application.Current.cs`
  - Updated all references in library code (28 occurrences)
  - Updated all references in examples (50+ occurrences)
  - Updated all references in tests (607 occurrences)
  - Updated `View.IsCurrentTop` to use the renamed property
  - Updated API documentation comments
  - All tests pass
  - No new warnings introduced

### Remaining Work

The following items from the original proposal are still pending:

- [ ] Implement `IRunnable` non-generic base interface
- [ ] Implement `IRunnable<TResult>` generic interface
- [ ] Create optional `Runnable` base class
- [ ] Replace `SessionToken` with `RunnableSessionToken`
- [ ] Replace `SessionStack` (ConcurrentStack<Toplevel>) with `RunnableSessionStack` (ConcurrentStack<IRunnable>)
- [ ] Add lifecycle events: `IsRunningChanging`, `IsRunningChanged`, `IsModalChanging`, `IsModalChanged`
- [ ] Migrate `Toplevel` to implement `IRunnable`
- [ ] Update all view classes to use new pattern
- [ ] Add comprehensive tests for new architecture
- [ ] Update all documentation

## See Also

- [Original Issue #4148](https://github.com/gui-cs/Terminal.Gui/issues/4148)
- [Application Lifecycle Documentation](application.md)
- [View Documentation](View.md)
