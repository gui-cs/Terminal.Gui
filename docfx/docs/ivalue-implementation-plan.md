# IValue&lt;T&gt; Implementation Plan

> **Status**: Implementation Plan
>
> **Created**: 2026-01-21
>
> **Author**: Claude Opus 4.5
>
> **Related**: [Command Propagation Analysis](./command-propagation-analysis.md)

## Table of Contents

- [Overview](#overview)
- [Goals](#goals)
- [Design](#design)
- [Views Classification](#views-classification)
- [Implementation Plan](#implementation-plan)
- [Testing Strategy](#testing-strategy)
- [Migration Notes](#migration-notes)

---

## Overview

The `IValue<T>` interface provides a standardized way for Views to expose their primary value. This enables:

1. **Generic programming**: Code can work with any value-bearing View without knowing its specific type
2. **Command propagation**: `CommandContext.Value` can carry the source View's value up the hierarchy
3. **Prompt pattern**: `Prompt<TView, TResult>` can automatically extract results from Views implementing `IValue<T>`

Currently, only `ColorPicker` and `AttributePicker` implement `IValue<T>`. This plan extends the pattern to all appropriate Views.

---

## Goals

1. **Add non-generic `IValue` interface** - Enables boxing values for `CommandContext.Value`
2. **Standardize value access** - All value-bearing Views implement `IValue<T>`
3. **Preserve existing APIs** - Existing properties (`Text`, `Date`, `CheckedState`) remain; `Value` maps to them
4. **Enable command propagation** - `InvokeCommand` can populate `ctx.Value` from any `IValue` implementer

---

## Design

### Non-Generic IValue Interface

Add to `Terminal.Gui/ViewBase/IValue.cs`:

```csharp
/// <summary>
/// Non-generic interface for accessing a View's value as a boxed object.
/// Used by command propagation to carry values without knowing the generic type.
/// </summary>
/// <remarks>
/// This interface enables <see cref="CommandContext.Value"/> to be populated
/// from any View that has a value, regardless of the value's type.
/// </remarks>
public interface IValue
{
    /// <summary>
    /// Gets the value as a boxed object.
    /// </summary>
    /// <returns>The current value, or <see langword="null"/> if no value is set.</returns>
    object? GetValue ();
}
```

### Updated IValue&lt;T&gt; Interface

```csharp
/// <summary>
/// Interface for Views that provide a strongly-typed value.
/// </summary>
/// <typeparam name="TValue">The type of the value.</typeparam>
/// <remarks>
/// <para>
/// Views implementing this interface can be used with <c>Prompt&lt;TView, TResult&gt;</c>
/// for automatic result extraction without requiring an explicit <c>resultExtractor</c>.
/// </para>
/// <para>
/// Implementers should use <see cref="CWPPropertyHelper.ChangeProperty{T}"/> to implement
/// the <see cref="Value"/> property setter, which follows the Cancellable Work Pattern (CWP).
/// </para>
/// </remarks>
public interface IValue<TValue> : IValue
{
    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    TValue? Value { get; set; }

    /// <summary>
    /// Raised when <see cref="Value"/> is about to change.
    /// Set <see cref="ValueChangingEventArgs{T}.Handled"/> to <see langword="true"/> to cancel.
    /// </summary>
    event EventHandler<ValueChangingEventArgs<TValue?>>? ValueChanging;

    /// <summary>
    /// Raised when <see cref="Value"/> has changed.
    /// </summary>
    event EventHandler<ValueChangedEventArgs<TValue?>>? ValueChanged;

    /// <inheritdoc/>
    object? IValue.GetValue () => Value;
}
```

### Integration with CommandContext

In `View.Command.cs`, when invoking a command:

```csharp
// In InvokeCommand, before raising events:
if (this is IValue valueProvider)
{
    ctx.Value = valueProvider.GetValue ();
}
```

---

## Views Classification

### Views That Should NOT Implement IValue&lt;T&gt;

| Category | Views | Reason |
|----------|-------|--------|
| **Containers** | `View`, `Window`, `FrameView`, `TabView`, `Tab`, `TileView`, `ScrollView`, `Padding`, `Margin`, `Adornment` | Hold other Views, no own value |
| **Display-Only** | `Label`, `Line`, `SpinnerView`, `GraphView`, `LegendAnnotation` | Show information, not user input |
| **Menu/Navigation** | `Bar`, `Menu`, `MenuBar`, `MenuItem`, `MenuBarItem`, `Shortcut`, `StatusBar`, `PopoverMenu` | Commands/navigation, not values |
| **Actions** | `Button` | Triggers action, no value |
| **Result Pattern** | `Runnable`, `Dialog`, `FileDialog`, `OpenDialog`, `SaveDialog`, `Wizard`, `WizardStep`, `Prompt<T>`, `MessageBox` | Use `Result` pattern instead |
| **Deprecated** | `ComboBox` | Being replaced |
| **Deferred** | `TextView`, `TreeView<T>`, `HexView`, `ProgressBar` | Complexity or semantic questions |

### Views That SHOULD Implement IValue&lt;T&gt;

#### Group 1: Already Have `Value` Property (Add Interface Only)

| View | Value Type | Notes |
|------|------------|-------|
| `ColorPicker` | `Color?` | Already implements `IValue<Color?>` |
| `AttributePicker` | `Attribute?` | Already implements `IValue<Attribute?>` |
| `NumericUpDown<T>` | `T` | Has `Value`, add interface |
| `NumericUpDown` | `int` | Has `Value`, add interface |
| `LinearRange<T>` | `T` | Has `Value`, add interface |
| `LinearRange` | `object` | Has `Value`, add interface |
| `OptionSelector` | `int?` | Has `Value`, add interface |
| `OptionSelector<TEnum>` | `TEnum?` | Has `Value`, add interface |
| `FlagSelector` | `int?` | Has `Value`, add interface |
| `FlagSelector<TFlagsEnum>` | `TFlagsEnum?` | Has `Value`, add interface |
| `ScrollBar` | `int` | Has `Value`, add interface |

#### Group 2: Need Property Mapping (Existing Property → Value)

| View | Value Type | Existing Property | Implementation |
|------|------------|-------------------|----------------|
| `CheckBox` | `CheckState` | `CheckedState` | `Value` maps to `CheckedState` |
| `TextField` | `string` | `Text` | `Value` maps to `Text` |
| `DateField` | `DateTime` | `Date` | `Value` maps to `Date` |
| `TimeField` | `TimeSpan` | `Time` | `Value` maps to `Time` |
| `DatePicker` | `DateTime` | `Date` | `Value` maps to `Date` |
| `ListView` | `int?` | `SelectedItem` | `Value` maps to `SelectedItem` |
| `CharMap` | `Rune` | `SelectedCodePoint` | `Value` maps to selection |

---

## Implementation Plan

### Phase 1: Add Non-Generic Interface

**File**: `Terminal.Gui/ViewBase/IValue.cs`

1. Add `IValue` interface with `GetValue()` method
2. Update `IValue<T>` to inherit from `IValue`
3. Add default implementation: `object? IValue.GetValue() => Value;`

**Estimated changes**: ~15 lines

### Phase 2: Views with Existing `Value` Property

These views already have a `Value` property. Add the interface declaration and ensure events exist.

#### 2.1 NumericUpDown&lt;T&gt; / NumericUpDown

**File**: `Terminal.Gui/Views/NumericUpDown.cs`

- Add `: IValue<T>` to class declaration
- Verify `Value` property exists with getter/setter
- Verify `ValueChanging` and `ValueChanged` events exist
- Add explicit `IValue.GetValue()` if needed

#### 2.2 LinearRange&lt;T&gt; / LinearRange

**File**: `Terminal.Gui/Views/LinearRange.cs`

- Add `: IValue<T>` to class declaration
- Same verification as above

#### 2.3 SelectorBase / OptionSelector / FlagSelector

**File**: `Terminal.Gui/Views/Selectors/SelectorBase.cs`

- Add `: IValue<int?>` to `SelectorBase` (or appropriate base)
- Generic variants inherit automatically

**Files**: `OptionSelector.cs`, `FlagSelector.cs`

- Verify generic variants properly expose typed `Value`

#### 2.4 ScrollBar

**File**: `Terminal.Gui/Views/ScrollBar.cs`

- Add `: IValue<int>` to class declaration
- Verify events exist or add them

### Phase 3: Views Needing Property Mapping

These views have a different property name that should map to `Value`.

#### 3.1 CheckBox

**File**: `Terminal.Gui/Views/CheckBox.cs`

```csharp
public class CheckBox : View, IValue<CheckState>
{
    // Existing CheckedState property and events...

    /// <inheritdoc/>
    public CheckState Value
    {
        get => CheckedState;
        set => CheckedState = value;
    }

    /// <inheritdoc/>
    public event EventHandler<ValueChangingEventArgs<CheckState>>? ValueChanging
    {
        add => CheckedStateChanging += (s, e) => value?.Invoke (s, new (e.CurrentValue, e.NewValue));
        remove => { } // Complex - may need different approach
    }

    // ... or rename existing events
}
```

**Decision needed**: Should we:
- (A) Add `Value` as alias to `CheckedState` (keeps both APIs)
- (B) Rename `CheckedState` to `Value` (breaking change, cleaner)
- (C) Keep `CheckedState`, add `Value` that delegates, adapt events

**Recommendation**: Option (A) - Add `Value` as alias. Keeps backward compatibility.

#### 3.2 TextField

**File**: `Terminal.Gui/Views/TextField.cs`

```csharp
public class TextField : View, IValue<string>
{
    /// <inheritdoc/>
    public string? Value
    {
        get => Text;
        set => Text = value ?? string.Empty;
    }

    // Events: TextChanging/TextChanged → ValueChanging/ValueChanged
}
```

**Note**: `Text` is `string`, not `string?`. Decide if `Value` should allow null.

#### 3.3 DateField

**File**: `Terminal.Gui/Views/DateField.cs`

```csharp
public class DateField : TextField, IValue<DateTime>
{
    /// <inheritdoc/>
    public DateTime Value
    {
        get => Date;
        set => Date = value;
    }
}
```

#### 3.4 TimeField

**File**: `Terminal.Gui/Views/TimeField.cs`

```csharp
public class TimeField : TextField, IValue<TimeSpan>
{
    /// <inheritdoc/>
    public TimeSpan Value
    {
        get => Time;
        set => Time = value;
    }
}
```

#### 3.5 DatePicker

**File**: `Terminal.Gui/Views/DatePicker.cs`

```csharp
public class DatePicker : View, IValue<DateTime>
{
    /// <inheritdoc/>
    public DateTime Value
    {
        get => Date;
        set => Date = value;
    }
}
```

#### 3.6 ListView

**File**: `Terminal.Gui/Views/ListView.cs`

```csharp
public class ListView : View, IValue<int?>
{
    /// <inheritdoc/>
    public int? Value
    {
        get => SelectedItem;
        set => SelectedItem = value ?? 0;
    }
}
```

**Note**: `SelectedItem` is `int`, not `int?`. Consider whether -1 or 0 represents "no selection".

#### 3.7 CharMap

**File**: `Terminal.Gui/Views/CharMap.cs`

- Investigate current selection property
- Add `IValue<Rune>` or `IValue<int>` (codepoint)

### Phase 4: Update InvokeCommand

**File**: `Terminal.Gui/ViewBase/View.Command.cs`

In `InvokeCommand()`, set `ctx.Value`:

```csharp
public bool? InvokeCommand (Command command, ICommandContext? ctx = null)
{
    CommandContext context = ctx as CommandContext? ?? new () { Command = command };

    // Set SourceId and Value
    context.SourceId = Id;
    if (this is IValue valueProvider)
    {
        context.Value = valueProvider.GetValue ();
    }

    // ... rest of method
}
```

---

## Testing Strategy

### Unit Tests

For each View implementing `IValue<T>`:

1. **Value property get/set**: Verify round-trip
2. **ValueChanging event**: Verify fires before change, can cancel
3. **ValueChanged event**: Verify fires after change with correct old/new values
4. **GetValue()**: Verify returns boxed value correctly
5. **Integration with existing property**: If `Value` maps to another property, verify sync

### Integration Tests

1. **Command propagation**: Verify `ctx.Value` is populated when command invoked
2. **Prompt pattern**: Verify `Prompt<TView, TResult>` extracts value correctly

### Test File Locations

- `Tests/UnitTestsParallelizable/Views/{ViewName}Tests.cs` - Per-view tests
- `Tests/UnitTestsParallelizable/ViewBase/IValueTests.cs` - Interface contract tests

---

## Migration Notes

### Breaking Changes

**None expected** - All changes are additive:
- New interface methods have default implementations
- Existing properties remain unchanged
- New `Value` properties are aliases, not replacements

### Deprecations

Consider deprecating (in future release):
- View-specific property names in favor of unified `Value`
- View-specific event names in favor of `ValueChanging`/`ValueChanged`

### Documentation Updates

1. Update `docfx/docs/View.md` - Document `IValue<T>` pattern
2. Update individual View API docs - Note `IValue<T>` implementation
3. Add examples showing generic value access

---

## Files to Modify

| File | Changes |
|------|---------|
| `Terminal.Gui/ViewBase/IValue.cs` | Add `IValue` interface, update `IValue<T>` |
| `Terminal.Gui/ViewBase/View.Command.cs` | Set `ctx.Value` in `InvokeCommand` |
| `Terminal.Gui/Views/NumericUpDown.cs` | Add `IValue<T>` interface |
| `Terminal.Gui/Views/LinearRange.cs` | Add `IValue<T>` interface |
| `Terminal.Gui/Views/Selectors/SelectorBase.cs` | Add `IValue<int?>` interface |
| `Terminal.Gui/Views/Selectors/OptionSelector.cs` | Verify interface inheritance |
| `Terminal.Gui/Views/Selectors/FlagSelector.cs` | Verify interface inheritance |
| `Terminal.Gui/Views/ScrollBar.cs` | Add `IValue<int>` interface |
| `Terminal.Gui/Views/CheckBox.cs` | Add `IValue<CheckState>` with `Value` alias |
| `Terminal.Gui/Views/TextField.cs` | Add `IValue<string>` with `Value` alias |
| `Terminal.Gui/Views/DateField.cs` | Add `IValue<DateTime>` with `Value` alias |
| `Terminal.Gui/Views/TimeField.cs` | Add `IValue<TimeSpan>` with `Value` alias |
| `Terminal.Gui/Views/DatePicker.cs` | Add `IValue<DateTime>` with `Value` alias |
| `Terminal.Gui/Views/ListView.cs` | Add `IValue<int?>` with `Value` alias |
| `Terminal.Gui/Views/CharMap.cs` | Add `IValue<Rune>` with `Value` alias |

---

## Open Questions

1. **Event adapter pattern**: For Views with existing events (`CheckedStateChanging`), should we:
   - (A) Add separate `ValueChanging`/`ValueChanged` events that delegate
   - (B) Rename existing events (breaking change)
   - (C) Use explicit interface implementation for events

2. **Nullable value types**: Should `Value` be nullable for all types, or match existing property nullability?

3. **TextField null handling**: `Text` is non-nullable `string`. Should `IValue<string>.Value` be `string?` and convert null to empty?

4. **ListView selection**: Current `SelectedItem` is `int`. What represents "no selection"? -1? Should we use `int?`?

---

## Success Criteria

1. All identified Views implement `IValue<T>`
2. `IValue.GetValue()` returns correct boxed value for all implementers
3. All existing tests pass (no regressions)
4. New tests cover `IValue<T>` contract for each View
5. `CommandContext.Value` populated correctly during command invocation
6. Build completes with no new warnings

---

## Revision History

| Date | Author | Changes |
|------|--------|---------|
| 2026-01-21 | Claude Opus 4.5 | Initial plan created |
