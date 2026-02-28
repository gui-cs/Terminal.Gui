# Plan: Composite View Value Propagation in the Command Chain

## Context

When a composite view (`OptionSelector`, `FlagSelector`) consumes a command dispatch from a child
`CheckBox`, `CommandContext.Value` flowing up the chain carries the **child's** value (`CheckState`),
not the composite's semantic value (`int?` index or bitmask). This forces consumers like
`PopoverMenus.cs` to work around it by navigating `source.SuperView` and reading `.Value` directly.

**The bug is confirmed real.** `PopoverMenus.cs` line ~143 must navigate
`source?.SuperView is OptionSelector<Schemes>` and read `schemeOptionSelector.Value` directly
because `args.Value.Value` delivers `CheckState`, not `Schemes`. Same pattern appears in `Menus.cs`.

### Root Cause Trace

Path: CheckBox clicked inside OptionSelector (BubblingUp + ConsumeDispatch)

1. CheckBox calls `InvokeCommand(Activate)` →
   `CommandContext { Source=CheckBox, Value=CheckState.Checked }`
2. OptionSelector's `DefaultActivateHandler` runs; `ConsumeDispatch=true` →
   `_dispatchState |= DispatchOccurred`
3. `RefreshValue(ctx)` asks `GetDispatchTarget()` (the CheckBox) for its value →
   `ctx.Value = CheckState.Checked` (still wrong)
4. `RaiseActivated(ctx)` → `OptionSelector.OnActivated(ctx)`:

```csharp
// Current OptionSelector.OnActivated — CWP violation:
protected override void OnActivated (ICommandContext? ctx)
{
    base.OnActivated (ctx);    // fires Activated: ctx.Value = CheckState  ← WRONG
    ApplyActivation (ctx);     // updates this.Value to int?               ← TOO LATE
}
```

5. `BubbleActivatedUp(ctx)` propagates `ctx.Value = CheckState` to ancestors ← ALSO WRONG

The existing self-refresh block (View.Command.cs lines 559–565) only fires when
`ReferenceEquals(src, this)`, which is false here (src = CheckBox, this = OptionSelector).

**Same pattern in `FlagSelector.OnActivated`**: `base.OnActivated(ctx)` fires before the
checkbox toggle which updates `this.Value` via `CheckboxOnValueChanged`.

---

## Files Involved

| File | Role |
|------|------|
| `Terminal.Gui/Views/Selectors/OptionSelector.cs` | `OnActivated`, `ApplyActivation` |
| `Terminal.Gui/Views/Selectors/FlagSelector.cs` | `OnActivated` |
| `Terminal.Gui/ViewBase/View.Command.cs` | `DefaultActivateHandler`, `RefreshValue`, `BubbleActivatedUp` |
| `Terminal.Gui/Input/CommandContext.cs` | `CommandContext` record struct |
| `Terminal.Gui/Input/ICommandContext.cs` | `ICommandContext` interface |
| `Examples/UICatalog/Scenarios/PopoverMenus.cs` | Consumer that uses the workaround |
| `Examples/UICatalog/Scenarios/Menus.cs` | Consumer that uses the same workaround |
| `docfx/docs/command.md` | ConsumeDispatch / Selector documentation |

---

## RED Regression Tests (Already Written)

Four tests in `Tests/UnitTestsParallelizable/` document the bug and will turn GREEN after the fix:

- `OptionSelectorTests.Activated_Event_Value_Is_OptionSelector_Int_Not_CheckState`
  — direct subscriber on the composite receives `CheckState` (bug); must receive `int?`.

- `FlagSelectorTests.Activated_Event_Value_Is_FlagSelector_Bitmask_Not_CheckState`
  — same, must receive `int?` bitmask.

- `OptionSelectorTests.Activated_Event_Ancestor_Receives_OptionSelector_Value_Via_BubbleActivatedUp`
  — ancestor with `CommandsToBubbleUp` receives `CheckState` (bug); must receive `int?`.

- `FlagSelectorTests.Activated_Event_Ancestor_Receives_FlagSelector_Value_Via_BubbleActivatedUp`
  — same, must receive `int?` bitmask.

All four currently fail with:
```
Assert.Null() Failure: Value of type 'Nullable<CheckState>' has a value
Expected: null
Actual:   Checked
```

