# Plan: Fix CWP Violation in Command.HotKey and Command.Activate Handlers

## Problem Statement

The command handlers for `Command.HotKey` and `Command.Activate` in `View.Command.cs` violate the Cancellable Workflow Pattern (CWP) by calling `SetFocus()` AFTER the virtual methods and events, instead of BEFORE.

**CWP Rule**: Work happens BEFORE notifications, not after.

**Current (WRONG)**:
```csharp
if (RaiseHandlingHotKey (ctx) is true) { return true; }
SetFocus ();  // ❌ Work AFTER notification
return true;
```

**CWP-Correct**:
```csharp
SetFocus ();  // ✅ Work BEFORE notification
if (RaiseHandlingHotKey (ctx) is true) { return true; }
return true;
```

## Breaking Changes

These views call `SetFocus()` in their event overrides, assuming focus hasn't been set yet:

1. **CheckBox.cs:44-58** - calls `SetFocus()` in `OnHandlingHotKey`
2. **Shortcut.cs:331-367, 298-328** - calls `SetFocus()` in both `OnHandlingHotKey` and `OnActivating`
3. **Label.cs:52-81** - calls `SetFocus()` in `InvokeHotKeyOnNextPeer`
4. **FlagSelector.cs:80-102** - calls `SetFocus()` in `Activating` event handler

## Recommended Approach: Immediate Fix

Make this a clean breaking change now rather than carrying technical debt forward:

1. Fix `View.Command.cs` to be CWP-correct
2. Update all dependent views to remove redundant `SetFocus()` calls
3. Update tests to verify new behavior
4. Document as breaking change

## Implementation Steps

### Step 1: Fix View.Command.cs HotKey Handler

**File**: `Terminal.Gui/ViewBase/View.Command.cs`

**Change** `SetupCommands()` method (lines 18-32):

```csharp
// HotKey - SetFocus and raise HandlingHotKey
AddCommand (Command.HotKey,
            ctx =>
            {
                // CWP: Do work BEFORE notification
                SetFocus ();

                if (RaiseHandlingHotKey (ctx) is true)
                {
                    return true;
                }

                // Always return true on hotkey, even if SetFocus fails because
                // hotkeys are always handled by the View (unless RaiseHandlingHotKey cancels).
                return true;
            });
```

### Step 2: Fix View.Command.cs Activate Handler

**File**: `Terminal.Gui/ViewBase/View.Command.cs`

**Change** `SetupCommands()` method (lines 34-51):

```csharp
// Space or single-click - Raise Activating
AddCommand (Command.Activate,
            ctx =>
            {
                // CWP: Do work BEFORE notification
                bool focusSet = false;
                if (CanFocus)
                {
                    focusSet = SetFocus ();
                }

                if (RaiseActivating (ctx) is true)
                {
                    return true;
                }

                // For Activate, if the view is focusable and SetFocus succeeded,
                // the event is handled.
                return focusSet;
            });
```

### Step 3: Update CheckBox.cs

**File**: `Terminal.Gui/Views/CheckBox.cs`

**Change** `OnHandlingHotKey` (lines 44-58):

Remove the `SetFocus()` call and update comment:

```csharp
protected override bool OnHandlingHotKey (CommandEventArgs args)
{
    // Invoke Activate on ourselves
    if (InvokeCommand (Command.Activate, args.Context) is not true)
    {
        return base.OnHandlingHotKey (args);
    }

    // CWP-correct: SetFocus() is now called BEFORE this method by the default HotKey handler.
    // No need to call it here anymore.
    return true;
}
```

### Step 4: Update Shortcut.cs

**File**: `Terminal.Gui/Views/Shortcut.cs`

**Change 1** - `OnHandlingHotKey` (lines 331-367):

Remove `SetFocus()` call:

```csharp
protected override bool OnHandlingHotKey (CommandEventArgs args)
{
    bool ret = base.OnHandlingHotKey (args);

    if (ret)
    {
        return ret;
    }

    // CWP-correct: SetFocus() is now called BEFORE this method by the default HotKey handler.
    // Removed: SetFocus ();

    if (IsFromShortcut (args))
    {
        // ... rest of method unchanged ...
    }
}
```

