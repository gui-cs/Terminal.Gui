# Application.Run Terminology Proposal (Revised)

## Executive Summary

This proposal addresses specific terminology issues in the Terminal.Gui Application.Run lifecycle while preserving what works well. Based on maintainer feedback, we keep `Begin`, `End`, and `RequestStop` unchanged, and focus on the real sources of confusion.

## What Works Well (Keep Unchanged)

- ✅ **`Begin` and `End`** - Clear, concise lifecycle pairing without being wordy
- ✅ **`RequestStop`** - Non-blocking nature is appropriately conveyed by "Request"
- ✅ **Distinction between `RunLoop` and `RunIteration`** - `RunLoop` starts the driver's mainloop, `RunIteration` processes one iteration

## The Real Problems

### Problem 1: `RunState` Sounds Like State Data

**Current:**
```csharp
RunState runState = Application.Begin(toplevel);
Application.RunLoop(runState);
Application.End(runState);
```

**Issue:** The name `RunState` suggests it holds state/data about the run, but it's actually:
- A token/handle returned by `Begin()` to pair with `End()`
- An execution context for the Toplevel
- Not primarily about "state" - it's about identity/scoping

**Proposed Options:**

**Option A: `RunToken`**
```csharp
RunToken token = Application.Begin(toplevel);
Application.RunLoop(token);
Application.End(token);
```
- ✅ Clear it's a token, not state data
- ✅ Concise (not wordy)
- ✅ Industry standard pattern (CancellationToken, etc.)

**Option B: `ExecutionContext`**
```csharp
ExecutionContext context = Application.Begin(toplevel);
Application.RunLoop(context);
Application.End(context);
```
- ✅ Accurately describes bounded execution scope
- ✅ Familiar from .NET (HttpContext, DbContext)
- ⚠️ Slightly longer

**Option C: Keep `RunState` but clarify in docs**
- ⚠️ Name remains misleading even with good documentation

### Problem 2: `EndAfterFirstIteration` Confuses "End" with Loop Control

**Current:**
```csharp
Application.EndAfterFirstIteration = true;  // Controls RunLoop, not End()
RunState rs = Application.Begin(window);
Application.RunLoop(rs);  // Stops after 1 iteration due to flag
Application.End(rs);       // This is actual "End"
```

**Issue:** 
- "End" in the flag name suggests the `End()` method, but it actually controls `RunLoop()`
- The flag stops the loop, not the lifecycle
- Creates confusion about when `End()` gets called

**Proposed Options:**

**Option A: `StopAfterFirstIteration`**
```csharp
Application.StopAfterFirstIteration = true;
```
- ✅ "Stop" aligns with `RequestStop` which also affects the loop
- ✅ Clearly about loop control, not lifecycle end
- ✅ Minimal change

**Option B: `SingleIteration`**
```csharp
Application.SingleIteration = true;
```
- ✅ Shorter, positive framing
- ✅ Describes the mode, not the action
- ⚠️ Less obvious it's about stopping

**Option C: `RunLoopOnce`**
```csharp
Application.RunLoopOnce = true;
```
- ✅ Very explicit about what happens
- ⚠️ Slightly awkward phrasing

### Problem 3: "Run" Overload (Lower Priority)

**Current:**
```csharp
Application.Run(window);        // Complete lifecycle
Application.RunLoop(state);     // Starts the driver's mainloop  
Application.RunIteration(state); // One iteration
```

**Issue:** Three different APIs with "Run" in the name doing different things at different levels.

**Note:** @tig's feedback indicates the distinction between `RunLoop` and `RunIteration` is important and understood. The "Run" prefix may not be a critical issue if the distinction is clear.

**Possible future consideration (not recommended now):**
- Document the distinction more clearly
- Keep names as-is since they work with understanding

## Recommended Changes

### Minimal Impact Recommendation

Change only what's most confusing:

1. **`RunState` → `RunToken`** (or `ExecutionContext`)
   - Clear it's a token/handle
   - Less ambiguous than "state"
   - Concise

2. **`EndAfterFirstIteration` → `StopAfterFirstIteration`**
   - Aligns with `RequestStop` terminology
   - Clearly about loop control
   - Minimal change

3. **Keep everything else:**
   - `Begin` / `End` - Perfect as-is
   - `RequestStop` - Clear non-blocking signal
   - `RunLoop` / `RunIteration` - Distinction is valuable
   - `Run()` - Familiar high-level API

### Usage Comparison

**Current (Confusing):**
```csharp
// High-level
Application.Run(window);

// Low-level
Application.EndAfterFirstIteration = true;
RunState rs = Application.Begin(window);
Application.RunLoop(rs);
Application.End(rs);
```

**Proposed (Clearer):**
```csharp
// High-level (unchanged)
Application.Run(window);

// Low-level (clearer)
Application.StopAfterFirstIteration = true;
RunToken token = Application.Begin(window);
Application.RunLoop(token);
Application.End(token);
```

## Understanding RunLoop vs RunIteration

It's important to preserve the distinction:

