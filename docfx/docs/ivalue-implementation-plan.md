# IValue&lt;T&gt; Implementation Plan

> **Status**: Implementation Complete
>
> **Created**: 2026-01-21
>
> **Completed**: 2026-01-21
>
> **Author**: Claude Opus 4.5
>
> **Related**: [Command Propagation Analysis](./command-propagation-analysis.md)

## Table of Contents

- [Overview](#overview)
- [Goals](#goals)
- [Design](#design)
- [Views Classification](#views-classification)
- [Implementation Summary](#implementation-summary)
- [Testing Strategy](#testing-strategy)
- [Migration Notes](#migration-notes)

---

## Overview

The `IValue<T>` interface provides a standardized way for Views to expose their primary value. This enables:

1. **Generic programming**: Code can work with any value-bearing View without knowing its specific type
2. **Command propagation**: `CommandContext.Value` can carry the source View's value up the hierarchy
3. **Prompt pattern**: `Prompt<TView, TResult>` can automatically extract results from Views implementing `IValue<T>`

---

## Goals

1. **Add non-generic `IValue` interface** - Enables boxing values for `CommandContext.Value` ✅
2. **Standardize value access** - All value-bearing Views implement `IValue<T>` ✅
3. **Preserve existing APIs** - Existing properties (`Text`, `Date`, `CheckedState`) remain; `Value` maps to them ✅
4. **Enable command propagation** - `InvokeCommand` can populate `ctx.Value` from any `IValue` implementer (pending CommandContext update)

---

## Design

### Non-Generic IValue Interface

Located in `Terminal.Gui/ViewBase/IValue.cs`:

```csharp
/// <summary>
/// Non-generic interface for accessing a View's value as a boxed object.
/// Used by command propagation to carry values without knowing the generic type.
/// </summary>
public interface IValue
{
    /// <summary>
    /// Gets the value as a boxed object.
    /// </summary>
    object? GetValue ();
}
```

### IValue&lt;T&gt; Interface

```csharp
/// <summary>
/// Interface for Views that provide a strongly-typed value.
/// </summary>
public interface IValue<TValue> : IValue
{
    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    TValue? Value { get; set; }

    /// <summary>
    /// Raised when <see cref="Value"/> is about to change.
    /// Set <see cref="ValueChangingEventArgs{T}.Handled"/> to cancel.
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
| **Deferred** | `TextView`, `TreeView<T>`, `HexView`, `ProgressBar`, `LinearRange<T>` | Complexity or semantic questions |

### Views That Implement IValue&lt;T&gt;

#### Already Implemented (Before This Work)

| View | Value Type | Status |
|------|------------|--------|
| `ColorPicker` | `Color?` | ✅ Already implemented |
| `AttributePicker` | `Attribute?` | ✅ Already implemented |
| `NumericUpDown<T>` | `T` | ✅ Already implemented |
| `NumericUpDown` | `int` | ✅ Already implemented |

#### Implemented in This Work

| View | Value Type | Existing Property | Status |
|------|------------|-------------------|--------|
| `CheckBox` | `CheckState` | `CheckedState` | ✅ Completed |
| `TextField` | `string` | `Text` | ✅ Completed |
| `SelectorBase` | `int?` | `Value` | ✅ Completed |
| `OptionSelector` | `int?` | Inherits from SelectorBase | ✅ Completed |
| `OptionSelector<TEnum>` | `TEnum?` | `Value` | ✅ Completed |
| `FlagSelector` | `int?` | Inherits from SelectorBase | ✅ Completed |
| `FlagSelector<TFlagsEnum>` | `TFlagsEnum?` | `Value` | ✅ Completed |
| `ScrollBar` | `int` | `Position` | ✅ Completed |
| `DateField` | `DateTime?` | `Date` | ✅ Completed |
| `TimeField` | `TimeSpan` | `Time` | ✅ Completed |
| `DatePicker` | `DateTime` | `Date` | ✅ Completed |
| `ListView` | `int?` | `SelectedItem` | ✅ Completed |
| `CharMap` | `Rune` | `SelectedCodePoint` | ✅ Completed |

#### Deferred

| View | Reason |
|------|--------|
| `LinearRange<T>` | Complex multi-value semantics - see detailed analysis below |

### LinearRange&lt;T&gt; Design Challenges

`LinearRange<T>` presents unique challenges that don't fit the simple `IValue<T>` pattern. Here's why:

#### 1. Multiple Selection Types (`LinearRangeType`)

LinearRange supports five different selection modes, each with different value semantics:

| Type | Selection Semantics | What is "the value"? |
|------|---------------------|---------------------|
| `Single` | One option selected | `T` - the selected option's data |
| `Multiple` | Multiple options selected | `List<T>` - all selected options' data |
| `Range` | Start and end points | `(T, T)` - tuple of start/end data |
| `LeftRange` | From start to selected | `T` - the end point's data |
| `RightRange` | From selected to end | `T` - the start point's data |

**Question**: What type parameter should `IValue<???>` use? The answer depends on the `Type` property.

#### 2. Index vs Data Value

LinearRange has two distinct concepts:

- **Indices** (`_setOptions: List<int>`) - Which positions are selected
- **Data** (`Options[i].Data: T`) - The actual typed values at those positions

For example:
```csharp
LinearRange<string> range = new (["Low", "Medium", "High"]);
range.SetOption(1);  // Selects index 1
// Index value: 1
// Data value: "Medium"
```

**Question**: Should `Value` return the index or the data? Most use cases want the data, but indices are simpler to work with programmatically.

#### 3. No Single Value Property

Unlike other Views with a clear "main value" property:

| View | Main Property | Type |
|------|--------------|------|
| CheckBox | `CheckedState` | `CheckState` |
| TextField | `Text` | `string` |
| DateField | `Date` | `DateTime?` |
| LinearRange | ??? | ??? |

LinearRange exposes:
- `GetSetOptions()` → `List<int>` (selected indices)
- `Options` → `List<LinearRangeOption<T>>` (all options)
- `FocusedOption` → `int` (cursor position, not necessarily selected)

To get the actual `T` values requires: `GetSetOptions().Select(i => Options[i].Data)`

#### 4. Potential Solutions

**Option A: IValue&lt;T?&gt; for Single Selection Only**
```csharp
public class LinearRange<T> : View, IValue<T?>
{
    public T? Value => _setOptions.Count > 0 ? Options[_setOptions[0]].Data : default;
}
```
- Pro: Simple, works for Single/LeftRange/RightRange
- Con: Loses information for Multiple and Range types

**Option B: IValue&lt;IReadOnlyList&lt;T&gt;&gt;**
```csharp
public class LinearRange<T> : View, IValue<IReadOnlyList<T>>
{
    public IReadOnlyList<T> Value => GetSetOptions().Select(i => Options[i].Data).ToList();
}
```
- Pro: Works for all types
- Con: Always returns a list even for single selection; awkward API

**Option C: Multiple Interfaces Based on Type**
```csharp
// Different classes for different selection modes
public class SingleSelectLinearRange<T> : LinearRange<T>, IValue<T?> { }
public class MultiSelectLinearRange<T> : LinearRange<T>, IValue<IReadOnlyList<T>> { }
public class RangeLinearRange<T> : LinearRange<T>, IValue<(T, T)> { }
```
- Pro: Type-safe, clear semantics
- Con: Breaking change, proliferation of types

**Option D: Custom Value Type**
```csharp
public record LinearRangeValue<T>(LinearRangeType Type, IReadOnlyList<T> Values)
{
    public T? Single => Type == LinearRangeType.Single ? Values.FirstOrDefault() : default;
    public (T?, T?) Range => Type == LinearRangeType.Range ? (Values.ElementAtOrDefault(0), Values.ElementAtOrDefault(1)) : default;
}

public class LinearRange<T> : View, IValue<LinearRangeValue<T>> { }
```
- Pro: Captures full semantics
- Con: Complex, non-obvious API

#### 5. Recommendation

Defer implementation until a design decision is made. The most pragmatic approach may be **Option A** (single value for Single type), with documentation that `IValue<T>` only applies when `Type == LinearRangeType.Single`. Users needing multi-select can use `GetSetOptions()` directly.

Alternatively, consider whether `LinearRange<T>` should even implement `IValue<T>`. Its primary use case is interactive range/slider selection, not value propagation through command hierarchy.

---

## Implementation Summary

### Implementation Pattern

Each View follows this pattern:

1. **Add interface to class declaration**: `public class ViewName : BaseClass, IValue<T>`
2. **Add `Value` property** that aliases the existing property (e.g., `Date`, `Time`, `SelectedItem`)
3. **Add `ValueChanging` event** with cancellation support
4. **Add `ValueChanged` event** with old/new values
5. **Update existing property setter** to:
   - Check for equality (early return if same)
   - Call `RaiseValueChanging()` (return if cancelled)
   - Set backing field
   - Call existing event/method
   - Call `RaiseValueChanged()`
6. **Add explicit `IValue.GetValue()`** when needed (derived classes shadowing base Value type)

### Files Modified

| File | Changes | Status |
|------|---------|--------|
| `Terminal.Gui/ViewBase/IValue.cs` | Added `IValue` interface, updated `IValue<T>` | ✅ |
| `Terminal.Gui/Views/CheckBox.cs` | Added `IValue<CheckState>` with `Value` alias | ✅ |
| `Terminal.Gui/Views/TextInput/TextField/TextField.cs` | Added `IValue<string>` with `Value` alias | ✅ |
| `Terminal.Gui/Views/TextInput/TextField/TextField.Text.cs` | Updated Text setter for events | ✅ |
| `Terminal.Gui/Views/Selectors/SelectorBase.cs` | Added `IValue<int?>`, updated events | ✅ |
| `Terminal.Gui/Views/Selectors/FlagSelector.cs` | Updated Value setter for new events | ✅ |
| `Terminal.Gui/Views/Selectors/FlagSelectorTEnum.cs` | Added explicit `IValue.GetValue()` | ✅ |
| `Terminal.Gui/Views/Selectors/OptionSelectorTEnum.cs` | Added explicit `IValue.GetValue()` | ✅ |
| `Terminal.Gui/Views/ScrollBar/ScrollBar.cs` | Added `IValue<int>` with `Value` alias for `Position` | ✅ |
| `Terminal.Gui/Views/TextInput/DateField.cs` | Added `IValue<DateTime?>` with `Value` alias | ✅ |
| `Terminal.Gui/Views/TextInput/TimeField.cs` | Added `IValue<TimeSpan>` with `Value` alias | ✅ |
| `Terminal.Gui/Views/DatePicker.cs` | Added `IValue<DateTime>` with `Value` alias | ✅ |
| `Terminal.Gui/Views/ListView.cs` | Added `IValue<int?>` with `Value` alias | ✅ |
| `Terminal.Gui/Views/CharMap/CharMap.cs` | Added `IValue<Rune>` with `Value` alias | ✅ |

### Additional Files Updated

The following files were updated to use `ValueChangedEventArgs<T>.NewValue` instead of `.Value`:

- `Examples/UICatalog/Scenarios/PosEditor.cs`
- `Examples/UICatalog/Scenarios/DimEditor.cs`
- `Examples/UICatalog/Scenarios/UICatalogRunnable.cs`
- `Examples/UICatalog/Scenarios/ColorPicker.cs`
- `Examples/UICatalog/Scenarios/MarginEditor.cs`
- `Examples/UICatalog/Scenarios/Themes.cs`
- `Examples/UICatalog/Scenarios/TextAlignmentAndDirection.cs`
- `Examples/UICatalog/Scenarios/Shortcuts.cs`
- `Tests/UnitTestsParallelizable/Views/FlagSelectorTests.cs`
- `Tests/UnitTestsParallelizable/Views/SelectorBaseTests.cs`

---

## Design Decisions Made

### 1. Event Pattern

**Decision**: Add separate `ValueChanging`/`ValueChanged` events alongside existing events.

For Views with existing events (e.g., `CheckedStateChanging`), we added new `ValueChanging`/`ValueChanged` events that fire in addition to the existing ones. This preserves backward compatibility while providing the standard interface.

### 2. Nullable Value Types

**Decision**: Match the semantics of the existing property where possible.

- `DateField` uses `DateTime?` because `Date` can be null
- `TimeField` uses `TimeSpan` (non-nullable) to match `Time`
- `ListView` uses `int?` because `SelectedItem` can be null (no selection)

### 3. Derived Class Shadowing

**Decision**: Use `new` keyword and explicit interface implementation when needed.

When a derived class (e.g., `DateField`) has a different `Value` type than its base (e.g., `TextField`), we use:
- `new` keyword on the `Value` property
- `new` keyword on `ValueChanging`/`ValueChanged` events
- Explicit `object? IValue.GetValue()` implementation

### 4. Property Aliasing vs Renaming

**Decision**: Add `Value` as an alias, keep existing properties.

The `Value` property delegates to the existing property (`Date`, `Time`, `SelectedItem`, etc.). This maintains full backward compatibility - no breaking changes.

### 5. LinearRange Deferral

**Decision**: Defer `LinearRange<T>` implementation.

`LinearRange<T>` has a complex design with `Start`, `End`, and computed values. It doesn't fit the simple single-value pattern. A separate design discussion is needed.

---

## Testing Strategy

### Unit Tests

For each View implementing `IValue<T>`:

1. **Value property get/set**: Verify round-trip
2. **ValueChanging event**: Verify fires before change, can cancel
3. **ValueChanged event**: Verify fires after change with correct old/new values
4. **GetValue()**: Verify returns boxed value correctly
5. **Integration with existing property**: If `Value` maps to another property, verify sync

### Test Results

All 109 related tests pass:
- DateField tests
- TimeField tests
- ListView tests
- DatePicker tests
- CharMap tests
- SelectorBase tests
- FlagSelector tests

---

## Migration Notes

### Breaking Changes

**ValueChangedEventArgs**: The `Value` property was renamed to `NewValue` for clarity (alongside `OldValue`). Code using `.Value` must change to `.NewValue`.

```csharp
// Before
selector.ValueChanged += (s, e) => DoSomething(e.Value);

// After
selector.ValueChanged += (s, e) => DoSomething(e.NewValue);
```

### Non-Breaking Additions

- All `IValue<T>` implementations are additive
- Existing properties remain unchanged
- New `Value` properties are aliases, not replacements

---

## Future Work

1. **CommandContext.Value Integration**: Update `View.Command.cs` `InvokeCommand()` to set `ctx.Value` from `IValue` implementers
2. **LinearRange<T>**: Design and implement appropriate value pattern
3. **TextView**: Consider `IValue<string>` for text content
4. **TreeView<T>**: Consider `IValue<T?>` for selected item

---

## Revision History

| Date | Author | Changes |
|------|--------|---------|
| 2026-01-21 | Claude Opus 4.5 | Initial plan created |
| 2026-01-21 | Claude Opus 4.5 | Implementation completed for all identified Views except LinearRange<T> |