**Change 2** - `OnActivating` (lines 298-328):

Remove `SetFocus()` call:

```csharp
protected override bool OnActivating (CommandEventArgs args)
{
    bool ret = base.OnActivating (args);

    if (ret)
    {
        return ret;
    }

    // CWP-correct: SetFocus() is now called BEFORE this method by the default Activate handler.
    // Removed: SetFocus ();

    if (IsFromShortcut (args))
    {
        // ... rest of method unchanged ...
    }
}
```

### Step 5: Update Label.cs

**File**: `Terminal.Gui/Views/Label.cs`

**Change** `InvokeHotKeyOnNextPeer` (lines 52-81):

Remove `SetFocus()` call:

```csharp
private bool? InvokeHotKeyOnNextPeer (ICommandContext commandContext)
{
    if (RaiseHandlingHotKey (commandContext) == true)
    {
        return true;
    }

    if (CanFocus)
    {
        // CWP-correct: SetFocus() is now called BEFORE RaiseHandlingHotKey by the default HotKey handler.
        // Removed: SetFocus ();
        return true;
    }

    // ... rest of method unchanged ...
}
```

### Step 6: Update FlagSelector.cs

**File**: `Terminal.Gui/Views/Selectors/FlagSelector.cs`

**Change** `OnCheckboxOnActivating` (lines 80-102):

Remove `SetFocus()` call and update comment:

```csharp
private void OnCheckboxOnActivating (object? sender, CommandEventArgs args)
{
    if (sender is not CheckBox checkbox)
    {
        return;
    }

    // CWP-correct: SetFocus() is now called BEFORE events by the default Activate handler.
    // No need to call it explicitly anymore.
    // Removed: if (checkbox.CanFocus) { checkbox.SetFocus (); }

    // Activating doesn't normally propagate, so we do it here
    if (InvokeCommand (Command.Activate, args.Context) is true)
    {
        args.Handled = true;
    }
}
```

### Step 7: Add/Update Tests

**File**: `Tests/UnitTestsParallelizable/ViewBase/Keyboard/HotKeyTests.cs`

Add new test after existing tests:

```csharp
// Claude - Opus 4.5
[Theory]
[InlineData (KeyCode.Null, false)]
[InlineData (KeyCode.ShiftMask, true)]
public void HotKey_SetsFocus_BeforeHandlingHotKeyEvent (KeyCode mask, bool expected)
{
    // Arrange
    View view = new () { HotKeySpecifier = (Rune)'^', Title = "^Test", CanFocus = true };
    bool focusSetBeforeEvent = false;

    view.HandlingHotKey += (_, _) =>
    {
        // Verify focus was set BEFORE the event was raised (CWP-correct)
        focusSetBeforeEvent = view.HasFocus;
    };

    Assert.False (view.HasFocus);

    // Act
    view.NewKeyDownEvent (KeyCode.T | mask);

    // Assert
    Assert.Equal (expected, focusSetBeforeEvent);
    Assert.Equal (expected, view.HasFocus);
}
```

**File**: `Tests/UnitTestsParallelizable/ViewBase/ViewCommandTests.cs`

Add new test:

