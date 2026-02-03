# Testing Plan for Issue #4674: MouseBinding for xxxReleased Flags

## Issue Summary

`MouseBinding` for `xxxReleased` mouse flags is not working because `HandleAutoGrabRelease` returns `true` when `MouseHoldRepeat` is null, preventing command invocation.

**Current behavior (View.Mouse.cs:603-619):**
```csharp
private bool HandleAutoGrabRelease (Mouse mouse)
{
    if (!mouse.IsReleased)
    {
        return false;
    }

    if (App is null || !App.Mouse.IsGrabbed (this))
    {
        return false;
    }

    // Update MouseState
    UpdateMouseStateOnRelease ();

    return !MouseHoldRepeat.HasValue;  // BUG: Returns true, preventing command invocation
}
```

**Proposed fix:**
```csharp
private bool HandleAutoGrabRelease (Mouse mouse)
{
    if (!mouse.IsReleased)
    {
        return false;
    }

    if (App is null || !App.Mouse.IsGrabbed (this))
    {
        return false;
    }

    // Update MouseState
    UpdateMouseStateOnRelease ();

    if (MouseHoldRepeat != null)
    {
        // Allow command invocation to proceed
        return false;
    }

    return InvokeCommandsBoundToMouse (mouse) is true;  // FIX: Invoke bound commands
}
```

## Current Test Coverage Analysis

### Existing Test Files
1. **MouseBindingTests.cs** - Tests MouseBinding struct (✓ adequate)
2. **MouseBindingsTests.cs** - Tests MouseBindings collection (✓ adequate)
3. **MouseHoldRepeatTests.cs** - Tests MouseHoldRepeat with Released flags (✓ good coverage)
4. **MouseTests.cs** - General mouse event tests (⚠️ missing Released binding tests)
5. **MouseInjectionDocTests.cs** - Shows proper input injection pattern (✓ good reference)

### Coverage Gaps Identified

#### Critical Gap 1: No tests for custom Released bindings without MouseHoldRepeat
**Risk:** High - This is the exact scenario the bug affects

Missing test scenarios:
- Add custom MouseBinding for `LeftButtonReleased` → verify command is invoked
- Add custom MouseBinding for `MiddleButtonReleased` → verify command is invoked
- Add custom MouseBinding for `RightButtonReleased` → verify command is invoked
- Verify Released bindings work when `ShouldAutoGrab` is true (MouseHighlightStates set)
- Verify Released bindings work when `ShouldAutoGrab` is false

#### Critical Gap 2: No tests for Released binding interaction with MouseHighlightStates
**Risk:** Medium - AutoGrab affects Released event handling

Missing test scenarios:
- Released binding with `MouseHighlightStates = MouseState.In`
- Released binding with `MouseHighlightStates = MouseState.Pressed`
- Released binding with `MouseHighlightStates = MouseState.None`
- Verify mouse grab/ungrab lifecycle with Released bindings

#### Critical Gap 3: Edge cases for Released bindings
**Risk:** Medium - Boundary conditions

Missing test scenarios:
- Multiple commands bound to same Released flag
- Released binding when view is disabled
- Released binding when view is not visible
- Press inside, release outside with Released binding

## Test Implementation Plan

### Phase 1: Core Released Binding Tests (Priority: Critical)

Create new test class: **Tests/UnitTestsParallelizable/ViewBase/Mouse/MouseReleasedBindingTests.cs**

Use the standard input injection pattern from MouseInjectionDocTests.cs:
```csharp
VirtualTimeProvider time = new ();
using IApplication app = Application.Create (time);
app.Init (DriverRegistry.Names.ANSI);

IRunnable runnable = new Runnable ();
View view = new () { /* ... */ };
(runnable as View)?.Add (view);
app.Begin (runnable);

// Test code here

(runnable as View)?.Dispose ();
```