---

## Option B: Value Chain (`ICommandContext.Values`)

**Philosophy:** The command context accumulates all `IValue` values encountered up the chain,
like a call stack. Consumers can inspect the full chain or use `ctx.Value` as a shortcut to
the most recently pushed value (the outermost composite's value).

### New API

```csharp
// ICommandContext addition:
public IReadOnlyList<object?> Values { get; }

// Value becomes a convenience accessor:
public object? Value => Values.Count > 0 ? Values [^1] : null;
```

```csharp
// CommandContext changes:
public IReadOnlyList<object?> Values { get; init; } = [];
public object? Value => Values.Count > 0 ? Values [^1] : null;  // convenience

public CommandContext WithValue (object? value) =>
    this with { Values = [..Values, value] };  // appends, not replaces
```

### Framework Behavior

- `InvokeCommand` appends the source view's value: `Values = [CheckState.Checked]`
- `RefreshValue` appends the dispatch target's value: `Values = [CheckState.Checked]` (same)
- After `RaiseActivated`, post-refresh appends composite's value:
  `Values = [CheckState.Checked, Schemes.Dark]`
- Consumer sees `ctx.Value = Schemes.Dark` (last) and can inspect `ctx.Values` for the full chain

### Subscriber Examples

**Direct subscriber on composite (test pattern, simple case):**
```csharp
// After fix: ctx.Value = int? index.  ctx.Values = [CheckState.Checked, (int?)1]
optionSelector.Activated += (_, args) =>
{
    if (args.Value?.Value is int? index) // Schemes, int? — works cleanly
    {
        ApplyScheme (index);
    }
};
```

**Ancestor subscriber with no intermediate composite (`BubbleActivatedUp` path):**
```csharp
// ancestor.CommandsToBubbleUp = [Command.Activate];
// ctx.Values = [CheckState.Checked, (int?)1]
// ctx.Value = (int?)1 — correct, no workaround needed
ancestor.Activated += (_, args) =>
{
    if (args.Value?.Value is int? scheme) { ApplyScheme (scheme); }
};
```

**Multi-layer: OptionSelector inside MenuItem inside PopoverMenu:**
```csharp
// BubbleActivatedUp chain appends at each IValue layer:
// ctx.Values = [CheckState.Checked, Schemes.Dark, MenuItem]
// ctx.Value = MenuItem (last)
// Subscriber must pick the right index — or use ctx.Value (MenuItem) and navigate:
_appWindow.Activated += (_, args) =>
{
    // Option 1: use ctx.Value (MenuItem, the last appended)
    if (args.Value?.Value is MenuItem { CommandView: OptionSelector<Schemes> sel })
    {
        ApplyScheme (sel.Value);
    }

    // Option 2: inspect the chain by type (index-free but requires knowledge of depth)
    object? scheme = args.Value?.Values.FirstOrDefault (v => v is Schemes);
};
```

**MenuBars scenario (MenuItem path, already working via `Menu.IValue<MenuItem?>`):**
```csharp
// ctx.Values = [MenuItem, MenuItem] (same value, appended at MenuItem + Menu levels)
// ctx.Value = MenuItem (last) — clean
MenuBar?.Activated += (_, args) =>
{
    if (args?.Value?.Value is MenuItem menuItem)
    {
        lastActivatedText.Text = menuItem.Title!;
    }
};
```

**Pros:** More information-rich; `ctx.Value` remains the "right" value; full chain available for
debugging and complex scenarios; subscriber can always recover any level's value.
**Cons:** Allocates a new list on each bubble step; `ICommandContext` interface gains new member;
in multi-layer scenarios subscriber must know which index (or use `.FirstOrDefault`) to find the
right value — couples subscriber to hierarchy depth/composition.

---

## Option C: Declarative `OpaqueCommandValue` Flag

**Philosophy:** A composite view declaratively states "my subviews' `IValue` values are private
to me; I own the value semantics for this command context." The framework enforces the
substitution; composites don't need to manually inject values into the context.

### New API

```csharp
// On View:
/// <summary>
/// When <see langword="true"/>, the framework uses this view's own
/// <see cref="IValue.GetValue"/> result for <see cref="CommandContext.Value"/>
/// after consuming a dispatch, rather than the dispatch target's value.
/// </summary>
/// <remarks>
/// Set this to <see langword="true"/> on composite views (e.g., OptionSelector,
/// FlagSelector) that implement <see cref="IValue"/> and want their semantic value
/// — not their child CheckBox's CheckState — to appear in ctx.Value for ancestor
/// subscribers.
/// </remarks>
protected virtual bool OpaqueCommandValue => false;
```

### Framework Behavior

**`View.Command.cs` — `DefaultActivateHandler` `DispatchOccurred` branch:**

```csharp
if (_dispatchState.HasFlag (DispatchState.DispatchOccurred))
{
    ctx = RefreshValue (ctx);

    RaiseActivated (ctx);

    // If the composite declares OpaqueCommandValue, replace ctx.Value with
    // the composite's own post-mutation value before notifying ancestors.
    if (OpaqueCommandValue && this is IValue selfOpaque && ctx is CommandContext ccOpaque)
    {
        ctx = ccOpaque.WithValue (selfOpaque.GetValue ());
    }

    BubbleActivatedUp (ctx);
}
```

### Selector Declarations

```csharp
// OptionSelector:
protected override bool OpaqueCommandValue => true;

// FlagSelector:
protected override bool OpaqueCommandValue => true;
```

**Note:** The `OnActivated` ordering fix is still required for `Activated` direct subscribers.
`OpaqueCommandValue` only controls the `BubbleActivatedUp` context value.

### Subscriber Examples

**Direct subscriber on composite (test pattern, simple case) — requires `OnActivated` ordering fix:**
```csharp
// After fix: ctx.Value = int? index.
optionSelector.Activated += (_, args) =>
{
    if (args.Value?.Value is int? index) { ApplyScheme (index); }
};
```

**Ancestor subscriber with no intermediate composite (`BubbleActivatedUp` path):**
```csharp
// ancestor.CommandsToBubbleUp = [Command.Activate];
// OptionSelector.OpaqueCommandValue = true → ctx.Value = (int?)1
// No PopoverMenu in chain → value passes through unchanged
ancestor.Activated += (_, args) =>
{
    if (args.Value?.Value is int? scheme) { ApplyScheme (scheme); }
};
```

**Multi-layer: OptionSelector inside MenuItem inside PopoverMenu.**

If PopoverMenu sets `OpaqueCommandValue = true`:
```csharp
// ctx.Value = MenuItem (PopoverMenu replaces int? with its own IValue result)
// Subscriber navigates via MenuItem.CommandView to get the composite value.
// This is the same navigation currently required, just through a different route:

// CURRENT workaround (navigate via ctx.Source.SuperView):
if (args.Value?.TryGetSource (out source) is true
    && source?.SuperView is OptionSelector<Schemes> { Id: "schemeOptionSelector" } sel)
{
    if (sel.Value is { } scheme) { ApplyScheme (scheme); }
}

// WITH Option C + PopoverMenu.OpaqueCommandValue=true (navigate via ctx.Value as MenuItem):
if (args.Value?.Value is MenuItem { Id: "schemeMenuItem", CommandView: OptionSelector<Schemes> sel })
{
    if (sel.Value is { } scheme) { ApplyScheme (scheme); }
}
```

If PopoverMenu does **not** set `OpaqueCommandValue`:
```csharp
// ctx.Value = (int?)1 from OptionSelector passes through unchanged — cleanest subscriber:
if (args.Value?.Value is int? scheme) { ApplyScheme (scheme); }
```

**MenuBars scenario (MenuItem path, already working):**
```csharp
// Menu.OpaqueCommandValue = true (or Menu already sets value via IValue<MenuItem?>)
// ctx.Value = MenuItem — unchanged, continues to work
MenuBar?.Activated += (_, args) =>
{
    if (args?.Value?.Value is MenuItem menuItem)
    {
        lastActivatedText.Text = menuItem.Title!;
    }
};
```

**Menus.cs and PopoverMenus.cs — before and after (OptionSelector in menu):**
```csharp
// BEFORE (current workaround — ctx.Value = CheckState, must navigate SuperView):
if (args.Value?.TryGetSource (out source) is true
    && source?.SuperView is OptionSelector<Schemes> { Id: "schemeOptionSelector" } schemeOptionSelector)
{
    if (schemeOptionSelector.Value is { } scheme) { _appWindow.SchemeName = scheme.ToString (); }
}

// AFTER with Option C, PopoverMenu.OpaqueCommandValue NOT set
// (ctx.Value = Schemes flows through unchanged — workaround unnecessary):
if (args.Value?.Value is Schemes scheme) { _appWindow.SchemeName = scheme.ToString (); }

// AFTER with Option C, PopoverMenu.OpaqueCommandValue = true
// (ctx.Value = MenuItem — navigate via CommandView, still cleaner than current workaround):
if (args.Value?.Value is MenuItem { CommandView: OptionSelector<Schemes> sel }
    && sel.Value is { } scheme)
{
    _appWindow.SchemeName = scheme.ToString ();
}
```

**Pros:** Explicit, intentional; framework handles the substitution; future composites just set
the flag; named concept makes the design easy to understand; a view implementing `IValue` for
unrelated purposes does not accidentally affect the command value chain unless it opts in.
**Cons:** Still requires `OnActivated` ordering fix for direct subscribers; adds a new
`protected virtual` property to `View`; the flag only helps the `BubbleActivatedUp` path.

---

## Comparison

| Criterion | B: Value Chain | C: Opaque Flag |
|-----------|----------------|----------------|
| Public API changes | `ICommandContext.Values`; `Value` becomes `Values[^1]` | `View.OpaqueCommandValue` (protected virtual, additive) |
| Framework changes | `RefreshValue` + multiple sites | +6 lines in `DefaultActivateHandler` |
| Selector changes | Reorder `OnActivated` + append value | Reorder `OnActivated` + set flag |
| Direct subscriber | `ctx.Value = composite's value` ✓ | `ctx.Value = composite's value` ✓ |
| Ancestor (no wrapper) | `ctx.Value = composite's value` ✓ | `ctx.Value = composite's value` ✓ |
| Ancestor (wrapped in PopoverMenu, OpaqueCommandValue=true) | `ctx.Value = outermost value`; inner value at `ctx.Values[n]` | `ctx.Value = outermost value`; inner value via `CommandView` |
| Information available | Full value chain in `ctx.Values` | Single value (outermost opaque composite's) |
| Future composite support | Convention (must append) | Declaration (set flag) |
| Accidental participation | Any `IValue` in chain appends (by design, but verbose) | Only explicit `OpaqueCommandValue=true` views participate |
| `ctx.Value` semantics | `Values[^1]` — last appended (outermost composite) | Single value (replaced by each `OpaqueCommandValue` composite) |

---

## Tests (All Options)

Four RED regression tests already written in `Tests/UnitTestsParallelizable/`. These document
the bug and will turn GREEN once either option is implemented:

- `OptionSelectorTests.Activated_Event_Value_Is_OptionSelector_Int_Not_CheckState`
- `FlagSelectorTests.Activated_Event_Value_Is_FlagSelector_Bitmask_Not_CheckState`
- `OptionSelectorTests.Activated_Event_Ancestor_Receives_OptionSelector_Value_Via_BubbleActivatedUp`
- `FlagSelectorTests.Activated_Event_Ancestor_Receives_FlagSelector_Value_Via_BubbleActivatedUp`

---

## Verification

1. `dotnet build --no-restore` — no new warnings
2. `dotnet test Tests/UnitTestsParallelizable --no-build` — all four new tests pass (turn GREEN)
3. Run UICatalog `PopoverMenus` scenario:
   - Scheme selector changes color scheme correctly
   - Borders checkbox toggles borders
   - EventLog shows correct values (not `CheckState`)
4. Confirm `args.Value.Value` in `_appWindow.Activated` is `Schemes` enum, not `CheckState`
5. Confirm `MenuBars` scenario still shows correct `MenuItem` in "Last Activated (from ctx.Value)"

---

## Rejected Options

### ~~Option A: Ordering Fix + Framework Self-Refresh~~ *(Considered and rejected)*

**Why rejected:** Option A applies the post-`RaiseActivated` self-refresh unconditionally to
**any** `IValue`-implementing view in the `BubbleActivatedUp` chain
(`if (this is IValue compositeValue)`). This creates a hidden coupling: if a view implements
`IValue` for any purpose, it silently replaces `ctx.Value` as the command bubbles through it.
In a multi-layer hierarchy (e.g., OptionSelector inside a MenuItem inside PopoverMenu), the
subscriber at the top always sees the value of the *outermost* `IValue`-implementing ancestor,
not the originating composite's value — regardless of design intent. Adding `IValue` to any
view for unrelated reasons would accidentally change command value semantics. Option C solves
this with an explicit `OpaqueCommandValue` opt-in; Option B solves it by retaining the full
chain. Option A provides neither the explicitness of C nor the richness of B.

---

## Implementation: Option B Selected

### API Changes

1. **`ICommandContext.Values`**: New `IReadOnlyList<object?> Values` property. Contains all values
   accumulated as the command propagates up the view hierarchy.

2. **`ICommandContext.Value`**: Now a computed property returning `Values[^1]` (the last appended
   value). Returns `null` when `Values` is empty.

3. **`CommandContext.WithValue(object?)`**: Changed from replacing `Value` to appending to `Values`.
   Returns a new context with the value appended to the chain.

### Framework Changes

1. **`InvokeCommand`**: Initial context created with `Values = [sourceValue]` when source implements
   `IValue`; `Values = []` for non-`IValue` views.

2. **`RefreshValue`**: Appends (not replaces) the dispatch target's post-change value to the chain.

3. **`RaiseActivated`**: After `OnActivated` completes, if `DispatchOccurred` and `this is IValue`,
   appends the composite's post-mutation value. This ensures direct `Activated` subscribers see the
   composite's semantic value. Guarded by `_dispatchState.HasFlag(DispatchOccurred)` to prevent
   unrelated `IValue` ancestors (like MenuBar receiving a bridged command) from appending stale values.

4. **`DefaultActivateHandler` ConsumeDispatch branch**: After `RaiseActivated`, also appends the
   composite's value for the `BubbleActivatedUp` context (since `RaiseActivated`'s local append
   doesn't propagate back to the caller due to struct value semantics).

5. **`BubbleActivatedUp`**: Carries `Values` chain through ancestor notifications. Dispatch-target
   refresh preserved for the `ReferenceEquals(source, dispatchTarget)` case.

6. **`CommandBridge`**: Carries `Values` (not just `Value`) across bridge boundaries.

### Key Design Decisions

- **`_dispatchState.HasFlag(DispatchOccurred)` guard in `RaiseActivated`**: MenuBar inherits IValue
  from Menu. Without this guard, BubbleActivatedUp → RaiseActivated on MenuBar would append
  `MenuBar.GetValue()` which returns null (the *MenuBar's* Menu._value, not the PopoverMenu's
  Menu._value). The DispatchOccurred flag only fires when the view's own DefaultActivateHandler
  consumed a dispatch, preventing spurious appends from bridged/bubbled contexts.

- **Struct value semantics**: `CommandContext` is a readonly record struct. `RaiseActivated` appends
  to a local copy; the caller's ctx is unaffected. Both the ConsumeDispatch branch and the self-
  refresh block must separately append to their own ctx references.

### Verification Results

- 4 RED regression tests → GREEN:
  - `OptionSelectorTests.Activated_Event_Value_Is_OptionSelector_Int_Not_CheckState`
  - `FlagSelectorTests.Activated_Event_Value_Is_FlagSelector_Bitmask_Not_CheckState`
  - `OptionSelectorTests.Activated_Event_Ancestor_Receives_OptionSelector_Value_Via_BubbleActivatedUp`
  - `FlagSelectorTests.Activated_Event_Ancestor_Receives_FlagSelector_Value_Via_BubbleActivatedUp`
- 5 new base-level ViewCommandTests (using test-only classes):
  - `Values_ConsumeDispatch_Composite_Appends_Own_Value`
  - `Values_BubbleActivatedUp_Carries_Composite_Value_To_Ancestor`
  - `Values_Chain_Accumulates_From_Source_Through_Composites`
  - `Values_Bridge_Preserves_Full_Chain`
  - `Values_NonIValue_View_Has_Empty_Values`
- 14,357 parallelizable tests pass, 0 failures
- 1,012 non-parallelizable tests pass, 0 failures
