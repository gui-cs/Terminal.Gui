# Command Manager Exploration

**Branch**: `copilot/4620-command-manager-exploration`  
**Worktree**: `C:\Users\Tig\s\gui-cs\4620-command-manager`  
**Base Branch**: `copilot/fix-command-propagation-issue-clean` (from 4620)  
**Created**: 2026-02-21

## Goal

Explore refactoring View.Command.cs for better:
1. **Encapsulation** - Command routing logic separated from View concerns
2. **Isolated Testability** - Test command flow without full View hierarchy
3. **Clean Design** - Single Responsibility Principle, clearer abstractions
4. **Controllable Logging** - Event flow logging without embedded `Logging.Debug()` statements

## Context from command-system-redesign-requirements.md

The Phase A-E redesign (documented in `command-system-redesign-requirements.md`) is largely complete with some deviations:

### Completed
- **Phase A** (Foundation): `CommandOutcome`, `CommandRouting`, immutable `CommandContext`, `WeakReference` fix
- **Phase B** (Dispatch): `GetDispatchTarget()`, `ConsumeDispatch` pattern
- **Phase C** (Bridge): `CommandBridge` for cross-boundary routing
- **Phase E**: ✅ Route tracing now implemented via `CommandTrace` infrastructure

### Deferred
- **Phase D**: 267 `AddCommand` call sites need `bool?` → `CommandOutcome` migration

### Current Pain Points from the Code

Looking at `View.Command.cs` (1030 lines):

1. **Embedded Logging** - Multiple commented-out `Logging.Debug()` statements:
   - Line 168: `InvokeCommand` - execution tracing
   - Line 215: `RaiseCommandNotBound` - event flow
   - Line 252: `DefaultAcceptHandler` - handler entry
   - Line 298: `DefaultAcceptHandler` - pre-RaiseAccepted
   - Lines 340, 345, 351: `RaiseAccepting` - event propagation chain
   - Line 420: `RaiseAccepted` - completion
   - Line 476, 485: `RaiseActivating` - event flow
   - Line 530: `RaiseActivated` - completion
   - Line 669, 677: `RaiseHandlingHotKey` - HotKey flow
   - Line 800: `TryDispatchToTarget` - dispatch logic

   **Pattern**: Debug statements commented out because they're too noisy during normal debugging, but needed when debugging command flow issues.

2. **Mixed Responsibilities** - `View.Command.cs` handles:
   - Command registration (`AddCommand`, `_commandImplementations` dictionary)
   - Command invocation (`InvokeCommand` overloads)
   - Event raising (CWP pattern: OnXxxing → Xxxing → OnXxxed → Xxxed)
   - Routing logic (bubble up, dispatch down, bridge)
   - Default handlers for each command type
   - Dispatch pattern support (`GetDispatchTarget`, `ConsumeDispatch`)

3. **Testing Challenges** - Command flow tests require:
   - Full View hierarchy setup
   - Application.Init for focus
   - Mocking SuperView/SubView relationships
   - Complex state setup to trigger specific routing paths

## Exploration Ideas

### 1. CommandRouter / CommandManager Class

Extract routing logic into a separate class that can be tested independently:

```csharp
internal class CommandRouter
{
    private readonly View _owner;
    private readonly Dictionary<Command, CommandImplementation> _implementations;
    
    public bool? Route(Command command, ICommandContext ctx)
    {
        // Current logic from InvokeCommand
        // Returns execution result
    }
    
    public bool TryBubbleUp(ICommandContext ctx) { }
    public bool TryDispatchDown(View target, ICommandContext ctx) { }
    public bool IsSourceWithinView(View target, ICommandContext ctx) { }
}
```

**Benefits**:
- Test routing without View infrastructure
- Inject mock router for testing View behavior
- Clearer separation of concerns

**Challenges**:
- Router needs access to View state (SuperView, SubViews, CommandsToBubbleUp)
- Breaking change to View's protected API surface
- Risk of over-abstraction

### 2. Route Tracing System (Phase E completion)

Implement the planned `TraceRoute` infrastructure with a pluggable tracing backend:

```csharp
[Conditional("DEBUG")]
internal static void TraceRoute(
    View view,
    Command command,
    CommandRouting routing,
    CommandOutcome outcome,
    string phase)
{
    CommandTrace.Log(new RouteEntry {
        View = view.ToIdentifyingString(),
        Command = command,
        Routing = routing,
        Outcome = outcome,
        Phase = phase,
        Timestamp = DateTime.UtcNow
    });
}

// Pluggable backend
public static class CommandTrace
{
    public static ICommandTraceBackend Backend { get; set; } = new NullBackend();
    
    internal static void Log(RouteEntry entry) => Backend.Log(entry);
}

public interface ICommandTraceBackend
{
    void Log(RouteEntry entry);
}

// Implementations:
// - NullBackend (default, no-op)
// - ConsoleBackend (Logging.Debug)
// - TestCaptureBackend (captures for assertions)
```

**Benefits**:
- Remove all commented `Logging.Debug()` statements
- Enable/disable tracing at runtime via backend swap
- Test assertions against captured trace
- No performance impact in release builds (`[Conditional("DEBUG")]`)

**Challenges**:
- Need to identify all trace points in command flow
- Adds API surface area
- Need to document when/how to use

### 3. Event Pipeline / Command Middleware Pattern

Model command processing as a pipeline with discrete stages:

```csharp
internal class CommandPipeline
{
    private readonly List<ICommandStage> _stages = new();
    
    public void AddStage(ICommandStage stage) => _stages.Add(stage);
    
    public bool? Execute(Command command, ICommandContext ctx)
    {
        var context = new PipelineContext(command, ctx);
        
        foreach (var stage in _stages)
        {
            stage.Execute(context);
            if (context.IsHandled) break;
        }
        
        return context.Result;
    }
}

public interface ICommandStage
{
    void Execute(PipelineContext context);
}

// Stages:
// - ValidationStage (check if command is supported)
// - PreviewStage (OnXxxing virtual + event)
// - DispatchStage (GetDispatchTarget + DispatchDown)
// - BubbleStage (TryBubbleUp)
// - ExecuteStage (raise Xxxed events)
```

**Benefits**:
- Each stage is independently testable
- Clear ordering and flow
- Easy to add new stages (e.g., tracing, validation)
- Stages can be composed/configured per-view-type

**Challenges**:
- Significant refactoring required
- Performance overhead (foreach, interface dispatch)
- May be over-engineering for current needs
- Breaking change

### 4. Minimal: Just Add Route Tracing (Recommended Starting Point) ✅ IMPLEMENTED

Keep existing architecture, add only the tracing system from Idea #2:

1. ✅ Implement `CommandTrace` + backends
2. ✅ Add `TraceRoute()` calls at key points:
   - Entry to `InvokeCommand`
   - Entry/exit of `RaiseXxxing` / `RaiseXxxed`
   - Entry/exit of `TryDispatchToTarget`
   - Entry/exit of `TryBubbleUp`
   - Entry/exit of `DispatchDown`
3. ✅ Replace all commented `Logging.Debug()` with `TraceRoute()`
4. ✅ Add tests using `ListBackend` to verify routing paths

**Files Added:**
- `Terminal.Gui/Input/CommandTrace.cs` - Core infrastructure with:
  - `RouteTraceEntry` record for capturing trace data
  - `CommandTracePhase` enum (Entry, Exit, Routing, Event, Handler)
  - `ICommandTraceBackend` interface
  - `NullBackend` (default no-op)
  - `LoggingBackend` (forwards to `Logging.Debug`)
  - `ListBackend` (captures for testing)
- `Tests/UnitTestsParallelizable/Input/CommandTraceTests.cs` - 10 unit tests

**Files Modified:**
- `Terminal.Gui/ViewBase/View.Command.cs` - Replaced 19 commented `Logging.Debug()` calls with `CommandTrace.TraceRoute()`

**Benefits**:
- Minimal change, low risk
- Addresses primary pain point (logging)
- Foundation for future refactoring
- Completes Phase E
- Zero overhead in Release builds (`[Conditional("DEBUG")]`)