#### Test Group 1.1: Basic Released Binding Invocation
```csharp
// Claude - Opus 4.5
[Fact]
public void LeftButtonReleased_CustomBinding_InvokesCommand_WhenMouseHighlightStatesNone()
{
    // Arrange
    VirtualTimeProvider time = new ();
    using IApplication app = Application.Create (time);
    app.Init (DriverRegistry.Names.ANSI);

    IRunnable runnable = new Runnable ();
    View view = new ()
    {
        X = 0, Y = 0,
        Width = 10,
        Height = 10,
        MouseHighlightStates = MouseState.None
    };
    (runnable as View)?.Add (view);
    app.Begin (runnable);

    // Add custom binding for Released
    view.MouseBindings.Add (MouseFlags.LeftButtonReleased, Command.Accept);

    var commandInvoked = false;
    view.InvokingCommand += (s, e) =>
    {
        if (e.Command == Command.Accept)
        {
            commandInvoked = true;
        }
    };

    // Act - Press then Release
    app.InjectMouse (new () { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new (0, 0) });
    app.InjectMouse (new () { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new (0, 0) });

    // Assert
    Assert.True (commandInvoked, "Command.Accept should have been invoked on LeftButtonReleased");

    (runnable as View)?.Dispose ();
}

[Fact]
public void MiddleButtonReleased_CustomBinding_InvokesCommand()

[Fact]
public void RightButtonReleased_CustomBinding_InvokesCommand()
```

#### Test Group 1.2: Released Binding with AutoGrab (MouseHighlightStates)
```csharp
// Claude - Opus 4.5
[Theory]
[InlineData (MouseState.In)]
[InlineData (MouseState.Pressed)]
[InlineData (MouseState.In | MouseState.Pressed)]
public void LeftButtonReleased_CustomBinding_InvokesCommand_WithMouseHighlightStates (MouseState highlightStates)
{
    // Arrange
    VirtualTimeProvider time = new ();
    using IApplication app = Application.Create (time);
    app.Init (DriverRegistry.Names.ANSI);

    IRunnable runnable = new Runnable ();
    View view = new ()
    {
        X = 0, Y = 0,
        Width = 10,
        Height = 10,
        MouseHighlightStates = highlightStates  // Triggers AutoGrab
    };
    (runnable as View)?.Add (view);
    app.Begin (runnable);

    // Add custom binding for Released
    view.MouseBindings.Add (MouseFlags.LeftButtonReleased, Command.Accept);

    var commandInvoked = false;
    view.InvokingCommand += (s, e) =>
    {
        if (e.Command == Command.Accept)
        {
            commandInvoked = true;
        }
    };

    // Act - Press (triggers grab), then Release (triggers ungrab)
    app.InjectMouse (new () { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new (0, 0) });
    Assert.True (app.Mouse.IsGrabbed (view), "Mouse should be grabbed after press");

    app.InjectMouse (new () { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new (0, 0) });

    // Assert
    Assert.True (commandInvoked, "Command.Accept should have been invoked on LeftButtonReleased");
    Assert.False (app.Mouse.IsGrabbed (view), "Mouse should be ungrabbed after click completes");

    (runnable as View)?.Dispose ();
}
```

#### Test Group 1.3: Released Binding vs. Default Behavior
```csharp
// Claude - Opus 4.5
[Fact]
public void LeftButtonReleased_NoCustomBinding_DoesNotInvokeAnyCommand()
{
    // Verify that without custom binding, Released events don't invoke commands
    // (only Pressed or Clicked do by default)
}

[Fact]
public void LeftButtonReleased_CustomBinding_CoexistsWithPressedBinding()
{
    // Verify both Pressed and Released bindings can coexist and both fire
}

[Fact]
public void LeftButtonReleased_MultipleCommands_InvokesAllCommands()
{
    // Add Released binding with multiple commands
    view.MouseBindings.Add (MouseFlags.LeftButtonReleased, [Command.Accept, Command.HotKey]);
    // Verify both commands are invoked
}
```

### Phase 2: Interaction Tests (Priority: High)

Add to **Tests/UnitTestsParallelizable/ViewBase/Mouse/MouseHoldRepeatTests.cs**:

#### Test Group 2.1: MouseHoldRepeat + Custom Released Binding
```csharp
// Claude - Opus 4.5
[Fact]
public void MouseHoldRepeat_Released_WithAdditionalReleasedBinding_DoesNotConflict()
{
    // Arrange
    VirtualTimeProvider time = new ();
    using IApplication app = Application.Create (time);
    app.Init (DriverRegistry.Names.ANSI);

    IRunnable runnable = new Runnable ();
    View view = new ()
    {
        X = 0, Y = 0,
        Width = 10,
        Height = 10,
        MouseHoldRepeat = MouseFlags.LeftButtonReleased  // Binds Released to Activate
    };
    (runnable as View)?.Add (view);
    app.Begin (runnable);

    // Note: MouseHoldRepeat already binds LeftButtonReleased to Activate
    // Verify this works correctly

    var activateCount = 0;
    view.Activating += (s, e) =>
    {
        activateCount++;
        e.Handled = true;
    };

    // Act
    app.InjectMouse (new () { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new (0, 0) });
    app.InjectMouse (new () { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new (0, 0) });

    // Assert
    Assert.Equal (1, activateCount);

    (runnable as View)?.Dispose ();
}

[Fact]
public void MouseHoldRepeat_Clicked_WithReleasedBinding_OnlyClickedInvokes()
{
    // When MouseHoldRepeat is set to Clicked, Released bindings should not fire
    // (because MouseHoldRepeat only allows the configured event through)
}
```

### Phase 3: Edge Cases (Priority: Medium)

Add to **MouseReleasedBindingTests.cs**:

#### Test Group 3.1: Position-based Released Scenarios
```csharp
// Claude - Opus 4.5
[Fact]
public void Released_PressInsideReleaseOutside_InvokesCommand_WhenAutoGrab()
{
    // With AutoGrab, releasing outside view should still invoke command
    // because the view grabbed the mouse
}

[Fact]
public void Released_PressInsideReleaseInside_InvokesCommand()
{
    // Standard case - press and release both inside view
}

[Fact]
public void Released_PressOutsideReleaseInside_DoesNotInvokeCommand()
{
    // If view didn't grab the mouse (press was outside),
    // release inside shouldn't invoke command
}
```

#### Test Group 3.2: State-based Released Scenarios
```csharp
// Claude - Opus 4.5
[Fact]
public void Released_WhenViewDisabled_DoesNotInvokeCommand()
{
    // Arrange - view with Released binding
    // Act - Disable view after press, before release
    // Assert - Command not invoked
}

[Fact]
public void Released_WhenViewNotVisible_DoesNotInvokeCommand()
{
    // Similar to disabled test
}

[Fact]
public void Released_WhenMouseNotGrabbed_DoesNotInvokeCommand_IfAutoGrabEnabled()
{
    // If AutoGrab is enabled but mouse isn't grabbed, Released shouldn't fire
}
```

### Phase 4: Regression Tests (Priority: High)

Add to **Tests/UnitTestsParallelizable/ViewBase/Mouse/MouseTests.cs**:

```csharp
// Claude - Opus 4.5
[Fact]
public void Pressed_Bindings_StillWorkAfterFix()
{
    // Verify default LeftButtonPressed → Activate binding still works
}

[Fact]
public void Clicked_Bindings_StillWorkAfterFix()
{
    // Verify Clicked events still invoke commands correctly
}

[Fact]
public void MouseHoldRepeat_StillWorksAfterFix()
{
    // Verify MouseHoldRepeat behavior unchanged
    // Use existing MouseHoldRepeatTests as reference
}
```

## Test Execution Strategy

### Pre-Implementation Testing
1. Run existing tests to establish baseline:
   ```bash
   dotnet test Tests/UnitTestsParallelizable --no-build --filter "Category=Mouse"
   ```
2. Document any existing failures
3. Verify all new tests will be in `UnitTestsParallelizable` project

### During Implementation
1. **Implement Phase 1 tests first** (expect failures with current code)
2. **Verify Phase 1 tests fail** as expected (proves bug exists and tests are valid)
3. **Apply the fix** to `View.Mouse.cs:HandleAutoGrabRelease`
4. **Verify Phase 1 tests now pass**
5. Implement Phases 2-4
6. Run all tests

### Post-Implementation Testing
1. Run full test suite:
   ```bash
   dotnet restore
   dotnet build --no-restore
   dotnet test Tests/UnitTestsParallelizable --no-build
   dotnet test Tests/UnitTests --no-build
   ```
2. Verify code coverage has increased (target: 70%+)
3. Check for any new warnings

## Test File Organization

### New File
**Tests/UnitTestsParallelizable/ViewBase/Mouse/MouseReleasedBindingTests.cs**
```csharp
using Xunit.Abstractions;

namespace ViewBaseTests.MouseTests;

// Claude - Opus 4.5
/// <summary>
///     Tests for MouseBinding with xxxReleased flags.
///     Verifies that custom bindings for Released events are properly invoked.
///     Related to issue #4674.
/// </summary>
[Trait ("Category", "Input")]
[Trait ("Category", "Mouse")]
public class MouseReleasedBindingTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    // Phase 1, 3 tests here
}
```

