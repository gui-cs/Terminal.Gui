# View.NewMouseEvent Rewrite Plan

**Issue**: #4474 - Remove Click Delay and Simplify Mouse Event Handling

**Goal**: Rewrite `View.NewMouseEvent` to align with the mouse behavior specification, removing legacy complexity and fixing the 500ms click delay bug.

**Status**: Planning Phase

---

## Overview

The current `View.NewMouseEvent` implementation has accumulated significant technical debt marked as "LEGACY - Can be rewritten". This rewrite will:

1. Remove unnecessary complexity
2. Align with the specification's clean separation of concerns
3. Fix bugs related to mouse grab and click handling
4. Ensure all tests pass

---

## Current State Analysis

### What Works (Keep)
- Pre-condition validation (enabled, visible checks)
- `RaiseMouseEvent` / `OnMouseEvent` / `MouseEvent` pattern (low-level handler)
- `MouseHoldRepeater` concept (timer-based repetition for continuous press)
- `MouseState` tracking for visual feedback
- Command invocation via `MouseBindings`

### What's Problematic (Fix)
1. **Grab Logic Scattered**: `WhenGrabbedHandlePressed`, `WhenGrabbedHandleReleased`, `WhenGrabbedHandleClicked` mix concerns
2. **Pressed→Clicked Conversion**: Done in multiple places (`ConvertPressedToClicked`, `ConvertReleasedToClicked`)
3. **MouseState Updates**: Mixed into grab handlers instead of being explicit
4. **Flow Unclear**: Hard to follow the execution path
5. **Comments Misleading**: TODOs and questions that need answers

### Dependencies
- `MouseHoldRepeater` - Keep, but verify usage
- `App.Mouse.GrabMouse/UngrabMouse` - Keep interface, simplify calls
- `MouseBindings` - Keep, ensure proper integration
- `MouseState` - Keep, clarify update points

---

## Architecture Principles (From Spec)

### Separation of Concerns

| Concern | Purpose | Implementation |
|---------|---------|----------------|
| **MouseState** | Visual feedback | Updated on press/release/enter/leave |
| **Mouse Grab** | Capture all events until release | Auto-grab on press if `MouseHighlightStates` or `MouseHoldRepeat` |
| **Commands** | Execute actions | Invoked via `MouseBindings` on clicked events |
| **MouseHoldRepeat** | Timer-based repetition | Independent of click logic |

### Key Behaviors

1. **Auto-Grab**: When `MouseHighlightStates != None` OR `MouseHoldRepeat == true`
   - Grab on first button press
   - Ungrab on button release (clicked event)
   - Set focus if `CanFocus`

2. **MouseState Management**:
   - `In` - Mouse over viewport (set by MouseEnter/Leave)
   - `Pressed` - Button down while over viewport
   - `PressedOutside` - Button down, mouse moved outside (only if `!MouseHoldRepeat`)

3. **Click Conversion**:
   - Driver emits: `Pressed`, `Released`, `Clicked` (from MouseInterpreter)
   - View only needs to handle `Clicked` events for commands
   - Pressed/Released are for grab lifecycle

4. **Command Invocation**:
   - `Clicked` events → `Command.Accept` (default binding)
   - `Pressed` events → `Command.Activate` (current default, may need review)
   - Wheel events → bound commands

---

## Rewrite Plan - Phased Approach

### Phase 1: Preparation & Analysis ✅
**Goal**: Understand current behavior, identify all tests

**Tasks**:
- [x] Review specification documents
- [x] Analyze current `View.NewMouseEvent` implementation
- [x] Identify all tests marked `Skip = "Broken in #4474"`
- [x] Create this plan document
- [x] Run all tests to establish baseline
- [x] Document baseline test results

**Deliverables**:
- This plan document
- Baseline test results: **382 tests passing, 23 tests skipped**
- List of all affected tests

**Baseline Results**:
- UnitTestsParallelizable/ViewBase/Mouse/MouseTests.cs: 3 skipped tests
- Total across all projects: 23 skipped tests marked with `Skip = "Broken in #4474"`
- All other mouse tests passing

### Phase 2: Simplify Mouse Grab Logic ✅
**Goal**: Consolidate scattered grab logic into clear, linear flow

**Tasks**:
- [x] Extract `ShouldAutoGrab` property: `MouseHighlightStates != None || MouseHoldRepeat`
- [x] Create `HandleAutoGrabPress(mouse)`: Grab, set focus, update MouseState
- [x] Create `HandleAutoGrabRelease(mouse)`: Update MouseState only
- [x] Create `HandleAutoGrabClicked(mouse)`: Ungrab
- [x] Create `UpdateMouseStateOnPress(position)`: Explicit state management
- [x] Create `UpdateMouseStateOnRelease()`: Explicit state management
- [x] Update `NewMouseEvent` to use new helpers
- [x] Remove `WhenGrabbedHandlePressed`, `WhenGrabbedHandleReleased`, `WhenGrabbedHandleClicked`
- [x] Run tests, verify no regressions