**Challenges**:
- Doesn't address encapsulation or testability
- Tracing alone may not be sufficient for complex routing debugging

## Investigation Plan

### Step 1: Understand Current Logging Patterns
- [x] Catalog all commented `Logging.Debug()` statements
- [x] Identify what information they capture
- [x] Determine which are essential vs. noise - All were converted to trace calls

### Step 2: Prototype Route Tracing ✅ COMPLETE
- [x] Implement `CommandTrace` infrastructure
- [x] Add tracing to ALL command flows (not just Activate)
- [x] Create test using `ListBackend`
- [x] Measure impact on code clarity - Improved, trace calls are more descriptive

### Step 3: Evaluate Extraction Feasibility
- [ ] Identify View state accessed by routing logic
- [ ] Determine if extraction to `CommandRouter` is viable
- [ ] Prototype router for one command type
- [ ] Assess testability improvement

### Step 4: Consider Pipeline Pattern
- [ ] Sketch pipeline design for one command
- [ ] Prototype single stage
- [ ] Evaluate complexity vs. benefit

### Step 5: Recommendation
- [ ] Document findings
- [ ] Recommend approach (likely #4 - minimal tracing)
- [ ] Outline implementation plan if recommendation accepted

## Current State Analysis

### View.Command.cs Structure (1030 lines)

```
Lines 1-23:    SetupCommands (default handler registration)
Lines 25-88:   Command Management (AddCommand, GetSupportedCommands)
Lines 91-197:  Invoke (InvokeCommands, InvokeCommand overloads)
Lines 200-246: CommandNotBound (default handler + CWP)
Lines 248-442: Accept (default handler + CWP, DefaultAcceptView logic)
Lines 444-556: Activate (default handler + CWP)
Lines 558-738: HotKey (default handler + CWP)
Lines 742-879: Dispatch (GetDispatchTarget, ConsumeDispatch, TryDispatchToTarget)
Lines 881-946: Bubbling (CommandsToBubbleUp, TryBubbleUp, CommandWillBubbleToAncestor)
Lines 948-1030: DispatchDown, IsSourceWithinView, CommandBridge support
```

### Key Observation

The file is already well-organized with regions. The mixing of concerns isn't in code structure but in *what gets tested together*. Command routing tests need full View setup, making it hard to isolate routing logic bugs from View lifecycle bugs.

## Related Files to Examine

- [ ] `Terminal.Gui/Commands/ICommandContext.cs` - Context interface
- [ ] `Terminal.Gui/Commands/CommandContext.cs` - Immutable context struct
- [ ] `Terminal.Gui/Commands/CommandRouting.cs` - Routing enum
- [ ] `Terminal.Gui/Commands/CommandBridge.cs` - Cross-boundary routing
- [ ] `Tests/UnitTestsParallelizable/ViewBase/ViewCommandTests.cs` - Test patterns
- [ ] `Terminal.Gui/Views/Shortcut.cs` - Relay dispatch pattern
- [ ] `Terminal.Gui/Views/OptionSelector.cs` - Consume dispatch pattern

## Questions to Answer

1. **Logging Control**: Can we replace commented Logging.Debug with a runtime-controlled trace system?
2. **Isolation**: Can we test routing logic without View hierarchy setup?
3. **Encapsulation**: Where should routing logic live? View? Separate class? Extension methods?
4. **Performance**: What's the overhead of tracing/middleware? Is it acceptable?
5. **Migration Path**: If we refactor, can it be incremental? What's the compat story?
6. **Testing ROI**: Will better isolation actually make tests easier to write/maintain?

## Success Criteria

This exploration is successful if:
1. We can demonstrate easier testing of command flow (fewer setup lines)
2. We can enable/disable detailed logging without code changes
3. The solution is maintainable (not over-engineered)
4. The approach is compatible with existing Phase A-E work
5. There's a clear migration path if adopted

## Next Steps

1. Examine existing test patterns in ViewCommandTests.cs
2. Prototype minimal route tracing (Idea #4)
3. Compare before/after test code complexity
4. Document findings and recommendation
