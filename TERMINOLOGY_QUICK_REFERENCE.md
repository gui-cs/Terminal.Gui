# Application Run Terminology - Quick Reference

## Current State (Confusing)

```
Application.Run(toplevel)
  ├─ Application.Begin(toplevel) → RunState
  ├─ Application.RunLoop(RunState)
  │   └─ Application.RunIteration()
  └─ Application.End(RunState)
```

**Problems:**
- "Run" means too many things (lifecycle, loop, method names)
- "RunState" sounds like state data, but it's a token
- "Begin/End" - begin/end what?
- "RunLoop" vs "RunIteration" - relationship unclear

## Proposed (Clear) - Option 1: Session-Based ⭐

```
Application.Run(toplevel)  // High-level API (unchanged)
  ├─ Application.BeginSession(toplevel) → ToplevelSession
  ├─ Application.ProcessEvents(ToplevelSession)
  │   └─ Application.ProcessEventsIteration()
  └─ Application.EndSession(ToplevelSession)
```

**Benefits:**
- "Session" clearly indicates bounded execution period
- "ProcessEvents" describes what the loop does
- "BeginSession/EndSession" are unambiguous pairs
- "ToplevelSession" clearly indicates a session token

## Proposed (Clear) - Option 2: Modal/Show

```
Application.ShowModal(toplevel)  // or keep Run()
  ├─ Application.Activate(toplevel) → ToplevelHandle
  ├─ Application.EventLoop(ToplevelHandle)
  │   └─ Application.ProcessEvents()
  └─ Application.Deactivate(ToplevelHandle)
```

**Benefits:**
- Aligns with WPF/WinForms patterns
- "Activate/Deactivate" are familiar GUI concepts
- "EventLoop" is industry standard terminology

## Proposed (Clear) - Option 3: Lifecycle

```
Application.Run(toplevel)
  ├─ Application.Start(toplevel) → ExecutionContext
  ├─ Application.Execute(ExecutionContext)
  │   └─ Application.Tick()
  └─ Application.Stop(ExecutionContext)
```

**Benefits:**
- Explicit lifecycle phases (Start → Execute → Stop)
- "Tick" is familiar from game development
- Simple, clear verbs

## Side-by-Side Comparison

| Concept | Current | Option 1 (Session) ⭐ | Option 2 (Modal) | Option 3 (Lifecycle) |
|---------|---------|---------------------|------------------|---------------------|
| Complete lifecycle | `Run()` | `Run()` | `ShowModal()` or `Run()` | `Run()` |
| Session token | `RunState` | `ToplevelSession` | `ToplevelHandle` | `ExecutionContext` |
| Initialize | `Begin()` | `BeginSession()` | `Activate()` | `Start()` |
| Event loop | `RunLoop()` | `ProcessEvents()` | `EventLoop()` | `Execute()` |
| One iteration | `RunIteration()` | `ProcessEventsIteration()` | `ProcessEvents()` | `Tick()` |
| Cleanup | `End()` | `EndSession()` | `Deactivate()` | `Stop()` |
| Stop loop | `RequestStop()` | `StopProcessingEvents()` | `Close()` or `RequestStop()` | `RequestStop()` |
| Stop mode flag | `EndAfterFirstIteration` | `StopAfterFirstIteration` | `SingleIteration` | `StopAfterFirstTick` |

## Usage Examples

### High-Level (All Options - Unchanged)

```csharp
// Simple case - most users use this
Application.Init();
Application.Run(myWindow);
Application.Shutdown();
```

### Low-Level - Current (Confusing)

```csharp
Application.Init();

RunState runState = Application.Begin(myWindow);  // Begin what?
Application.RunLoop(runState);                     // Run vs RunLoop?
Application.End(runState);                         // End what?

Application.Shutdown();
```

### Low-Level - Option 1: Session-Based ⭐

```csharp
Application.Init();

ToplevelSession session = Application.BeginSession(myWindow);  // Clear: starting a session
Application.ProcessEvents(session);                             // Clear: processing events
Application.EndSession(session);                                // Clear: ending the session

Application.Shutdown();
```