**Results**:
- ✅ All 382 tests still passing
- ✅ Code is now more linear and easier to read
- ✅ MouseState management is explicit and documented
- ✅ Grab lifecycle is consolidated in dedicated helper methods
- ✅ `NewMouseEvent` has clear numbered sections (1-6)

**Acceptance Criteria**:
- ✅ All grab-related tests pass
- ✅ Focus setting tests pass
- ✅ MouseState tests pass

### Phase 3: Clean Up Click Conversion
**Goal**: Remove redundant click conversion logic

**Current Issues**:
- `ConvertPressedToClicked()` - Should not be needed if MouseInterpreter works
- `ConvertReleasedToClicked()` - Only used in grab release scenario

**Analysis Needed**:
- Does MouseInterpreter emit `Clicked` events immediately after `Released`?
- If yes, remove conversion logic entirely
- If no, fix MouseInterpreter first (separate task)

**Proposed Approach**:
```csharp
// Option A: MouseInterpreter emits Clicked (spec says it should)
// -> Remove all ConvertXXXToClicked methods
// -> Bindings work with Clicked events directly

// Option B: We need conversion for grab scenario
// -> Keep only ConvertReleasedToClicked in HandleAutoGrabRelease
// -> Document why it's needed
```

**Tasks**:
- [ ] Verify MouseInterpreter behavior (does it emit Clicked after Released?)
- [ ] If yes: Remove `ConvertPressedToClicked` and `ConvertReleasedToClicked`
- [ ] If no: File bug, keep minimal conversion in grab handler
- [ ] Update `RaiseCommandsBoundToButtonClickedFlags` to not do conversion
- [ ] Run tests, verify command invocation still works

**Acceptance Criteria**:
- Commands are invoked correctly
- No redundant conversions
- Tests for Accepting/Activating pass

### Phase 4: Clarify MouseState Updates
**Goal**: Make MouseState updates explicit and obvious

**Current Issues**:
- MouseState updates scattered across grab handlers
- Unclear when `Pressed` vs `PressedOutside` is set
- Missing documentation

**Proposed Approach**:
```csharp
private void UpdateMouseStateOnPress(Point position)
{
    if (Viewport.Contains(position))
    {
        if (MouseHighlightStates.HasFlag(MouseState.Pressed))
        {
            MouseState |= MouseState.Pressed;
        }
        MouseState &= ~MouseState.PressedOutside;
    }
    else
    {
        if (MouseHighlightStates.HasFlag(MouseState.PressedOutside) && !MouseHoldRepeat)
        {
            MouseState |= MouseState.PressedOutside;
        }
    }
}

private void UpdateMouseStateOnRelease()
{
    MouseState &= ~MouseState.Pressed;
    MouseState &= ~MouseState.PressedOutside;
}
```

**Tasks**:
- [ ] Extract `UpdateMouseStateOnPress(position)` method
- [ ] Extract `UpdateMouseStateOnRelease()` method
- [ ] Call from appropriate points in grab handlers
- [ ] Add XML documentation explaining each state
- [ ] Run tests, verify visual behavior

**Acceptance Criteria**:
- MouseState changes are explicit and documented
- Visual highlight tests pass
- Border pressed tests pass

### Phase 5: Streamline Command Invocation
**Goal**: Ensure command invocation is clean and efficient

**Current State**:
```csharp
RaiseCommandsBoundToButtonClickedFlags()
  -> ConvertPressedToClicked()  // Should not be needed
  -> InvokeCommandsBoundToMouse()

RaiseCommandsBoundToWheelFlags()
  -> InvokeCommandsBoundToMouse()
```

**Proposed Simplification**:
```csharp
// Only invoke commands for:
// 1. Clicked events (single, double, triple)
// 2. Wheel events
// Pressed/Released are for grab lifecycle only
```

**Tasks**:
- [ ] Review default bindings in `SetupMouse()`
- [ ] Verify `Pressed` bindings are needed (spec says commands on `Clicked`)
- [ ] Remove `Pressed` bindings if not needed
- [ ] Ensure `RaiseCommandsBoundToButtonClickedFlags` only handles clicks
- [ ] Run tests for command invocation

**Acceptance Criteria**:
- Default bindings align with spec
- Commands invoked at correct times
- No duplicate command invocations

