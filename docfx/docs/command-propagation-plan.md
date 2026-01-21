# Command Propagation Implementation Plan

> **Status**: Ready for Implementation
>
> **Issue**: [Menu etc. with Selectors are broken - Formalize/Fix Command propagation](https://github.com/gui-cs/Terminal.Gui/issues/4473)
>
> **Dependencies**:
> - ✅ Command Binding Refactor - Complete
> - ✅ IValue Interface Implementation - Complete
>
> **Implementation Status**:
> - ⏸️ PropagatedCommands mechanism - NOT STARTED
> - ⏸️ CommandContext WeakReference enhancement - NOT STARTED
> - 🤔 CommandContext Value enhancement - DEBATABLE (may not be needed)

## Table of Contents

- [Problem Statement](#problem-statement)
- [Solution Overview](#solution-overview)
- [Design Requirements](#design-requirements)
- [Proposed Solution](#proposed-solution)
- [Implementation Tasks](#implementation-tasks)
- [Testing Requirements](#testing-requirements)

---

## Problem Statement

### Current Behavior

Command propagation exists only for `Command.Accept`:
- Hard-coded in `View.RaiseAccepting()`
- Propagates to default button (`IsDefault == true`)
- Bubbles to `SuperView`

`Command.Activate` does **not** propagate, causing:
- **MenuBar broken**: Can't respond to MenuItem activations to show popovers
- **Workarounds required**: Custom events like `SelectedMenuItemChanged` needed
- **Tight coupling**: SubViews must know about SuperView needs

### Example: Broken FlagSelector in MenuBar

```
MenuBar
  └─ MenuBarItem
      └─ PopoverMenu
          └─ Menu
              └─ MenuItem
                  └─ FlagSelector (CommandView)
                      └─ CheckBox
```

When user activates CheckBox:
1. CheckBox raises `Activating`
2. FlagSelector forwards to itself
3. MenuItem forwards to itself
4. **STOPS** - Menu never sees it
5. MenuBar never sees it
6. Popover state can't be managed

---

## Solution Overview

**Add `PropagatedCommands` property to View** - opt-in list of commands that propagate to SuperView.

**Default**: `[Command.Accept]` (preserves current behavior)

**For MenuBar/Menu**: Set to `[Command.Accept, Command.Activate]`

**Propagation flow**:
```csharp
SubView → RaiseActivating() → not handled
  → SuperView.PropagatedCommands.Contains(Command.Activate)?
    → YES: SuperView.InvokeCommand(Command.Activate, ctx)
    → NO: stop
```

---

## Design Requirements

### Functional Requirements

1. ✅ **Generalized Propagation**: Support any command, not just `Accept`
2. ✅ **Opt-In Model**: SuperViews declare which commands they want via `PropagatedCommands`
3. ✅ **Backward Compatible**: Default `[Command.Accept]` preserves existing behavior
4. ✅ **CWP Compliant**: Maintains Cancellable Work Pattern structure
5. ✅ **Decoupled**: SubViews remain ignorant of SuperView needs
6. ✅ **Lifecycle Safe**: Use `WeakReference<View>` for `CommandContext.Source` to prevent dangling references

### WeakReference for Source Tracking

**Problem**: During propagation, intermediate views may dispose the original source view, causing dangling references.

**Solution**: Use `WeakReference<View>` for `CommandContext.Source`.

**Benefits**:
- Lifecycle safety - disposed views can be garbage collected
- Type safety - pattern match on View types after `TryGetTarget`
- Safe access pattern: `ctx.Source?.TryGetTarget(out View? view)`

**Handler Patterns**:
```csharp
// Pattern 1: Type-based with view access
if (args.Context?.Source?.TryGetTarget (out var view) && view is CheckBox cb) { }

// Pattern 2: Id-based identification
if (args.Context?.Source?.TryGetTarget (out var view) && view.Id == "AutoSave") { }
```

### Value Propagation (Optional/Debatable)

**Concept**: Add `object? Value` to `CommandContext` and populate from `IValue.GetValue()`.

**Benefit**: Handlers can access data without needing the source View reference.

**Pattern**:
```csharp
// Value-only pattern (if Value is implemented)
if (args.Context?.Value is CheckState state) { }
```

**Status**: 🤔 Debatable - may not be needed if WeakReference pattern is sufficient.

---

## Proposed Solution

### 1. Add `PropagatedCommands` Property

**Location**: `Terminal.Gui/ViewBase/View.Command.cs`

```csharp
/// <summary>
///     Gets or sets the list of commands that should propagate to this View from unhandled subviews.
///     When a subview raises a command that is not handled, and the command is in the SuperView's
///     <see cref="PropagatedCommands"/> list, the command will be invoked on the SuperView.
/// </summary>
/// <remarks>
///     By default, only <see cref="Command.Accept"/> propagates (backward compatibility).
///     To enable <see cref="Command.Activate"/> propagation for hierarchical views:
///     <code>
///         menuBar.PropagatedCommands = [Command.Accept, Command.Activate];
///     </code>
/// </remarks>
public IReadOnlyList<Command> PropagatedCommands { get; set; } = [Command.Accept];
```

### 2. Update `CommandContext` to Use WeakReference

**Location**: `Terminal.Gui/Input/CommandContext.cs`

```csharp
public record struct CommandContext : ICommandContext
{
    public Command Command { get; set; }
    public WeakReference<View>? Source { get; set; }  // Lifecycle-safe reference
    public IInputBinding? Binding { get; set; }
}
```

**Update `ICommandContext` interface**:
```csharp
public interface ICommandContext
{
    Command Command { get; }
    WeakReference<View>? Source { get; set; }
    IInputBinding Binding { get; }
}
```

### 3. Update `InvokeCommand` to Create WeakReference

**Location**: `Terminal.Gui/ViewBase/View.Command.cs`

```csharp
// In InvokeCommand methods
return implementation! (new CommandContext {
    Command = command,
    Source = new WeakReference<View> (this),
    Binding = binding
});
```

### 4. Add `PropagateCommand` Helper Method

**Location**: `Terminal.Gui/ViewBase/View.Command.cs`

```csharp
/// <summary>
///     Propagates a command to the SuperView if the command is in SuperView's <see cref="PropagatedCommands"/> list.
///     Handles the special case of invoking Command.Accept on a peer IsDefault button.
/// </summary>
protected bool? PropagateCommand (Command command, ICommandContext? ctx, bool handled)
{
    if (handled)
    {
        return true;
    }

    // Special case: Command.Accept checks for IsDefault peer button first
    if (command == Command.Accept)
    {
        View? isDefaultView = SuperView?.GetSubViews (includePadding: true)
            .FirstOrDefault (v => v is Button { IsDefault: true });

        if (isDefaultView != this && isDefaultView is Button { IsDefault: true })
        {
            bool? buttonHandled = isDefaultView.InvokeCommand (Command.Accept, ctx);
            if (buttonHandled == true)
            {
                return true;
            }
        }
    }

    // Check if SuperView wants this command propagated
    if (SuperView?.PropagatedCommands?.Contains (command) == true)
    {
        return SuperView.InvokeCommand (command, ctx);
    }

    return handled;
}
```

### 5. Update `RaiseAccepting` to Use Helper

```csharp
protected bool? RaiseAccepting (ICommandContext? ctx)
{
    CommandEventArgs args = new () { Context = ctx };

    // CWP: Virtual method first
    args.Handled = OnAccepting (args) || args.Handled;

    // CWP: Event second
    if (!args.Handled && Accepting is { })
    {
        Accepting?.Invoke (this, args);
    }

    // CWP: Raise non-cancellable "Accepted" event if handled
    if (args.Handled)
    {
        RaiseAccepted (ctx);
    }

    // NEW: Use propagation helper (maintains backward compatibility)
    return PropagateCommand (Command.Accept, ctx, args.Handled);
}
```

### 6. Update `RaiseActivating` to Use Helper

```csharp
protected virtual bool? RaiseActivating (ICommandContext? ctx)
{
    CommandEventArgs args = new () { Context = ctx };

    // CWP: Virtual method first
    if (OnActivating (args) || args.Handled)
    {
        return true;
    }

    // CWP: Event second
    Activating?.Invoke (this, args);

    // NEW: Use propagation helper (enables opt-in propagation)
    return PropagateCommand (Command.Activate, ctx, args.Handled ?? false);
}
```

### 7. Update MenuBar to Opt-In

**Location**: `Terminal.Gui/Views/Menu/MenuBar.cs`

```csharp
public MenuBar (IEnumerable<MenuBarItem> menuBarItems) : base (menuBarItems)
{
    // ... existing initialization ...

    // NEW: Opt-in to Command.Activate propagation for popover management
    PropagatedCommands = [Command.Accept, Command.Activate];
}
```

### 8. Handle Propagated Activations in MenuBar

```csharp
protected override bool OnActivating (CommandEventArgs args)
{
    // Find the MenuBarItem - check if source is a MenuBarItem or contained within one
    MenuBarItem? menuBarItem = null;
    if (args.Context?.Source?.TryGetTarget (out View? sourceView) && sourceView is MenuBarItem mbi)
    {
        menuBarItem = mbi;
    }
    else
    {
        menuBarItem = SubViews.OfType<MenuBarItem> ()
            .FirstOrDefault (m => args.Context?.Source?.TryGetTarget (out View? view) &&
                                  (view == m || m.SubViews.Contains (view)));
    }

    if (menuBarItem is { PopoverMenuOpen: false })
    {
        if (!CanFocus)
        {
            Active = true;
        }

        ShowItem (menuBarItem);

        if (!menuBarItem.HasFocus)
        {
            menuBarItem.SetFocus ();
        }

        return true;
    }

    return base.OnActivating (args);
}
```

### 9. Remove Legacy Workarounds

**Remove from `Menu.cs`**:
- `SelectedMenuItemChanged` event
- `RaiseSelectedMenuItemChanged` method

**Remove from `PopoverMenu.cs`**:
- `MenuOnSelectedMenuItemChanged` handler
- Event subscription in `Root` setter

**Remove from `MenuBar.cs`**:
- `OnSelectedMenuItemChanged` override

---

## Implementation Tasks

### Core Files to Modify

| File | Changes | Priority |
|------|---------|----------|
| **Terminal.Gui/Input/CommandContext.cs** | Change `View? Source` to `WeakReference<View>? Source`<br>Update `ICommandContext` interface | **HIGH** |
| **Terminal.Gui/ViewBase/View.Command.cs** | Add `PropagatedCommands` property<br>Add `PropagateCommand()` helper<br>Update `RaiseAccepting()` and `RaiseActivating()`<br>Update `InvokeCommand()` to create WeakReference | **HIGH** |
| **Terminal.Gui/Views/Menu/MenuBar.cs** | Set `PropagatedCommands = [Command.Accept, Command.Activate]`<br>Add `OnActivating()` override<br>Remove `OnSelectedMenuItemChanged` | **HIGH** |
| **Terminal.Gui/Views/Menu/Menu.cs** | Set `PropagatedCommands = [Command.Accept, Command.Activate]`<br>Remove `SelectedMenuItemChanged` event/method | **HIGH** |
| **Terminal.Gui/Views/Menu/PopoverMenu.cs** | Set `PropagatedCommands = [Command.Accept, Command.Activate]`<br>Remove event subscription | **MEDIUM** |
| **Terminal.Gui/Views/Shortcut.cs** | Verify `CommandViewOnActivating` forwards context correctly<br>Update XML docs | **LOW** |
| **Terminal.Gui/Views/Selectors/SelectorBase.cs** | Verify `OnCheckboxOnActivating` forwards context correctly | **LOW** |

### Optional Enhancement: Value Property

**⚠️ DEBATABLE** - Only implement if WeakReference pattern proves insufficient.

| File | Changes |
|------|---------|
| **Terminal.Gui/Input/CommandContext.cs** | Add `object? Value` property |
| **Terminal.Gui/ViewBase/View.Command.cs** | Update `InvokeCommand` to populate `Value` from `IValue.GetValue()` |

**Implementation**:
```csharp
// In InvokeCommand methods
object? value = this is IValue valueView ? valueView.GetValue () : null;
return implementation! (new CommandContext {
    Command = command,
    Source = new WeakReference<View> (this),
    Value = value,  // Optional
    Binding = binding
});
```

---

## Testing Requirements

### Unit Tests

**Location**: `Tests/UnitTestsParallelizable/`

#### 1. PropagatedCommands Default Behavior
```csharp
// Claude - Opus 4.5
[Fact]
public void PropagatedCommands_DefaultIsAcceptOnly ()
{
    View view = new ();
    Assert.Equal ([Command.Accept], view.PropagatedCommands);
}

[Fact]
public void CommandAccept_PropagatesByDefault ()
{
    View superView = new ();
    View subView = new ();
    superView.Add (subView);

    bool superViewAcceptingCalled = false;
    superView.Accepting += (s, e) => superViewAcceptingCalled = true;

    subView.InvokeCommand (Command.Accept);

    Assert.True (superViewAcceptingCalled);
}

[Fact]
public void CommandActivate_DoesNotPropagateByDefault ()
{
    View superView = new ();
    View subView = new ();
    superView.Add (subView);

    bool superViewActivatingCalled = false;
    superView.Activating += (s, e) => superViewActivatingCalled = true;

    subView.InvokeCommand (Command.Activate);

    Assert.False (superViewActivatingCalled);
}
```

#### 2. PropagatedCommands Opt-In
```csharp
[Fact]
public void CommandActivate_PropagatesWhenOptedIn ()
{
    View superView = new () { PropagatedCommands = [Command.Accept, Command.Activate] };
    View subView = new ();
    superView.Add (subView);

    bool superViewActivatingCalled = false;
    superView.Activating += (s, e) => superViewActivatingCalled = true;

    subView.InvokeCommand (Command.Activate);

    Assert.True (superViewActivatingCalled);
}

[Fact]
public void PropagatedCommands_CanDisableAllPropagation ()
{
    View superView = new () { PropagatedCommands = [] };
    View subView = new ();
    superView.Add (subView);

    bool superViewAcceptingCalled = false;
    superView.Accepting += (s, e) => superViewAcceptingCalled = true;

    subView.InvokeCommand (Command.Accept);

    Assert.False (superViewAcceptingCalled);
}
```

#### 3. WeakReference Source Tracking
```csharp
[Fact]
public void Propagation_UsesWeakReferenceForSource ()
{
    View superView = new () { PropagatedCommands = [Command.Activate] };
    View subView = new () { Id = "SubView" };
    superView.Add (subView);

    WeakReference<View>? receivedSource = null;

    superView.Activating += (s, args) =>
    {
        receivedSource = args.Context?.Source;
    };

    subView.InvokeCommand (Command.Activate);

    Assert.NotNull (receivedSource);
    Assert.True (receivedSource!.TryGetTarget (out View? targetView));
    Assert.Equal (subView, targetView);
}
```

#### 4. MenuBar Integration
```csharp
[Fact]
public void MenuBar_ReceivesActivateFromMenuItem ()
{
    MenuBar menuBar = new ();
    MenuBarItem menuBarItem = new ("File", [new MenuItem { Title = "New" }]);
    menuBar.Add (menuBarItem);

    bool menuBarActivatingCalled = false;
    menuBar.Activating += (s, e) => menuBarActivatingCalled = true;

    MenuItem menuItem = menuBarItem.PopoverMenu.Root.SubViews [0] as MenuItem;
    menuItem!.InvokeCommand (Command.Activate);

    Assert.True (menuBarActivatingCalled);
}
```

#### 5. FlagSelector Scenario (Critical)
```csharp
[Fact]
public void MenuBar_ReceivesActivateFromCheckBoxInFlagSelector ()
{
    // Hierarchy: MenuBar → MenuBarItem → PopoverMenu → Menu → MenuItem → FlagSelector → CheckBox
    MenuBar menuBar = new ();

    CheckBox autoSaveCheckBox = new () { Id = "AutoSave", Title = "_Auto Save" };
    FlagSelector flagSelector = new ();
    flagSelector.Add (autoSaveCheckBox);

    MenuItem menuItem = new () { CommandView = flagSelector };
    Menu menu = new ([menuItem]);
    MenuBarItem menuBarItem = new ("File", menu);
    menuBar.Add (menuBarItem);

    bool menuBarActivatingCalled = false;
    View? receivedSourceView = null;
    menuBar.Activating += (s, args) =>
    {
        menuBarActivatingCalled = true;
        if (args.Context?.Source?.TryGetTarget (out View? view))
        {
            receivedSourceView = view;
        }
    };

    // Activate the checkbox
    autoSaveCheckBox.InvokeCommand (Command.Activate);

    Assert.True (menuBarActivatingCalled, "MenuBar should receive Activate from CheckBox");
    Assert.Equal (autoSaveCheckBox, receivedSourceView);
}
```

#### 6. Propagation Stops When Handled
```csharp
[Fact]
public void Propagation_StopsWhenIntermediateHandlerSetsHandled ()
{
    View grandSuperView = new () { PropagatedCommands = [Command.Activate] };
    View superView = new () { PropagatedCommands = [Command.Activate] };
    View subView = new ();

    grandSuperView.Add (superView);
    superView.Add (subView);

    bool grandSuperViewCalled = false;
    grandSuperView.Activating += (s, e) => grandSuperViewCalled = true;

    superView.Activating += (s, e) => e.Handled = true; // Stops here

    subView.InvokeCommand (Command.Activate);

    Assert.False (grandSuperViewCalled, "Propagation should stop at intermediate SuperView");
}
```

#### 7. Context Preservation
```csharp
[Fact]
public void Propagation_PreservesSourceAndBinding ()
{
    View superView = new () { PropagatedCommands = [Command.Activate] };
    View subView = new () { Id = "SubView" };
    superView.Add (subView);

    View? receivedSource = null;
    IInputBinding? receivedBinding = null;

    superView.Activating += (s, args) =>
    {
        if (args.Context?.Source?.TryGetTarget (out View? view))
        {
            receivedSource = view;
        }
        receivedBinding = args.Context?.Binding;
    };

    KeyBinding binding = new (Command.Activate, Key.Enter);
    subView.InvokeCommand (Command.Activate, new CommandContext
    {
        Command = Command.Activate,
        Source = new WeakReference<View> (subView),
        Binding = binding
    });

    Assert.Equal (subView, receivedSource);
    Assert.Equal (binding, receivedBinding);
}
```

### Integration Tests

Test the complete MenuBar scenario with:
- Nested menus (PopoverMenu ownership)
- CommandView (Shortcut aggregation)
- Event interception (FlagSelector)

---

## Success Criteria

### Must Have
- ✅ `PropagatedCommands` property implemented
- ✅ `PropagateCommand()` helper works for all commands
- ✅ `WeakReference<View>` for `CommandContext.Source`
- ✅ `Command.Accept` propagation unchanged (backward compatible)
- ✅ `Command.Activate` can propagate when opted-in
- ✅ MenuBar responds to MenuItem activations
- ✅ All unit tests pass
- ✅ Legacy workarounds (`SelectedMenuItemChanged`) removed

### Should Have
- ⚠️ Documentation updated (command.md, events.md, View.md)
- ⚠️ UICatalog scenario demonstrates propagation

### Debatable
- 🤔 `CommandContext.Value` population from `IValue.GetValue()`

---

## References

### Documentation
- [Cancellable Work Pattern](cancellable-work-pattern.md)
- [Command Deep Dive](command.md)
- [Events Deep Dive](events.md)
- [View Deep Dive](View.md)

### Related Issues
- Issue #4473: Menu etc. with Selectors are broken - Formalize/Fix Command propagation
- Issue #4050: PropagatedCommands design

### Revision History

| Date | Author | Changes |
|------|--------|---------|
| 2026-01-09 | GitHub Copilot | Initial analysis |
| 2026-01-21 | Claude Opus 4.5 | WeakReference design revision; condensed from 1576 to 567 lines; renamed to command-propagation-plan.md; made WeakReference core (not optional); parent/child → superView/subView |
