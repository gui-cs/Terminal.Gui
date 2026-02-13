# Design Proposal: ViewportSettings-Driven ScrollBar Enablement

**Issue:** [#4714](https://github.com/gui-cs/Terminal.Gui/issues/4714)
**Supersedes:** PR #4715 (`ShowScroll` property approach)

---

## Problem Statement

The current scrollbar system conflates three distinct concerns into two interacting booleans on `ScrollBar`:

1. **Enablement** — Should this View have a scrollbar at all?
2. **Display policy** — When enabled, should it auto-show/hide or remain always-visible?
3. **Visibility** — Is the scrollbar currently visible?

Today, `AutoShow` acts as both enabler and display policy, while `Visible` is the shared output of both `AutoShow`'s internal logic and user code. This creates an unresolvable conflict:

```
User sets:          Visible = false      (intent: "I don't want this scrollbar")
AutoShow fires:     Visible = true       (because content > viewport)
Result:             User's intent is lost
```

There is no way to distinguish "the user disabled this scrollbar" from "the scrollbar is temporarily hidden because content fits." The `ShowScroll` property proposed in PR #4715 addresses this by adding a third boolean, but this creates a 2^3 = 8 state matrix (`ShowScroll x AutoShow x Visible`) that is difficult to reason about and places a View-level concern (enablement) on the ScrollBar control itself.

## Root Cause

**ScrollBar enablement is a View-level concern, not a ScrollBar-level concern.**

When a developer says "this ListView should have a vertical scrollbar," they are describing a property of the *View*, not configuring a standalone UI control. The current API forces them to reach into the ScrollBar and toggle its internals — which then fight back.

## Design Principles

1. **ViewportSettings is the single source of truth** for how a View's viewport behaves. Scrollbar enablement *is* a viewport behavior.
2. **ScrollBar remains a clean standalone control.** Its `AutoShow` and `Visible` properties work unchanged for standalone usage.
3. **The View integration layer translates** between ViewportSettings flags and ScrollBar state. No circular dependencies.
4. **AutoShow is a helper behavior, not the enablement mechanism.** It controls *when* an enabled scrollbar is visible, not *whether* the scrollbar exists.

## Proposed Design

### 1. New ViewportSettingsFlags

Add three flags to `ViewportSettingsFlags`:

```csharp
/// <summary>
///     If set, the built-in <see cref="View.VerticalScrollBar"/> is enabled and will automatically
///     show/hide based on whether the content height exceeds the viewport height.
/// </summary>
HasVerticalScrollBar   = 0b_0001_0000_0000_0000, // bit 12

/// <summary>
///     If set, the built-in <see cref="View.HorizontalScrollBar"/> is enabled and will automatically
///     show/hide based on whether the content width exceeds the viewport width.
/// </summary>
HasHorizontalScrollBar = 0b_0010_0000_0000_0000, // bit 13

/// <summary>
///     Combines <see cref="HasVerticalScrollBar"/> and <see cref="HasHorizontalScrollBar"/>.
/// </summary>
HasScrollBars = HasVerticalScrollBar | HasHorizontalScrollBar,
```

### 2. ViewportSettings Setter Synchronizes ScrollBar State

When `ViewportSettings` changes, the View detects scrollbar flag transitions and configures accordingly:

```csharp
// In View.Content.cs - ViewportSettings setter
public ViewportSettingsFlags ViewportSettings
{
    get => _viewportSettings;
    set
    {
        if (_viewportSettings == value)
        {
            return;
        }

        ViewportSettingsFlags oldFlags = _viewportSettings;
        _viewportSettings = value;

        SyncScrollBarsToSettings (oldFlags, value);

        if (IsInitialized)
        {
            SetViewport (Viewport);
        }
    }
}
```

### 3. SyncScrollBarsToSettings in View.ScrollBars.cs

This method is the bridge between the View-level flag and ScrollBar configuration:

```csharp
private void SyncScrollBarsToSettings (ViewportSettingsFlags oldFlags, ViewportSettingsFlags newFlags)
{
    if (this is Adornment)
    {
        return;
    }

    // Vertical
    bool hadVertical = oldFlags.HasFlag (ViewportSettingsFlags.HasVerticalScrollBar);
    bool hasVertical = newFlags.HasFlag (ViewportSettingsFlags.HasVerticalScrollBar);

    if (!hadVertical && hasVertical)
    {
        // Enabling: access triggers lazy creation, then enable auto-show
        VerticalScrollBar.AutoShow = true;
    }
    else if (hadVertical && !hasVertical)
    {
        // Disabling: only if the scrollbar was ever created
        if (_verticalScrollBar.IsValueCreated)
        {
            _verticalScrollBar.Value.AutoShow = false;
            _verticalScrollBar.Value.Visible = false;
        }
    }

    // Horizontal — same pattern
    bool hadHorizontal = oldFlags.HasFlag (ViewportSettingsFlags.HasHorizontalScrollBar);
    bool hasHorizontal = newFlags.HasFlag (ViewportSettingsFlags.HasHorizontalScrollBar);

    if (!hadHorizontal && hasHorizontal)
    {
        HorizontalScrollBar.AutoShow = true;
    }
    else if (hadHorizontal && !hasHorizontal)
    {
        if (_horizontalScrollBar.IsValueCreated)
        {
            _horizontalScrollBar.Value.AutoShow = false;
            _horizontalScrollBar.Value.Visible = false;
        }
    }
}
```

### 4. ScrollBar — No Changes Required

`ScrollBar.AutoShow` and `ScrollBar.Visible` remain exactly as they are. Their behavior is correct for a standalone control. The problem was never in ScrollBar — it was in the absence of a View-level authority over enablement.

### 5. Guard in ScrollBar.ShowHide() (Optional Enhancement)

For defense-in-depth, `ShowHide()` on a View's built-in scrollbar could check the owning View's ViewportSettings before setting `Visible = true`. This prevents accidental re-enablement if something sets `AutoShow = true` without going through ViewportSettings:

```csharp
private void ShowHide ()
{
    if (AutoShow)
    {
        // If this scrollbar is integrated with a View (lives in Padding),
        // respect the View's ViewportSettings as the authority.
        if (SuperView is Padding padding && padding.Parent is View ownerView)
        {
            ViewportSettingsFlags requiredFlag = Orientation == Orientation.Vertical
                ? ViewportSettingsFlags.HasVerticalScrollBar
                : ViewportSettingsFlags.HasHorizontalScrollBar;

            if (!ownerView.ViewportSettings.HasFlag (requiredFlag))
            {
                Visible = false;
                return;
            }
        }

        Visible = VisibleContentSize < ScrollableContentSize;
    }

    _slider.VisibleContentSize = VisibleContentSize;
    _slider.Size = CalculateSliderSize ();
    _sliderPosition = CalculateSliderPositionFromContentPosition (_value);
    _slider.Position = _sliderPosition.Value;
}
```

This guard is optional. Without it, the design still works because `SyncScrollBarsToSettings` sets `AutoShow = false` when the flag is cleared, which prevents `ShowHide()` from running the auto-show logic. The guard adds belt-and-suspenders safety.

## Usage Scenarios

### Scenario 1: Enable auto-showing vertical scrollbar (most common)

```csharp
// Before (current)
VerticalScrollBar.AutoShow = true;

// After (proposed)
ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;
```

### Scenario 2: Enable both scrollbars

```csharp
// Before
VerticalScrollBar.AutoShow = true;
HorizontalScrollBar.AutoShow = true;

// After
ViewportSettings |= ViewportSettingsFlags.HasScrollBars;
```

### Scenario 3: Disable a scrollbar permanently

```csharp
// Before — BROKEN (AutoShow overrides this)
VerticalScrollBar.Visible = false;

// After — works reliably
ViewportSettings &= ~ViewportSettingsFlags.HasVerticalScrollBar;
```

### Scenario 4: Always-visible scrollbar (no auto-hide)

```csharp
// Enable the scrollbar via flag, then override the display policy
ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;
VerticalScrollBar.AutoShow = false;
// AutoShow = false with Visible already true means: stay visible always
```

### Scenario 5: One axis only

```csharp
// Only horizontal scrolling
ViewportSettings |= ViewportSettingsFlags.HasHorizontalScrollBar;
// Vertical scrollbar is never created (lazy loading preserved)
```

### Scenario 6: Standalone ScrollBar (not part of a View)

```csharp
// Completely unchanged — ScrollBar works independently
ScrollBar scrollBar = new () { Orientation = Orientation.Vertical };
scrollBar.AutoShow = true;
scrollBar.ScrollableContentSize = 500;
someContainer.Add (scrollBar);
```

## Migration

### Internal Views (Terminal.Gui library)

| Current | Proposed |
|---------|----------|
| `VerticalScrollBar.AutoShow = true` | `ViewportSettings \|= HasVerticalScrollBar` |
| `HorizontalScrollBar.AutoShow = true` | `ViewportSettings \|= HasHorizontalScrollBar` |
| Both AutoShow = true | `ViewportSettings \|= HasScrollBars` |
| `AutoShow = false; Visible = false` | Don't set the flag (default) |

Affected files (~15):
- `DialogTResult.cs` — `ViewportSettings |= HasScrollBars`
- `CharMap.cs` — `ViewportSettings |= HasVerticalScrollBar` (horizontal stays manual)
- `EventLog.cs` — `ViewportSettings |= HasScrollBars`
- `ThemeViewer.cs` — `ViewportSettings |= HasScrollBars`
- `FileDialog.cs` — No flags (already default, can remove explicit `AutoShow = false` lines)
- `UICatalogRunnable.cs` — `ViewportSettings |= HasVerticalScrollBar` / `HasScrollBars`
- Various scenarios — straightforward 1:1 replacement

### TextView: Special Case

`TextView` has its own `ScrollBars` property that manually manages visibility. This can be refactored to use ViewportSettings internally:

```csharp
public bool ScrollBars
{
    get => ViewportSettings.HasFlag (ViewportSettingsFlags.HasScrollBars);
    set
    {
        if (value)
        {
            ViewportSettings |= ViewportSettingsFlags.HasScrollBars;
        }
        else
        {
            ViewportSettings &= ~ViewportSettingsFlags.HasScrollBars;
        }
    }
}
```

The existing `UpdateHorizontalScrollBarVisibility()` logic for WordWrap interaction can remain, operating within the enabled/disabled framework.

### External Consumers

Code that sets `AutoShow = true` on View's built-in scrollbars will still compile and work — `AutoShow` is not removed. But the recommended pattern becomes ViewportSettings. The `AutoShow` property on built-in scrollbars could be marked `[Obsolete]` in a future release with a message directing developers to ViewportSettings.

## State Diagram

```
                ViewportSettings
                ┌───────────────────────┐
                │  HasVerticalScrollBar? │
                └───────┬───────────────┘
                        │
              ┌─── NO ──┴── YES ──┐
              │                   │
              ▼                   ▼
     ┌────────────────┐  ┌────────────────────┐
     │ ScrollBar not  │  │ ScrollBar created   │
     │ created (lazy) │  │ AutoShow = true     │
     │ or disabled    │  └────────┬────────────┘
     │ Visible=false  │           │
     │ AutoShow=false │  ┌── Content > Viewport? ──┐
     └────────────────┘  │                         │
                    YES ─┘                         └── NO
                         │                         │
                         ▼                         ▼
                  ┌─────────────┐          ┌─────────────┐
                  │ Visible=true│          │Visible=false │
                  │ Padding += 1│          │Padding as-is │
                  └─────────────┘          └─────────────┘
```

## What Changes

| Component | Change | Impact |
|-----------|--------|--------|
| `ViewportSettingsFlags` | Add 3 flags (2 primary + 1 combo) | Additive, no breaking change |
| `View.ViewportSettings` setter | Call `SyncScrollBarsToSettings` on change | Internal wiring |
| `View.ScrollBars.cs` | Add `SyncScrollBarsToSettings` method | New private method |
| `ScrollBar` | Optional guard in `ShowHide()` | Defense-in-depth, non-breaking |
| Internal Views | Replace `AutoShow = true` with flag | ~15 files, mechanical |
| `TextView.ScrollBars` | Delegate to ViewportSettings internally | Encapsulated refactor |

## What Does NOT Change

- `ScrollBar` public API (fully backward compatible, works standalone)
- Lazy loading of built-in scrollbars
- Padding thickness management on visibility changes
- Viewport <-> ScrollBar value synchronization
- ViewportSettings constraint enforcement logic
- All existing `ViewportSettingsFlags` values and semantics

## Why Not ShowScroll (PR #4715)?

The `ShowScroll` approach:
1. Adds a View-level concern (enablement) to ScrollBar, coupling it to its host
2. Creates a 3-boolean interaction matrix that is hard to document and test
3. Doesn't integrate with ViewportSettings, where developers already configure viewport behavior
4. Is additive complexity on ScrollBar rather than leveraging existing architecture

The ViewportSettings approach:
1. Keeps ScrollBar clean — it doesn't know or care about "enablement"
2. Uses an existing, well-understood mechanism (flags enum on View)
3. Gives independent per-axis control via standard flag operations
4. Fits the mental model: "this View has scrollbars" is a View property
5. Reduces the total number of concepts a developer needs to understand

## Summary

The core insight is that **scrollbar enablement belongs to the View, not the ScrollBar**. ViewportSettings already controls viewport behavior — scrollbar presence is viewport behavior. By adding flags to ViewportSettings and having the View.ScrollBars integration layer translate those flags into ScrollBar configuration, we get a clean, authoritative, independently-controllable design with no new properties on ScrollBar and no fighting between systems.
