# Application.Run Terminology - Visual Guide

## The Two Problems

### Problem 1: RunState Sounds Like State Data

```
Current (Confusing):
┌─────────────────────────────────────┐
│  RunState rs = Begin(window);      │  ← "State"? What state does it hold?
│                                     │
│  Application.RunLoop(rs);           │
│                                     │
│  Application.End(rs);               │
└─────────────────────────────────────┘

Users think: "What state information does RunState contain?"
Reality: It's a token/handle for the Begin/End pairing


Proposed (Clear):
┌─────────────────────────────────────┐
│  RunToken token = Begin(window);    │  ✅ Clear: it's a token, not state
│                                     │
│  Application.RunLoop(token);        │
│                                     │
│  Application.End(token);            │
└─────────────────────────────────────┘

Users understand: "It's a token for the Begin/End pairing"
```

### Problem 2: EndAfterFirstIteration Confuses End() with Loop Control

```
Current (Confusing):
┌──────────────────────────────────────────┐
│  EndAfterFirstIteration = true;          │  ← "End"? Like End() method?
│                                          │
│  RunState rs = Begin(window);            │
│                                          │
│  RunLoop(rs);  // Stops after 1 iteration│
│                                          │
│  End(rs);      // This is "End"          │
└──────────────────────────────────────────┘

Users think: "Does EndAfterFirstIteration call End()?"
Reality: It controls RunLoop() behavior, not End()


Proposed (Clear):
┌──────────────────────────────────────────┐
│  StopAfterFirstIteration = true;         │  ✅ Clear: controls loop stopping
│                                          │
│  RunToken token = Begin(window);         │
│                                          │
│  RunLoop(token);  // Stops after 1 iter  │
│                                          │
│  End(token);      // Cleanup             │
└──────────────────────────────────────────┘

Users understand: "Stop controls the loop, End cleans up"
```

## Understanding RunLoop vs RunIteration

**This distinction is valuable and should be preserved:**

```
┌─────────────────────────────────────────────────────────┐
│                  RunLoop(token)                         │
│                                                         │
│  Starts the driver's MainLoop                           │
│  Loops until stopped:                                   │
│                                                         │
│  ┌────────────────────────────────────────────────┐    │
│  │  while (toplevel.Running)                      │    │
│  │  {                                             │    │
│  │      RunIteration(ref token);  ←──────────┐    │    │
│  │      RunIteration(ref token);  ←──────────┤    │    │
│  │      RunIteration(ref token);  ←──────────┤    │    │
│  │      ...                                  │    │    │
│  │  }                                        │    │    │
│  └────────────────────────────────────────────────┘    │
│                                                         │
└─────────────────────────────────────────────────────────┘
                                                  │
                                                  │
                                    ┌─────────────▼───────────────┐
                                    │  RunIteration(ref token)    │
                                    │                             │
                                    │  Processes ONE iteration:   │
                                    │   1. Process driver events  │
                                    │   2. Layout (if needed)     │
                                    │   3. Draw (if needed)       │
                                    │                             │
                                    │  Returns immediately        │
                                    └─────────────────────────────┘

Key Insight: 
- RunLoop = The LOOP itself (blocking, manages iterations)
- RunIteration = ONE iteration (immediate, processes events)
```

## Complete Lifecycle Visualization

```
Application.Run(window)
┌───────────────────────────────────────────────────────┐
│                                                       │
│  1. Begin(window) → RunToken                          │
│     ┌─────────────────────────────────┐              │
│     │ • Initialize window             │              │
│     │ • Layout views                  │              │
│     │ • Draw to screen                │              │
│     └─────────────────────────────────┘              │
│                                                       │
│  2. RunLoop(token)                                    │
│     ┌─────────────────────────────────┐              │
│     │ Start driver's MainLoop         │              │
│     │                                 │              │
│     │ while (Running) {               │              │
│     │   RunIteration(ref token)       │              │
│     │     ├─ Process events           │              │
│     │     ├─ Layout (if needed)       │              │
│     │     └─ Draw (if needed)         │              │
│     │ }                               │              │
│     │                                 │              │
│     │ Exits when:                     │              │
│     │  - RequestStop() called         │              │
│     │  - StopAfterFirstIteration=true │              │
│     └─────────────────────────────────┘              │
│                                                       │
│  3. End(token)                                        │
│     ┌─────────────────────────────────┐              │
│     │ • Cleanup window                │              │
│     │ • Dispose token                 │              │
│     └─────────────────────────────────┘              │
│                                                       │
└───────────────────────────────────────────────────────┘
```

## Manual Control Pattern

When you need fine-grained control:

```
┌────────────────────────────────────────────┐
│  RunToken token = Begin(window);           │
│  StopAfterFirstIteration = true;           │  ✅ Clear: stop after one iter
│                                            │
│  while (!myCondition)                      │
│  {                                         │
│      // Process one iteration              │
│      RunIteration(ref token, first);       │
│      first = false;                        │
│                                            │
│      // Your custom logic here             │
│      DoCustomProcessing();                 │
│  }                                         │
│                                            │
│  End(token);                               │
└────────────────────────────────────────────┘

vs Old (Confusing):

┌────────────────────────────────────────────┐
│  RunState rs = Begin(window);              │
│  EndAfterFirstIteration = true;            │  ❌ Confusing: sounds like End()
│                                            │
│  while (!myCondition)                      │
│  {                                         │
│      RunIteration(ref rs, first);          │
│      first = false;                        │
│      DoCustomProcessing();                 │
│  }                                         │
│                                            │
│  End(rs);                                  │
└────────────────────────────────────────────┘
```

## RequestStop Flow

```
User Action (e.g., Quit Key)
        │
        ▼
┌────────────────────────┐
│  RequestStop(window)   │  ✅ Keep: "Request" is clear
└────────────────────────┘
        │
        ▼
┌────────────────────────┐
│  window.Running=false  │
└────────────────────────┘
        │
        ▼
┌────────────────────────┐
│  RunLoop exits         │
└────────────────────────┘
        │
        ▼
┌────────────────────────┐
│  End() cleans up       │
└────────────────────────┘
```

## What We're Keeping

```
✅ KEEP AS-IS:

Begin/End
┌──────────────────┐     ┌──────────────────┐
│  Begin(window)   │ ... │  End(token)      │
└──────────────────┘     └──────────────────┘
     ↑                           ↑
     │                           │
  Clear, concise          Clear, concise
  Not wordy               Not wordy
  

RequestStop
┌─────────────────────┐
│  RequestStop()      │
└─────────────────────┘
         ↑
         │
  "Request" appropriately
  conveys non-blocking


RunLoop / RunIteration
┌─────────────────┐     ┌──────────────────┐
│  RunLoop()      │     │  RunIteration()  │
│  (the loop)     │     │  (one iteration) │
└─────────────────┘     └──────────────────┘
         ↑                       ↑
         │                       │
    Distinction is important and valuable
```

## Side-by-Side Summary

```
╔═══════════════════════════════════╦═══════════════════════════════════╗
║          CURRENT                  ║          PROPOSED                 ║
╠═══════════════════════════════════╬═══════════════════════════════════╣
║  RunState rs = Begin(window);     ║  RunToken token = Begin(window);  ║
║  EndAfterFirstIteration = true;   ║  StopAfterFirstIteration = true;  ║
║  RunLoop(rs);                     ║  RunLoop(token);                  ║
║  End(rs);                         ║  End(token);                      ║
║                                   ║                                   ║
║  ❌ "State" misleading            ║  ✅ "Token" clear                 ║
║  ❌ "End" confuses with End()     ║  ✅ "Stop" aligns with RequestStop║
╚═══════════════════════════════════╩═══════════════════════════════════╝
```

## Terminology Mapping

```
CHANGE (2 names):
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
RunState                 →  RunToken
EndAfterFirstIteration   →  StopAfterFirstIteration


KEEP UNCHANGED:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Begin                    →  Begin              ✅
End                      →  End                ✅
RequestStop              →  RequestStop        ✅
RunLoop                  →  RunLoop            ✅
RunIteration             →  RunIteration       ✅
Run                      →  Run                ✅
```

## Benefits Visualized

```
Before:
┌─────────────────────────────────────────┐
│  Users think:                           │
│  • "What state does RunState hold?"     │
│  • "Does EndAfterFirstIteration call    │
│     End()?"                             │
│                                         │
│  Result: Confusion, questions           │
└─────────────────────────────────────────┘

After:
┌─────────────────────────────────────────┐
│  Users understand:                      │
│  • "RunToken is a token"                │
│  • "StopAfterFirstIteration controls    │
│     the loop"                           │
│                                         │
│  Result: Clear, self-documenting        │
└─────────────────────────────────────────┘
```

## Summary

**2 Changes Only:**
- `RunState` → `RunToken` (clear it's a token)
- `EndAfterFirstIteration` → `StopAfterFirstIteration` (clear it controls loop)

**Everything Else Stays:**
- `Begin` / `End` - Clear and concise
- `RequestStop` - Appropriately non-blocking
- `RunLoop` / `RunIteration` - Valuable distinction

**Result:**
- ✅ Addresses confusion at the source
- ✅ Minimal disruption (2 names)
- ✅ Preserves what works well
- ✅ Respects maintainer feedback

See [TERMINOLOGY_PROPOSAL.md](TERMINOLOGY_PROPOSAL.md) for complete analysis.