### Modified Files
- **Tests/UnitTestsParallelizable/ViewBase/Mouse/MouseHoldRepeatTests.cs**
  - Add Phase 2 tests

- **Tests/UnitTestsParallelizable/ViewBase/Mouse/MouseTests.cs**
  - Add Phase 4 regression tests

## Success Criteria

### Minimum Test Coverage Required Before Fix
1. ✅ At least 9 tests covering basic Released binding scenarios (Phase 1)
   - 3 basic invocation tests (Left, Middle, Right)
   - 3 with MouseHighlightStates combinations
   - 3 behavior verification tests

2. ✅ At least 2 tests covering MouseHoldRepeat interaction (Phase 2)

3. ✅ At least 6 tests covering edge cases (Phase 3)
   - 3 position-based scenarios
   - 3 state-based scenarios

4. ✅ At least 3 regression tests (Phase 4)

**Total:** Minimum 20 new tests

### Test Quality Requirements
1. All tests must be in `UnitTestsParallelizable` project
2. All tests must use InputInjection pattern (no custom mocks)
3. All tests must follow MouseInjectionDocTests.cs pattern:
   - VirtualTimeProvider for time control
   - IApplication.Create() and app.Init()
   - app.InjectMouse() for event injection
4. All tests must include clear arrange/act/assert sections
5. All tests must dispose resources properly
6. All tests must include `// Claude - Opus 4.5` comment at class or method level

### Coverage Requirements
1. Code coverage must not decrease
2. Target: 70%+ coverage on modified code paths
3. All branches in `HandleAutoGrabRelease` must be tested

## Implementation Order

1. **FIRST:** Create MouseReleasedBindingTests.cs with Phase 1 tests
2. **SECOND:** Run tests - verify they FAIL (proves bug exists)
3. **THIRD:** Apply fix to `View.Mouse.cs:HandleAutoGrabRelease`
4. **FOURTH:** Run Phase 1 tests - verify they PASS
5. **FIFTH:** Add Phase 2-4 tests
6. **SIXTH:** Run full test suite
7. **SEVENTH:** Verify coverage and create PR

## Risk Assessment

### High Risk Areas
1. **AutoGrab lifecycle** - Released events depend on proper grab/ungrab
   - Mitigation: Test with various MouseHighlightStates (Phase 1.2)

2. **MouseHoldRepeat interaction** - Both use Released flags
   - Mitigation: Dedicated tests (Phase 2)

3. **Regression** - Fix must not break existing behavior
   - Mitigation: Phase 4 regression tests + full test suite run

### Medium Risk Areas
1. **Position-based behavior** - Press/release at different locations
   - Mitigation: Phase 3.1 tests

2. **State changes** - View disabled/hidden during press/release
   - Mitigation: Phase 3.2 tests

## Standard Test Pattern

All tests should follow this pattern:

```csharp
// Claude - Opus 4.5
[Fact]
public void TestName_Scenario_ExpectedBehavior()
{
    // Arrange
    VirtualTimeProvider time = new ();
    using IApplication app = Application.Create (time);
    app.Init (DriverRegistry.Names.ANSI);

    IRunnable runnable = new Runnable ();
    View view = new ()
    {
        X = 0, Y = 0,
        Width = 10,
        Height = 10
        // Configure view as needed
    };
    (runnable as View)?.Add (view);
    app.Begin (runnable);

    // Configure bindings and event handlers

    // Act
    app.InjectMouse (new () { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new (0, 0) });
    app.InjectMouse (new () { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new (0, 0) });

    // Assert
    Assert.True (/* condition */);

    (runnable as View)?.Dispose ();
}
```

## References

- Issue: https://github.com/gui-cs/Terminal.Gui/issues/4674
- Source: Terminal.Gui/ViewBase/Mouse/View.Mouse.cs:603-619
- Test pattern: Tests/UnitTestsParallelizable/Input/Mouse/MouseInjectionDocTests.cs
- Related tests: Tests/UnitTestsParallelizable/ViewBase/Mouse/MouseHoldRepeatTests.cs