### Low-Level - Option 2: Modal/Show

```csharp
Application.Init();

ToplevelHandle handle = Application.Activate(myWindow);  // Clear: activating for display
Application.EventLoop(handle);                           // Clear: running event loop
Application.Deactivate(handle);                          // Clear: deactivating

Application.Shutdown();
```

### Low-Level - Option 3: Lifecycle

```csharp
Application.Init();

ExecutionContext context = Application.Start(myWindow);  // Clear: starting execution
Application.Execute(context);                            // Clear: executing
Application.Stop(context);                               // Clear: stopping

Application.Shutdown();
```

## Manual Event Loop Control

### Current (Confusing)

```csharp
RunState rs = Application.Begin(myWindow);
Application.EndAfterFirstIteration = true;

while (!done)
{
    Application.RunIteration(ref rs, firstIteration);  // What's RunIteration vs RunLoop?
    firstIteration = false;
    // Do custom processing...
}

Application.End(rs);
```

### Option 1: Session-Based (Clear) ⭐

```csharp
ToplevelSession session = Application.BeginSession(myWindow);
Application.StopAfterFirstIteration = true;

while (!done)
{
    Application.ProcessEventsIteration(ref session, firstIteration);  // Clear: process one iteration
    firstIteration = false;
    // Do custom processing...
}

Application.EndSession(session);
```

### Option 2: Modal/Show (Clear)

```csharp
ToplevelHandle handle = Application.Activate(myWindow);
Application.SingleIteration = true;

while (!done)
{
    Application.ProcessEvents(ref handle, firstIteration);  // Clear: process events
    firstIteration = false;
    // Do custom processing...
}

Application.Deactivate(handle);
```

### Option 3: Lifecycle (Clear)

```csharp
ExecutionContext context = Application.Start(myWindow);
Application.StopAfterFirstTick = true;

while (!done)
{
    Application.Tick(ref context, firstIteration);  // Clear: one tick
    firstIteration = false;
    // Do custom processing...
}

Application.Stop(context);
```

## Recommendation: Option 1 (Session-Based)

**Why Session-Based wins:**
1. ✅ Most accurate - "session" perfectly describes bounded execution
2. ✅ Least disruptive - keeps Begin/End pattern, just clarifies it
3. ✅ Most descriptive - "ProcessEvents" is clearer than "RunLoop"
4. ✅ Industry standard - "session" is widely understood in software
5. ✅ Extensible - easy to add session-related features later

**Implementation:**
- Add new APIs alongside existing ones
- Mark old APIs `[Obsolete]` with helpful messages
- Update docs to use new terminology
- Maintain backward compatibility indefinitely

## Related Concepts

### Application Lifecycle

```
Application.Init()           // Initialize the application
  ├─ Create driver
  ├─ Setup screen
  └─ Initialize subsystems

Application.Run(toplevel)    // Run a toplevel (modal)
  ├─ BeginSession
  ├─ ProcessEvents
  └─ EndSession

Application.Shutdown()       // Shutdown the application
  ├─ Cleanup resources
  └─ Restore terminal
```

### Session vs Application Lifecycle

| Application Lifecycle | Session Lifecycle |
|----------------------|-------------------|
| `Init()` - Once per app | `BeginSession()` - Per toplevel |
| `Run()` - Can have multiple | `ProcessEvents()` - Within one session |
| `Shutdown()` - Once per app | `EndSession()` - Per toplevel |

## Events and Notifications

| Current | Proposed (Option 1) |
|---------|---------------------|
| `NotifyNewRunState` | `NotifyNewSession` |
| `NotifyStopRunState` | `NotifyStopSession` |
| `RunStateEventArgs` | `ToplevelSessionEventArgs` |

## See Also

- [Full Proposal Document](TERMINOLOGY_PROPOSAL.md) - Detailed rationale and analysis
- [Migration Guide](docs/migration-guide.md) - How to update your code (TODO)
- [API Reference](docfx/api/Terminal.Gui.App.Application.yml) - API documentation
