# IRunnable Architecture Proposal

**Status**: Phase 1 Complete ✅ - Phase 2 In Progress

**Version**: 1.8 - Phase 1 Implemented

**Date**: 2025-01-21

**Phase 1 Completion**: Issue #4400 closed with full implementation including fluent API and automatic disposal

## Summary

This proposal recommends decoupling Terminal.Gui's "Runnable" concept from `Toplevel` and `ViewArrangement.Overlapped`, elevating it to a first-class interface-based abstraction. 

**Key Insight**: Analysis of the codebase reveals that **all runnable sessions are effectively modal** - they block in `Application.Run()` until stopped and capture input. The distinction between "modal" and "non-modal" in the current design is artificial:

- The `Modal` property only affects input propagation and Z-order, not the fundamental run loop behavior
- All `Toplevel`s block in `Run()` - there's no "background" runnable concept
- Non-modal `Toplevel`s (like `WizardAsView`) are just embedded views with `Modal = false`, not true sessions
- Overlapped windows are managed by `ViewArrangement.Overlapped`, not runnability

By introducing `IRunnable<TResult>`, we create a clean separation where:

- **Runnable** = Can be run as a **UI**-blocking session with `Application.Run()` and returns a result
- **Overlapped** = `ViewArrangement.Overlapped` for window management (orthogonal to runnability)
- **Embedded** = Just views, not runnable at all

## Terminology

This proposal introduces new terminology to clarify the architecture:

| Term | Definition |
|------|------------|
| **`IRunnable`** | Base interface for Views capable of running as an independent session with `Application.Run()` without returning a result. Replaces `Toplevel` as the contract for runnable views. When an `IRunnable` is passed to `IApplication.Run`, `Run` blocks until the `IRunnable` `Stops`. |
| **`IRunnable<TResult>`** | Generic interface derived from `IRunnable` that can return a typed result. |
| **`Runnable<TResult>`** | Optional base class that implements `IRunnable<TResult>` and derives from `View`, providing default lifecycle behavior. Views can derive from this or implement `IRunnable<TResult>` directly. |
| **`TResult`** | Type parameter specifying the type of result data returned when the runnable completes (e.g., `int` for button index, `string` for file path, enum, or other complex type). `Result` is `null` if the runnable stopped without the user explicitly accepting it (ESC pressed, window closed, etc.). |
| **`Result`** | Property on `IRunnable<TResult>` that holds the typed result data. Should be set in `IsRunningChanging` handler (when `newValue = false`) **before** the runnable is popped from `RunnableSessionStack`. This allows subscribers to inspect results and optionally cancel the stop. Available after `IApplication.Run` returns. `null` indicates cancellation/non-acceptance. |
| **RunnableSession** | A running instance of an `IRunnable`. Managed by `IApplication` via `Begin()`, `Run()`, `RequestStop()`, and `End()` methods. Represented by a `RunnableSessionToken` on the `RunnableSessionStack`. |
| **`RunnableSessionToken`** | Object returned by `Begin()` that represents a running session. Wraps an `IRunnable` instance (via a `Runnable` property) and is stored in `RunnableSessionStack`. Disposed when session ends. |
| **`RunnableSessionStack`** | A stack of `RunnableSessionToken` instances, each wrapping an `IRunnable`. Tracks all running runnables in the application. Literally a `ConcurrentStack<RunnableSessionToken>`. Replaces `SessionStack` (formerly `Toplevels`). 
| **`IsRunning`** | Boolean property on `IRunnable` indicating whether the runnable is currently on the `RunnableSessionStack` (i.e., `RunnableSessionStack.Any(token => token.Runnable == this)`). Read-only, derived from stack state. Runnables are added during `IApplication.Begin` and removed in `IApplication.End`. Replaces `Toplevel.Running`. |
| **`IsRunningChanging`** | Cancellable event raised **before** an `IRunnable` is added to or removed from `RunnableSessionStack`. When transitioning to `IsRunning = true`, can be canceled to prevent starting. When transitioning to `IsRunning = false`, allows code to prevent closure (e.g., prompt to save changes) AND is the ideal place to extract `Result` before the runnable is removed from the stack. Event args (`CancelEventArgs<bool>`) provide the new state in `NewValue`. Replaces `Toplevel.Closing` and partially `Toplevel.Activate`. |
| **`IsRunningChanged`** | Non-cancellable event raised **after** a runnable has been added to or removed from `RunnableSessionStack`. Fired after `IsRunning` has changed to the new value (true = started, false = stopped). For post-state-change logic (e.g., setting focus after start, cleanup after stop). Replaces `Toplevel.Activated` and `Toplevel.Closed`. |
| **`IsInitialized`**  (`View` property) | Boolean property (on `View`) indicating whether a view has completed two-phase initialization (`View.BeginInit/View.EndInit`). From .NET's `ISupportInitialize` pattern. If the `IRunnable.IsInitialized == false`, `BeginInit` is called from `IApplication.Begin` after `IsRunning` has changed to `true`.  `EndInit` is called immediately after `BeginInit`. |
| **`Initialized`**  (`View` event) | Non-cancellable event raised as `View.EndInit()` completes. |
| **`TopRunnable`** (`IApplication` property) | Used to be `Top`, but was recently renamed to `Current` because it was confusing relative to `Toplevel`. It's precise definition in this proposal is "The `IRunnable` that is on the top of the `RunnableSessionStack` stack. And by definition, and per-implementation, this `IRunnable` is capturing all mouse and keyboard input and is thus "Modal". Note, any other `IRunnable` instances on `RunnableSessionStack` continue to be laid out, drawn, and receive iteration events; they just don't get any user input. Another interesting note: No code in the solution other than ./App, ./ViewBase, and tests reference `IApplication.Current` (an indication the previous de-coupling was successful). It also means the name of this property is not that important because it's just an implementation detail, primarily used to enable tests to not have to actually call `Run`. View has `public bool IsCurrentTop => App?.Current == this;`; thus we rename `IApplication.Current` to `IApplication.TopRunnable` and it's synonymous with `IRunnable.IsModal`. |
| **`IsModal`** | Boolean property on `IRunnable` indicating whether the `IRunnable` is at the top of the `RunnableSessionStack` (i.e., `this == app.TopRunnable` or `app.RunnableSessionStack.Peek().Runnable == this`). The `IRunnable` at the top of the stack gets all mouse/keyboard input and thus is running "modally". Read-only, derived from stack state. `IsModal` represents the concept from the end-user's perspective. |
| **`IsModalChanging`** | Cancellable event raised **before** an `IRunnable` transitions to/from the top of the `RunnableSessionStack`. When becoming modal (`newValue = true`), can be canceled to prevent activation. Event args (`CancelEventArgs<bool>`) provide the new state. Replaces `Toplevel.Activate` and `Toplevel.Deactivate`. |
| **`IsModalChanged`** | Non-cancellable event raised **after** an `IRunnable` has transitioned to/from the top of the `RunnableSessionStack`. Fired after `IsModal` has changed to the new value (true = became modal, false = no longer modal). For post-activation logic (e.g., setting focus, updating UI state). Replaces `Toplevel.Activated` and `Toplevel.Deactivated`. |
| **`End`** (`IApplication` method) | Ends a running `IRunnable` instance by removing its `RunnableSessionToken` from the `RunnableSessionStack`. `IsRunningChanging` with `newValue = false` is raised **before** the token is popped from the stack (allowing result extraction and cancellation). `IsRunningChanged` is raised **after** the `Pop` operation. Then, `RunnableSessionStack.Peek()` is called to see if another `IRunnable` instance can transition to `IApplication.TopRunnable`/`IRunnable.IsModal = true`. |
| **`ViewArrangement.Overlapped`** | Layout mode for windows that can overlap with Z-order management. Orthogonal to runnability - overlapped windows can be embedded views (not runnable) or runnable sessions. |