- **`RunLoop(token)`** - Starts the driver's MainLoop and runs until stopped
  - This is a blocking call that manages the loop
  - Calls `RunIteration` repeatedly
  - Returns when `RequestStop()` is called or `StopAfterFirstIteration` is true

- **`RunIteration(ref token)`** - Processes ONE iteration
  - Processes pending driver events
  - Does layout if needed
  - Draws if needed
  - Returns immediately

**Visual:**
```
RunLoop(token):
  ┌─────────────────────┐
  │ while (Running)     │
  │   RunIteration()    │ ← One call
  │   RunIteration()    │ ← Another call
  │   RunIteration()    │ ← Another call
  └─────────────────────┘
```

This distinction is valuable and should be preserved.

## Migration Strategy

### Phase 1: Add New Names with Obsolete Attributes

```csharp
// Add new type
public class RunToken { ... }

// Add conversion from old to new
public static implicit operator RunToken(RunState state) => new (state.Toplevel);

// Mark old type obsolete
[Obsolete("Use RunToken instead. RunState will be removed in a future version.")]
public class RunState { ... }

// Add new property
public static bool StopAfterFirstIteration { get; set; }

// Mark old property obsolete
[Obsolete("Use StopAfterFirstIteration instead.")]
public static bool EndAfterFirstIteration 
{ 
    get => StopAfterFirstIteration;
    set => StopAfterFirstIteration = value;
}
```

### Phase 2: Update Documentation

- Update all docs to use new terminology
- Add migration guide
- Explain the distinction between RunLoop and RunIteration

### Phase 3: Update Examples

- Examples use new APIs
- Keep old examples in "legacy" section temporarily

### Phase 4: Future Removal (Multiple Releases Later)

- After sufficient adoption period, consider removing obsolete APIs
- Or keep them indefinitely with internal delegation

## Alternative Naming Options

### For RunState/RunToken

| Option | Pros | Cons | Recommendation |
|--------|------|------|----------------|
| `RunToken` | Clear it's a token, concise | New terminology | ⭐ Best |
| `ExecutionContext` | Industry standard | Slightly longer | Good alternative |
| `RunHandle` | Clear it's a handle | "Handle" sounds Win32-ish | Acceptable |
| `RunContext` | Familiar pattern | "Context" overloaded in .NET | OK |
| Keep `RunState` | No change needed | Remains misleading | Not recommended |

### For EndAfterFirstIteration

| Option | Pros | Cons | Recommendation |
|--------|------|------|----------------|
| `StopAfterFirstIteration` | Aligns with RequestStop | Slightly longer | ⭐ Best |
| `SingleIteration` | Shorter | Less obvious meaning | Good alternative |
| `RunLoopOnce` | Very explicit | Awkward phrasing | OK |
| Keep `EndAfterFirstIteration` | No change | Continues confusion | Not recommended |

## Comparison with Other Frameworks

**Token/Context Pattern:**
- .NET: `CancellationToken` - token for cancellation scope
- ASP.NET: `HttpContext` - context for HTTP request
- Entity Framework: `DbContext` - context for database session
- **Terminal.Gui:** `RunToken` (proposed) - token for execution scope

**Loop Control Flags:**
- WinForms: `Application.Exit()` - stops message loop
- WPF: `Dispatcher.InvokeShutdown()` - stops dispatcher
- **Terminal.Gui:** `RequestStop()` (keep), `StopAfterFirstIteration` (proposed)

## FAQ

**Q: Why not change `Begin` and `End` to `BeginSession` and `EndSession`?**

A: Per maintainer feedback, "Session" makes the names wordy without adding clarity. `Begin` and `End` are clear, concise, and work well as a lifecycle pair.

**Q: Why keep `RunLoop`?**

A: The distinction between `RunLoop` (starts the driver's mainloop) and `RunIteration` (one iteration) is important and well-understood. The "Run" prefix is not the primary source of confusion.

**Q: Why change `RunState`?**

A: "State" implies the object holds state/data about the run. In reality, it's a token/handle for the Begin/End pairing. Calling it a "Token" or "Context" is more accurate.

**Q: Why change `EndAfterFirstIteration`?**

A: "End" in the flag name creates confusion with the `End()` method. The flag controls loop behavior, not lifecycle cleanup. "Stop" aligns better with `RequestStop` which also affects the loop.

**Q: Is this bikeshedding?**

A: No. These specific names (`RunState`, `EndAfterFirstIteration`) cause real confusion. The changes are minimal, focused, and address documented pain points while preserving what works.

## Summary

**Recommended Changes (Minimal Impact):**

1. `RunState` → `RunToken` 
2. `EndAfterFirstIteration` → `StopAfterFirstIteration`

**Keep Unchanged:**
- `Begin` / `End` - Clear and concise
- `RequestStop` - Appropriately conveys non-blocking
- `RunLoop` / `RunIteration` - Distinction is valuable
- `Run()` - Familiar high-level API

**Benefits:**
- ✅ Eliminates the two primary sources of confusion
- ✅ Maintains clarity of successful patterns
- ✅ Minimal disruption (2 names only)
- ✅ Complete backward compatibility via obsolete attributes
- ✅ Respects maintainer feedback

This focused approach addresses real problems without over-engineering the solution.
