# SelectorBase Hierarchy Simplification Plan

## Context

The SelectorBase hierarchy has 5 classes across 6 files for what amounts to two concrete behaviors (single-select and multi-select). The user's intuition is that the factoring is dirty and could be simplified, and that OptionSelector/FlagSelector should be sealed.

**Current hierarchy:**
```
SelectorBase (abstract, public, 539 lines)
├── OptionSelector (public, 217 lines)
│   └── OptionSelector<TEnum> (sealed, 48 lines)
└── FlagSelector (public, 342 lines)
    └── FlagSelector<TFlagsEnum> (sealed, 43 lines)
```

**Key constraint:** SelectorBase is used polymorphically in UICatalog (`List<SelectorBase>`, `foreach (SelectorBase selector in ...)`), so it must remain public. ~165 tests cover this hierarchy.

---

## Problems Found

### P1: SelectorBase has FlagSelector-specific hooks
`OnCreatingSubViews()` and `OnCreatedSubViews()` are virtual methods in SelectorBase that ONLY FlagSelector overrides—they exist solely for the "None" checkbox hack. These pollute the base class API.

### P2: Manual checkbox Activating handlers are a workaround
Both subclasses subscribe to `checkbox.Activating` in `OnSubViewAdded` and manually call `InvokeCommand(Command.Activate, args.Context)` to re-invoke activation on themselves. OptionSelector even has a TODO:
```
// TODO: This should not be needed. Figure out why SelectorBase bubble up is not handling this properly.
```
`CommandsToBubbleUp = [Command.Activate, Command.Accept]` should make checkbox commands bubble up automatically. The manual event subscription + re-invocation is a **workaround** that adds ~40 lines per subclass.

### P3: FlagSelector overrides Value with duplicated logic
FlagSelector's Value setter duplicates the base pattern (same-value check, RaiseValueChanging, set field, UpdateChecked, RaiseValueChanged) but adds `_updatingChecked` re-entrancy guard and null-specific handling. The re-entrancy guard is needed because FlagSelector subscribes to `checkbox.ValueChanged` which calls back into `Value`, creating a feedback loop.

### P4: Cannot truly seal OptionSelector/FlagSelector
The generic wrappers (`OptionSelector<TEnum>`, `FlagSelector<TFlagsEnum>`) inherit from the non-generic versions, preventing `sealed`. These generic wrappers are ~45-line boilerplate each (shadow Value, shadow ValueChanged, override OnValueChanged, implement IValue.GetValue()).

### P5: SelectorStyles has FlagSelector-only flags
`ShowNoneFlag` and `ShowAllFlag` are documented as "Valid only for FlagSelector" but live in the shared enum.

---

## Recommended Changes (Priority Order)

### Step 1: Investigate and fix the bubble-up workaround (P2) — HIGH VALUE

**Why:** This is the core "dirty factoring" issue. Both subclasses work around broken command bubbling by manually intercepting checkbox events and re-invoking commands. Fixing this would eliminate:
- `OptionSelector.OnCheckboxOnActivating` (~35 lines)
- `FlagSelector.OnCheckboxOnActivating` (~20 lines)
- The `checkbox.Activating +=` subscriptions in both `OnSubViewAdded` methods
- Potentially `OptionSelector.CheckboxOnAccepted` handler too (redundant with OnActivated?)

**Investigation needed:** Why doesn't `CommandsToBubbleUp = [Command.Activate, Command.Accept]` handle the checkbox→selector bubbling automatically? Possible causes:
1. The bubbling fires OnActivating/OnActivated but the subclasses need to intercept BEFORE the checkbox processes the command (to prevent checkbox from toggling itself)
2. CWP event ordering: the `Activating` event fires during the checkbox's Raise flow, before bubbling happens
3. FlagSelector specifically needs to prevent the checkbox from self-toggling (it does the toggle manually in OnActivated)

**Approach:** Trace the command flow for a checkbox Space press through the bubbling system. Determine if the manual handlers can be replaced with OnActivating/OnActivated overrides that receive the bubbled command, or if the bubbling mechanism needs adjustment.

**Files:** `SelectorBase.cs`, `OptionSelector.cs`, `FlagSelector.cs`
**Tests:** All 165 selector tests, plus ShortcutTests.Command.cs (3 selector-related tests)

### Step 2: Remove FlagSelector-specific hooks from SelectorBase (P1) — MEDIUM VALUE

Make `CreateSubViews()` virtual. FlagSelector overrides it to handle None checkbox inline. Remove `OnCreatingSubViews()` and `OnCreatedSubViews()` from SelectorBase.