**Key Architectural Changes:**
- **Simplified**: One interface `IRunnable<TResult>` replaces both `Toplevel` and the artificial `Modal` property distinction
- **All sessions block**: No concept of "non-modal runnable" - if it's runnable, `Run()` blocks until `RequestStop()`
- **Type-safe results**: Generic `TResult` parameter provides compile-time type safety
- **Decoupled from layout**: Being runnable is independent of `ViewArrangement.Overlapped`
- **Consistent patterns**: All lifecycle events follow Terminal.Gui's Cancellable Work Pattern
- **Result extraction in `Stopping`**: `OnStopping()` is the correct place to extract `Result` before disposal

## Table of Contents

- [Background](#background)
- [Problems with Current Design](#problems-with-current-design)
- [Proposed Architecture](#proposed-architecture)
- [Detailed API Design](#detailed-api-design)
- [Migration Path](#migration-path)
- [Implementation Strategy](#implementation-strategy)
- [Benefits](#benefits)
- [Open Questions](#open-questions)

## Background

### Current State

In Terminal.Gui v2, the concept of a "runnable" view is embedded in the `Toplevel` class:

```csharp
public partial class Toplevel : View
{
    public bool Running { get; set; }
    public bool Modal { get; set; }
    public bool IsLoaded { get; private set; }
    
    // Lifecycle events
    public event EventHandler<ToplevelEventArgs>? Activate;
    public event EventHandler<ToplevelEventArgs>? Deactivate;
    public event EventHandler? Loaded;
    public event EventHandler? Ready;
    public event EventHandler<ToplevelClosingEventArgs>? Closing;
    public event EventHandler<ToplevelEventArgs>? Closed;
    public event EventHandler? Unloaded;
}
```

To run a view, it must derive from `Toplevel`:

```csharp
// Current pattern
var dialog = new Dialog(); // Dialog -> Window -> Toplevel
Application.Run(dialog);
```

`Toplevel` serves multiple purposes:

1. **Session Management**: Manages the running session lifecycle
2. **Full-Screen Container**: By default sizes to fill the screen
3. **Overlapped Support**: Sets `Arrangement = ViewArrangement.Overlapped`
4. **Modal Support**: Has a `Modal` property

This creates unnecessary coupling:

- Only `Toplevel` derivatives can be run
- `Toplevel` always implies overlapped arrangement
- Modal behavior is a property, not a characteristic of the session
- The `SessionStack` contains `Toplevel` objects, coupling session management to the view hierarchy


## Problems with Current Design

### 1. Tight Coupling

**Problem**: Runnable behavior is hardcoded into `Toplevel`, creating artificial constraints.

**Consequence**: 
- Cannot run arbitrary `View` subclasses (e.g., a `FrameView` or custom `View`)
- Forces inheritance hierarchy: must derive from `Toplevel` even when full-screen/overlapped behavior isn't needed
- Code that manages sessions is scattered between `Application`, `ApplicationImpl`, `Toplevel`, and session management code

**Example Limitation**:
```csharp
// Want to run a specialized view as a session
var customView = new MyCustomView();
// Cannot do: Application.Run(customView); 
// Must do: wrap in Toplevel or derive from Toplevel
```

### 2. Overlapped Coupling

**Problem**: `Toplevel` constructor sets `Arrangement = ViewArrangement.Overlapped`, conflating "runnable" with "overlapped".

**Consequence**:
- Cannot have a runnable tiled view without explicitly unsetting `Overlapped`
- Unclear separation between layout mode (overlapped vs. tiled) and execution mode (runnable)
- Architecture implies overlapped views must be runnable, which isn't necessarily true

```csharp
// Toplevel constructor
public Toplevel ()
{
    Arrangement = ViewArrangement.Overlapped; // Why is this hardcoded?
    Width = Dim.Fill ();
    Height = Dim.Fill ();
}
```

### 3. Modal as Property - Actually Not a Distinction

**Problem**: `Modal` is a boolean property on `Toplevel` that creates an **artificial distinction**.

**Reality Check**: All `Toplevel`s are effectively "modal" in that they:
1. Block in `Application.Run()` until `RequestStop()` is called
2. Have exclusive access to the run loop while running
3. Must complete before control returns to the caller

**What `Modal = false` Actually Does:**
- Allows keyboard events to propagate to the SuperView
- Doesn't enforce Z-order "topmost" behavior
- That's it - it's just input routing, not a fundamental session characteristic

**Evidence from codebase:**

```csharp
// WizardAsView.cs - "Non-modal" is actually just an embedded View
var wizard = new Wizard { /* ... */ };
wizard.Modal = false;  // Just affects input propagation and border

// NOTE: The wizard is NOT run separately!
topLevel.Add (wizard);        // Added as a subview (embedded)
Application.Run (topLevel);   // Only the topLevel is run

// The distinction is artificial:
// - "Modal" Wizard = Application.Run(wizard) - BLOCKS until stopped
// - "Non-Modal" Wizard = topLevel.Add(wizard) - NOT runnable, just a View
// Both named "Wizard" but completely different usage patterns!
```

The confusion arises because **`Modal` is a property that affects behavior whether the Toplevel is runnable OR embedded**:
- If run with `Application.Run()`: controls input capture and Z-order
- If embedded with `superView.Add()`: still affects input propagation, but it's not a session

**The real distinction**:
- **Runnable** (call `Application.Run(x)`) - Always blocks, has session lifecycle
- **Embedded** (call `superView.Add(x)`) - Just a view in the hierarchy, no session

**Consequence**:
- Confusing semantics: "non-modal runnable" is an oxymoron
- Modal behavior is scattered across the codebase in conditional checks
- Session management has complex logic for Modal state transitions

```csharp
// ApplicationImpl.Run.cs:98-101 - Complex conditional
if ((Current?.Modal == false && toplevel.Modal)
    || (Current?.Modal == false && !toplevel.Modal)
    || (Current?.Modal == true && toplevel.Modal))
{
    // All this complexity for input routing!
}
```

**Better Model**: Remove the `Modal` property. If you want embedded Wizard-like behavior, just add it as a View (don't make it runnable).

### 4. Session Management Complexity

**Problem**: The `RunnableSessionStack` manages `Toplevel` instances, coupling session lifecycle to view hierarchy.

**Consequence**:
- `SessionToken` stores a `Toplevel`, not a more abstract "runnable session"
- Complex logic for managing the relationship between `RunnableSessionStack`, `Current`, and `CachedSessionTokenToplevel`
- Unclear ownership: who owns the `Toplevel` lifecycle?

```csharp
public class SessionToken : IDisposable
{
    public Toplevel? Toplevel { get; internal set; } // Tight coupling
}
```

### 5. Lifecycle Events Are Misnamed and Hacky

**Problem**: Events like `Activate`, `Deactivate`, `Loaded`, `Ready`, `Closing`, `Closed`, `Unloaded` are on `Toplevel` but conceptually belong to the runnable session, not the view.

**Consequence**:
- Events fire on the view object, mixing view lifecycle with session lifecycle
- Cannot easily monitor session changes independently of view state
- Event args reference `Toplevel` specifically
- **Hacky `ToplevelTransitionManager`**: The `Ready` event requires a separate manager class to track which toplevels have been "readied" across session transitions

**Why is this hacky?** The `Ready` event is fired during the first `RunIteration()` (in the main loop), not during `Begin()` like other lifecycle events. This requires tracking state externally and checking every iteration. With proper CWP-aligned lifecycle, this complexity disappears - `Started` fires after `Begin()` completes, no tracking needed.

### 6. Unclear Responsibilities

**Problem**: It's unclear what `Toplevel` is responsible for.

Is `Toplevel`:
- A full-screen container view?
- The base class for runnable views?
- The representation of a running session?
- A view with special overlapped arrangement?

**Consequence**: Confused codebase where responsibilities blur.

### 7. Violates Cancellable Work Pattern

**Problem**: `Toplevel`'s lifecycle methods don't follow Terminal.Gui's **Cancellable Work Pattern** (CWP), which is used throughout the framework for `View.Draw`, `View.Keyboard`, `View.Command`, and property changes.

**Consequence**:

Current `Toplevel.OnClosing` implementation:
```csharp
internal virtual bool OnClosing(ToplevelClosingEventArgs ev)
{
    Closing?.Invoke(this, ev);  // ? Event fired INSIDE virtual method
    return ev.Cancel;
}
```

**What's wrong:**
1. **Wrong Order**: Event is raised inside the virtual method, not after
2. **No Pre-Check**: Virtual method doesn't return bool to cancel before event
3. **Inconsistent Naming**: Should be `OnStopping`/`Stopping` (cancellable) and `OnStopped`/`Stopped` (non-cancellable)
4. **Manual Checking**: `Application.RequestStop` manually checks `ev.Cancel` instead of relying on method return value

**Impact**: 
- Developers familiar with CWP from other Terminal.Gui components are confused by inconsistent patterns
- Cannot properly override lifecycle methods following the standard pattern
- Event subscription doesn't work as expected compared to other Terminal.Gui events
- Testing is harder because flow is non-standard

### 8. View Initialization Doesn't Follow CWP

**Problem**: `View.BeginInit/EndInit/Initialized` doesn't follow the Cancellable Work Pattern, creating inconsistency with the rest of Terminal.Gui.

**What's Wrong:**
1. **No Pre-Notification Virtual**: No `OnInitializing()` virtual method before initialization
2. **No Cancellation**: Cannot cancel initialization
3. **Event After Work**: `Initialized` event fires after all work is done, no chance to participate
4. **Inconsistent with CWP**: Doesn't match the pattern used elsewhere in Terminal.Gui

**Impact**: 
- Inconsistent with rest of Terminal.Gui's event model
- Cannot hook into initialization at the right point in the lifecycle
- Subclasses cannot easily customize initialization behavior
- Makes the IRunnable lifecycle confusing since `Initialized` event doesn't follow CWP

**Proposed Fix**: Add `Initializing` (cancellable) event and `OnInitializing`/`OnInitialized` virtual methods to match CWP pattern used throughout Terminal.Gui.

## Proposed Architecture

### Core Concept: Simplify and Clarify

**Key Insight**: After analyzing the codebase, there's no valid use case for "non-modal runnables". Every `Toplevel` that calls `Application.Run()` blocks until `RequestStop()`. The `Modal` property only controls input routing, not the fundamental session behavior.

**Simplified Model:**

1. **`IRunnable<TResult>`** - Interface for views that can run as **blocking** sessions with typed results
2. **`ViewArrangement.Overlapped`** - Layout mode for window management (orthogonal to runnability)
3. **Embedded Views** - Views that aren't runnable at all (e.g., `Wizard` with `Modal = false` is just a view)

### Architecture Tenets

1. **Interface-Based**: Use `IRunnable<TResult>` interface to define runnable behavior, not inheritance
2. **Composition Over Inheritance**: Views can implement `IRunnable<TResult>` without inheriting from `Toplevel`
3. **All Sessions Block**: `Application.Run()` blocks until `RequestStop()` is called (no background/non-blocking sessions)
4. **Type-Safe Results**: Generic `TResult` parameter provides compile-time type safety for return values
5. **Clean Separation**: View hierarchy (SuperView/SubViews) is independent of session hierarchy (RunnableSessionStack)
6. **Cancellable Work Pattern**: All lifecycle phases follow Terminal.Gui's Cancellable Work Pattern for consistency
7. **Result Extraction in `Stopping`**: `OnStopping()` is called before disposal, perfect for extracting `Result`

### Result Extraction Pattern

The `Result` property is a nullable generic (`public TResult? Result`) to represent the outcome of the runnable operation, allowing for rich result data and context. 

**Critical Timing**: `Result` must be extracted in `RaiseIsRunningChanging()` when the new value is `false`, which is called by `RequestStop()` before the run loop exits and before disposal. This ensures the data is captured while views are still accessible.

```csharp
protected override bool RaiseIsRunningChanging ()
{
    // Extract Result BEFORE disposal
    // At this point views are still alive and accessible
    Result = ExtractResultFromViews();
    
    return base.OnStopping();  // Allow cancellation
}
```

---

## Detailed API Design

### 1. `IRunnable` Non-Generic Base Interface

The non-generic base interface provides common members for all runnables, enabling type-safe heterogeneous collections:

```csharp
namespace Terminal.Gui.App;

/// <summary>
/// Non-generic base interface for runnable views. Provides common members without type parameter.
/// </summary>
/// <remarks>
/// <para>
/// This interface enables storing heterogeneous runnables in collections (e.g., <see cref="RunnableSessionStack"/>)
/// while preserving type safety at usage sites via <see cref="IRunnable{TResult}"/>.
/// </para>
/// <para>
/// Most code should use <see cref="IRunnable{TResult}"/> directly. This base interface is primarily
/// for framework infrastructure (session management, stacking, etc.).
/// </para>
/// </remarks>
public interface IRunnable
{
    #region Running or not (added to/removed from RunnableSessionStack)

    // TODO: Determine if this should support set for testing purposes.
    // TODO: If IApplication.RunnableSessionStack should be public/internal or wrapped.
    /// <summary>
    /// Gets whether this runnable session is currently running.
    /// </summary>
    bool IsRunning { get => App?.RunnableSessionStack.Contains(this); }

    /// <summary>Called when IsRunning is changing; raises IsRunningChanging.</summary>
    /// <returns>True if the change was canceled; otherwise false.</returns>
    bool RaiseIsRunningChanging(bool oldIsRunning, bool newIsRunning);

    /// <summary>
    /// Raised when IsRunning is changing (e.g when <see cref="IApplication.Begin"> or <see cref="IApplication.End"/> is called).
    /// Can be canceled by setting <see cref="CancelEventArgs.Cancel"/> to true.
    /// </summary>
    event EventHandler<CancelEventArgs<bool>>? IsRunningChanging;
    
    /// <summary>Called after IsRunning has changed.</summary>
    /// <param name="newIsRunning">The new value of IsRunning (true = started, false = stopped).</param>
    void RaiseIsRunningChangedEvent(bool newIsRunning);

    /// <summary>
    /// Raised after the session has started or stopped (IsRunning has changed).
    /// </summary>
    /// <remarks>
    /// Subscribe to perform post-state-change logic. When newValue is false (stopped), 
    /// this is the ideal place to extract <see cref="Result"/> before views are disposed.
    /// </remarks>
    event EventHandler<EventArgs<bool>>? IsRunningChanged;


    #endregion Running or not (added to/removed from RunnableSessionStack)

    #region Modal or not (top of RunnableSessionStack or not)

    // TODO: Determine if this should support set for testing purposes.
    /// <summary>
    /// Gets whether this runnable session is a the top of the Runnable Stack and thus
    /// exclusively receiving mouse and keyboard input.
    /// </summary>
    bool IsModal { get => App?.TopRunnable == this; }

    /// <summary>Called when IsModal is changing; raises IsModalChanging.
    /// <returns>True if the change was canceled; otherwise false.</returns>
    bool RaiseIsModalChanging(bool oldIsModal, bool newIsModal);

    /// <summary>
    /// Called when the user does something to cause this runnable to be put at the top
    /// of the Runnable Stack or not. This is typically because `Run` was called or `RequestStop` 
    /// was called.
    /// Can be canceled by setting <see cref="CancelEventArgs.Cancel"/> to true.
    /// </summary>
    event EventHandler<CancelEventArgs<bool>>? IsModalChanging;

    /// <summary>Called after IsModal has changed.</summary>
    /// <param name="newIsModal">The new value of IsModal (true = became modal/top, false = no longer modal).</param>
    void RaiseIsModalChangedEvent(bool newIsModal);

    /// <summary>
    /// Raised after the session has become modal (top of stack) or ceased being modal.
    /// </summary>
    /// <remarks>
    /// Subscribe to perform post-activation logic (e.g., setting focus, updating UI state).
    /// </remarks>
    event EventHandler<EventArgs<bool>>? IsModalChanged;

    #endregion Modal or not (top of RunnableSessionStack or not)

}
```

### 2. `IRunnable<TResult>` Generic Interface

The generic interface extends the base with typed result support:

```csharp
namespace Terminal.Gui.App;

/// <summary>
/// Defines a view that can be run as an independent blocking session with <see cref="IApplication.Run"/>,
/// returning a typed result.
/// </summary>
/// <typeparam name="TResult">
/// The type of result data returned when the session completes.
/// Common types: <see cref="int"/> for button indices, <see cref="string"/> for file paths,
/// custom types for complex form data.
/// </typeparam>
/// <remarks>
/// <para>
/// A runnable view executes as a self-contained blocking session with its own lifecycle,
/// event loop iteration, and focus management. <see cref="IApplication.Run"/> blocks until
/// <see cref="IApplication.RequestStop"/> is called.
/// </para>
/// <para>
/// When <see cref="Result"/> is <c>null</c>, the session was stopped without being accepted
/// (e.g., ESC key pressed, window closed). When non-<c>null</c>, it contains the result data
/// extracted in <see cref="OnStopping"/> before views are disposed.
/// </para>
/// <para>
/// Implementing <see cref="IRunnable{TResult}"/> does not require deriving from any specific
/// base class or using <see cref="ViewArrangement.Overlapped"/>. These are orthogonal concerns.
/// </para>
/// <para>
/// This interface follows the Terminal.Gui Cancellable Work Pattern for all lifecycle events.
/// </para>
/// </remarks>
public interface IRunnable<TResult> : IRunnable
{
    /// <summary>
    /// Gets the result data extracted when the session was accepted, or null if not accepted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implementations should set this in <see cref="OnStopping"/> by extracting data from
    /// views before they are disposed.
    /// </para>
    /// <para>
    /// <c>null</c> indicates the session was stopped without accepting (ESC key, close without action).
    /// Non-<c>null</c> contains the type-safe result data.
    /// </para>
    /// </remarks>
    TResult? Result { get; set; }
}
```

**Design Rationale:**
- **Non-generic base**: Enables `RunnableSessionStack` to store `ConcurrentStack<IRunnable>` without type erasure
- **Generic extension**: Preserves type safety at usage sites: `var dialog = new Dialog(); int? result = dialog.Result;`
- **Common lifecycle**: Both interfaces share the same lifecycle events via the base

**Note**: The `Initialized` event is already defined on `View` via `ISupportInitializeNotification` and does not need to be redefined here.

### Why This Model Works

1. **Natural nesting**: Each `Run()` call creates a nested blocking context
2. **Automatic cleanup**: When a session ends, previous session automatically becomes modal again
3. **Z-order enforcement**: Topmost session (IsModal=true) is always visually on top
4. **Input capture**: Only `TopRunnable` (IsModal=true) receives keyboard/mouse input
5. **All sessions active**: All sessions on stack (IsRunning=true) continue to be laid out and drawn
6. **No race conditions**: Serial call stack eliminates concurrency issues

### Code Example

```csharp
public class MainWindow : Runnable<object>
{
    private void OpenFile()
    {
        var fileDialog = new FileDialog();
        
        // This blocks until fileDialog closes
        Application.Run(fileDialog);
        
        // FileDialog has stopped, we're back here
        if (fileDialog.Result is string path)
        {
            LoadFile(path);
        }

        fileDialog.Dispose();
        
        // MainWindow's Run() loop continues
    }
}

public class FileDialog : Runnable<string>
{
    protected override bool OnIsRunningChanging(bool oldIsRunning, bool newIsRunning)
    {
        if (!newIsRunning)  // Stopping
        {
            if (SelectedPath == null)
            {
                // Confirm cancellation with nested modal
                int result = MessageBox.Query(
                    "Confirm", 
                    "Cancel file selection?", 
                    "Yes", "No");
                
                if (result == 1)  // No
                {
                    return true;  // Cancel stopping
                }
            }
            
            Result = SelectedPath;
        }
        
        return base.OnIsRunningChanging(oldIsRunning, newIsRunning);
    }
}
```

### RunnableSessionStack Implementation

```csharp
public interface IApplication
{
    /// <summary>
    /// Gets the stack of active runnable session tokens.
    /// Sessions execute serially - the top of stack is the currently modal session.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Session tokens are pushed onto the stack when <see cref="Run"/> is called and popped when
    /// <see cref="RequestStop"/> completes. The stack grows during nested modal calls and
    /// shrinks as they complete.
    /// </para>
    /// <para>
    /// Only the top session (<see cref="TopRunnable"/>) has exclusive keyboard/mouse input (IsModal=true). 
    /// All other sessions on the stack continue to be laid out, drawn, and receive iteration events (IsRunning=true),
    /// but they don't receive user input.
    /// </para>
    /// <example>
    /// Stack during nested modals:
    /// <code>
    /// RunnableSessionStack (top to bottom):
    /// - MessageBox (TopRunnable, IsModal=true, IsRunning=true, has input)
    /// - FileDialog (IsModal=false, IsRunning=true, continues to update/draw)
    /// - MainWindow (IsModal=false, IsRunning=true, continues to update/draw)
    /// </code>
    /// </example>
    /// </remarks>
    ConcurrentStack<RunnableSessionToken> RunnableSessionStack { get; }
    
    /// <summary>
    /// Gets or sets the topmost runnable session (the one capturing input).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Always equals <c>RunnableSessionStack.Peek().Runnable</c> when stack is non-empty.
    /// </para>
    /// <para>
    /// This is the runnable with <see cref="IRunnable.IsModal"/> = true.
    /// </para>
    /// </remarks>
    IRunnable? TopRunnable { get; set; }
}
```

### Why Not Parallel Sessions?

**Question**: Why not allow multiple non-modal sessions running in parallel (like tiled window managers)?

**Answer**: This adds enormous complexity with little benefit:

1. **Input routing**: Which session gets keyboard/mouse events?
2. **Focus management**: How does focus move between parallel sessions?
3. **Z-order**: How are overlapping sessions drawn?
4. **Coordination**: How do sessions communicate?
5. **Thread safety**: Concurrent access to Application state

**Alternative**: Use embedded views with `ViewArrangement.Overlapped`:

```csharp
// Instead of parallel runnables, use embedded overlapped windows
var mainView = new Runnable<object>();

var window1 = new Window 
{ 
    X = 0, 
    Y = 0, 
    Width = 40, 
    Height = 20,
    Arrangement = ViewArrangement.Overlapped 
};

var window2 = new Window 
{ 
    X = 10, 
    Y = 5, 
    Width = 40, 
    Height = 20,
    Arrangement = ViewArrangement.Overlapped 
};

mainView.Add(window1);
mainView.Add(window2);

// Only mainView is runnable, windows are embedded
Application.Run(mainView);
mainView.Dispose();
```

**Benefits of serial-only model:**
- **Simplicity**: Clear execution flow
- **Predictability**: One active session at a time
- **Composability**: Overlapped windows via `ViewArrangement`, runnability via `IRunnable`
- **Testability**: Easier to test serial workflows

### 2. `Runnable<TResult>` Base Class (Complete Implementation)

Provides a default implementation for convenience:

```csharp
namespace Terminal.Gui.ViewBase;

/// <summary>
/// Base implementation of <see cref="IRunnable{TResult}"/> for views that can be run as blocking sessions.
/// </summary>
/// <typeparam name="TResult">The type of result data returned when the session completes.</typeparam>
/// <remarks>
/// Views can derive from this class or implement <see cref="IRunnable{TResult}"/> directly.
/// </remarks>
public class Runnable<TResult> : View, IRunnable<TResult>
{
    /// <inheritdoc/>
    public TResult? Result { get; set; }

    #region IRunnable Implementation - IsRunning (from base interface)

    /// <inheritdoc/>
    public bool RaiseIsRunningChanging(bool oldIsRunning, bool newIsRunning)
    {
        // Clear previous result when starting
        if (newIsRunning)
        {
            Result = default;
        }
        
        // CWP Phase 1: Virtual method (pre-notification)
        if (OnIsRunningChanging(oldIsRunning, newIsRunning))
        {
            return true;  // Canceled
        }

        // CWP Phase 2: Event notification
        var args = new CancelEventArgs<bool> { CurrentValue = oldIsRunning, NewValue = newIsRunning };
        IsRunningChanging?.Invoke(this, args);

        return args.Cancel;
    }

    /// <inheritdoc/>
    public event EventHandler<CancelEventArgs<bool>>? IsRunningChanging;

    /// <inheritdoc/>
    public void RaiseIsRunningChangedEvent(bool newIsRunning)
    {
        // CWP Phase 3: Post-notification (work already done by Application.Begin/End)
        OnIsRunningChanged(newIsRunning);
        
        var args = new EventArgs<bool> { CurrentValue = newIsRunning };
        IsRunningChanged?.Invoke(this, args);
    }

    /// <inheritdoc/>
    public event EventHandler<EventArgs<bool>>? IsRunningChanged;

    /// <summary>
    /// Called before <see cref="IsRunningChanging"/> event. Override to cancel state change or extract <see cref="Result"/>.
    /// </summary>
    /// <param name="oldIsRunning">The current value of IsRunning.</param>
    /// <param name="newIsRunning">The new value of IsRunning (true = starting, false = stopping).</param>
    /// <returns>True to cancel; false to proceed.</returns>
    /// <remarks>
    /// <para>
    /// Default implementation returns false (allow change).
    /// </para>
    /// <para>
    /// <b>IMPORTANT</b>: When <paramref name="newIsRunning"/> is false (stopping), this is the ideal place 
    /// to extract <see cref="Result"/> from views before the runnable is removed from the stack.
    /// At this point, all views are still alive and accessible, and subscribers can inspect the result
    /// and optionally cancel the stop.
    /// </para>
    /// <example>
    /// <code>
    /// protected override bool OnIsRunningChanging(bool oldIsRunning, bool newIsRunning)
    /// {
    ///     if (!newIsRunning)  // Stopping
    ///     {
    ///         // Extract result before removal from stack
    ///         Result = _textField.Text;
    ///         
    ///         // Or check if user wants to save first
    ///         if (HasUnsavedChanges())
    ///         {
    ///             var result = MessageBox.Query("Save?", "Save changes?", "Yes", "No", "Cancel");
    ///             if (result == 2) return true;  // Cancel stopping
    ///             if (result == 0) Save();
    ///         }
    ///     }
    ///     
    ///     return base.OnIsRunningChanging(oldIsRunning, newIsRunning);
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    protected virtual bool OnIsRunningChanging(bool oldIsRunning, bool newIsRunning) => false;

    /// <summary>
    /// Called after <see cref="IsRunning"/> has changed. Override for post-state-change logic.
    /// </summary>
    /// <param name="newIsRunning">The new value of IsRunning (true = started, false = stopped).</param>
    /// <remarks>
    /// Default implementation does nothing. Overrides should call base to ensure extensibility.
    /// </remarks>
    protected virtual void OnIsRunningChanged(bool newIsRunning)
    {
        // Default: no-op
    }

    #endregion

    #region IRunnable Implementation - IsModal (from base interface)

    /// <inheritdoc/>
    public bool RaiseIsModalChanging(bool oldIsModal, bool newIsModal)
    {
        // CWP Phase 1: Virtual method (pre-notification)
        if (OnIsModalChanging(oldIsModal, newIsModal))
        {
            return true;  // Canceled
        }

        // CWP Phase 2: Event notification
        var args = new CancelEventArgs<bool> { CurrentValue = oldIsModal, NewValue = newIsModal };
        IsModalChanging?.Invoke(this, args);

        return args.Cancel;
    }

    /// <inheritdoc/>
    public event EventHandler<CancelEventArgs<bool>>? IsModalChanging;

    /// <inheritdoc/>
    public void RaiseIsModalChangedEvent(bool newIsModal)
    {
        // CWP Phase 3: Post-notification (work already done by Application)
        OnIsModalChanged(newIsModal);
        
        var args = new EventArgs<bool> { CurrentValue = newIsModal };
        IsModalChanged?.Invoke(this, args);
    }

    /// <inheritdoc/>
    public event EventHandler<EventArgs<bool>>? IsModalChanged;

    /// <summary>
    /// Called before <see cref="IsModalChanging"/> event. Override to cancel activation/deactivation.
    /// </summary>
    /// <param name="oldIsModal">The current value of IsModal.</param>
    /// <param name="newIsModal">The new value of IsModal (true = becoming modal/top, false = no longer modal).</param>
    /// <returns>True to cancel; false to proceed.</returns>
    /// <remarks>
    /// Default implementation returns false (allow change).
    /// </remarks>
    protected virtual bool OnIsModalChanging(bool oldIsModal, bool newIsModal) => false;

    /// <summary>
    /// Called after <see cref="IsModal"/> has changed. Override for post-activation logic.
    /// </summary>
    /// <param name="newIsModal">The new value of IsModal (true = became modal, false = no longer modal).</param>
    /// <remarks>
    /// <para>
    /// Default implementation does nothing. Overrides should call base to ensure extensibility.
    /// </para>
    /// <para>
    /// Common uses: setting focus when becoming modal, updating UI state.
    /// </para>
    /// </remarks>
    protected virtual void OnIsModalChanged(bool newIsModal)
    {
        // Default: no-op
    }

    #endregion

    /// <summary>
    /// Requests that this runnable session stop.
    /// </summary>
    public virtual void RequestStop()
    {
        Application.RequestStop(this);
    }
}
```

**Key Design Point**: `OnStopping()` is called **before** the run loop exits and **before** disposal, making it the perfect place to extract `Result` while views are still accessible.

### 3. Event Args

Terminal.Gui's existing event args types are used:

- **`EventArgs<T>`** - For non-cancellable events that need to pass data
- **`CancelEventArgs<T>`** - For cancellable events that need to pass data
- **`CancelEventArgs`** - For cancellable events without additional data
- **`ResultEventArgs<T>`** - For events that produce a result

### 4. Updated `IApplication` Interface

Modified methods to work with `IRunnable`:

```csharp
namespace Terminal.Gui.App;

public interface IApplication
{
    /// <summary>
    /// Gets or sets the topmost runnable session (the one capturing keyboard/mouse input).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>null</c> when no session is running.
    /// </para>
    /// <para>
    /// This is the runnable with <see cref="IRunnable.IsModal"/> = true.
    /// Always equals <c>RunnableSessionStack.Peek().Runnable</c> when stack is non-empty.
    /// </para>
    /// </remarks>
    IRunnable? TopRunnable { get; set; }

    /// <summary>
    /// Gets the stack of all runnable session tokens.
    /// </summary>
    /// <remarks>
    /// The top of the stack (<c>Peek().Runnable</c>) is the <see cref="TopRunnable"/> session (IsModal=true).
    /// All sessions on the stack have IsRunning=true and continue to receive layout, draw, and iteration events.
    /// </remarks>
    ConcurrentStack<RunnableSessionToken> RunnableSessionStack { get; }

    /// <summary>
    /// Prepares the provided runnable for execution and creates a session token.
    /// </summary>
    /// <param name="runnable">The runnable to begin executing.</param>
    /// <returns>A RunnableSessionToken that must be passed to <see cref="End"/> when the session completes.</returns>
    RunnableSessionToken Begin(IRunnable runnable);

    // Three forms of Run():

    /// <summary>
    /// Runs a new session with the provided runnable view.
    /// </summary>
    /// <param name="runnable">The runnable to execute.</param>
    /// <param name="errorHandler">Optional handler for unhandled exceptions.</param>
    void Run(IRunnable runnable, Func<Exception, bool>? errorHandler = null);

    /// <summary>
    /// Creates and runs a new session with a runnable of the specified type.
    /// </summary>
    /// <typeparam name="TRunnable">The type of runnable to create and run. Must have a parameterless constructor.</typeparam>
    /// <param name="errorHandler">Optional handler for unhandled exceptions.</param>
    /// <returns>The runnable instance that was created and run.</returns>
    /// <remarks>
    /// This is a convenience method that creates an instance of <typeparamref name="TRunnable"/> and runs it.
    /// Equivalent to: <c>var r = new TRunnable(); Run(r); return r;</c>
    /// </remarks>
    TRunnable Run<TRunnable>(Func<Exception, bool>? errorHandler = null) where TRunnable : IRunnable, new();

    /// <summary>
    /// Creates and runs a default container runnable (e.g., <see cref="Window"/> or <see cref="Runnable{Object}"/>).
    /// </summary>
    /// <param name="errorHandler">Optional handler for unhandled exceptions.</param>
    /// <returns>The default runnable that was created and run.</returns>
    /// <remarks>
    /// <para>
    /// This is a convenience method for the common use case where the developer just wants a default
    /// container view without specifying a type. It creates a <see cref="Runnable{Object}"/> instance
    /// and runs it, allowing the developer to populate it via the <see cref="Starting"/> event.
    /// </para>
    /// <example>
    /// <code>
    /// var app = Application.Create();
    /// app.Init();
    /// 
    /// IRunnable? mainRunnable = null;
    /// 
    /// // Listen for when the default runnable starts
    /// app.IsRunningChanged += (s, e) =>
    /// {
    ///     if (e.CurrentValue && app.TopRunnable != null)
    ///     {
    ///         // Populate app.TopRunnable with views
    ///         app.TopRunnable.Add(new MenuBar { /* ... */ });
    ///         app.TopRunnable.Add(new StatusBar { /* ... */ });
    ///         // ...
    ///     }
    /// };
    /// 
    /// mainRunnable = app.Run();  // Creates default Runnable{object} and runs it
    /// app.Shutdown();
    /// </code>
    /// </example>
    /// </remarks>
    IRunnable Run(Func<Exception, bool>? errorHandler = null);

    /// <summary>
    /// Requests that the specified runnable session stop.
    /// </summary>
    /// <param name="runnable">The runnable to stop. If null, stops <see cref="TopRunnable"/>.</param>
    void RequestStop(IRunnable? runnable = null);

    /// <summary>
    /// Ends the session associated with the token.
    /// </summary>
    /// <param name="sessionToken">The token returned by <see cref="Begin"/>.</param>
    void End(RunnableSessionToken sessionToken);
}
```

### 5. Updated `RunnableSessionToken`

Wraps an `IRunnable` instance:

```csharp
namespace Terminal.Gui.App;

/// <summary>
/// Represents a running session created by <see cref="IApplication.Begin"/>.
/// Wraps an <see cref="IRunnable"/> instance and is stored in <see cref="IApplication.RunnableSessionStack"/>.
/// </summary>
public class RunnableSessionToken : IDisposable
{
    internal RunnableSessionToken(IRunnable runnable)
    {
        Runnable = runnable;
    }

    /// <summary>
    /// Gets or sets the runnable associated with this session.
    /// Set to null by <see cref="IApplication.End"/> when the session completes.
    /// </summary>
    public IRunnable? Runnable { get; internal set; }

    public void Dispose()
    {
        if (Runnable != null)
        {
            throw new InvalidOperationException(
                "RunnableSessionToken.Dispose called but Runnable is not null. " +
                "Call IApplication.End(sessionToken) before disposing.");
        }
    }
}
```

### 6. `ApplicationImpl.Run` Implementation

Here's how the three forms of `Run()` work with `IRunnable`:

```csharp
namespace Terminal.Gui.App;

public partial class ApplicationImpl
{
    // Form 1: Run with provided runnable
    public void Run(IRunnable runnable, Func<Exception, bool>? errorHandler = null)
    {
        if (runnable is null)
        {
            throw new ArgumentNullException(nameof(runnable));
        }

        if (!Initialized)
        {
            throw new NotInitializedException(nameof(Run));
        }

        // Begin the session (adds to stack, raises IsRunningChanging/IsRunningChanged)
        RunnableSessionToken token = Begin(runnable);

        try
        {
            // All runnables block until RequestStop() is called
            RunLoop(runnable, errorHandler);
        }
        finally
        {
            // End the session (raises IsRunningChanging/IsRunningChanged, pops from stack)
            End(token);
        }
    }

    // Form 2: Run with type parameter (convenience)
    public TRunnable Run<TRunnable>(Func<Exception, bool>? errorHandler = null) 
        where TRunnable : IRunnable, new()
    {
        if (!Initialized)
        {
            throw new NotInitializedException(nameof(Run));
        }

        TRunnable runnable = new();
        Run(runnable, errorHandler);
        return runnable;
    }

    // Form 3: Run with default container (convenience)
    public IRunnable Run(Func<Exception, bool>? errorHandler = null)
    {
        if (!Initialized)
        {
            throw new NotInitializedException(nameof(Run));
        }

        // Create a default container runnable
        // Using Runnable<object> as a generic container (result not meaningful)
        var runnable = new Runnable<object>
        {
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        
        Run(runnable, errorHandler);
        return runnable;
    }

    private void RunLoop(IRunnable runnable, Func<Exception, bool>? errorHandler)
    {
        // Main loop - blocks until RequestStop() is called
        // Note: IsRunning is a derived property (stack.Contains), so we check it each iteration
        while (runnable.IsRunning && !_isDisposed)
        {
            try
            {
                // Process one iteration of the event loop
                Coordinator.RunIteration();
            }
            catch (Exception ex)
            {
                if (errorHandler is null || !errorHandler(ex))
                {
                    throw;
                }
            }
        }
    }

    private RunnableSessionToken Begin(IRunnable runnable)
    {
        // Create token wrapping the runnable
        var token = new RunnableSessionToken(runnable);

        // Raise IsRunningChanging (false -> true) - can be canceled
        if (runnable.RaiseIsRunningChanging(false, true))
        {
            // Starting was canceled
            return token;  // Don't add to stack
        }

        // Push token onto Runnable Stack (IsRunning becomes true)
        RunnableSessionStack.Push(token);

        // Update TopRunnable to the new top of stack
        IRunnable? previousTop = TopRunnable;
        TopRunnable = runnable;

        // Raise IsRunningChanged (now true)
        runnable.RaiseIsRunningChangedEvent(true);

        // If there was a previous top, it's no longer modal
        if (previousTop != null)
        {
            // Raise IsModalChanging (true -> false)
            previousTop.RaiseIsModalChanging(true, false);
            // IsModal is now false (derived property)
            previousTop.RaiseIsModalChangedEvent(false);
        }

        // New runnable becomes modal
        // Raise IsModalChanging (false -> true)
        runnable.RaiseIsModalChanging(false, true);
        // IsModal is now true (derived property)
        runnable.RaiseIsModalChangedEvent(true);

        // Initialize if needed
        if (runnable is View view && !view.IsInitialized)
        {
            view.BeginInit();
            view.EndInit();
            // Initialized event is raised by View.EndInit()
        }

        // Initial Layout and draw
        LayoutAndDraw(true);

        // Set focus
        if (runnable is View viewToFocus && !viewToFocus.HasFocus)
        {
            viewToFocus.SetFocus();
        }

        if (PositionCursor())
        {
            Driver?.UpdateCursor();
        }

        return token;
    }

    private void End(RunnableSessionToken token)
    {
        if (token.Runnable is null)
        {
            return;  // Already ended
        }

        IRunnable runnable = token.Runnable;

        // Raise IsRunningChanging (true -> false) - can be canceled
        // This is where Result should be extracted!
        if (runnable.RaiseIsRunningChanging(true, false))
        {
            // Stopping was canceled
            return;
        }

        // Current runnable is no longer modal
        // Raise IsModalChanging (true -> false)
        runnable.RaiseIsModalChanging(true, false);
        // IsModal is now false (will be false after pop)
        runnable.RaiseIsModalChangedEvent(false);

        // Pop token from Runnable Stack (IsRunning becomes false)
        if (RunnableSessionStack.TryPop(out RunnableSessionToken? popped) && popped == token)
        {
            // Restore previous top runnable
            if (RunnableSessionStack.TryPeek(out RunnableSessionToken? previousToken))
            {
                TopRunnable = previousToken.Runnable;
                
                // Previous runnable becomes modal again
                if (TopRunnable != null)
                {
                    // Raise IsModalChanging (false -> true)
                    TopRunnable.RaiseIsModalChanging(false, true);
                    // IsModal is now true (derived property)
                    TopRunnable.RaiseIsModalChangedEvent(true);
                }
            }
            else
            {
                TopRunnable = null;
            }
        }

        // Raise IsRunningChanged (now false)
        runnable.RaiseIsRunningChangedEvent(false);

        // Set focus to new TopRunnable if exists
        if (TopRunnable is View viewToFocus && !viewToFocus.HasFocus)
        {
            viewToFocus.SetFocus();
        }

        // Clear the token
        token.Runnable = null;
    }

    public void RequestStop(IRunnable? runnable = null)
    {
        runnable ??= TopRunnable;

        if (runnable is null)
        {
            return;
        }

        // Trigger the run loop to exit
        // The End() method will be called from the finally block in Run()
        // and that's where IsRunningChanging/IsRunningChanged will be raised
        _stopRequested = runnable;
    }
}

```

### 8. Updated View Hierarchy

```csharp
// Old hierarchy
View
  ├─ Toplevel (runnable, overlapped, modal property)
  │    ├─ Window (overlapped)
  │    │    ├─ Dialog (modal, centered)
  │    │    │    └─ MessageBox
  │    │    └─ Wizard (modal, multi-step)
  │    └─ (other Toplevel derivatives)
  └─ (all other views)

// New hierarchy  
View
  ├─ Runnable (implements IRunnable)
  │    ├─ Window (can be run, overlapped by default)
  │    ├─ Dialog (implements IModalRunnable<int>, centered)
  │    │    └─ MessageBox
  │    └─ Wizard (implements IModalRunnable<WizardResult>, multi-step)
  └─ (all other views, can optionally implement IRunnable)
```

### 9. Usage Examples

#### Three Forms of Run()

**Form 1: Run with provided runnable**

```csharp
Application app = Application.Create ();
app.Init ();

Runnable<object> myView = new ()
{
    Width = Dim.Fill (),
    Height = Dim.Fill ()
};
myView.Add (new MenuBar { /* ... */ });
myView.Add (new StatusBar { /* ... */ });

app.Run (myView);  // Run the specific runnable
myView.Dispose ();
app.Shutdown ();
```

**Form 2: Run with type parameter (generic convenience)**

```csharp
Application app = Application.Create ();
app.Init ();

Dialog dialog = app.Run<Dialog> ();  // Creates and runs Dialog
// dialog.Result contains the result after it closes

dialog.Dispose ();
app.Shutdown ();
```

**Form 3: Run with default container (parameterless convenience)**

```csharp
Application app = Application.Create ();
app.Init ();

// Subscribe to application-level event to populate the default runnable
app.IsRunningChanged += (s, e) =>
{
    if (e.CurrentValue && app.TopRunnable != null)
    {
        // Populate app.TopRunnable with views when it starts
        app.TopRunnable.Add (new MenuBar { /* ... */ });
        app.TopRunnable.Add (new Label 
        { 
            Text = "Hello World!", 
            X = Pos.Center (), 
            Y = Pos.Center () 
        });
        app.TopRunnable.Add (new StatusBar { /* ... */ });
    }
};

IRunnable mainRunnable = app.Run ();  // Creates default Runnable<object> and runs it
mainRunnable.Dispose ();

app.Shutdown ();
```

**Why three forms?**
- **Form 1**: Most control, you create and configure the runnable
- **Form 2**: Convenience for creating typed runnables with default constructors
- **Form 3**: Simplest for quick apps, populate via `Starting` event

#### Using IsRunningChanged Event

```csharp
Runnable<object> runnable = new ();

// Listen for when it starts running
runnable.IsRunningChanged += (s, e) =>
{
    if (e.CurrentValue)  // Started running
    {
        // View is on the stack, initialized, and laid out
        // Safe to perform post-start logic
        SetupDataBindings ();
        LoadInitialData ();
    }
    else  // Stopped running
    {
        // Clean up resources
        SaveState ();
    }
};

app.Run (runnable);
runnable.Dispose ();
```

#### Override Pattern (Canceling Stop with Cleanup and Result Extraction)

```csharp
public class MyRunnable : Runnable<string>
{
    private TextField? _textField;
    
    protected override bool OnIsRunningChanging(bool oldIsRunning, bool newIsRunning)
    {
        if (!newIsRunning)  // Stopping
        {
            // Extract Result BEFORE being removed from stack
            if (HasUnsavedChanges ())
            {
                var result = MessageBox.Query ("Unsaved Changes", 
                    "Save before closing?", "Yes", "No", "Cancel");
                
                if (result == 2)  // Cancel
                {
                    return true;  // Cancel stopping
                }
                else if (result == 0)  // Yes
                {
                    SaveChanges ();
                    Result = _textField?.Text;  // Extract result
                }
                else  // No
                {
                    Result = null;  // Explicitly null (canceled)
                }
            }
            else
            {
                Result = _textField?.Text;  // Extract result
            }
        }
        else  // Starting
        {
            // Clear previous result
            Result = default;
            
            // Can prevent starting if needed
            if (!CanStart ())
            {
                return true;  // Cancel starting
            }
        }
        
        return base.OnIsRunningChanging (oldIsRunning, newIsRunning);
    }
    
    protected override void OnIsRunningChanged (bool newIsRunning)
    {
        if (newIsRunning)  // Started
        {
            // Post-start initialization
            SetFocus ();
            StartBackgroundWork ();
        }
        else  // Stopped
        {
            // Cleanup after successful stop
            DisconnectFromServer ();
            SaveState ();
        }
        
        base.OnIsRunningChanged (newIsRunning);
    }
    
    protected override void OnIsModalChanged (bool newIsModal)
    {
        if (newIsModal)  // Became modal (top of stack)
        {
            // Set focus, update UI for being active
            UpdateTitle ("Active");
        }
        else  // No longer modal (another runnable on top)
        {
            // Dim UI, show as inactive
            UpdateTitle ("Inactive");
        }
        
        base.OnIsModalChanged (newIsModal);
    }
}
```

#### Modal Dialog with Automatic Result Capture

`Dialog` implements `IRunnable<int>` and overrides `OnIsRunningChanging` to extract result before disposal:

```csharp
public class Dialog : Runnable<int>
{
    private Button[]? _buttons;
    
    protected override bool OnIsRunningChanging(bool oldIsRunning, bool newIsRunning)
    {
        if (!newIsRunning)  // Stopping
        {
            // Extract Result BEFORE views are disposed
            // Find which button was clicked
            Result = _buttons?.Select((b, i) => (button: b, index: i))
                             .FirstOrDefault(x => x.button.HasFocus)
                             .index ?? -1;
        }
        
        return base.OnIsRunningChanging(oldIsRunning, newIsRunning);
    }
}

// Usage
Dialog dialog = new ();
Application.Run (dialog);

// Type-safe result - no casting needed
var result = dialog.Result ?? -1;

// Pattern matching
if (dialog.Result is int buttonIndex)
{
    switch (buttonIndex)
    {
        case 0:
            // First button clicked
            break;
        case 1:
            // Second button clicked
            break;
        case -1:
            // Canceled (ESC, closed)
            break;
    }
}
dialog.Dispose ();
```

This works seamlessly with buttons calling `Application.RequestStop()` in their handlers.

#### MessageBox Example - Type-Safe and Simple

With `Dialog` implementing `IRunnable<int>`, MessageBox is beautifully simple:

```csharp
// MessageBox.Query implementation (simplified)
private static int QueryFull (string title, string message, params string [] buttons)
{
    using Dialog d = new () { Title = title, Text = message };
    
    // Create buttons with handlers that call RequestStop
    for (var i = 0; i < buttons.Length; i++)
    {
        var buttonIndex = i;  // Capture for closure
        d.AddButton (new Button 
        { 
            Text = buttons [i], 
            IsDefault = (i == 0),  // First button is default
            Accept = (s, e) =>
            {
                // Store which button was clicked
                d.Result = buttonIndex;
                Application.RequestStop ();
            }
        });
    }
    
    // Run modal - blocks until RequestStop()
    Application.Run (d);
    
    // Type-safe result - no casting needed!
    return d.Result ?? -1;  // null = canceled (ESC pressed, etc.)
}
```

**Pattern**: Buttons set `Result` in their handlers, then call `RequestStop()`. The `OnIsRunningChanging` override can extract additional data if needed.

#### OptionSelector Example - Type-Safe Pattern

Custom dialog that returns a typed enum:

```csharp
// Custom dialog that returns an Alignment enum
public class AlignmentDialog : Runnable<Alignment>
{
    private RadioGroup? _selector;
    
    public AlignmentDialog ()
    {
        Title = "Choose Alignment";
        
        _selector = new ()
        {
            RadioLabels = new [] { "Start", "Center", "End" }
        };
        
        Add (_selector);
        
        Button okButton = new () { Text = "OK", IsDefault = true };
        okButton.Accept += (s, e) =>
        {
            Application.RequestStop ();
        };
        AddButton (okButton);
    }
    
    protected override bool OnIsRunningChanging (bool oldIsRunning, bool newIsRunning)
    {
        if (!newIsRunning)  // Stopping
        {
            // Extract the selected value BEFORE disposal
            Result = _selector?.SelectedItem switch
            {
                0 => Alignment.Start,
                1 => Alignment.Center,
                2 => Alignment.End,
                _ => (Alignment?)null
            };
        }
        
        return base.OnIsRunningChanging (oldIsRunning, newIsRunning);
    }
}

// Usage - type-safe!
AlignmentDialog dialog = new ();
Application.Run (dialog);

if (dialog.Result is Alignment alignment)
{
    ApplyAlignment (alignment);  // No casting needed!
}
dialog.Dispose ();
```

#### FileDialog Example

```csharp
public class FileDialog : Runnable<string>
{
    private TextField? _pathField;
    
    public FileDialog ()
    {
        Title = "Open File";
        
        _pathField = new () { Width = Dim.Fill () };
        Add (_pathField);
        
        Button okButton = new () { Text = "OK", IsDefault = true };
        okButton.Accept += (s, e) =>
        {
            Application.RequestStop ();
        };
        
        AddButton (okButton);
    }
    
    protected override bool OnIsRunningChanging (bool oldIsRunning, bool newIsRunning)
    {
        if (!newIsRunning)  // Stopping
        {
            // Extract result BEFORE disposal
            Result = _pathField?.Text;
        }
        
        return base.OnIsRunningChanging (oldIsRunning, newIsRunning);
    }
}

// Usage - type-safe!
FileDialog fileDialog = new ();
Application.Run (fileDialog);

if (fileDialog.Result is { } path)
{
    OpenFile (path);  // string, no cast needed!
}
fileDialog.Dispose ();
```

#### Key Benefits

1. **Zero boilerplate** - No manual Accepting handlers in MessageBox
2. **Fully type-safe** - No casting, compile-time type checking
3. **Natural C# idioms** - `null` = not accepted, pattern matching for accepted
4. **Safe disposal** - Data extracted before views are disposed
5. **Extensible** - Works with any type: `int`, `string`, enums, custom objects
6. **Clean separation** - Dialog captures data, controls handle their own Accept logic
7. **Consistent** - All lifecycle events follow Terminal.Gui's Cancellable Work Pattern

---

## Migration Path

### Phase 0: Rename `Current` to `TopRunnable` **DONE**

- Issue #4148
- This is a minor rename for clarity. Can be done after Phase 1 is complete.
- Rename `IApplication.Current` → `IApplication.TopRunnable`
- Update `View.IsCurrentTop` → `View.IsTopRunnable`

### Phase 1: Add IRunnable Support ✅ COMPLETE

- Issue #4400 - **COMPLETED**

**Implemented:**

1. ✅ Add `IRunnable` (non-generic) interface alongside existing `Toplevel`
2. ✅ Add `IRunnable<TResult>` (generic) interface
3. ✅ Add `Runnable<TResult>` base class
4. ✅ Add `RunnableSessionToken` class
5. ✅ Update `IApplication.RunnableSessionStack` to hold `RunnableSessionToken`
6. ✅ Update `IApplication` to support both `Toplevel` and `IRunnable`
7. ✅ Implement CWP-based `IsRunningChanging`/`IsRunningChanged` events
8. ✅ Implement CWP-based `IsModalChanging`/`IsModalChanged` events
9. ✅ Update `Begin()`, `End()`, `RequestStop()` to raise these events
10. ✅ Add `Run()` overloads: `Run(IRunnable)`, `Run<T>()`

**Bonus Features Added:**

11. ✅ Fluent API - `Init()`, `Run<T>()` return `IApplication` for method chaining
12. ✅ Automatic Disposal - `Shutdown()` returns result and disposes framework-owned runnables
13. ✅ Clear Ownership Semantics - "Whoever creates it, owns it"
14. ✅ 62 Parallelizable Unit Tests - Comprehensive test coverage
15. ✅ Example Application - `Examples/FluentExample` demonstrating the pattern
16. ✅ Complete API Documentation - XML docs for all new types

**Key Design Decisions:**

- Fluent API with `Init()` → `Run<T>()` → `Shutdown()` chaining
- `Run<TRunnable>()` returns `IApplication` (breaking change from returning `TRunnable`)
- `Shutdown()` returns `object?` (result from last run runnable)
- Framework automatically disposes runnables created by `Run<T>()`
- Caller disposes runnables passed to `Run(IRunnable)`

**Migration Example:**

```csharp
// Before (manual disposal):
var dialog = new MyDialog();
app.Run(dialog);
var result = dialog.Result;
dialog.Dispose();

// After (fluent with automatic disposal):
var result = Application.Create()
                        .Init()
                        .Run<MyDialog>()
                        .Shutdown() as MyResultType;
```

### Phase 2: Migrate Existing Views

- Issue (TBD)

1. Make `Toplevel` implement `IRunnable` (adapter pattern for compatibility)
2. Update `Dialog` to inherit from `Runnable<int>` instead of `Window`
3. Update `MessageBox` to use `Dialog.Result`
4. Update `Wizard` to inherit from `Runnable<WizardResult>`
5. Update all examples to use new `IRunnable` pattern

### Phase 3: Deprecate and Remove Toplevel

- Issue (TBD)

1. Mark `Toplevel` as `[Obsolete]`
2. Update all internal code to use `IRunnable`/`Runnable<TResult>`
3. Remove `Toplevel` class entirely (breaking change for v3)

### Phase 4: Upgrade View Initialization (Optional Enhancement)

- Issue (TBD)

1. Refactor `View.BeginInit()`/`View.EndInit()`/`Initialized` to be CWP compliant
2. This is independent of the runnable architecture but would improve consistency 
