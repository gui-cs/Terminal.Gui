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

### AutoShow Is Three Modes Crammed Into a Bool

| `AutoShow` | Intent | Actual behavior |
|------------|--------|-----------------|
| `false` (default) | "Don't manage visibility" | Nothing — `Visible` stays at whatever it is |
| `true` | "Show when needed" | `Visible = content > viewport` on every frame |
| `true` then set to `false` | "Stop auto-showing" | **Forces `Visible = true`** — always visible! |

The third row is the poison. Setting `AutoShow = false` after it was `true` calls `Visible = true` (line 212 of ScrollBar.cs). This means `AutoShow` is a one-way ratchet: once enabled, there is no way to return to the "I'll manage visibility myself" state. The developer loses control.

## Root Cause

Two distinct problems:

1. **ScrollBar enablement is a View-level concern** placed on the ScrollBar control. When a developer says "this ListView should have a vertical scrollbar," they are describing a property of the *View*, not configuring a standalone UI control. The current API forces them to reach into the ScrollBar and toggle its internals — which then fight back.

2. **ScrollBar's own display policy is a boolean that can't represent its actual states.** `AutoShow` is not a toggle between two modes — it's a lossy encoding of three modes (manual, auto, always-visible) into a single bit.

## Design Principles

1. **ViewportSettings is the single source of truth** for whether a View's built-in scrollbars are enabled. Scrollbar enablement *is* a viewport behavior.
2. **ScrollBar gets a clean visibility policy enum** replacing the confusing `AutoShow` boolean. Every state is reachable, no side effects, no ratchets.
3. **The View integration layer translates** between ViewportSettings flags and ScrollBar state. No circular dependencies.
4. **ScrollBar works great standalone.** The new `VisibilityMode` enum is clearer than `AutoShow` for standalone usage too.

---

## Proposed Design

### Part A: ScrollBar — Replace `AutoShow` with `ScrollBarVisibilityMode`

This change improves ScrollBar as a standalone control, independent of the View integration.

#### New Enum

```csharp
/// <summary>
///     Controls how a <see cref="ScrollBar"/> manages its own <see cref="View.Visible"/> state.
/// </summary>
public enum ScrollBarVisibilityMode
{
    /// <summary>
    ///     The scrollbar does not manage its own visibility. The developer controls
    ///     <see cref="View.Visible"/> directly to show or hide the scrollbar.
    /// </summary>
    Manual,

    /// <summary>
    ///     The scrollbar is automatically shown when <see cref="ScrollBar.ScrollableContentSize"/>
    ///     exceeds <see cref="ScrollBar.VisibleContentSize"/>, and hidden otherwise.
    /// </summary>
    Auto,

    /// <summary>
    ///     The scrollbar is always visible regardless of content size.
    /// </summary>
    Always
}
```

#### Replace AutoShow Property

```csharp
// OLD — confusing boolean with hidden side effects
public bool AutoShow
{
    get => _autoShow;
    set
    {
        if (_autoShow != value)
        {
            _autoShow = value;
            if (!AutoShow) { Visible = true; }  // WHY?!
            ShowHide ();
            SetNeedsLayout ();
        }
    }
}

// NEW — explicit three-state enum, no side effects
private ScrollBarVisibilityMode _visibilityMode;

public ScrollBarVisibilityMode VisibilityMode
{
    get => _visibilityMode;
    set
    {
        if (_visibilityMode != value)
        {
            _visibilityMode = value;
            ShowHide ();
            SetNeedsLayout ();
        }
    }
}
```

#### Clean ShowHide()

