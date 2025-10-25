# Application.Run Terminology Proposal

## Executive Summary

This document proposes improved terminology for the Terminal.Gui application execution lifecycle. The current `Run` terminology is overloaded and confusing, encompassing multiple distinct concepts. This proposal introduces clearer, more precise naming that better communicates the purpose and relationships of each API.

## Problem Statement

The current terminology around `Application.Run` is confusing because:

1. **"Run" is overloaded** - It refers to both:
   - The complete lifecycle (Begin → RunLoop → End → Stop)
   - The event loop itself (RunLoop)
   - Multiple API methods (Run, RunLoop, RunIteration)

2. **Relationships are unclear** - Users don't understand that:
   - `Run()` is a convenience method = `Begin()` + `RunLoop()` + `End()`
   - `RunState` is a session token, not a state object
   - `RequestStop()` affects `RunLoop()` which triggers `End()`

3. **Inconsistent with industry patterns** - Other frameworks use clearer terms:
   - WPF: `Show()`, `ShowDialog()`, `Close()`
   - WinForms: `Show()`, `ShowDialog()`, `Application.Run()`
   - Avalonia: `Show()`, `ShowDialog()`, `StartWithClassicDesktopLifetime()`

## Current Terminology Analysis

### Current APIs and Their Actual Purposes

| Current Name | Actual Purpose | Confusion Point |
|-------------|----------------|-----------------|
| `Run()` | Complete lifecycle: Begin + Loop + End | Overloaded - means too many things |
| `RunState` | Session token/handle for a Toplevel execution | Sounds like state data, not a handle |
| `Begin()` | Initialize and prepare a Toplevel for execution | "Begin" what? Begin running? |
| `RunLoop()` | Execute the event loop until stopped | Clear, but tied to "Run" |
| `RunIteration()` | Execute one iteration of the event loop | Clear |
| `End()` | Clean up after a Toplevel execution session | "End" what? End running? |
| `RequestStop()` | Signal the event loop to stop | What does it stop? |
| `EndAfterFirstIteration` | Exit after one loop iteration | Tied to "End" but affects loop |

### Current Flow

```
Application.Run(toplevel)
  └─> Application.Begin(toplevel) → returns RunState
      └─> Initialize
      └─> Layout
      └─> Draw
  └─> Application.RunLoop(runState)
      └─> while (Running)
          └─> Application.RunIteration()
              └─> Process events
              └─> Layout (if needed)
              └─> Draw (if needed)
  └─> Application.End(runState)
      └─> Clean up
      └─> Dispose RunState
```

## Proposed Terminology

### Option 1: Session-Based Terminology (Recommended)

This option emphasizes that running a Toplevel is a "session" with clear lifecycle management.

| Current | Proposed | Rationale |
|---------|----------|-----------|
| `Run()` | `Run()` | Keep for backward compatibility and familiarity |
| `RunState` | `ToplevelSession` | Clear that it's a session token, not state |
| `Begin()` | `BeginSession()` | Clear what is beginning |
| `RunLoop()` | `ProcessEvents()` | Describes what it does, not abstract "run" |
| `RunIteration()` | `ProcessEventsIteration()` | Consistent with ProcessEvents |
| `End()` | `EndSession()` | Clear what is ending |
| `RequestStop()` | `StopProcessingEvents()` | Clear what stops |
| `EndAfterFirstIteration` | `StopAfterFirstIteration` | Consistent with Stop terminology |

**Usage Example:**
```csharp
// High-level (unchanged)
Application.Run(myWindow);

// Low-level (new names)
ToplevelSession session = Application.BeginSession(myWindow);
Application.ProcessEvents(session);
Application.EndSession(session);
```

### Option 2: Modal/Show Terminology

This option aligns with WPF/WinForms patterns, emphasizing the modal/non-modal nature.

| Current | Proposed | Rationale |
|---------|----------|-----------|
| `Run()` | `ShowModal()` or `Run()` | Emphasizes modal nature, or keep Run |
| `RunState` | `ToplevelHandle` | It's a handle/token for the execution |
| `Begin()` | `Activate()` | Activates the Toplevel for display |
| `RunLoop()` | `EventLoop()` | Standard terminology |
| `RunIteration()` | `ProcessEvents()` | Processes one iteration of events |
| `End()` | `Deactivate()` | Deactivates the Toplevel |
| `RequestStop()` | `Close()` or `RequestStop()` | Familiar to GUI developers |
| `EndAfterFirstIteration` | `SingleIteration` | Mode rather than action |

**Usage Example:**
```csharp
// High-level
Application.ShowModal(myWindow);

// Low-level
ToplevelHandle handle = Application.Activate(myWindow);
while (!stopped)
    Application.ProcessEvents(handle);
Application.Deactivate(handle);
```

### Option 3: Lifecycle Terminology

This option uses explicit lifecycle phases.

| Current | Proposed | Rationale |
|---------|----------|-----------|
| `Run()` | `Run()` | Keep for compatibility |
| `RunState` | `ExecutionContext` | It's a context for execution |
| `Begin()` | `Start()` | Lifecycle: Start → Execute → Stop |
| `RunLoop()` | `Execute()` | The execution phase |
| `RunIteration()` | `Tick()` | Common game/event loop term |
| `End()` | `Stop()` | Lifecycle: Start → Execute → Stop |
| `RequestStop()` | `RequestStop()` | Keep, it's clear |
| `EndAfterFirstIteration` | `StopAfterFirstTick` | Consistent with Tick |

**Usage Example:**
```csharp
// High-level (unchanged)
Application.Run(myWindow);

// Low-level
ExecutionContext context = Application.Start(myWindow);
Application.Execute(context);
Application.Stop(context);
```