### Phase 6: Rewrite NewMouseEvent Method
**Goal**: Implement clean, linear flow using helper methods from phases 2-5

**Proposed Structure**:
```csharp
public bool? NewMouseEvent(Mouse mouse)
{
    // 1. Pre-conditions
    if (!ValidatePreConditions(mouse))
    {
        return false;
    }

    // 2. Setup (legacy MouseHoldRepeater initialization)
    EnsureMouseHoldRepeaterInitialized();

    // 3. MouseHoldRepeat timer management
    if (MouseHoldRepeat)
    {
        ManageMouseHoldRepeatTimer(mouse);
    }

    // 4. Low-level MouseEvent (cancellable)
    if (RaiseMouseEvent(mouse) || mouse.Handled)
    {
        return true;
    }

    // 5. Auto-grab lifecycle
    if (ShouldAutoGrab)
    {
        if (HandleAutoGrabLifecycle(mouse))
        {
            return mouse.Handled;
        }
    }

    // 6. Command invocation
    if (mouse.IsSingleDoubleOrTripleClicked)
    {
        return RaiseCommandsBoundToButtonClickedFlags(mouse);
    }

    if (mouse.IsWheel)
    {
        return RaiseCommandsBoundToWheelFlags(mouse);
    }

    return false;
}
```

**Tasks**:
- [ ] Implement `ValidatePreConditions(mouse)`
- [ ] Implement `EnsureMouseHoldRepeaterInitialized()`
- [ ] Implement `ManageMouseHoldRepeatTimer(mouse)`
- [ ] Implement `ShouldAutoGrab` property
- [ ] Implement `HandleAutoGrabLifecycle(mouse)` using phase 2-4 helpers
- [ ] Update `RaiseCommandsBoundToButtonClickedFlags` (remove conversion)
- [ ] Add comprehensive XML documentation
- [ ] Run all tests

**Acceptance Criteria**:
- Method is easy to read and understand
- Each section has clear purpose
- All existing tests pass
- No new bugs introduced

### Phase 7: Fix Skipped Tests
**Goal**: Update skipped tests to work with new implementation

**Known Skipped Tests**:
```csharp
// Tests/UnitTestsParallelizable/ViewBase/Mouse/MouseTests.cs
[Fact(Skip = "Broken in #4474")]
public void MouseClick_OnSubView_RaisesSelectingEvent()

[Fact(Skip = "Broken in #4474")]
public void MouseClick_RaisesSelecting_WhenCanFocus()

[Theory(Skip = "Broken in #4474")]
public void MouseClick_SetsFocus_If_CanFocus()
```

**Tasks**:
- [ ] Search all test projects for `Skip = "Broken in #4474"`
- [ ] Catalog all skipped tests
- [ ] Analyze each test's expectations
- [ ] Update tests to match new behavior (if spec changed)
- [ ] Fix implementation if test expectations are correct
- [ ] Remove `Skip` attributes
- [ ] Verify all tests pass

**Acceptance Criteria**:
- No tests marked `Skip = "Broken in #4474"`
- All mouse-related tests pass
- Test coverage maintained or improved

### Phase 8: Add New Tests
**Goal**: Ensure comprehensive test coverage for new implementation

**Test Scenarios from Spec**:

1. **Normal Button (MouseHoldRepeat = false)**
   - [ ] Single click (press + immediate release) → 1 Accept
   - [ ] Press and hold (2+ seconds) → 1 Accept on release
   - [ ] Double-click → 2 Accepts with ClickCount 1, 2
   - [ ] Triple-click → 3 Accepts with ClickCount 1, 2, 3