**FlagSelector.CreateSubViews override pattern:**
```csharp
public override void CreateSubViews ()
{
    // Pre-creation: add None checkbox if ShowNoneFlag and 0 not in Values
    bool addNone = Styles.HasFlag (SelectorStyles.ShowNoneFlag)
                   && Values is { } && !Values.Contains (0);

    base.CreateSubViews ();

    if (addNone)
    {
        // Insert None checkbox at position 0
        // (need to handle this differently since base already created views)
    }

    // Post-creation: remove zero-value checkbox if ShowNoneFlag not set
    if (!Styles.HasFlag (SelectorStyles.ShowNoneFlag))
    {
        CheckBox? noneCheckBox = SubViews.OfType<CheckBox> ()
            .FirstOrDefault (cb => (int)cb.Data! == 0);
        if (noneCheckBox is not null)
        {
            Remove (noneCheckBox);
            noneCheckBox.Dispose ();
        }
    }
}
```

**Files:** `SelectorBase.cs` (remove 2 virtual methods, make CreateSubViews virtual), `FlagSelector.cs` (override CreateSubViews, remove hook overrides)
**Tests:** No behavioral change, all tests pass as-is.

### Step 3: Simplify FlagSelector Value re-entrancy (P3) — MEDIUM VALUE

The `_updatingChecked` guard and `UncheckNone`/`UncheckAll` methods exist because:
1. `CheckboxOnValueChanged` → sets `Value` → causes `UpdateChecked()` → changes checkbox values → fires more `ValueChanged` events → infinite loop
2. Setting Value to null needs different unchecking logic than setting it to a valid flag value

**Approach:** Instead of the `_updatingChecked` guard scattered across 3 methods, use a single re-entrancy guard in the Value setter and simplify `UncheckNone`/`UncheckAll` into `UpdateChecked` itself (which already handles all flag states, just add null handling).

**Files:** `FlagSelector.cs`
**Tests:** FlagSelector tests, especially None flag tests and concurrent modification test.

### Step 4: Effectively seal via documentation + EditorBrowsable (P4) — LOW VALUE

Since true `sealed` requires eliminating generic subclasses (not practical), document that OptionSelector and FlagSelector are not intended for external subclassing.

- Add `[EditorBrowsable(EditorBrowsableState.Advanced)]` to constructors if we want to discourage accidental subclassing
- Add XML doc: "This class is not designed for external inheritance. Use `OptionSelector<TEnum>` for type-safe enum selection."

**Files:** `OptionSelector.cs`, `FlagSelector.cs`

### Step 5: Improve SelectorStyles documentation (P5) — LOW VALUE

Add to `ShowNoneFlag` and `ShowAllFlag` docs: "Has no effect on OptionSelector."

**Files:** `SelectorStyles.cs`

---

## What NOT to Change

- **Don't merge generic wrappers** — they're small, sealed, and necessary for the typed API
- **Don't consolidate checkbox event subscriptions** — the handlers have genuinely different logic
- **Don't make SelectorBase internal** — it's used polymorphically in UICatalog
- **Don't split SelectorStyles** — not worth the API complexity

---

## Verification

1. `dotnet build --no-restore` — no warnings
2. `dotnet test Tests/UnitTestsParallelizable --no-build --filter "FullyQualifiedName~Selector"` — all ~124 tests pass
3. `dotnet test Tests/UnitTestsParallelizable --no-build --filter "FullyQualifiedName~ShortcutTests"` — shortcut integration tests pass
4. `dotnet test Tests/UnitTestsParallelizable --no-build --filter "FullyQualifiedName~CommandBubbling"` — bubbling tests pass
5. Run UICatalog Selectors scenario manually to verify visual behavior

## Critical Files
- `Terminal.Gui/Views/Selectors/SelectorBase.cs`
- `Terminal.Gui/Views/Selectors/OptionSelector.cs`
- `Terminal.Gui/Views/Selectors/FlagSelector.cs`
- `Terminal.Gui/Views/Selectors/OptionSelectorTEnum.cs`
- `Terminal.Gui/Views/Selectors/FlagSelectorTEnum.cs`
- `Terminal.Gui/Views/Selectors/SelectorStyles.cs`
- `Tests/UnitTestsParallelizable/Views/SelectorBaseTests.cs`
- `Tests/UnitTestsParallelizable/Views/OptionSelectorTests.cs`
- `Tests/UnitTestsParallelizable/Views/FlagSelectorTests.cs`
- `Tests/UnitTestsParallelizable/ViewBase/ShortcutTests.Command.cs`
