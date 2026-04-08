# Plan: Move Tab-Related Functionality from Border to BorderView

## Problem

`Border` is instantiated on **every** `View` — it should be as lightweight as possible. Currently it carries tab-related members (`TabSide`, `TabOffset`, `TabLength`, `TabEnd`, `EffectiveTabLength`, `SettingsChanged`) that are only meaningful when `BorderSettings.Tab` is active. This functionality belongs on `BorderView`, which is lazily created only when needed.

## Goals

1. **Minimize Border's footprint** — Border stores only `Thickness`, `LineStyle`, and `Settings`.
2. **Move tab configuration to BorderView** — `TabSide`, `TabOffset`, `TabLength`, `EffectiveTabLength` become properties on `BorderView`. `TabEnd` is deleted (zero consumers).
3. **Update all consumers** — backwards compatibility is not a concern. All call sites change from `view.Border.TabSide` to `((BorderView)view.Border.View!).TabSide` (or use a helper/local).
4. **No behavioral changes** — rendering, tests, and UICatalog scenarios produce identical output.

## Approach

**Option B: Move completely, remove from Border.** All tab properties are deleted from `Border` and added to `BorderView`. Every consumer is updated. This gives the cleanest separation.

---

## Current State Inventory

### Members on `Border` today

| Member | Kind | Tab-Only? | Consumers |
|--------|------|-----------|-----------|
| `Thickness` | inherited | No | Everywhere — **stays** |
| `LineStyle` | property | No | Everywhere — **stays** |
| `Settings` | property | No (but triggers tab setup) | Everywhere — **stays** |
| `SettingsChanged` | event | Yes (only subscriber: BorderView) | 1 internal — **remove** |
| `TabSide` | property | **Yes** | ~48 locations — **move** |
| `TabOffset` | property | **Yes** | ~80 locations — **move** |
| `TabLength` | property | **Yes** | ~13 locations — **move** |
| `TabEnd` | computed property | **Yes** | 0 consumers — **delete** |
| `EffectiveTabLength` | internal property | **Yes** | ~11 locations — **move** |

### Members on `BorderView` today

BorderView already has all the tab **rendering** logic. It reads tab configuration from `Border` via its `Adornment` reference. After this refactor, it owns the configuration too.

---

## Execution Order

Work in three phases, building green after each.

### Phase 1: `EffectiveTabLength` and `TabEnd`

Low-risk warm-up. `EffectiveTabLength` is `internal` and `TabEnd` is dead code.

### Phase 2: `TabSide`, `TabOffset`, `TabLength`

The bulk of the work — these are public properties with many consumers.

### Phase 3: `SettingsChanged` event

Cleanup — replace the event with a direct call.

---

## Phase 1: Move `EffectiveTabLength`, Delete `TabEnd`

### Step 1.1: Add `EffectiveTabLength` to `BorderView`

Add to `BorderView.cs` (tab support region):

```csharp
internal int EffectiveTabLength
{
    get
    {
        if (TabLength is { } explicitLength)
        {
            return explicitLength;
        }

        if (TitleView is not (ITitleView itv and View tv))
        {
            return 0;
        }

        if (itv.MeasuredTabLength > 0)
        {
            return itv.MeasuredTabLength;
        }

        // TitleView hasn't been laid out yet — set text and orientation, then measure.
        tv.Text = Adornment?.Parent?.Title ?? string.Empty;
        itv.Orientation = TabSide is Side.Left or Side.Right ? Orientation.Vertical : Orientation.Horizontal;

        int measured = TabSide is Side.Top or Side.Bottom ? tv.GetAutoWidth () : tv.GetAutoHeight ();
        itv.MeasuredTabLength = measured;

        return measured;
    }
}
```

Note: This initially reads `TabSide` and `TabLength` from `Border` (via `Adornment`). After Phase 2, these become local properties and the reads simplify.

### Step 1.2: Delete `EffectiveTabLength` from `Border`

Remove the full `EffectiveTabLength` property from `Border.cs`.

### Step 1.3: Update consumers of `EffectiveTabLength`

All consumers currently access `border.EffectiveTabLength` or `tab.Border.EffectiveTabLength`.

**Library code (`Tabs.cs`)** — 4 reads. Pattern: `tab.Border.EffectiveTabLength`. Change to:

```csharp
((BorderView)tab.Border.View!).EffectiveTabLength
```

Or introduce a local helper in `Tabs.cs`:

```csharp
private static BorderView GetBorderView (View tab) => (BorderView)tab.Border.View!;
```

