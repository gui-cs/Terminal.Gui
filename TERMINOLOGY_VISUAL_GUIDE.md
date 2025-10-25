# Application.Run Terminology - Visual Guide

This document provides visual representations of the Application execution lifecycle to clarify the terminology.

## Current Terminology (Confusing)

### The Problem: "Run" Everywhere

```
┌─────────────────────────────────────────────────────────┐
│                   Application.Run()                     │  ← High-level API
│                                                         │
│  "Run" means the complete lifecycle                    │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
        ┌─────────────────────────────────┐
        │   Application.Begin(toplevel)   │  ← "Begin" what?
        │                                 │
        │   Returns: RunState             │  ← Sounds like state data
        └─────────────────────────────────┘
                          │
                          ▼
        ┌─────────────────────────────────┐
        │  Application.RunLoop(runState)  │  ← "Run" again? Run vs RunLoop?
        │                                 │
        │  ┌───────────────────────────┐ │
        │  │ while (running)           │ │
        │  │   RunIteration()          │ │  ← "Run" again? What's the difference?
        │  │     ProcessInput()        │ │
        │  │     Layout/Draw()         │ │
        │  └───────────────────────────┘ │
        └─────────────────────────────────┘
                          │
                          ▼
        ┌─────────────────────────────────┐
        │   Application.End(runState)     │  ← "End" what?
        └─────────────────────────────────┘

Issues:
❌ "Run" appears 4 times meaning different things
❌ RunState sounds like state, but it's a token
❌ Begin/End don't clarify what's beginning/ending
❌ RunLoop vs RunIteration relationship unclear
```

## Proposed Terminology - Option 1: Session-Based ⭐

### The Solution: Clear, Explicit Names

```
┌─────────────────────────────────────────────────────────┐
│                   Application.Run()                     │  ← High-level (unchanged)
│                                                         │
│  Complete lifecycle: Begin + ProcessEvents + End       │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
        ┌─────────────────────────────────────┐
        │ Application.BeginSession(toplevel)  │  ✅ Clear: starting a session
        │                                     │
        │ Returns: ToplevelSession            │  ✅ Clear: it's a session token
        └─────────────────────────────────────┘
                          │
                          ▼
        ┌─────────────────────────────────────┐
        │ Application.ProcessEvents(session)  │  ✅ Clear: processing events
        │                                     │
        │  ┌──────────────────────────────┐  │
        │  │ while (running)              │  │
        │  │   ProcessEventsIteration()   │  │  ✅ Clear: one iteration of processing
        │  │     ProcessInput()           │  │
        │  │     Layout/Draw()            │  │
        │  └──────────────────────────────┘  │
        └─────────────────────────────────────┘
                          │
                          ▼
        ┌─────────────────────────────────────┐
        │  Application.EndSession(session)    │  ✅ Clear: ending the session
        └─────────────────────────────────────┘

Benefits:
✅ "Session" clearly indicates bounded execution
✅ "ProcessEvents" describes the action
✅ BeginSession/EndSession are unambiguous
✅ Terminology is consistent and clear
```

## Lifecycle Comparison

### Application Lifecycle (Init/Shutdown) vs Session Lifecycle (Begin/ProcessEvents/End)

```
┌────────────────────────── Application Lifetime ──────────────────────────┐
│                                                                           │
│  Application.Init()          ← Initialize once per application           │
│      ├─ Create driver                                                    │
│      ├─ Initialize screen                                                │
│      └─ Setup subsystems                                                 │
│                                                                           │
│  ┌──────────────────────── Session 1 ────────────────────────┐          │
│  │  Application.BeginSession(window1) → session1             │          │
│  │      ├─ Initialize window1                                │          │
│  │      ├─ Layout window1                                    │          │
│  │      └─ Draw window1                                      │          │
│  │                                                            │          │
│  │  Application.ProcessEvents(session1)                      │          │
│  │      └─ Event loop until RequestStop()                    │          │
│  │                                                            │          │
│  │  Application.EndSession(session1)                         │          │
│  │      └─ Cleanup window1                                   │          │
│  └────────────────────────────────────────────────────────────┘          │
│                                                                           │
│  ┌──────────────────────── Session 2 ────────────────────────┐          │
│  │  Application.BeginSession(dialog) → session2              │          │
│  │  Application.ProcessEvents(session2)                      │          │
│  │  Application.EndSession(session2)                         │          │
│  └────────────────────────────────────────────────────────────┘          │
│                                                                           │
│  Application.Shutdown()      ← Cleanup once per application              │
│      ├─ Dispose driver                                                   │
│      └─ Restore terminal                                                 │
│                                                                           │
└───────────────────────────────────────────────────────────────────────────┘

Key Insight: Multiple sessions within one application lifetime
```

