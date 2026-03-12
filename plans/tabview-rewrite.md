# TabView Rewrite Plan

> **Issue**: [#4183](https://github.com/gui-cs/Terminal.Gui/issues/4183) — Rewrite `TabView` to use modern v2 capabilities
> **Related**: [#3407](https://github.com/gui-cs/Terminal.Gui/issues/3407) — Refactor `Border` to use subviews for lines
> **Branch**: `issue-4183-tabview`

---

## Design Philosophy

Amazon Principal Engineer tenets applied:

- **Exemplary Practitioner**: TabView becomes THE reference implementation for building compound views in Terminal.Gui v2. It should teach other contributors the right patterns.
- **Technically Fearless**: Tackle the Border gap problem head-on. The whole point is to prove the v2 infrastructure works.
- **Balanced and Pragmatic**: Solve the Border gap problem with a minimal, targeted enhancement — not the full #3407 refactor. Full #3407 can come later and will slot in cleanly.
- **Illuminate and Clarify**: Simple architecture. Tabs are just Views added to TabView. TabRow is an internal rendering detail in Padding. No complex switch statements, no scattered layout math, no manual line drawing.
- **Flexible in Approach**: Follow the proven Wizard (hide/show SubViews) and Dialog (buttons in Padding) patterns rather than inventing new paradigms.
- **Respect What Came Before**: Preserve the spirit and capabilities of the original TabView (scrolling tabs, hotkeys, top/bottom positioning, mouse support) while completely rethinking the implementation.
- **Have Resounding Impact**: Proves out Command propagation, content scrolling, KeyBindings, MouseBindings, Adornments, and LineCanvas auto-joins working together in a real compound view.

---

## Visual Reference: How Tabs Must Render

From the existing test suite — these are the correct rendering patterns:

### Tabs on Top (two tabs, first selected)

```
╭──┬──╮
│T1│T2│
│  ╰──┴───╮
│content  │
└─────────┘
```

- Tab headers share edges (┬ junctions, not separate boxes)
- Selected tab (T1): bottom border is OPEN — it connects directly to the content area
- Unselected tab (T2): bottom border is closed (╰──┴)
- The separator line extends past the last tab to meet the right border (───╮)
- The left border │ is continuous from the top of the selected tab through the content area

### Tabs on Top (second tab selected)

```
╭──┬──╮
│T1│T2│
├──╯  ╰──╮
│content  │
└─────────┘
```

- Now Tab2 is selected: its bottom is open, Tab1's bottom is closed
- ├──╯ shows Tab1's bottom-right corner connecting to the left border via a T-junction

### Tabs on Top with scroll overflow (right arrow)

```
╭───────╮
│1234567│
│       ╰►
│content │
└────────┘
```

- ► scroll indicator (matching `Glyphs.RightArrow`, same glyph as ScrollBar uses)

### Tabs on Top with scroll overflow (left arrow)

```
   ╭──╮
   │T3│
◄  ╰──┴──╮
│content  │
└─────────┘
```

- ◄ scroll indicator (matching `Glyphs.LeftArrow`, same glyph as ScrollBar uses)

### Tabs on Bottom (two tabs, first selected)

```
┌─────────┐
│content  │
│  ╭──┬──╯
│T1│T2│
╰──┴──╯
```

### Tabs on Bottom (second tab selected)

```
┌─────────┐
│content  │
├──╮  ╭──╯
│T1│T2│
╰──┴──╯
```

---

## Problem Analysis

### Why the Current TabView Must Be Rewritten

The current TabView is **~1,500 lines across 6 files** with:

1. **~800 lines of manual drawing code** in `TabRow.OnRenderingLineCanvas()` — hand-computing line positions, corners, and intersections that the LineCanvas auto-join system was built to handle automatically.
2. **Manual viewport calculation** in `CalculateViewport()` — doing layout math that `Dim.Auto` and content scrolling eliminate.
3. **Custom mouse handling** in `TabRow.OnMouseEvent()` — manual hit-testing that MouseBindings handles.
4. **Custom keyboard handling** — not using the Command system properly.
5. **`TabStyle` configuration class** — manages state that should just be properties on the views themselves.
6. **Separate `Tab.View` / `Tab.DisplayText`** — splits what should be a single View into two concepts.

### The Border Gap Problem

For the selected tab to visually "connect" to the content area, the content area's border needs a **gap** where the selected tab sits. The current code achieves this with 800+ lines of manual LineCanvas. The question is: how do we achieve the same visual with zero custom drawing?

### Evaluation of Approaches

| Approach | Pros | Cons | Verdict |
|----------|------|------|---------|
| **Full #3407** (Border as Line subviews) | Most flexible | Massive refactor; high risk; blocks TabView | Defer |
| **Border.Gaps API** (gap regions on Border) | Targeted; minimal API; solves TabView cleanly | New API on Border | **Selected** |
| **Manual LineCanvas** (draw separator with gaps) | No Border changes | Still "custom drawing"; defeats the purpose | Reject |
| **Thickness manipulation only** | Zero infra changes | Can't create partial gap in content border | Reject |

---

## Architecture

### Developer-Facing Model

Developers interact with `TabView` and `Tab` only. `TabRow` is an internal implementation detail they never see.

```
TabView : View
├── Tab "Settings" (SubView — content for the Settings tab)
│   └── [developer's content views]
├── Tab "Advanced" (SubView — content for the Advanced tab)
│   └── [developer's content views]
├── Tab "About" (SubView — content for the About tab)
│   └── [developer's content views]
│
└── (internal, in Padding)
    └── TabRow — renders tab headers from Tab.Title values
        ├── [tab header views, created dynamically]
        ├── ScrollLeftButton (visible when tabs overflow)
        └── ScrollRightButton (visible when tabs overflow)
```

### Key Design Decisions

1. **Tabs are SubViews of TabView** — developers `Add()` Tab views directly to the TabView instance, just like adding WizardSteps to a Wizard. No separate `AddTab()` method needed (though one can exist as convenience).

2. **Tab IS the content** — a Tab's `Title` property provides the tab header text. The Tab view itself IS the content panel. No separate `ContentView` reference. This is simpler and follows the WizardStep pattern.

3. **Wizard-style show/hide** — when switching tabs, TabView hides all Tabs except the selected one (`tab.Visible = false/true`), exactly like Wizard does with WizardSteps. No add/remove of content views.

4. **TabRow lives in Padding** — like Dialog places its button container in Padding, TabView places TabRow in Padding. TabRow dynamically creates tab header views by reading `this.SuperView!.Parent!.SubViews.OfType<Tab> ()`. Tab header views pull their display text from `Tab.Title`.

5. **TabsOnBottom via Padding adjustment** — like Wizard/Dialog adjust Padding thickness for their controls, TabView sets `Padding.Top` or `Padding.Bottom` and positions TabRow accordingly.

6. **Scroll indicators match ScrollBar** — use `Glyphs.LeftArrow` / `Glyphs.RightArrow` (same glyphs as ScrollBar).

7. **Nullable int-based selection** — `SelectedTabIndex` (`int?`) is the primary API. `null` = no tab selected. `SelectedTab` is a convenience derived from the index.

8. **SuperViewRendersLineCanvas** throughout — enables auto-joins between tab header borders and the TabView border.

---

## Example Usage

### Basic Usage

```csharp
TabView tabView = new ()
{
    X = 0,
    Y = 0,
    Width = Dim.Fill (),
    Height = Dim.Fill ()
};

// Create tabs - Tab IS the content. Title is the header text.
Tab settingsTab = new ()
{
    Title = "_Settings"  // underscore = hotkey
};
settingsTab.Add (
    new Label { Text = "Font Size:", X = 0, Y = 0 },
    new NumericUpDown<int> { X = 12, Y = 0, Value = 12 }
);

Tab advancedTab = new ()
{
    Title = "_Advanced"
};
advancedTab.Add (
    new CheckBox { Text = "Enable logging", X = 0, Y = 0 },
    new CheckBox { Text = "Debug mode", X = 0, Y = 1 }
);

// Add tabs directly to TabView — just like adding SubViews
tabView.Add (settingsTab, advancedTab);

// Select the first tab
tabView.SelectedTabIndex = 0;  // null = no tab selected

window.Add (tabView);
```

### EnableForDesign (AllViewsTester)

```csharp
public bool EnableForDesign ()
{
    Tab tab1 = new () { Title = "Tab_1" };
    tab1.Add (new Label { Text = "Label in Tab1" });

    Tab tab2 = new () { Title = "Tab _2" };
    tab2.Add (new TextField { Text = "TextField in Tab2", Width = 15 });

    Tab tab3 = new () { Title = "Tab T_hree" };
    tab3.Add (new Label { Text = "Label in Tab3" });

    Add (tab1, tab2, tab3);
    SelectedTabIndex = 0;

    return true;
}
```

### Tabs on Bottom

```csharp
TabView tabView = new ()
{
    TabsOnBottom = true,
    Width = Dim.Fill (),
    Height = Dim.Fill ()
};
```

### Responding to Tab Changes

```csharp
tabView.SelectedTabChanged += (_, args) =>
{
    statusBar.Text = $"Switched to tab {args.NewValue?.Title}";
};
```

### Dynamic Add/Remove

```csharp
// Add a new tab at runtime
Tab newTab = new () { Title = "New _Tab" };
newTab.Add (new Label { Text = "Dynamic content" });
tabView.Add (newTab);

// Remove a tab
tabView.Remove (someTab);
```

---

## Detailed Design

### TabView

```csharp
public class TabView : View, IDesignable
{
    private TabRow _tabRow;  // Lives in Padding
    private int? _selectedTabIndex;

    // Selection (nullable — null means no tab selected)
    public int? SelectedTabIndex { get; set; }
    public Tab? SelectedTab => SelectedTabIndex.HasValue
        ? Tabs.ElementAtOrDefault (SelectedTabIndex.Value)
        : null;

    // Configuration
    public bool TabsOnBottom { get; set; }
    public uint MaxTabTextWidth { get; set; }  // Default: 30

    // Computed
    public IReadOnlyList<Tab> Tabs =>
        SubViews.OfType<Tab> ().ToList ();

    // Events (CWP pattern)
    public event EventHandler<ValueChangedEventArgs<Tab?>>? SelectedTabChanged;
    protected virtual void OnSelectedTabChanged (ValueChangedEventArgs<Tab?> args);

    // Commands
    // Command.Left → Select previous tab
    // Command.Right → Select next tab
    // Command.LeftStart → Select first tab
    // Command.RightEnd → Select last tab
}
```

**Constructor**:
- Sets `TabStop = TabBehavior.TabGroup`
- Sets `SuperViewRendersLineCanvas = true`
- Creates `_tabRow` and adds it to `Padding`
- Sets `Padding.Top = 2` (tab header height; adjusted when `TabsOnBottom` changes)
- Registers Commands via `AddCommand ()`
- Binds keys via `KeyBindings.Add ()`

**SelectedTabIndex setter** (Wizard pattern):
1. If `value` is not null, validate index range (`0..Tabs.Count - 1`)
2. Hide all Tabs: `foreach (Tab tab in Tabs) { tab.Visible = false; }`
3. If `value` is not null, show selected Tab: `selectedTab.Visible = true`
4. Tell `_tabRow` to update tab header appearance (selected vs unselected; null = none selected)
5. Raise `OnSelectedTabChanged` → fire `SelectedTabChanged`
6. `SetNeedsLayout ()`

**OnSubViewAdded / OnSubViewRemoved**:
- When a Tab is added/removed, tell `_tabRow` to rebuild tab headers
- If the selected tab is removed, select the nearest remaining tab

**TabsOnBottom setter**:
```csharp
// Tabs on top:
Padding!.Thickness = Padding!.Thickness with { Top = tabRowHeight, Bottom = 0 };
_tabRow.Y = 0;

// Tabs on bottom:
Padding!.Thickness = Padding!.Thickness with { Top = 0, Bottom = tabRowHeight };
_tabRow.Y = Pos.AnchorEnd ();
```

### TabRow (internal)

```csharp
internal class TabRow : View
{
    private View _scrollLeftButton;
    private View _scrollRightButton;
    private int _scrollOffset;

    // Accesses tabs via: SuperView!.Parent!.SubViews.OfType<Tab> ()
    private TabView TabView => (TabView)SuperView!.Parent!;
}
```

**Constructor**:
- `Width = Dim.Fill ()`
- `Height = Dim.Auto (DimAutoStyle.Content)` (or fixed at 2)
- `SuperViewRendersLineCanvas = true`
- Creates scroll buttons using `Glyphs.LeftArrow` / `Glyphs.RightArrow`

**Tab Header Management**:
- For each `Tab` in `TabView.Tabs`, TabRow creates/maintains a small header View
- Each header View:
  - `Text = tab.Title`
  - `BorderStyle = LineStyle.Rounded`
  - `Width = Dim.Auto (DimAutoStyle.Text)`, clamped to `MaxTabTextWidth`
  - Positioned: `X = Pos.Right (previousHeader)` (overlapping by 1 for shared edges)
  - `MouseBindings.Add (MouseFlags.Button1Clicked, Command.Activate)`
- When a header is activated, TabRow finds the corresponding Tab index and sets `TabView.SelectedTabIndex`

**UpdateTabAppearance (selectedIndex)**:
- Selected header: `Border.Thickness = new Thickness (1, 1, 1, 0)` — no bottom border (tabs on top) or no top border (tabs on bottom)
- Unselected headers: `Border.Thickness = new Thickness (1)` — full border
- Updates `TabView.Border.TopGaps` (or `BottomGaps`) to create the gap under the selected tab

**Scroll Logic**:
- Scrolls by whole tabs (not pixels)
- Headers before `_scrollOffset` are `Visible = false`
- Headers past the viewport boundary are `Visible = false`
- Shows/hides scroll buttons based on overflow state

### Tab

```csharp
public class Tab : View
{
    // Title (inherited from View) = tab header text, supports _ hotkey convention
    // Tab IS the content panel — developers Add() their content views directly
}
```

**Constructor**:
- `CanFocus = true`
- `Width = Dim.Fill ()`
- `Height = Dim.Fill ()`
- `Visible = false` (hidden until selected)

Tab is intentionally minimal. It's just a View with semantics. The `Title` property (from View) provides the tab header text. Developers add content SubViews to the Tab just like any other View.

---

## Border.Gaps Enhancement

### New Type

**File**: `Terminal.Gui/Drawing/BorderGap.cs`

```csharp
/// <summary>
///     Defines a gap in a border line where the line should not be drawn.
///     Used by <see cref="Border"/> to create openings in border lines, such as
///     where a selected tab connects to its content area.
/// </summary>
/// <param name="Position">
///     The position along the border side where the gap starts (0-based,
///     relative to the inner edge of the border).
/// </param>
/// <param name="Length">The length of the gap in columns or rows.</param>
public readonly record struct BorderGap (int Position, int Length);
```

### Border.cs Changes

```csharp
// New properties
public List<BorderGap> TopGaps { get; } = [];
public List<BorderGap> BottomGaps { get; } = [];
public List<BorderGap> LeftGaps { get; } = [];
public List<BorderGap> RightGaps { get; } = [];

public void ClearAllGaps ()
{
    TopGaps.Clear ();
    BottomGaps.Clear ();
    LeftGaps.Clear ();
    RightGaps.Clear ();
}
```

In `OnDrawingContent ()`, after computing `borderBounds`, apply exclusion regions:

```csharp
foreach (BorderGap gap in TopGaps)
{
    Parent?.LineCanvas.Exclude (
        new Region (new Rectangle (
            borderBounds.X + gap.Position,
            borderBounds.Y,
            gap.Length,
            1)));
}
// Symmetric for BottomGaps, LeftGaps, RightGaps
```

**Estimated change**: ~40 lines in Border.cs, ~15 lines for BorderGap.cs.

### Relationship to #3407

This is **not** a replacement for #3407. It's a pragmatic stepping stone:

- **Now**: `Border.Gaps` gives TabView what it needs with minimal infrastructure change
- **Later**: #3407 decomposes Border into Line subviews for far more flexibility
- **Compatibility**: When #3407 lands, Gaps can be retained or deprecated — either path is clean

---

## How the Rendering Works

### Step-by-step for "Tabs on Top, Tab1 selected"

Given a 10-wide TabView with two tabs:

```
╭──┬──╮
│T1│T2│
│  ╰──┴───╮
│content  │
└─────────┘
```

1. **TabView.Border** draws the full rectangle (left, top, right, bottom) using `LineStyle.Rounded`
2. **TabView.Border.TopGaps** has a gap at the selected tab's position — this punches out the top border where the selected tab sits
3. **TabRow** (in Padding.Top) contains tab header views, each with `BorderStyle = LineStyle.Rounded`
4. **Tab1 header** (selected): `Border.Thickness = (1, 1, 1, 0)` — no bottom border, so its left/right borders extend down and meet the content area
5. **Tab2 header** (unselected): `Border.Thickness = (1, 1, 1, 1)` — full border with rounded bottom corners
6. **SuperViewRendersLineCanvas** merges all LineCanvas operations:
   - Tab1's left border + TabView's left border → continuous │
   - Tab1/Tab2 shared edge → ┬ junction at top, separate corners at bottom
   - Tab2's bottom-right + separator line → ┴ junction
   - Separator line to right border → ╮ junction

The entire visual is produced by **standard Border rendering + auto-joins**. Zero custom LineCanvas code.

---

## Implementation Plan

### Phases (Test-First, CI-Gated)

Each phase follows this workflow:
1. **Write tests** for the phase
2. **Implement** until all tests pass locally (both test projects)
3. **Commit and push** to the PR branch
4. **Wait for CI** — all GitHub Actions runners must pass (~10 min). Use `gh run list` / `gh run watch` to monitor. **Do NOT proceed to the next phase until CI is green.**
5. **Update this plan's Status table** to mark the phase ✅ Done

**Debugging guidance:** When tests fail or behavior is unexpected, **use `Tracing.Trace` calls and log output** to diagnose the problem — do NOT try to reason over the code or rely on memory. Add temporary trace calls, run the failing test in Debug, read the trace output, then fix. Remove temporary traces after diagnosis. Note: `Trace` methods are `[Conditional("DEBUG")]` so they are unavailable in Release builds — never assert on or depend on trace output in unit tests.


### Phase 0: Delete Old TabView and Scenario

1. **Delete** all existing TabView source files:
   - `Terminal.Gui/Views/TabView/TabView.cs`
   - `Terminal.Gui/Views/TabView/TabRow.cs`
   - `Terminal.Gui/Views/TabView/Tab.cs`
   - `Terminal.Gui/Views/TabView/TabStyle.cs`
   - `Terminal.Gui/Views/TabView/TabChangedEventArgs.cs`
   - `Terminal.Gui/Views/TabView/TabMouseEventArgs.cs`
2. **Delete** the old UICatalog TabView scenario:
   - `Examples/UICatalog/Scenarios/TabViewExample.cs`
3. **Delete** old TabView tests:
   - `Tests/UnitTests/Views/TabViewTests.cs`
   - `Tests/UnitTestsParallelizable/Views/TabViewTests.cs`
   - `Tests/UnitTestsParallelizable/Views/TabViewCommandTests.cs`
4. **Fix all compilation errors** caused by deletions (remove references to deleted types in other files)
5. Build must succeed. Tests that referenced TabView are gone, so test suite should still pass.

**Files**: Deletions only + fixup of compile errors

### Phase 1: Border.Gaps Enhancement

1. Create `BorderGap.cs` record struct
2. Add gap lists and `ClearAllGaps ()` to `Border.cs`
3. Implement gap exclusion in `Border.OnDrawingContent ()`
4. Write tests in `Tests/UnitTestsParallelizable/Drawing/BorderGapTests.cs`

**Files**: `BorderGap.cs` (new), `Border.cs` (modified), `BorderGapTests.cs` (new)

### Phase 2: Core TabView + Tab + TabRow

1. **Create** new files: `TabView.cs`, `TabRow.cs`, `Tab.cs`
2. Implement:
   - `Tab` — minimal View (Title = header text, content = SubViews, `Width/Height = Dim.Fill`, `Visible = false`)
   - `TabView` — orchestrator with Padding-hosted TabRow, Wizard-style show/hide, nullable `SelectedTabIndex`
   - `TabRow` — internal, creates header views from `SuperView!.Parent!.SubViews.OfType<Tab> ()`, manages scroll offset
   - `SelectedTabIndex` setter with hide/show + tab header update
   - Basic layout (tabs on top only)
3. Write core tests: construction, add/remove tabs, selection, null selection, event firing
4. Verify rendering in a simple test

**Target**: ~300-400 lines total across three files

### Phase 3: TabViews Scenario (New)

Rewrite the UICatalog scenario from scratch as `TabViewExample.cs`. This scenario is the primary development testbed — build it early so all subsequent phases can be visually verified.

**Layout:**
```
┌──────────────────────────────────────────────────────────────┐
│                         TabViews                             │
├──────────────────────────────────────┬───────────────────────┤
│                                      │ Configuration         │
│  ┌─ TabView Demo ──────────────────┐ │                       │
│  │ ╭──────┬──────┬──────╮          │ │ ☐ TabsOnBottom        │
│  │ │ Tab1 │ Tab2 │ Tab3 │          │ │ MaxTabTextWidth: [30] │
│  │ │      ╰──────┴──────┴─────╮   │ │ Tabs: [Add] [Remove]  │
│  │ │ (tab content here)       │   │ │ SelectedTabIndex: [▾]  │
│  │ │                          │   │ │                       │
│  │ └──────────────────────────┘   │ │ ─── AdornmentsEditor  │
│  └─────────────────────────────────┘ │ ─── ViewportEditor    │
│                                      │                       │
├──────────────────────────────────────┴───────────────────────┤
│ EventLog                                                     │
│ > Tab changed: null → 0                                      │
│ > Tab changed: 0 → 1                                         │
└──────────────────────────────────────────────────────────────┘
```

**Components:**
1. **Demo area (left ~60%)**: A `TabView` instance with several example tabs (text, controls, nested views). This is the view being tested.
2. **Configuration pane (right ~40%)**: A `FrameView` with controls to manipulate the demo TabView, in the spirit of the LinearRanges scenario config pane:
   - `CheckBox` for `TabsOnBottom`
   - `NumericUpDown<uint>` for `MaxTabTextWidth`
   - `Button` to add a new tab dynamically
   - `Button` to remove the selected tab
   - `NumericUpDown<int?>` or `DropDownList` for `SelectedTabIndex` (including null)
   - Unicode/emoji tab name input for testing grapheme handling
   - `AdornmentsEditor` targeting the demo TabView
   - `ViewportSettingsEditor` targeting the demo TabView
3. **EventLog (bottom)**: An `EventLog` instance targeting the demo TabView, showing tab changes, activation, focus events. Collapsible via its built-in ExpanderButton.

**Key behaviors to demonstrate:**
- Tab switching via click, hotkey, and keyboard navigation
- Dynamic add/remove of tabs
- `SelectedTabIndex = null` (no selection state)
- `TabsOnBottom` toggle
- Scroll indicators when many tabs exist
- Adornment and viewport manipulation via editors
- All events logged

**Files**: `Examples/UICatalog/Scenarios/TabViewExample.cs` (new)

### Phase 4: Navigation and Commands

1. Keyboard: Left/Right (switch tabs), Home/End (first/last), Up/Down (tab row ↔ content)
2. Mouse: Click tab header to select, scroll buttons, mouse wheel on TabRow
3. Tab hotkeys via `Title` underscore convention
4. Focus: TabView = `TabBehavior.TabGroup`, content = standard focus
5. Verify all navigation works in the TabViews scenario

### Phase 5: Visual Polish

1. Selected/unselected tab header border thickness manipulation
2. Border.TopGaps/BottomGaps for content area gap
3. LineCanvas auto-joins for T-junctions and corners
4. `TabsOnBottom` support (flip Padding, flip gap side)
5. Scroll indicators using `Glyphs.LeftArrow` / `Glyphs.RightArrow` (matching ScrollBar)
6. Visually verify all rendering patterns from the "Visual Reference" section above

### Phase 6: Events and API Completeness

1. `SelectedTabChanged` event (CWP pattern, like Wizard.StepChanged)
2. `EnableForDesign ()` for AllViewsTester
3. XML documentation on all public APIs

### Phase 7: Tests

All in `Tests/UnitTestsParallelizable/Views/TabViewTests.cs`:
- Construction, add/remove, nullable selection, event firing
- Navigation: keyboard commands, focus transitions
- Layout: tabs on top/bottom, scroll offset, tab visibility
- Mouse: click to select, scroll buttons, wheel scrolling
- Edge cases: zero tabs, one tab, remove selected tab, `SelectedTabIndex = null`
- Visual regression tests with ASCII art assertions matching the patterns from the Visual Reference section

---

## What Gets Deleted (Phase 0)

| File | Lines | Why |
|------|-------|-----|
| `Terminal.Gui/Views/TabView/TabView.cs` | 685 | Complete rewrite |
| `Terminal.Gui/Views/TabView/TabRow.cs` | 801 | Complete rewrite |
| `Terminal.Gui/Views/TabView/Tab.cs` | 34 | Complete rewrite (Tab IS content now) |
| `Terminal.Gui/Views/TabView/TabStyle.cs` | 20 | Eliminated; properties on TabView |
| `Terminal.Gui/Views/TabView/TabChangedEventArgs.cs` | 22 | Replace with `ValueChangedEventArgs<Tab?>` |
| `Terminal.Gui/Views/TabView/TabMouseEventArgs.cs` | 28 | Eliminated; standard Command/Mouse system |
| `Examples/UICatalog/Scenarios/TabViewExample.cs` | ~300 | Complete rewrite as new scenario |
| `Tests/UnitTests/Views/TabViewTests.cs` | ~1,507 | Rewritten as parallelizable tests |
| `Tests/UnitTestsParallelizable/Views/TabViewTests.cs` | ~56 | Rewritten |
| `Tests/UnitTestsParallelizable/Views/TabViewCommandTests.cs` | ~119 | Rewritten |

**Total deleted**: ~3,570 lines

## What Gets Created

| File | Est. Lines | Purpose |
|------|-----------|---------|
| `Terminal.Gui/Drawing/BorderGap.cs` | 15 | Gap record struct |
| `Terminal.Gui/Views/TabView/TabView.cs` | 180 | Orchestrator |
| `Terminal.Gui/Views/TabView/TabRow.cs` | 150 | Internal tab row in Padding |
| `Terminal.Gui/Views/TabView/Tab.cs` | 30 | Minimal content view |
| `Examples/UICatalog/Scenarios/TabViewExample.cs` | 250 | New scenario with config pane, EventLog, editors |
| `Tests/UnitTestsParallelizable/Views/TabViewTests.cs` | 400 | Comprehensive tests |
| `Tests/UnitTestsParallelizable/Drawing/BorderGapTests.cs` | 80 | Border.Gaps tests |

**Target new TabView code**: ~375 lines (4x reduction from 1,590)

---

## Risk Assessment

| Risk | Mitigation |
|------|-----------|
| LineCanvas auto-joins don't produce correct glyphs | Border.Gaps exclusion + careful positioning; fallback: minimal manual additions (still 95% simpler) |
| Tab header ↔ Tab content sync issues | TabRow rebuilds headers on SubViewAdded/Removed; Title changes propagate via TitleChanged event |
| Scroll snap doesn't work cleanly | Custom scroll logic in TabRow; don't rely on generic content scrolling |
| Performance with many tabs | Headers past viewport set `Visible = false`; tab content views hidden via Wizard pattern |

---

## Success Criteria

1. **4x code reduction**: ~375 lines vs ~1,590 lines
2. **Zero custom line drawing**: No manual LineCanvas calls in TabView/TabRow/Tab
3. **Full v2 infrastructure usage**: Commands, KeyBindings, MouseBindings, Dim.Auto, Adornments, SuperViewRendersLineCanvas, CWP events
4. **Follows proven patterns**: Wizard (show/hide), Dialog (Padding-hosted controls)
5. **All capabilities preserved**: Scrolling tabs, hotkeys, top/bottom positioning, mouse, dynamic add/remove
6. **Clean developer API**: `tabView.Add (new Tab { Title = "Settings" })` — intuitive, discoverable
7. **Clean, teachable code**: A new contributor can read TabView and learn compound view patterns

---

## Status

| Phase | Status |
|-------|--------|
| Phase 0: Delete Old TabView | ✅ Done |
| Phase 1: Border.Gaps Enhancement | ✅ Done |
| Phase 2: Core TabView + Tab + TabRow | ✅ Done |
| Phase 3: TabViews Scenario | ✅ Done |
| Phase 4: Navigation and Commands | ⚠️ Partial — basic Ctrl+Left/Right/Home/End works. Tab hotkeys and Tab-key focus navigation do NOT work. |
| Phase 5: Visual Polish | ⚠️ Partial — tabs-on-top rendering works. TabsOnBottom rendering **FIXED** (Padding.Top=1 compensation). Tab scrolling not implemented. |
| Phase 6: Events and API Completeness | ✅ Done |
| Phase 7: Tests | ⚠️ Partial — 71 tests pass (43 in TabViewTests + 28 in TabViewVisualTests). Comprehensive visual tests for tabs-on-top and tabs-on-bottom. No scroll tests. |

## Known Issues (To Fix)

### Issue 1: TabsOnBottom rendering — ✅ FIXED

**Root cause:** In `TabRow.UpdateHeaderAppearance()`, selected header with `Border.Thickness.Top=0` caused text to render at Y=0, colliding with the continuation line.

**Fix applied:** Added `Padding.Top=1` compensation on selected header when TabsOnBottom in `TabRow.UpdateHeaderAppearance()`. Also resets `Padding.Thickness = new Thickness(0)` for unselected tabs.

**Tests:** 13 new visual tests added in `TabViewVisualTests.cs` (refactored from `TabViewTests.cs`), covering tabs-on-top, tabs-on-bottom, state transitions, padding assertions, and edge cases. All pass.

### Issue 2: Tab scrolling not implemented

When there are too many tabs to fit in the available width, there is no scroll mechanism. The tabs just overflow. Need to implement scroll offset logic in TabRow with left/right scroll indicators (using `Glyphs.LeftArrow` / `Glyphs.RightArrow`).

### Issue 3: TabsOnBottom property type - ✅ FIXED

`TabsOnBottom` property should be of type `Side` enum (Top/Bottom) instead of bool for better extensibility and clarity. We may add Left & Right later.

The TabViews Scenario should be updated to so that the Configuration pane usesa an OptionSelector<Side> instead of a CheckBox for this setting. Also update the 3rd example tab such that the OptionSelector<Side> that is there now also controls the tabview.

### Issue 4: Tab Hotkeys do not work - ✅ FIXED

E.g. in the TabViewsExample scenario hitting Alt-h should activate the tab Titled "Tab T_hree", but it does not. Tests need to be written for this that fail, then the issue fixed.

### Issue 5: Tab-key focus navigation broken

When the Tab key is pressed, the focus should move to the next focusable element within the selected tab's content; if there's no next, it should nav to the tabrow. The TabView should be a TabGroup. This is not working currently. Tests need to be written for this that fail, then the issue fixed.

### Issue 6: Resizable arrangement broken for top

If `Arrangement = ViewArrangement.Resizable` is set on TabView, everything renders right, but Arranger is not enabling resizing the top. This is probably because we make the thickness of the TabView border top 0. Tests need to be written for this that fail, then the issue fixed.

## Open Questions

1. **Should `AddTab` / `RemoveTab` convenience methods exist?** Or just use `Add ()` / `Remove ()`? Leaning toward just `Add/Remove` for consistency, but `AddTab` could do validation (ensure only Tab types are added).

2. **Tab reordering (drag)?** Not in this PR. Can be added later.

3. **Closeable tabs (X button)?** Not in this PR. When #3407 lands, this becomes trivial. For now, use context menus.

4. **Exact tab overlap positioning**: Adjacent tab headers share edges (overlap by 1 column) vs. gap between them. Need to prototype and verify LineCanvas auto-join behavior.

5. **Tab.Title vs Tab.Text**: `Title` is rendered in the Border area. `Text` is rendered in the content area. For tab headers, `Title` is the right property. Need to confirm this works correctly for hotkey detection.