```csharp
// OLD — implicit modes, only handles one of three states
private void ShowHide ()
{
    if (AutoShow)
    {
        Visible = VisibleContentSize < ScrollableContentSize;
    }
    // When AutoShow is false... nothing. Visibility is in limbo.

    _slider.VisibleContentSize = VisibleContentSize;
    _slider.Size = CalculateSliderSize ();
    _sliderPosition = CalculateSliderPositionFromContentPosition (_value);
    _slider.Position = _sliderPosition.Value;
}

// NEW — every mode has explicit, documented behavior
private void ShowHide ()
{
    switch (VisibilityMode)
    {
        case ScrollBarVisibilityMode.Auto:
            Visible = VisibleContentSize < ScrollableContentSize;
            break;

        case ScrollBarVisibilityMode.Always:
            Visible = true;
            break;

        case ScrollBarVisibilityMode.Manual:
            // Hands off — the developer controls Visible directly
            break;
    }

    _slider.VisibleContentSize = VisibleContentSize;
    _slider.Size = CalculateSliderSize ();
    _sliderPosition = CalculateSliderPositionFromContentPosition (_value);
    _slider.Position = _sliderPosition.Value;
}
```

#### Why This Matters for Standalone ScrollBar

```csharp
// Auto show/hide based on content
ScrollBar scrollBar = new () { VisibilityMode = ScrollBarVisibilityMode.Auto };

// Always visible (e.g., a code editor that always shows scrollbars)
ScrollBar scrollBar = new () { VisibilityMode = ScrollBarVisibilityMode.Always };

// Developer manages visibility (e.g., complex layout with custom rules)
ScrollBar scrollBar = new () { VisibilityMode = ScrollBarVisibilityMode.Manual };
scrollBar.Visible = someCondition;
```

Every state is reachable. Every transition is clean. No surprises.

---

### Part B: ViewportSettingsFlags — Enable/Disable Built-In ScrollBars

This change addresses the View-level enablement concern.

#### New Flags

Add three flags to `ViewportSettingsFlags`:

```csharp
/// <summary>
///     If set, the built-in <see cref="View.VerticalScrollBar"/> is enabled with
///     <see cref="ScrollBarVisibilityMode.Auto"/> behavior. Clearing this flag disables
///     the scrollbar and sets its <see cref="ScrollBar.VisibilityMode"/> to
///     <see cref="ScrollBarVisibilityMode.Manual"/> with <see cref="View.Visible"/> = false.
/// </summary>
HasVerticalScrollBar   = 0b_0001_0000_0000_0000, // bit 12

/// <summary>
///     If set, the built-in <see cref="View.HorizontalScrollBar"/> is enabled with
///     <see cref="ScrollBarVisibilityMode.Auto"/> behavior. Clearing this flag disables
///     the scrollbar and sets its <see cref="ScrollBar.VisibilityMode"/> to
///     <see cref="ScrollBarVisibilityMode.Manual"/> with <see cref="View.Visible"/> = false.
/// </summary>
HasHorizontalScrollBar = 0b_0010_0000_0000_0000, // bit 13

/// <summary>
///     Combines <see cref="HasVerticalScrollBar"/> and <see cref="HasHorizontalScrollBar"/>.
/// </summary>
HasScrollBars = HasVerticalScrollBar | HasHorizontalScrollBar,
```

#### ViewportSettings Setter Synchronizes ScrollBar State

```csharp
// In View.Content.cs
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

#### SyncScrollBarsToSettings — The Bridge

In `View.ScrollBars.cs`, this method translates flag transitions into ScrollBar configuration:

```csharp
private void SyncScrollBarsToSettings (ViewportSettingsFlags oldFlags, ViewportSettingsFlags newFlags)
{
    if (this is Adornment)
    {
        return;
    }

    SyncOneScrollBar (
        oldFlags.HasFlag (ViewportSettingsFlags.HasVerticalScrollBar),
        newFlags.HasFlag (ViewportSettingsFlags.HasVerticalScrollBar),
        Orientation.Vertical
    );

    SyncOneScrollBar (
        oldFlags.HasFlag (ViewportSettingsFlags.HasHorizontalScrollBar),
        newFlags.HasFlag (ViewportSettingsFlags.HasHorizontalScrollBar),
        Orientation.Horizontal
    );
}