## Event Flow During ProcessEvents

### Current (Confusing)

```
RunLoop(runState)
    │
    └─> while (toplevel.Running)
            │
            ├─> RunIteration(runState)    ← What's the difference?
            │       │
            │       ├─> MainLoop.RunIteration()
            │       │       └─> Process driver events
            │       │
            │       ├─> Layout (if needed)
            │       └─> Draw (if needed)
            │
            └─> (repeat)
```

### Proposed (Clear)

```
ProcessEvents(session)
    │
    └─> while (toplevel.Running)
            │
            ├─> ProcessEventsIteration(session)  ✅ Clear: one iteration of event processing
            │       │
            │       ├─> MainLoop.RunIteration()
            │       │       └─> Process driver events
            │       │
            │       ├─> Layout (if needed)
            │       └─> Draw (if needed)
            │
            └─> (repeat)
```

## Manual Control Pattern

When you need fine-grained control over the event loop:

### Current (Confusing)

```
RunState rs = Begin(toplevel);        ← Begin what?
EndAfterFirstIteration = true;        ← End what?

while (!done)
{
    RunIteration(ref rs, first);      ← Run what? How does this relate to RunLoop?
    first = false;
    
    // Custom processing
    DoMyCustomStuff();
}

End(rs);                              ← End what?
```

### Proposed (Clear)

```
ToplevelSession session = BeginSession(toplevel);     ✅ Clear: starting a session
StopAfterFirstIteration = true;                       ✅ Clear: stop after one iteration

while (!done)
{
    ProcessEventsIteration(ref session, first);       ✅ Clear: process one iteration
    first = false;
    
    // Custom processing
    DoMyCustomStuff();
}

EndSession(session);                                  ✅ Clear: ending the session
```

## RequestStop Flow

### Current

```
User Action (e.g., Quit Key)
    │
    ▼
Application.RequestStop(toplevel)
    │
    ▼
Sets toplevel.Running = false
    │
    ▼
RunLoop detects !Running
    │
    ▼
RunLoop exits
    │
    ▼
Application.End() cleans up
```

### Proposed (Same flow, clearer names)

```
User Action (e.g., Quit Key)
    │
    ▼
Application.StopProcessingEvents(toplevel)    ✅ Clear: stops event processing
    │
    ▼
Sets toplevel.Running = false
    │
    ▼
ProcessEvents detects !Running
    │
    ▼
ProcessEvents exits
    │
    ▼
Application.EndSession() cleans up
```

## Nested Sessions (Modal Dialogs)

```
┌────────────── Main Window Session ──────────────┐
│                                                  │
│  session1 = BeginSession(mainWindow)            │
│                                                  │
│  ProcessEvents(session1) starts...              │
│      │                                           │
│      │  User clicks "Open Dialog" button        │
│      │                                           │
│      ├─> ┌──────── Dialog Session ──────┐      │
│      │   │                               │      │
│      │   │  session2 = BeginSession(dialog)     │
│      │   │                               │      │
│      │   │  ProcessEvents(session2)      │      │
│      │   │  (blocks until dialog closes) │      │
│      │   │                               │      │
│      │   │  EndSession(session2)         │      │
│      │   │                               │      │
│      │   └───────────────────────────────┘      │
│      │                                           │
│      │  (returns to main window)                │
│      │                                           │
│  ...ProcessEvents continues                     │
│                                                  │
│  EndSession(session1)                           │
│                                                  │
└──────────────────────────────────────────────────┘

Key Insight: Sessions can be nested (modal dialogs)
```

## Complete Example Flow

### Simple Application

