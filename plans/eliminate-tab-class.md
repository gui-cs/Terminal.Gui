# Plan: Eliminate Tab Class - Make Tabs Work with Any View

## Goal

Remove the `Tab` class entirely. `Tabs` should accept any `View` as a tab, configuring it with the necessary border/layout properties on add. This makes `Tabs` a general-purpose tabbed container rather than requiring a special `Tab` subclass.

## Current Architecture

- **`Tab`** (`Tab.cs`): A `View` subclass that sets specific defaults in its constructor:
  - `TabStop = TabBehavior.TabStop`, `CanFocus = true`
  - `BorderStyle = LineStyle.Rounded`
  - `Border.Settings = BorderSettings.Tab | BorderSettings.Title`
  - `Border.Thickness = new Thickness (1, 3, 1, 1)`
  - `Arrangement = ViewArrangement.Overlapped`
  - `Width = Dim.Auto ()`, `Height = Dim.Auto ()`
  - `TabIndex` property (internal set) for logical ordering

- **`Tabs`** (`Tabs.cs`): Container that manages `Tab` SubViews:
  - `TabCollection` filters `SubViews.OfType<Tab> ()` and orders by `TabIndex`
  - `OnSubViewAdded` checks `view is not Tab tab` and returns early for non-Tab views
  - `OnSubViewRemoved` checks `view is not Tab removedTab`
  - `IValue<Tab?>` for selected tab
  - All helper methods iterate `TabCollection` (which returns only `Tab` instances)

- **Consumers**: `AdornmentsEditor`, `ConfigurationEditor`, `TabsExample`, `EnableForDesign()`, tests

## Key Design Decision

`Tab.TabIndex` is currently the only property on `Tab` that isn't a standard `View` property. Everything else (`BorderStyle`, `Border.Settings`, `Border.Thickness`, `Arrangement`, etc.) is set by the `Tabs` container in `OnSubViewAdded` anyway (or could be).

The solution: **`Tabs` maintains an internal ordered list of views** to track index, and **configures all tab-related properties on add**.

## Implementation Plan

### Step 1: Add Internal Tracking List to Tabs

In `Tabs.cs`, add an internal list to track tab order:

```csharp
private readonly List<WeakReference<View>> _tabList = [];
```

Using `WeakReference<View>` avoids preventing garbage collection of views that have been removed from the SubViews hierarchy but still referenced by the list. This prevents confusion about ownership - `Tabs` tracks order but does not keep views alive.

This replaces filtering `SubViews.OfType<Tab> ()` and using `Tab.TabIndex`.

Helper to resolve live references:

```csharp
private IEnumerable<View> GetLiveTabViews ()
{
    foreach (WeakReference<View> wr in _tabList)
    {
        if (wr.TryGetTarget (out View? view))
        {
            yield return view;
        }
    }
}
```

### Step 2: Update TabCollection Property

Change `TabCollection` to return `View` instead of `Tab`:

```csharp
// Before
public IEnumerable<Tab> TabCollection => SubViews.OfType<Tab> ().OrderBy (t => t.TabIndex);

// After - resolves weak references, returning only live views in order
public IEnumerable<View> TabCollection => GetLiveTabViews ();
```

The internal list IS the ordering, so no need for `OrderBy`. Dead references are naturally skipped by `GetLiveTabViews()`.

### Step 3: Update IValue<T> from Tab? to View?

Change the `IValue` implementation:

```csharp
// Before
public class Tabs : View, IValue<Tab?>, IDesignable

// After
public class Tabs : View, IValue<View?>, IDesignable
```

Update all related members:
- `Value` property: `Tab?` -> `View?`
- `ValueChanging` event type parameter
- `ValueChanged` event type parameter
- `OnValueChanging` / `OnValueChanged` parameter types
- `ChangeValue` method parameter
- `_value` field type

### Step 4: Update OnSubViewAdded

Remove the `Tab`-only guard and configure ANY view as a tab:

```csharp
protected override void OnSubViewAdded (View view)
{
    // Add to internal tracking list (weak reference - Tabs doesn't own the view)
    _tabList.Add (new WeakReference<View> (view));

    // Configure the view as a tab
    view.TabStop = TabBehavior.TabStop;
    view.CanFocus = true;
    view.BorderStyle = _tabLineStyle;
    view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
    view.Border.TabSide = _tabSide;
    view.Arrangement = ViewArrangement.Overlapped;
    view.Width = Dim.Fill ();
    view.Height = Dim.Fill ();
    view.SuperViewRendersLineCanvas = true;

    // Focus first tab
    TabCollection.FirstOrDefault ()?.SetFocus ();

    UpdateTabBorderThickness ();
    UpdateTabOffsets ();

    base.OnSubViewAdded (view);
}
```

Key: All properties that `Tab` constructor used to set are now set by `Tabs.OnSubViewAdded`.

### Step 5: Update OnSubViewRemoved

Remove the `Tab`-only guard:

```csharp
protected override void OnSubViewRemoved (View view)
{
    base.OnSubViewRemoved (view);

    if (_disposing)
    {
        return;
    }

    _tabList.RemoveAll (wr => !wr.TryGetTarget (out View? target) || target == view);

    // If the removed view was selected, select the first tab
    if (Value == view)
    {
        _value = null;
        View? firstTab = TabCollection.FirstOrDefault ();

        if (firstTab is { })
        {
            Value = firstTab;
        }
    }

    UpdateTabBorderThickness ();
    UpdateTabOffsets ();
    UpdateZOrder ();
}
```

### Step 6: Update All Helper Methods

Replace `Tab`-typed iterations with `View`, using `TabCollection` (which resolves weak refs via `GetLiveTabViews()`):

- **`UpdateZOrder ()`**: Continues to use `SubViews` and `MoveSubViewToEnd`/`MoveSubViewToStart` since z-ordering is a SubViews concern. Replace `SubViews.OfType<Tab> ()` with just `SubViews` (all SubViews are tabs now). Replace `Tab?` local vars with `View?`. Use `TabCollection` only for logical-order iteration (e.g. `TakeWhile`/`SkipWhile` relative to focused tab).
- **`UpdateTabOffsets ()`**: Iterate `TabCollection` (`_tabList` order).
- **`UpdateTabBorderThickness ()`**: Iterate `TabCollection`.
- **`TabLineStyle` setter**: Iterate `TabCollection`.
- **`OnFocusedChanged ()`**: Use `TabCollection.FirstOrDefault (t => t.HasFocus)` to find focused tab by `_tabList` order.

**Important distinction:** Z-ordering manipulates `SubViews` (draw order). Focus/navigation should use `_tabList` (logical tab order). Currently focus/nav is broken because `SubViews` order doesn't match the visual tab order after z-reordering. This is a **known issue to fix in future work** (see below).

### Step 7: Update EnableForDesign

Replace `Tab` instances with plain `View`:

```csharp
// Before
Tab tab1 = new () { Title = "_Attribute" };

// After
View tab1 = new () { Title = "_Attribute" };
```

### Step 8: Update BorderView.cs Reference

In `BorderView.cs:562`:
```csharp
// Before
if (border.Parent is Tab { SuperView: { } } tab && tab.SuperView?.SubViews.LastOrDefault () == tab)

// After - check if parent's SuperView is Tabs instead
if (border.Parent is { SuperView: Tabs } tab && tab.SuperView?.SubViews.LastOrDefault () == tab)
```

### Step 9: Delete Tab.cs

Remove `Terminal.Gui/Views/TabView/Tab.cs` entirely.

### Step 10: Update Consumers

**`AdornmentsEditor.cs`** (lines 50-59):
```csharp
// Before
Tab marginTab = new Tab () { Title = "Margin" };
// After
View marginTab = new () { Title = "Margin" };
```

**`ConfigurationEditor.cs`** (line 85):
```csharp
// Before
Tab tab = new () { Title = config.Key.ToString () };
// After
View tab = new () { Title = config.Key.ToString () };
```

### Step 11: Update Tests

**`TabTests.cs`**: Either delete entirely (tests for a removed class) or convert to test that any View works as a tab.