```csharp
// Claude - Opus 4.5
[Fact]
public void HotKey_Command_FollowsCWP_SetsFocusBeforeEvent ()
{
    // Arrange
    View view = new () { CanFocus = true };
    bool focusStateInOnHandlingHotKey = false;
    bool focusStateInEvent = false;

    view.HandlingHotKey += (_, _) =>
    {
        focusStateInEvent = view.HasFocus;
    };

    // Use a derived class to test the virtual method
    TestView testView = new ();
    testView.OnHandlingHotKeyCallback = _ =>
    {
        focusStateInOnHandlingHotKey = testView.HasFocus;

        return false;
    };

    // Act
    view.InvokeCommand (Command.HotKey);
    testView.InvokeCommand (Command.HotKey);

    // Assert - CWP dictates work happens BEFORE notifications
    Assert.True (focusStateInEvent, "Focus should be set BEFORE HandlingHotKey event");
    Assert.True (focusStateInOnHandlingHotKey, "Focus should be set BEFORE OnHandlingHotKey virtual method");
}

private class TestView : View
{
    public Func<CommandEventArgs, bool>? OnHandlingHotKeyCallback { get; set; }

    public TestView ()
    {
        CanFocus = true;
    }

    protected override bool OnHandlingHotKey (CommandEventArgs args)
    {
        return OnHandlingHotKeyCallback?.Invoke (args) ?? base.OnHandlingHotKey (args);
    }
}
```

### Step 8: Update Documentation

**File**: `docfx/docs/command.md`

Find the section documenting `Command.HotKey` and update to reflect CWP-correct behavior:

```markdown
### Command.HotKey

Called when the user presses a key combination matching the View's HotKey.

**Default Behavior**:
1. Sets focus to the view (if `CanFocus` is true)
2. Calls `OnHandlingHotKey()` virtual method
3. Raises `HandlingHotKey` event
4. Returns `true` (hotkeys are always handled unless cancelled)

**CWP Pattern**: Focus is set BEFORE the virtual method and event are invoked, following
the Cancellable Workflow Pattern where work happens before notifications.
```

**File**: `.claude/rules/cwp-pattern.md`

Add example at the end showing the fix:

```markdown
## Example: View.Command.cs HotKey Handler (Fixed in v2.x)

**Before (CWP Violation)**:
```csharp
AddCommand (Command.HotKey, ctx =>
{
    if (RaiseHandlingHotKey (ctx) is true) { return true; }
    SetFocus ();  // ❌ Work AFTER notification (WRONG)
    return true;
});
```

**After (CWP Correct)**:
```csharp
AddCommand (Command.HotKey, ctx =>
{
    SetFocus ();  // ✅ Work BEFORE notification (CORRECT)
    if (RaiseHandlingHotKey (ctx) is true) { return true; }
    return true;
});
```
```

## Verification

After implementation, verify:

1. **Run all tests**:
   ```bash
   dotnet test Tests/UnitTestsParallelizable --no-build
   dotnet test Tests/UnitTests --no-build
   ```

2. **Manual testing**:
   - Run UICatalog
   - Test CheckBox hotkey behavior
   - Test Button hotkey behavior
   - Test Label hotkey behavior (with CanFocus = true)
   - Test Shortcut hotkey behavior
   - Verify focus is set before events fire

3. **Code review**:
   - Verify no other views override `OnHandlingHotKey` and call `SetFocus()`
   - Check for any other CWP violations in Command handlers

## Critical Files

- `Terminal.Gui/ViewBase/View.Command.cs` - Core fix (lines 18-51)
- `Terminal.Gui/Views/CheckBox.cs` - Remove SetFocus (line 55)
- `Terminal.Gui/Views/Shortcut.cs` - Remove SetFocus (lines 306, 339)
- `Terminal.Gui/Views/Label.cs` - Remove SetFocus (line 61)
- `Terminal.Gui/Views/Selectors/FlagSelector.cs` - Remove SetFocus (line 91)
- `Tests/UnitTestsParallelizable/ViewBase/Keyboard/HotKeyTests.cs` - New test
- `Tests/UnitTestsParallelizable/ViewBase/ViewCommandTests.cs` - New test

## Risks & Mitigation

**Risk**: Custom views in user code that override `OnHandlingHotKey` and call `SetFocus()`

**Mitigation**:
- Document as breaking change in CHANGELOG.md
- Provide clear migration guide
- This is alpha software (v2.x), breaking changes are expected

**Risk**: Subtle focus timing bugs in complex scenarios

**Mitigation**:
- Comprehensive test coverage
- Manual testing of all affected views
- UICatalog scenario testing