```
START
  │
  ├─> Application.Init()                     [Application Lifecycle]
  │       └─> Initialize driver, screen
  │
  ├─> window = new Window()
  │
  ├─> Application.Run(window)                [High-level API]
  │       │
  │       ├─> BeginSession(window)           [Session begins]
  │       │       └─> Initialize, layout, draw
  │       │
  │       ├─> ProcessEvents(session)         [Event processing]
  │       │       └─> Loop until stopped
  │       │
  │       └─> EndSession(session)            [Session ends]
  │               └─> Cleanup
  │
  ├─> window.Dispose()
  │
  └─> Application.Shutdown()                 [Application Lifecycle]
          └─> Restore terminal
END
```

### Application with Manual Control

```
START
  │
  ├─> Application.Init()
  │
  ├─> window = new Window()
  │
  ├─> session = Application.BeginSession(window)    [Manual Session Control]
  │       └─> Initialize, layout, draw
  │
  ├─> Application.StopAfterFirstIteration = true
  │
  ├─> while (!done)                                 [Custom Event Loop]
  │       │
  │       ├─> Application.ProcessEventsIteration(ref session, first)
  │       │       └─> Process one iteration
  │       │
  │       ├─> DoCustomProcessing()
  │       │
  │       └─> first = false
  │
  ├─> Application.EndSession(session)               [Manual Session Control]
  │       └─> Cleanup
  │
  ├─> window.Dispose()
  │
  └─> Application.Shutdown()
END
```

## Terminology Mapping Summary

### API Name Changes

```
CURRENT                          PROPOSED
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Application.Run()           →    Application.Run()                    (unchanged)

RunState                    →    ToplevelSession                     ✅ Clear: session token

Application.Begin()         →    Application.BeginSession()          ✅ Clear: begin a session

Application.RunLoop()       →    Application.ProcessEvents()         ✅ Clear: processes events

Application.RunIteration()  →    Application.ProcessEventsIteration() ✅ Clear: one iteration

Application.End()           →    Application.EndSession()            ✅ Clear: end the session

Application.RequestStop()   →    Application.StopProcessingEvents()  ✅ Clear: stops processing

EndAfterFirstIteration      →    StopAfterFirstIteration            ✅ Consistent naming

NotifyNewRunState          →    NotifyNewSession                   ✅ Consistent naming

NotifyStopRunState         →    NotifyStopSession                  ✅ Consistent naming

RunStateEventArgs          →    ToplevelSessionEventArgs           ✅ Consistent naming
```

## Benefits Visualized

### Before: Confusion

```
User thinks:
"What's the difference between Run, RunLoop, and RunIteration?"
"Is RunState storing state or just a handle?"
"What am I Beginning and Ending?"

         ┌─────────────┐
         │    Run()    │  ← What does "Run" mean exactly?
         └─────────────┘
                │
         ┌─────────────┐
         │   Begin()   │  ← Begin what?
         └─────────────┘
                │
         ┌─────────────┐
         │  RunLoop()  │  ← Is this the same as Run?
         └─────────────┘
                │
         ┌─────────────┐
         │    End()    │  ← End what?
         └─────────────┘

Result: User confusion, slower learning curve
```

### After: Clarity

```
User understands:
"Run() does the complete lifecycle"
"BeginSession/EndSession manage a session"
"ProcessEvents processes events until stopped"
"ToplevelSession is a token for the session"

         ┌─────────────────┐
         │     Run()       │  ✅ Complete lifecycle
         └─────────────────┘
                │
         ┌─────────────────┐
         │ BeginSession()  │  ✅ Start a session
         └─────────────────┘
                │
         ┌─────────────────┐
         │ ProcessEvents() │  ✅ Process events
         └─────────────────┘
                │
         ┌─────────────────┐
         │  EndSession()   │  ✅ End the session
         └─────────────────┘

Result: Clear understanding, faster learning curve
```

## See Also

- [TERMINOLOGY_PROPOSAL.md](TERMINOLOGY_PROPOSAL.md) - Full proposal with rationale
- [TERMINOLOGY_QUICK_REFERENCE.md](TERMINOLOGY_QUICK_REFERENCE.md) - Quick comparison tables
- [TERMINOLOGY_INDUSTRY_COMPARISON.md](TERMINOLOGY_INDUSTRY_COMPARISON.md) - Industry patterns