private void SyncOneScrollBar (bool hadFlag, bool hasFlag, Orientation orientation)
{
    if (!hadFlag && hasFlag)
    {
        // Enabling: access triggers lazy creation, then set Auto mode
        ScrollBar sb = orientation == Orientation.Vertical ? VerticalScrollBar : HorizontalScrollBar;
        sb.VisibilityMode = ScrollBarVisibilityMode.Auto;
    }
    else if (hadFlag && !hasFlag)
    {
        // Disabling: only if the scrollbar was ever created
        Lazy<ScrollBar> lazy = orientation == Orientation.Vertical ? _verticalScrollBar : _horizontalScrollBar;

        if (lazy.IsValueCreated)
        {
            lazy.Value.VisibilityMode = ScrollBarVisibilityMode.Manual;
            lazy.Value.Visible = false;
        }
    }
}
```

#### Guard in ShowHide() (Optional Defense-in-Depth)

For belt-and-suspenders safety, `ShowHide()` can check the owning View's flag before showing a built-in scrollbar. This prevents accidental re-enablement if something sets `VisibilityMode = Auto` without going through ViewportSettings:

```csharp
private void ShowHide ()
{
    switch (VisibilityMode)
    {
        case ScrollBarVisibilityMode.Auto:
            // If this scrollbar lives in a View's Padding, respect the View's
            // ViewportSettings as the authority on whether it should be enabled.
            if (SuperView is Padding padding && padding.Parent is View ownerView)
            {
                ViewportSettingsFlags requiredFlag = Orientation == Orientation.Vertical
                    ? ViewportSettingsFlags.HasVerticalScrollBar
                    : ViewportSettingsFlags.HasHorizontalScrollBar;

                if (!ownerView.ViewportSettings.HasFlag (requiredFlag))
                {
                    Visible = false;
                    break;
                }
            }

            Visible = VisibleContentSize < ScrollableContentSize;
            break;

        case ScrollBarVisibilityMode.Always:
            Visible = true;
            break;

        case ScrollBarVisibilityMode.Manual:
            break;
    }

    _slider.VisibleContentSize = VisibleContentSize;
    _slider.Size = CalculateSliderSize ();
    _sliderPosition = CalculateSliderPositionFromContentPosition (_value);
    _slider.Position = _sliderPosition.Value;
}
```

This guard is optional. Without it, the design still works because `SyncOneScrollBar` sets `VisibilityMode = Manual` when the flag is cleared, which prevents `ShowHide()` from running auto-show logic.

---

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

### Scenario 3: Disable a scrollbar reliably

```csharp
// Before — BROKEN (AutoShow overrides this on next frame change)
VerticalScrollBar.Visible = false;

// After — works reliably, scrollbar is definitively disabled
ViewportSettings &= ~ViewportSettingsFlags.HasVerticalScrollBar;
```

### Scenario 4: Always-visible scrollbar (no auto-hide)

```csharp
// Enable via flag, then override the display policy
ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;
VerticalScrollBar.VisibilityMode = ScrollBarVisibilityMode.Always;
```

### Scenario 5: One axis only

```csharp
// Only horizontal scrolling
ViewportSettings |= ViewportSettingsFlags.HasHorizontalScrollBar;
// Vertical scrollbar is never created (lazy loading preserved)
```

### Scenario 6: Standalone ScrollBar (not part of a View)

```csharp
// Auto show/hide
ScrollBar scrollBar = new ()
{
    Orientation = Orientation.Vertical,
    VisibilityMode = ScrollBarVisibilityMode.Auto,
    ScrollableContentSize = 500
};
someContainer.Add (scrollBar);

// Always visible
ScrollBar scrollBar = new ()
{
    Orientation = Orientation.Horizontal,
    VisibilityMode = ScrollBarVisibilityMode.Always,
    ScrollableContentSize = 200
};