2. **Repeat Button (MouseHoldRepeat = true)**
   - [ ] Single click → 1 Accept
   - [ ] Press and hold → 10+ Accepts (timer-based)
   - [ ] Double-click → 2 Accepts (timer doesn't start)
   - [ ] Hold then quick click → many + 1 Accept

3. **MouseState Transitions**
   - [ ] Enter → `In` flag set
   - [ ] Press inside → `Pressed` flag set
   - [ ] Move outside while pressed → `PressedOutside` set (if !MouseHoldRepeat)
   - [ ] Release → flags cleared

4. **Mouse Grab**
   - [ ] Press inside → auto-grab, set focus
   - [ ] Release inside → ungrab, invoke commands
   - [ ] Release outside → ungrab, no commands

**Tasks**:
- [ ] Write test for each scenario
- [ ] Use parallelizable test base class
- [ ] Follow existing test patterns
- [ ] Add `// CoPilot - AI Generated` comments
- [ ] Verify all new tests pass

**Acceptance Criteria**:
- All spec scenarios have test coverage
- Tests are parallelizable
- Tests follow project conventions

### Phase 9: Documentation & Cleanup
**Goal**: Update documentation and remove obsolete code

**Tasks**:
- [ ] Update XML docs in `View.Mouse.cs`
- [ ] Remove all "LEGACY" comments
- [ ] Remove all "TODO" and "QUESTION" comments (address or file issues)
- [ ] Update `mouse.md` if behavior changed
- [ ] Update `mouse-behavior-specification.md` to mark implemented
- [ ] Add code examples to documentation
- [ ] Run spell check on comments

**Acceptance Criteria**:
- No "LEGACY" comments remain
- All TODOs addressed or converted to issues
- Documentation is accurate and helpful
- Examples compile and run

### Phase 10: Final Validation
**Goal**: Ensure everything works end-to-end

**Tasks**:
- [ ] Run all unit tests (UnitTests + UnitTestsParallelizable)
- [ ] Run all integration tests
- [ ] Run UICatalog, test mouse behavior manually
- [ ] Test Button scenarios from spec
- [ ] Test ListView scenarios from spec
- [ ] Test Dialog/MessageBox scenarios
- [ ] Check for performance regressions
- [ ] Run with different terminals (if applicable)

**Acceptance Criteria**:
- All tests pass (100%)
- No performance regressions
- UICatalog behaves correctly
- No unexpected visual changes

---

## Success Criteria

### Must Have
- [x] All existing tests pass (baseline established)
- [ ] All skipped tests fixed and passing
- [ ] `NewMouseEvent` is clear and maintainable
- [ ] Grab logic is consolidated
- [ ] MouseState updates are explicit
- [ ] Command invocation is clean
- [ ] Comprehensive test coverage

### Nice to Have
- [ ] Improved performance
- [ ] Better XML documentation
- [ ] Code examples in docs
- [ ] Reduced code complexity metrics

### Risks & Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| Breaking existing behavior | High | Phased approach, run tests after each change |
| MouseInterpreter needs changes | Medium | Verify early, coordinate changes |
| Tests are wrong, not code | Medium | Review spec carefully, discuss with team |
| Performance regression | Low | Benchmark critical paths |

---

## Notes

- **Coding Standards**: Follow CONTRIBUTING.md strictly
  - Use explicit types (no `var` except built-ins)
  - Use target-typed `new ()`
  - Format with ReSharper
  - Add XML docs to public APIs

- **Testing Strategy**:
  - Run tests after each phase
  - Fix failures immediately
  - Don't accumulate technical debt

- **Communication**:
  - Update this plan as work progresses
  - Mark completed phases with ✅
  - Document decisions and rationale

---

## Timeline Estimate

| Phase | Estimated Time | Dependencies |
|-------|---------------|--------------|
| 1. Preparation | 1 hour | None |
| 2. Simplify Grab | 2 hours | Phase 1 |
| 3. Click Conversion | 1 hour | Phase 2 |
| 4. MouseState | 1 hour | Phase 2 |
| 5. Command Invocation | 1 hour | Phase 3 |
| 6. Rewrite NewMouseEvent | 2 hours | Phases 2-5 |
| 7. Fix Skipped Tests | 2 hours | Phase 6 |
| 8. Add New Tests | 2 hours | Phase 6 |
| 9. Documentation | 1 hour | Phase 8 |
| 10. Final Validation | 1 hour | Phase 9 |
| **Total** | **14 hours** | |

---

## Appendix: Code Patterns

### Helper Method Pattern
```csharp
/// <summary>
/// Brief description.
/// </summary>
/// <param name="paramName">Description.</param>
/// <returns>Description.</returns>
private bool HelperMethod(Mouse mouse)
{
    // Implementation
    return false;
}
```

### Property Pattern
```csharp
/// <summary>
/// Gets whether auto-grab should be enabled for this view.
/// </summary>
private bool ShouldAutoGrab => MouseHighlightStates != MouseState.None || MouseHoldRepeat;
```

### Test Pattern
```csharp
// CoPilot - AI Generated
[Fact]
public void NewMouseEvent_Press_AutoGrabs_WhenMouseHighlightStatesSet()
{
    // Arrange
    View view = new ()
    {
        Width = 10,
        Height = 10,
        MouseHighlightStates = MouseState.Pressed
    };

    // Act
    view.NewMouseEvent(new ()
    {
        Position = new (5, 5),
        Flags = MouseFlags.LeftButtonPressed
    });

    // Assert
    Assert.Equal(view, view.App?.Mouse.MouseGrabView);
}
```

---

**Last Updated**: 2025-01-21
**Author**: GitHub Copilot
**Status**: Ready for Phase 1 execution
