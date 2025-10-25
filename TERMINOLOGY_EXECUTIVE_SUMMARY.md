# Application.Run Terminology - Executive Summary

## The Ask

Propose improved terminology for `Application.Run` lifecycle APIs to reduce confusion.

## Maintainer Feedback

Based on @tig's review:
- ✅ Keep deep analysis and diagrams
- ✅ Keep `Begin` and `End` (not wordy like BeginSession/EndSession)
- ✅ Keep `RequestStop` (non-blocking nature is clear)
- ✅ Preserve distinction between `RunLoop` (starts driver's mainloop) and `RunIteration` (one iteration)

## The Real Problems (Only 2)

### 1. `RunState` Sounds Like State Data

```csharp
RunState rs = Application.Begin(window);  // ❌ What state does it hold?
```

**Reality:** It's a token/handle for Begin/End pairing, not state data.

**Solution:** Rename to `RunToken` (clear it's a token, concise).

### 2. `EndAfterFirstIteration` Confuses End() Method with Loop Control

```csharp
Application.EndAfterFirstIteration = true;  // ❌ Does this call End()?
```

**Reality:** It controls `RunLoop()` behavior, not lifecycle cleanup.

**Solution:** Rename to `StopAfterFirstIteration` (aligns with `RequestStop`, clearly about loop control).

## Proposed Changes (Minimal - 2 Names Only)

| Current | Proposed | Why |
|---------|----------|-----|
| `RunState` | `RunToken` | Clear it's a token, not state |
| `EndAfterFirstIteration` | `StopAfterFirstIteration` | Clear it controls loop, aligns with RequestStop |

## Keep Unchanged

| API | Why It Works |
|-----|--------------|
| `Begin` / `End` | Clear, concise - not wordy |
| `RequestStop` | "Request" appropriately conveys non-blocking |
| `RunLoop` / `RunIteration` | Distinction is important: RunLoop starts mainloop, RunIteration processes one iteration |

## Usage Comparison

### Before (Confusing)
```csharp
RunState rs = Application.Begin(window);
Application.EndAfterFirstIteration = true;
Application.RunLoop(rs);
Application.End(rs);
```

### After (Clear)
```csharp
RunToken token = Application.Begin(window);
Application.StopAfterFirstIteration = true;
Application.RunLoop(token);
Application.End(token);
```

## Benefits

- ✅ Addresses the 2 primary sources of confusion
- ✅ Minimal disruption (only 2 names)
- ✅ Backward compatible (obsolete attributes on old names)
- ✅ Respects maintainer feedback
- ✅ Preserves what works well

## Documents

- **TERMINOLOGY_PROPOSAL.md** - Complete analysis with rationale
- **TERMINOLOGY_QUICK_REFERENCE.md** - Quick comparison and examples
- **TERMINOLOGY_VISUAL_GUIDE.md** - Visual diagrams showing the issues

## Alternative Options

### For RunState
- **RunToken** ⭐ (Recommended) - Clear, concise
- ExecutionContext - Industry standard but longer
- Keep as-is - Not recommended (remains misleading)

### For EndAfterFirstIteration
- **StopAfterFirstIteration** ⭐ (Recommended) - Aligns with RequestStop
- SingleIteration - Shorter but less obvious
- Keep as-is - Not recommended (continues confusion)

## Migration Path

Backward compatible via obsolete attributes:

```csharp
[Obsolete("Use RunToken instead")]
public class RunState { ... }

[Obsolete("Use StopAfterFirstIteration instead")]
public static bool EndAfterFirstIteration { get; set; }
```

Users can migrate gradually with simple find/replace.

---

**Status:** Revised based on maintainer feedback
**Focus:** Minimal, targeted changes addressing real confusion
**Impact:** Low (2 names), High clarity gain
