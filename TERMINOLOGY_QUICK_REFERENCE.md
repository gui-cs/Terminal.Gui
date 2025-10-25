# Application.Run Terminology - Quick Reference

## The Problem

Current terminology has two specific issues:

1. **`RunState`** sounds like state data, but it's actually a token/handle
2. **`EndAfterFirstIteration`** uses "End" but controls loop behavior, not lifecycle

## Recommended Solution

### Minimal Changes

| Current | Proposed | Why |
|---------|----------|-----|
| `RunState` | `RunToken` | Clear it's a token, not state data |
| `EndAfterFirstIteration` | `StopAfterFirstIteration` | "Stop" aligns with `RequestStop`, clearly about loop control |

### Keep Unchanged

| API | Why It Works |
|-----|--------------|
| `Begin` / `End` | Clear, concise lifecycle pairing |
| `RequestStop` | "Request" appropriately conveys non-blocking |
| `RunLoop` / `RunIteration` | Distinction is important: RunLoop starts the driver's mainloop, RunIteration processes one iteration |

## Usage Comparison

### Current (Confusing)

```csharp
// What is RunState? State data or a handle?
RunState rs = Application.Begin(window);
Application.RunLoop(rs);
Application.End(rs);

// Does this call End()? No, it controls RunLoop()
Application.EndAfterFirstIteration = true;
```

### Proposed (Clear)

```csharp
// Clearly a token, not state data
RunToken token = Application.Begin(window);
Application.RunLoop(token);
Application.End(token);

// Clearly controls loop stopping, aligns with RequestStop
Application.StopAfterFirstIteration = true;
```

## Understanding RunLoop vs RunIteration

**Important distinction to preserve:**

```
RunLoop(token):          RunIteration(token):
┌──────────────────┐     ┌──────────────────┐
│ Starts driver's  │     │ Processes ONE    │
│ MainLoop         │     │ iteration:       │
│                  │     │  - Events        │
│ Loops calling:   │     │  - Layout        │
│  RunIteration()  │     │  - Draw          │
│  RunIteration()  │     │                  │
│  ...             │     │ Returns          │
│                  │     │ immediately      │
│ Until stopped    │     │                  │
└──────────────────┘     └──────────────────┘
```

This distinction is valuable and should be kept.

## Complete API Overview

```
Application.Run(window)        ← High-level: complete lifecycle
  ├─ Application.Begin(window) → RunToken
  │    └─ Initialize, layout, draw
  ├─ Application.RunLoop(token)
  │    └─ Loop: while(running) { RunIteration() }
  └─ Application.End(token)
       └─ Cleanup

Application.RunIteration(ref token) ← Low-level: one iteration

Application.RequestStop()       ← Signal loop to stop
Application.StopAfterFirstIteration ← Mode: stop after 1 iteration
```

## Alternative Options Considered

### For RunState

| Option | Pros | Cons |
|--------|------|------|
| **RunToken** ⭐ | Clear, concise | New term |
| ExecutionContext | Industry standard | Longer |
| RunHandle | Clear | Win32-ish |

### For EndAfterFirstIteration

| Option | Pros | Cons |
|--------|------|------|
| **StopAfterFirstIteration** ⭐ | Aligns with RequestStop | Slightly longer |
| SingleIteration | Shorter | Less obvious |
| RunLoopOnce | Explicit | Awkward |

## Migration Example

### Backward Compatible Migration

```csharp
// Old code continues to work with obsolete warnings
[Obsolete("Use RunToken instead")]
public class RunState { ... }

[Obsolete("Use StopAfterFirstIteration instead")]
public static bool EndAfterFirstIteration 
{ 
    get => StopAfterFirstIteration;
    set => StopAfterFirstIteration = value;
}

// New code uses clearer names
public class RunToken { ... }
public static bool StopAfterFirstIteration { get; set; }
```

### User Migration

```csharp
// Before
RunState rs = Application.Begin(window);
Application.EndAfterFirstIteration = true;
Application.RunLoop(rs);
Application.End(rs);

// After (simple find/replace)
RunToken token = Application.Begin(window);
Application.StopAfterFirstIteration = true;
Application.RunLoop(token);
Application.End(token);
```

## Why These Changes?

### RunState → RunToken

**Problem:** Users see "State" and think it holds state data. They ask:
- "What state does it hold?"
- "Can I query the state?"
- "Is it like a state machine?"

**Solution:** "Token" clearly indicates it's an identity/handle for Begin/End pairing, like `CancellationToken`.

### EndAfterFirstIteration → StopAfterFirstIteration

**Problem:** Users see "End" and think of `End()` method. They ask:
- "Does this call `End()`?"
- "Why is it called 'End' when it controls the loop?"

**Solution:** "Stop" aligns with `RequestStop` and clearly indicates loop control, not lifecycle cleanup.

## What We're NOT Changing

### Begin / End

✅ **Keep as-is** - Clear, concise lifecycle pairing
- Not wordy
- Industry standard pattern (BeginInvoke/EndInvoke, etc.)
- Works well

### RequestStop

✅ **Keep as-is** - Appropriately conveys non-blocking nature
- "Request" indicates it doesn't block
- Clear about what it does
- Works well

### RunLoop / RunIteration

✅ **Keep as-is** - Distinction is important and understood
- RunLoop = starts the driver's mainloop (blocking)
- RunIteration = processes one iteration (immediate)
- The distinction is valuable
- "Run" prefix is OK when the difference is clear

## Summary

**Changes (2 names only):**
- `RunState` → `RunToken`
- `EndAfterFirstIteration` → `StopAfterFirstIteration`

**Benefits:**
- ✅ Addresses the two primary sources of confusion
- ✅ Minimal disruption
- ✅ Backward compatible
- ✅ Respects maintainer feedback
- ✅ Preserves what works well

See [TERMINOLOGY_PROPOSAL.md](TERMINOLOGY_PROPOSAL.md) for detailed analysis.