**`TabsTests.cs`**:
- Replace all `Tab tab = new ()` with `View tab = new ()`
- Replace `Tab?` types with `View?`
- Remove any tests that specifically test `Tab.TabIndex` (no longer exists)
- Adjust assertions that check `Tab`-specific properties

**`TabCompositionTests.cs`**: Same pattern - replace `Tab` with `View`.

### Step 12: Add GetTabIndex / SetTabIndex Helper (Optional)

If consumers need to query a view's index within `Tabs`:

```csharp
/// <summary>
///     Gets the logical index of the specified view within this Tabs container.
/// </summary>
/// <returns>The zero-based index, or -1 if the view is not a tab in this container.</returns>
public int IndexOf (View view)
{
    var i = 0;

    foreach (WeakReference<View> wr in _tabList)
    {
        if (wr.TryGetTarget (out View? target) && target == view)
        {
            return i;
        }

        i++;
    }

    return -1;
}
```

This replaces `Tab.TabIndex` with a method on `Tabs`.

## Edge Cases to Handle

1. **Non-tab SubViews**: Consider whether `Tabs` should support non-tab SubViews (e.g., decorative views). Current behavior with `Tab` filtering excludes them. With the new design, ALL SubViews added via `Add()` become tabs. If non-tab SubViews are needed, add a separate `AddNonTab()` or use a flag. For now, treat all added views as tabs (simplest approach).

2. **Views that already have border settings**: `OnSubViewAdded` overwrites border settings. This is intentional - adding to `Tabs` means "this view IS a tab now." Document this.

3. **Re-adding a view**: If a view is removed and re-added, it gets fresh tab configuration. The internal list handles this naturally.

## Verification Steps

1. `dotnet build --no-restore` - ensure compilation
2. `dotnet test --project Tests/UnitTestsParallelizable --no-build --filter "Tab"` - run tab-related tests
3. Run UICatalog and verify TabsExample, AdornmentsEditor, ConfigurationEditor all work
4. Verify tab composition visuals (border joining) still render correctly

## Files Changed

| File | Action |
|------|--------|
| `Terminal.Gui/Views/TabView/Tab.cs` | **DELETE** |
| `Terminal.Gui/Views/TabView/Tabs.cs` | Major refactor |
| `Terminal.Gui/ViewBase/Adornment/BorderView.cs` | Update `is Tab` check |
| `Examples/UICatalog/Scenarios/EditorsAndHelpers/AdornmentsEditor.cs` | `Tab` -> `View` |
| `Examples/UICatalog/Scenarios/ConfigurationEditor.cs` | `Tab` -> `View` |
| `Tests/UnitTestsParallelizable/Views/TabView/TabTests.cs` | Delete or convert |
| `Tests/UnitTestsParallelizable/Views/TabView/TabsTests.cs` | Update types |
| `Tests/UnitTestsParallelizable/Views/TabView/TabCompositionTests.cs` | Update types |

## Future Work: Fix Focus/Navigation Order

**Known issue:** Focus/keyboard navigation currently follows `SubViews` order, which gets rearranged by `UpdateZOrder()` for drawing purposes. This means arrow-key navigation between tabs doesn't follow the logical tab order (`_tabList`). After nuking `Tab`, a follow-up task should:

- Override focus/navigation behavior in `Tabs` to use `_tabList` order instead of `SubViews` order
- Ensure left/right (or up/down for vertical tabs) arrow keys move through tabs in `_tabList` order
- `SubViews` order remains the z-order (draw order) and should NOT be used for navigation

This is out of scope for this refactor.

## Risk Assessment

- **Low risk**: The `Tab` class adds almost no behavior beyond what `Tabs.OnSubViewAdded` already sets. The refactor is mostly about moving property-setting from `Tab` constructor to `Tabs.OnSubViewAdded` and replacing type filtering with an explicit `WeakReference<View>` list.
- **Medium risk**: Border composition tests are sensitive to exact rendering. Need careful verification.
- **API breaking**: This is a breaking change - `Tab` class removal, `TabCollection` type change, `IValue<Tab?>` -> `IValue<View?>`. Acceptable for v2 alpha.
