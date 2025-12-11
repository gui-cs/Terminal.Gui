# MouseInterpreter Double-Click Bug

## Issue
The `MouseInterpreter` currently uses an **immediate click** approach where click events are emitted immediately upon button release. This causes incorrect event sequences for multi-click scenarios.

## Current (Incorrect) Behavior

### Double-Click Sequence:
```
Input:  Pressed ? Released ? Pressed ? Released
Output: Pressed ? Released ? Clicked ? ? Pressed ? Released ? DoubleClicked ?
```

### Triple-Click Sequence:
```
Input:  P ? R ? P ? R ? P ? R
Output: P ? R ? Clicked ? ? P ? R ? DoubleClicked ? ? P ? R ? TripleClicked ?
```

## Expected (Correct) Behavior

### Double-Click Sequence:
```
Input:  Pressed ? Released ? Pressed ? Released
Output: Pressed ? Released ? Pressed ? Released ? DoubleClicked ?
```

### Triple-Click Sequence:
```
Input:  P ? R ? P ? R ? P ? R
Output: P ? R ? P ? R ? P ? R ? TripleClicked ?
```

### Single Click (isolated):
```
Input:  Pressed ? Released ? [wait 500ms+]
Output: Pressed ? Released ? [wait 500ms+] ? Clicked ?
```

## Root Cause

The issue is in `MouseButtonClickTracker.UpdateState()`:

```csharp
// State changed - update tracking
if (Pressed)
{
    // Button was pressed, now released - this is a click!
    ++_consecutiveClicks;
    numClicks = _consecutiveClicks;  // ? IMMEDIATE emission
}
```

This emits clicks **immediately** on release, before knowing if another click will follow.

## Solution: Deferred Click Approach

We need to implement a **deferred click** system:

1. **On first release**: Don't emit Clicked yet - start a pending timer
2. **On second press within threshold**: Cancel pending click, continue tracking
3. **On second release within threshold**: Emit DoubleClicked
4. **On timeout**: Emit the pending Clicked event

### Implementation Requirements:

1. **Modify `MouseButtonClickTracker`**:
   - Add pending click state tracking
   - Store position and count of pending click
   - Modify `UpdateState()` to defer single clicks
   - Implement `CheckForExpiredClicks()` to return deferred clicks

2. **Modify `MouseInterpreter`**:
   - Add polling mechanism to check for expired clicks
   - Call `CheckForExpiredClicks()` on each button tracker
   - Yield expired click events

3. **Add polling in `InputProcessorImpl`**:
   - Periodically call a method to check for expired clicks
   - Emit them through the normal event pipeline

## Test Coverage

The following tests have been added to verify the fix:

- `DoubleClick_ShouldNotEmitSingleClick_BeforeDoubleClick()` - Verifies no Clicked before DoubleClicked
- `TripleClick_ShouldNotEmitSingleOrDoubleClick_BeforeTripleClick()` - Verifies no intermediate clicks
- `SingleClick_ShouldEmitClicked_AfterThresholdExpires()` - Verifies deferred single click
- `DoubleClick_EventSequence_ShouldBeCorrect()` - Verifies exact event count and order
- `DoubleClick_ShouldNotHaveClickedBetweenReleases()` - Verifies the bug from the screenshot

All these tests currently **FAIL** and should **PASS** after implementing the deferred click approach.

## References

- Issue: https://github.com/gui-cs/Terminal.Gui/issues/4474
- Screenshot showing duplicate events in Examples/UICatalog/Scenarios/Mouse.cs