Then: `GetBorderView (tab).EffectiveTabLength`

**Library code (`BorderView.cs`)** — 1 read in `DrawTabBorder`. Change `border.EffectiveTabLength` → `EffectiveTabLength` (now local).

**Tests** — ~5 assertions. Change `view.Border.EffectiveTabLength` → cast and access.

### Step 1.4: Delete `TabEnd` from `Border`

Remove the `TabEnd` computed property entirely. It has **zero consumers**.

### Step 1.5: Build and test

```bash
dotnet build --no-restore
dotnet test --project Tests/UnitTestsParallelizable --no-build
```

---

## Phase 2: Move `TabSide`, `TabOffset`, `TabLength`

### Step 2.1: Add properties to `BorderView`

Add to `BorderView.cs` (tab support region):

```csharp
public Side TabSide
{
    get;
    set
    {
        if (field == value)
        {
            return;
        }

        field = value;
        Adornment?.Parent?.SetNeedsLayout ();
    }
} = Side.Top;

public int TabOffset
{
    get;
    set
    {
        if (field == value)
        {
            return;
        }

        field = value;
        Adornment?.Parent?.SetNeedsLayout ();
    }
}

public int? TabLength
{
    get;
    set
    {
        if (field == value)
        {
            return;
        }

        field = value;
        Adornment?.Parent?.SetNeedsLayout ();
    }
}
```

### Step 2.2: Delete properties from `Border`

Remove `TabSide`, `TabOffset`, `TabLength` (including backing fields, setters, and XML docs) from `Border.cs`.

### Step 2.3: Update `BorderView` internal reads

All reads in `BorderView.cs` that go through `border.TabSide`, `border.TabOffset`, `border.TabLength` change to `TabSide`, `TabOffset`, `TabLength` (now `this`). Affected methods:

| Method | Properties read |
|--------|----------------|
| `ConfigureForTabMode()` | `border.TabSide` → `TabSide` |
| `UpdateTitleViewLayout()` | `border.TabSide`, `border.TabOffset`, `border.TabLength` → local |
| `GetTabBorderBounds()` | `border.TabSide` → `TabSide` |
| `DrawTabBorder()` | `border.TabSide`, `border.TabOffset`, `border.EffectiveTabLength` → local |
| `GetTabDepth()` | Uses `Adornment.Thickness` only — **no change** |
| `IsFocusedOrLastTab()` | No tab config reads — **no change** |

Also update `EffectiveTabLength` getter (from Phase 1) to read `TabSide`/`TabLength` from `this` instead of from Border.

### Step 2.4: Update `Tabs.cs`

This is the primary external consumer. All patterns are `view.Border.TabSide`, `tab.Border.TabOffset`, etc.

Add a static helper (or extension) to reduce cast noise:

```csharp
// In Tabs.cs (private helper)
private static BorderView GetBorderView (View tab) => (BorderView)tab.Border.View!;
```

Then update all call sites:

| Old | New |
|-----|-----|
| `view.Border.TabSide = _tabSide` | `GetBorderView (view).TabSide = _tabSide` |
| `tab.Border.TabOffset = offset` | `GetBorderView (tab).TabOffset = offset` |
| `tab.Border.TabLength = null` | `GetBorderView (tab).TabLength = null` |
| `tab.Border.EffectiveTabLength` | `GetBorderView (tab).EffectiveTabLength` |

Approximate count: ~15 sites in `Tabs.cs`.

### Step 2.5: Update `BorderEditor.cs`

`BorderEditor.cs` in `Examples/UICatalog/Scenarios/EditorsAndHelpers/` casts to `Border` and reads/writes `TabSide`, `TabOffset`. Change to cast to `BorderView` via `AdornmentToEdit.View`:

```csharp
// Old:
((Border)AdornmentToEdit).TabSide
// New:
((BorderView)AdornmentToEdit.View!).TabSide
```

Approximate count: ~5 sites.

### Step 2.6: Update `Adornments.cs` scenario

`Examples/UICatalog/Scenarios/Adornments.cs` reads `window.Border.TabSide`, `window.Border.TabOffset`, `window.Border.TabLength`. Change to access via `BorderView`:

```csharp
BorderView bv = (BorderView)window.Border.View!;
bv.TabSide ...
bv.TabOffset ...
```

Approximate count: ~5 sites.

### Step 2.7: Update `UICatalogRunnable.cs`

Has 2 commented-out references to `Border.TabSide`. Update or remove the comments.

### Step 2.8: Update tests

Tests that access `view.Border.TabSide`, `view.Border.TabOffset`, `view.Border.TabLength` need updating:

| Test file | Approx sites |
|-----------|-------------|
| `TabsTests.cs` | ~10 |
| `TabsScrollingTests.cs` | ~55 |
| `BorderViewTests.cs` | ~30 |
| `TitleViewTests.cs` | ~5 |
| `TabCompositionTests.cs` | ~3 |
| `AdornmentSubViewLineCanvasTests.cs` | ~2 |

Pattern: add a local helper or inline cast. For test files with many accesses, a helper at the top of the class:

```csharp
private static BorderView Bv (View v) => (BorderView)v.Border.View!;
```

### Step 2.9: Update docs

- `docfx/docs/borders.md` — update "Key Properties" table and code examples to use `BorderView` access pattern.
- `Border.cs` XML docs — remove tab-related examples and references.
- `BorderView.cs` XML docs — add docs for the new properties.
- `BorderSettings.cs` — update `Tab` doc to reference `BorderView.TabSide` etc. instead of `Border.TabSide`.

### Step 2.10: Build and test

```bash
dotnet build --no-restore
dotnet test --project Tests/UnitTestsParallelizable --no-build
dotnet test --project Tests/UnitTests --no-build
```

---

## Phase 3: Remove `SettingsChanged` Event

### Step 3.1: Replace event with direct call

In `Border.Settings` setter, replace:

```csharp
SettingsChanged?.Invoke (this, EventArgs.Empty);
```

with:

```csharp
(View as BorderView)?.ConfigureForTabMode ();
```

Make `ConfigureForTabMode` `internal` (currently `private`).

### Step 3.2: Remove event + subscription

- Delete `public event EventHandler? SettingsChanged;` from `Border.cs`.
- Delete `border.SettingsChanged += OnSettingsChanged;` from `BorderView` constructor.
- Delete the `OnSettingsChanged` bridge method from `BorderView`.

### Step 3.3: Build and test

```bash
dotnet build --no-restore
dotnet test --project Tests/UnitTestsParallelizable --no-build
```

---

## Files Changed

| File | Phase | Change |
|------|-------|--------|
| `Terminal.Gui/ViewBase/Adornment/Border.cs` | 1,2,3 | Remove `TabSide`, `TabOffset`, `TabLength`, `TabEnd`, `EffectiveTabLength`, `SettingsChanged`; update `Settings` setter |
| `Terminal.Gui/ViewBase/Adornment/BorderView.cs` | 1,2 | Add `TabSide`, `TabOffset`, `TabLength`, `EffectiveTabLength`; update all internal reads to use local props; make `ConfigureForTabMode` internal |
| `Terminal.Gui/ViewBase/Adornment/BorderSettings.cs` | 2 | Update XML doc for `Tab` to reference `BorderView` |
| `Terminal.Gui/Views/Tabs.cs` | 1,2 | Add helper; update ~15 call sites |
| `Examples/UICatalog/Scenarios/EditorsAndHelpers/BorderEditor.cs` | 2 | Update ~5 call sites |
| `Examples/UICatalog/Scenarios/Adornments.cs` | 2 | Update ~5 call sites |
| `Examples/UICatalog/UICatalogRunnable.cs` | 2 | Update 2 commented-out references |
| `Tests/.../TabsTests.cs` | 2 | Update ~10 sites |
| `Tests/.../TabsScrollingTests.cs` | 2 | Update ~55 sites |
| `Tests/.../BorderViewTests.cs` | 1,2 | Update ~30 sites |
| `Tests/.../TitleViewTests.cs` | 2 | Update ~5 sites |
| `Tests/.../TabCompositionTests.cs` | 2 | Update ~3 sites |
| `Tests/.../AdornmentSubViewLineCanvasTests.cs` | 2 | Update ~2 sites |
| `docfx/docs/borders.md` | 2 | Update property table and examples |

---

## Risk Assessment

| Risk | Mitigation |
|------|------------|
| `Tabs.cs` accesses tab properties before `BorderView` exists | `Tabs.cs` always sets `Border.Settings = Tab \| Title` first, which triggers `GetOrCreateView()` — `BorderView` exists before any tab property access |
| Tests that set `Border.TabOffset` without first enabling `BorderSettings.Tab` | These tests must also set `Border.Settings` to include `Tab` (which creates `BorderView`) before accessing tab properties. Fix in test updates. |
| Forgot a consumer — compile error | Good: compile errors are easy to find and fix. No silent runtime breakage. |
| `ConfigureForTabMode` visibility change | Making it `internal` is safe — it's only called from within the assembly |