## Recommendation: Option 1 (Session-Based)

**Recommended choice:** Option 1 - Session-Based Terminology

**Reasons:**

1. **Accuracy** - "Session" accurately describes what's happening: a bounded period of execution for a Toplevel
2. **Clarity** - "BeginSession/EndSession" are unambiguous pairs
3. **ProcessEvents** - Clearly communicates what the loop does
4. **Minimal conceptual shift** - The pattern is still Begin/Loop/End, just clearer
5. **Extensibility** - "Session" can encompass future session-related features

**Migration Strategy:**

1. **Phase 1: Add new APIs with Obsolete attributes on old ones**
   ```csharp
   [Obsolete("Use BeginSession instead")]
   public static RunState Begin(Toplevel toplevel)
   
   public static ToplevelSession BeginSession(Toplevel toplevel)
   ```

2. **Phase 2: Update documentation to use new terminology**
3. **Phase 3: Update examples to use new APIs**
4. **Phase 4: After 2-3 releases, consider removing obsolete APIs**

## Alternative Names Considered

### For RunState/ToplevelSession
- `ToplevelToken` - Too focused on the token aspect
- `ToplevelHandle` - C/Win32 feel, less modern
- `ExecutionSession` - Too generic
- `ToplevelContext` - Could work, but "context" is overloaded in .NET
- `ToplevelExecution` - Sounds like a verb, not a noun

### For Begin/BeginSession
- `StartSession` - Could work, but Begin/End is a common .NET pattern
- `OpenSession` - Open/Close works but less common for this use
- `InitializeSession` - Too long

### For RunLoop/ProcessEvents
- `EventLoop` - Good, but sounds like a noun not a verb
- `PumpEvents` - Win32 terminology, might work
- `HandleEvents` - Similar to ProcessEvents
- `MainLoop` - Confusing with MainLoop class

### For End/EndSession
- `CloseSession` - Could work with OpenSession
- `FinishSession` - Less common
- `TerminateSession` - Too harsh/formal

## Documentation Changes Required

1. **API Documentation**
   - Update XML docs for all affected methods
   - Add clear examples showing lifecycle
   - Document the relationship between high-level `Run()` and low-level session APIs

2. **Conceptual Documentation**
   - Create "Application Lifecycle" documentation page
   - Add diagrams showing the flow
   - Explain when to use `Run()` vs. low-level APIs

3. **Migration Guide**
   - Create mapping table (old → new)
   - Provide before/after code examples
   - Explain the rationale for changes

## Implementation Notes

### Backward Compatibility

- All existing APIs remain functional
- Mark old APIs with `[Obsolete]` attributes
- Provide clear upgrade path in obsolete messages
- Consider keeping old APIs indefinitely with internal delegation to new ones

### Internal Implementation

- New APIs can delegate to existing implementation
- Gradually refactor internals to use new terminology
- Update variable names and comments to use new terms

### Testing

- Keep all existing tests working
- Add new tests using new terminology
- Test obsolete warnings work correctly

## Comparison with Other Frameworks

| Framework | Show View | Modal | Event Loop | Close |
|-----------|-----------|-------|------------|-------|
| **WPF** | `Show()` | `ShowDialog()` | `Dispatcher.Run()` | `Close()` |
| **WinForms** | `Show()` | `ShowDialog()` | `Application.Run()` | `Close()` |
| **Avalonia** | `Show()` | `ShowDialog()` | `Start()` | `Close()` |
| **GTK** | `show()` | `run()` | `main()` | `close()` |
| **Terminal.Gui v2 (current)** | `Run()` | `Run()` | `RunLoop()` | `RequestStop()` |
| **Terminal.Gui v2 (proposed)** | `Run()` | `Run()` | `ProcessEvents()` | `StopProcessingEvents()` |

## FAQ

**Q: Why not just keep "Run"?**
A: "Run" is too overloaded. It doesn't distinguish between the complete lifecycle and the event loop, leading to confusion about what `RunLoop`, `RunState`, and `RunIteration` mean.

**Q: Why "Session" instead of "Context" or "Handle"?**
A: "Session" best captures the bounded execution period. "Context" is overloaded in .NET (DbContext, HttpContext, etc.). "Handle" is too low-level and platform-specific.

**Q: What about breaking existing code?**
A: We maintain complete backward compatibility by keeping old APIs and using `[Obsolete]` attributes. Users can migrate at their own pace.

**Q: Is this bikeshedding?**
A: No. Clear terminology is essential for framework usability. The current confusion around "Run" causes real problems for users learning the framework.

**Q: Why not align exactly with WPF/WinForms?**
A: Terminal.Gui has a different model - it exposes the event loop explicitly, which WPF/WinForms don't. We need terminology that fits our model while learning from established patterns.

## Conclusion

The proposed Session-Based terminology clarifies the Application execution lifecycle while maintaining backward compatibility. The new names are:

- **More descriptive** - `ToplevelSession` vs `RunState`, `ProcessEvents` vs `RunLoop`
- **More consistent** - `BeginSession`/`EndSession` pair, `ProcessEvents`/`StopProcessingEvents` pair
- **More familiar** - Aligns with common patterns while respecting Terminal.Gui's unique architecture
- **More maintainable** - Clear naming reduces cognitive load for contributors

The migration path is straightforward, with minimal disruption to existing users.

---

## Next Steps

1. **Community Feedback** - Gather feedback on this proposal from maintainers and community
2. **Refinement** - Adjust terminology based on feedback
3. **Implementation Plan** - Create detailed implementation plan with milestones
4. **Documentation** - Prepare comprehensive documentation updates
5. **Migration** - Implement changes with proper obsolete warnings and guidance