// Manual control
ScrollBar scrollBar = new ()
{
    VisibilityMode = ScrollBarVisibilityMode.Manual
};
scrollBar.Visible = myCustomCondition;
```

### Scenario 7: Custom visibility logic (e.g., CharMap's horizontal scrollbar)

```csharp
// Don't enable via ViewportSettings — use Manual mode on the built-in scrollbar
HorizontalScrollBar.VisibilityMode = ScrollBarVisibilityMode.Manual;
HorizontalScrollBar.Increment = COLUMN_WIDTH;

ViewportChanged += (_, _) =>
{
    HorizontalScrollBar.Visible = Viewport.Width < GetContentSize ().Width;
};
```

---

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
         ┌────────────────┐  ┌──────────────────────────┐
         │ ScrollBar not  │  │ ScrollBar created         │
         │ created (lazy) │  │ VisibilityMode = Auto     │
         │ or disabled:   │  └────────────┬─────────────┘
         │  Mode = Manual │               │
         │  Visible=false │     ┌─────────┴──────────┐
         └────────────────┘     │    VisibilityMode?  │
                                │                     │
                    ┌───────────┼──────────┐          │
                    ▼           ▼          ▼          │
                  Manual      Auto      Always        │
                    │           │          │          │
                    ▼           ▼          ▼          │
              ┌──────────┐ ┌────────┐ ┌────────┐     │
              │ Dev sets │ │Content │ │Visible │     │
              │ Visible  │ │> View? │ │= true  │     │
              │ directly │ │Yes→Vis │ │ always │     │
              └──────────┘ │No→Hide │ └────────┘     │
                           └────────┘                 │
                                                      │
         Dev can override VisibilityMode after ────────┘
         flag sets it to Auto (e.g., for Always)
```

---

## Migration

### Internal Views (Terminal.Gui library)

| Current | Proposed |
|---------|----------|
| `VerticalScrollBar.AutoShow = true` | `ViewportSettings \|= HasVerticalScrollBar` |
| `HorizontalScrollBar.AutoShow = true` | `ViewportSettings \|= HasHorizontalScrollBar` |
| Both `AutoShow = true` | `ViewportSettings \|= HasScrollBars` |
| `AutoShow = false; Visible = false` | Don't set the flag (default) |
| `AutoShow = false` (always visible) | Flag + `VisibilityMode = Always` |

Affected files (~15):
- `DialogTResult.cs` — `ViewportSettings |= HasScrollBars`
- `CharMap.cs` — `ViewportSettings |= HasVerticalScrollBar` (horizontal stays `Manual`)
- `EventLog.cs` — `ViewportSettings |= HasScrollBars`
- `ThemeViewer.cs` — `ViewportSettings |= HasScrollBars`
- `FileDialog.cs` — No flags (already default, remove explicit `AutoShow = false` lines)
- `UICatalogRunnable.cs` — `ViewportSettings |= HasVerticalScrollBar` / `HasScrollBars`
- Various scenarios — straightforward 1:1 replacement

### TextView: Special Case

`TextView` has its own `ScrollBars` property that manually manages visibility. This can delegate to ViewportSettings internally:

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

The existing `UpdateHorizontalScrollBarVisibility()` logic for WordWrap interaction can remain. With the new design, it would set `VisibilityMode = Manual` on the horizontal scrollbar and control `Visible` directly when WordWrap is enabled, while the vertical scrollbar stays in `Auto` mode via the flag.

### AutoShow — Deprecation Path

`AutoShow` is removed from `ScrollBar` and replaced by `VisibilityMode`. The mapping is:

| Old | New |
|-----|-----|
| `AutoShow = true` | `VisibilityMode = ScrollBarVisibilityMode.Auto` |
| `AutoShow = false` (initial) | `VisibilityMode = ScrollBarVisibilityMode.Manual` |
| `AutoShow = false` (after true) | `VisibilityMode = ScrollBarVisibilityMode.Always` |

Since this is a pre-release v2 alpha, a clean break is preferable to an `[Obsolete]` shim. If backward compatibility is required, `AutoShow` can be retained as:

```csharp
[Obsolete ("Use VisibilityMode instead.")]
public bool AutoShow
{
    get => VisibilityMode == ScrollBarVisibilityMode.Auto;
    set => VisibilityMode = value ? ScrollBarVisibilityMode.Auto : ScrollBarVisibilityMode.Always;
}
```

Note: this shim maps `AutoShow = false` to `Always` (matching the current side effect) but cannot represent `Manual`. This is acceptable because code using `AutoShow = false` today already gets the `Always` behavior due to the `Visible = true` side effect.

---

## What Changes

| Component | Change | Impact |
|-----------|--------|--------|
| **`ScrollBarVisibilityMode`** | New enum (3 values) | New type |
| **`ScrollBar`** | Replace `AutoShow` with `VisibilityMode` property; update `ShowHide()` | Breaking (v2 alpha) |
| **`ViewportSettingsFlags`** | Add 3 flags (2 primary + 1 combo) | Additive |
| **`View.ViewportSettings`** setter | Call `SyncScrollBarsToSettings` on change | Internal wiring |
| **`View.ScrollBars.cs`** | Add `SyncScrollBarsToSettings` / `SyncOneScrollBar` | New private methods |
| **Internal Views** | Replace `AutoShow = true` with ViewportSettings flag | ~15 files, mechanical |
| **`TextView.ScrollBars`** | Delegate to ViewportSettings internally | Encapsulated refactor |

## What Does NOT Change

- `ScrollBar` standalone functionality (works great on its own with clearer API)
- Lazy loading of built-in scrollbars
- Padding thickness management on visibility changes
- Viewport <-> ScrollBar value synchronization
- ViewportSettings constraint enforcement logic
- All existing `ViewportSettingsFlags` values and semantics
- `ScrollBar.Visible` — still the actual visibility state

---

## Why Not ShowScroll (PR #4715)?

The `ShowScroll` approach:
1. **Adds a View-level concern (enablement) to ScrollBar**, coupling it to its host
2. **Creates a 3-boolean interaction matrix** (`ShowScroll x AutoShow x Visible` = 8 states) that is hard to document and test
3. **Doesn't integrate with ViewportSettings**, where developers already configure viewport behavior
4. **Keeps the confusing `AutoShow` boolean**, just adding another boolean on top
5. **Is additive complexity** on ScrollBar rather than leveraging existing architecture

The ViewportSettings + VisibilityMode approach:
1. **Keeps ScrollBar clean** — it doesn't know or care about "enablement"
2. **Uses an existing, well-understood mechanism** (flags enum on View) for the View-level concern
3. **Replaces the confusing boolean** with a self-documenting enum for the ScrollBar-level concern
4. **Gives independent per-axis control** via standard flag operations
5. **Fits the mental model**: "this View has scrollbars" is a View property; "how does this scrollbar manage its visibility" is a ScrollBar property
6. **Reduces total state space**: `(flag on/off) x (Manual/Auto/Always)` = 6 meaningful states vs `ShowScroll x AutoShow x Visible` = 8 states with several invalid/contradictory combinations

---

## Summary

Two problems, two targeted solutions:

1. **View-level enablement** → `ViewportSettingsFlags.HasVerticalScrollBar` / `HasHorizontalScrollBar`. The flags are the master switch. Setting a flag creates the scrollbar and puts it in `Auto` mode. Clearing a flag disables it completely.

2. **ScrollBar display policy** → `ScrollBarVisibilityMode` enum (`Manual`, `Auto`, `Always`). Replaces the confusing `AutoShow` boolean. Every state is reachable, every transition is clean, no hidden side effects.

Together, these changes cleanly separate the three concerns (enablement, display policy, visibility) that are currently tangled in `AutoShow` + `Visible`, without adding new booleans and without bolting on complexity.
