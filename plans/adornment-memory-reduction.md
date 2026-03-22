# Adornment Memory Reduction — Status & Polish Plan

> **Issue:** [#4696](https://github.com/gui-cs/Terminal.Gui/issues/4696) — Reduce View class memory footprint  
> **Branch:** `copilot/reduce-view-class-memory-footprint`  
> **Status:** Phase 0 ✅ DONE · Phase 1 ✅ DONE · Polish ✅ DONE · All tests pass  
> **Last updated:** 2026-03-21 — All polish items complete. 40 new tests added.

---

## 1. What Was Done

Two-level split of adornments: lightweight settings objects (`AdornmentImpl` subclasses) that are always present and cheap, vs. heavy `AdornmentView` subclasses created lazily only when needed.

| Level | Class | Extends | Purpose |
|-------|-------|---------|---------|
| Lightweight | `Border`, `Margin`, `Padding` (via `AdornmentImpl`) | `object` | Store settings (Thickness, LineStyle, ShadowStyle) — always present, cheap |
| Heavy | `BorderView`, `MarginView`, `PaddingView` (via `AdornmentView`) | `View` | Full rendering, SubViews, mouse/keyboard, arrangement — created only when needed |

**Phase 0 (Infrastructure):** Created `IAdornment`, `IAdornmentView`, `AdornmentImpl`, `AdornmentView`. Pure additions.

**Phase 1 (Migration):** Migrated all three adornments simultaneously. Updated 30+ `is Adornment` type checks to `is AdornmentView`. All tests pass. Key lessons documented in §4 below.

**Current state:** Lazy creation is fully operational. `SetupAdornments()` only sets `Parent` on each lightweight adornment — it does NOT call `GetOrCreateView()`. The `AdornmentView` backing views start as `null` and are created on-demand only when features require them:

| Trigger | Adornment | Code path |
|---------|-----------|-----------|
| `LineStyle` set non-null | Border | `Border.LineStyle` setter → `GetOrCreateView()` |
| `Thickness` set non-empty (with `LineStyle` non-null) | Border | `Border.OnThicknessChanged()` → `GetOrCreateView()` |
| `ShadowStyle` set non-null | Margin | `Margin.ShadowStyle` setter → `GetOrCreateView()` |
| `Add(subView)` called | Any | `AdornmentImpl.Add()` → `GetOrCreateView()` |
| ScrollBar accessed | Padding | `View.CreateScrollBar()` → `Padding.GetOrCreateView()` |
| `Dialog<T>` button container | Padding | `DialogTResult` → `Padding.GetOrCreateView()` |

For a plain `View` with no border, no shadow, no padding subviews: **zero `AdornmentView` objects are created**. Drawing falls back to `AdornmentImpl.Draw()` which calls `Thickness.Draw()` directly when `View` is null. The entire framework is null-safe — all `.View` accesses use `?.`, `is { }`, or are behind explicit `GetOrCreateView()` calls.

The ~5 null-forgiving (`!`) usages (TabView, CharMap, ScrollBars) are safe because they follow explicit `GetOrCreateView()` calls or code paths that guaranteed View creation (e.g., setting `BorderStyle`).

---

## 2. Current Architecture

```
AdornmentImpl (abstract, lightweight settings)
├── Border    → CreateView() → BorderView : AdornmentView : View
├── Margin    → CreateView() → MarginView : AdornmentView : View
└── Padding   → CreateView() → PaddingView : AdornmentView : View

IAdornment (interface for settings)
IAdornmentView (interface for View-backed layer)
```

### Key Files
| File | Role |
|------|------|
| `AdornmentImpl.cs` | Base settings class: Thickness, GetOrCreateView(), convenience pass-throughs |
| `AdornmentView.cs` | Base View class: FrameToScreen, Contains, Scheme, clearing/drawing |
| `Border.cs` / `BorderView.cs` | Border settings + rendering (title, LineCanvas, arrangement) |
| `Margin.cs` / `MarginView.cs` | Margin settings + rendering (shadow, transparency, clip cache) |
| `Padding.cs` / `PaddingView.cs` | Padding settings + rendering (focus, mouse click) |
| `IAdornment.cs` / `IAdornmentView.cs` | Contracts |
| `View.Adornments.cs` | View partial: SetupAdornments, lifecycle, helpers |

---

## 3. TODO Inventory & Action Plan

### 3.1 AdornmentImpl.cs — Convenience Pass-Throughs

**Theme: "Remove this — un-needed complexity"** (20 TODOs)

These pass-throughs exist on `AdornmentImpl` so that callers can interact with adornments without knowing whether the backing `AdornmentView` exists. Since lazy creation is working and `.View` is frequently `null`, these pass-throughs are **essential infrastructure** — not un-needed complexity. The TODOs are misleading.

**Action plan:** Audit each pass-through. For pass-throughs that are actively used by the framework's null-safe drawing/layout paths, **keep and remove the misleading TODO comment**. For pass-throughs that have zero callers (or only test callers that could easily use `.View?.XXX`), remove.

| Pass-through | Lines | Used by framework? | Action |
|---|---|---|---|
| `NeedsDraw` (get) | 144-145 | Yes — `View.Drawing.cs` checks `Margin.NeedsDraw` | **Keep.** Remove TODO. |
| `ClearNeedsDraw()` | 148-158 | Not directly | **Evaluate.** May be dead code. |
| `SetNeedsDraw()` | 161-172 | Yes — `Thickness` setter calls it; drawing code calls it | **Keep.** Remove TODO. Simplify: drop `_needsDraw` backing field; if View is null the parent's draw handles it. |
| `NeedsLayout` (get/set) | 177-188 | Yes — `View.Drawing.cs:255` checks `Margin.NeedsLayout` | **Keep.** Remove TODO. |
| `SetNeedsLayout()` | 191-193 | Yes — `Thickness` setter calls it | **Keep.** Remove TODO. |
| `SetScheme()` / `GetScheme()` | 196-201 | Minimal | **Remove.** Callers should `GetOrCreateView()` if they need custom scheme. |
| `Diagnostics` | 205-216 | Yes — `AdornmentImpl.Draw()` uses `Diagnostics` | **Keep.** Fix: add own backing field (like `ViewportSettings`). Remove TODO. |
| `SubViews` | 220-224 | Yes — `View.Drawing.cs` iterates `Margin.SubViews`, `Border.SubViews`, `Padding.SubViews` | **Keep.** Remove TODO. |
| `Add(View)` | 227-228 | Yes — triggers `GetOrCreateView()` | **Keep.** Remove TODO. |
| `HasFocus` | 231 | Not directly | **Remove.** Callers can use `.View?.HasFocus ?? false`. |
| `ViewportSettings` | 234-247 | Yes — drawing code, transparency checks | **Keep.** Already has backing field. Remove TODO. |
| `Visible` | 250-261 | Not directly in framework | **Evaluate.** May be used by scenarios/tests. |
| `Contains()` | 267-283 | Yes — hit testing works without View | **Keep.** No TODO on this one. |
| `BeginInit()` / `EndInit()` | 286-291 | Yes — `View.Adornments.cs` calls these | **Keep.** Remove TODO. Wait — these delegate to `View?.BeginInit()` which is already called by `BeginInitAdornments()`. Possibly redundant. **Evaluate.** |
| `LayoutSubViews()` | 293-295 | Yes — `View.Layout.cs:727` calls `Margin.View?.LayoutSubViews()` directly | **Remove from AdornmentImpl.** Framework already goes through `.View?`. |
| `Enabled` | 297-310 | Not directly | **Remove.** Callers can use `.View?.Enabled`. |
| `Remove(View)` | 312-314 | Not directly | **Remove.** Callers can use `.View?.Remove()`. |
| `Id` | 316-319 | Minimal | **Remove from AdornmentImpl.** This is a View concept. |
| `SchemeName` | 320-334 | Not directly | **Evaluate.** Has backing field for deferred apply — useful pattern. Keep if actively used. |
| `IsInitialized` | 336-339 | Not directly in framework | **Remove.** Callers use `.View?.IsInitialized ?? false`. |
| `GetAttributeForRole()` | 343-345 | Yes — `AdornmentImpl.Draw()` no-View path needs attributes | Wait — the no-View Draw path uses `Diagnostics` but not `GetAttributeForRole()`. **Evaluate.** |
| `AddFrameToClip()` | 347-349 | Yes — `View.Drawing.cs` calls `Border.AddFrameToClip()` | **Keep.** Remove TODO. |
| `DoDrawSubViews()` | 351-353 | Yes — `View.Drawing.cs` calls `Border.DoDrawSubViews()` | **Keep.** Remove TODO. |
| `Dispose()` / `DisposeView()` | 355-387 | Yes — `View.Adornments.cs` calls `DisposeAdornments()` | **Keep.** Remove TODO. |

**Summary:** ~12 pass-throughs are actively used by the framework and must stay. ~8 can be removed. All misleading "Remove this" TODOs should be updated or removed.

### 3.2 AdornmentImpl.cs — Other TODOs

| TODO | Lines | Action |
|---|---|---|
| ~~"Thickness should only be on Adornment"~~ | ~~40~~ | ✅ DONE — Removed `Thickness` from `AdornmentView`. It now only lives on `AdornmentImpl`. All call sites in `BorderView`, `MarginView`, `AdornmentView` updated to use `Adornment!.Thickness`. TODO removed. |
| ~~"We should be able to remove this?"~~ (`FrameToScreen`, `ComputeFrameToScreen`) | ~~122-133~~ | ✅ DONE — Kept code (essential for no-View drawing path). Removed misleading TODO comments. |
| ~~"Diagnostics needs own backing field"~~ | ~~203~~ | ✅ DONE — Added `ViewDiagnosticFlags` backing field with same pattern as `ViewportSettings`. |

### 3.3 AdornmentView.cs TODO

| TODO | Line | Action |
|---|---|---|
| ~~"Thickness should only be on Adornment"~~ | ~~52~~ | ✅ DONE — Removed `Thickness` property entirely from `AdornmentView`. |

### 3.4 BorderView.cs TODOs

| TODO | Line | Action |
|---|---|---|
| "Move DrawIndicator out of Border and into View" | 75 | **Defer.** Future PR. Not in scope. |
| "This should be moved to LineCanvas as a new BorderStyle.Ruler" | 380 | **Defer.** Future PR. Not in scope. |
| ~~"This should not be done on each draw?" (gradient setup)~~ | ~~420~~ | ✅ DONE — Cached gradient `FillPair` and `Rectangle`; only recreated when bounds change. |

### 3.5 PaddingView.cs TODO

| TODO | Line | Action |
|---|---|---|
| "Move DrawIndicator out of Border and into View" | 50 | **Defer.** Same as BorderView — not in scope. |

### 3.6 Arranger.cs TODO

| TODO | Line | Action |
|---|---|---|
| ~~"Simplify this by having _border be of type Border (IAdornment)"~~ | ~~9~~ | ✅ RESOLVED — Kept `_border` as `BorderView`. Analysis showed Arranger needs extensive View-level access (~50 call sites: App, HotKeyBindings, CanFocus, SetFocus, Add, Remove, Frame, etc.). Changing to `Border` would require `.GetOrCreateView()`/`.View!` everywhere — more complex, not simpler. Settings are already accessed via `_border.Adornment!`. Replaced TODO with explanatory comment. |

---

## 4. Phase 1 Lessons Learned (Preserved for Reference)

1. **Constructor ordering:** `Adornment` back-reference must be set BEFORE subclass constructor body runs. Pass it as a constructor parameter, not post-construction.

2. **Lazy View creation works.** The codebase is fully null-safe — all `.View` accesses use `?.`, `is { }` pattern matching, or follow explicit `GetOrCreateView()` calls. `AdornmentImpl.Draw()` has a no-View fallback path using `Thickness.Draw()`. Only ~5 null-forgiving `!` usages exist, all safe.

3. **Bidirectional property propagation:** Settings object is "write" authority; View may be "read" authority for derived/computed state. Every getter/setter must be audited for correct delegation direction.

4. **`SubViews.Count > 0` layout guard:** Was a regression from an earlier fix (commit `e8c851631`). Use `git log -S` to check provenance of suspicious guards.

5. **Bulk test updates need spot-checking.** Mechanical renames compile but may have semantic issues (e.g., subscribing to wrong object's event).

---

## 5. Polish Plan — Ordered Work Items

### 5.1 Stale TODO Cleanup ✅ DONE

1. ✅ Removed "Thickness should only be on Adornment" TODO from `AdornmentImpl.cs` and `AdornmentView.cs` — Thickness removed from AdornmentView entirely
2. ✅ Removed "We should be able to remove this?" TODOs on `FrameToScreen`/`ComputeFrameToScreen`
3. ✅ Fixed Diagnostics to have its own backing field (same pattern as `ViewportSettings`)
4. ✅ Removed `SetScheme` / `GetScheme` pass-throughs — callers updated to `GetOrCreateView().SetScheme()`
5. ✅ Removed all 20+ misleading "Remove this" TODO comments on essential pass-throughs

### 5.2 Remove Unnecessary Pass-Throughs ✅ DONE

Removed pass-throughs not used by the framework:
- ✅ `SetScheme`, `GetScheme` — callers updated to `GetOrCreateView().SetScheme()`
- ✅ `Id` — callers updated to use `.View!.Id`
- ✅ `IsInitialized` — no callers
- ✅ `LayoutSubViews` — framework uses `.View?.LayoutSubViews()` directly
- ✅ `BeginInit`, `EndInit` — no external callers

Kept (have framework callers):
- `HasFocus` — used by `View.Navigation.cs` to check adornment focus
- `Enabled` — used by `View.cs` to propagate enabled state
- `Remove` — used by `View.ScrollBars.cs` to remove scrollbars from Padding

### 5.3 Arranger Simplification ✅ RESOLVED

Analysis showed `_border` must remain `BorderView` — Arranger needs ~50 View-level call sites. Replaced misleading TODO with explanatory comment. See §3.6.

### 5.4 BorderView Gradient Caching ✅ DONE

Added `_cachedGradientFill` and `_cachedGradientRect` fields. Gradient `FillPair` is only recreated when bounds change. Cache is cleared when gradient setting is turned off.

---

## 6. Test Coverage Plan

### 6.1 Current Coverage Summary

**137 test methods** across 13 files (~4,724 lines). Strong coverage for:
- ✅ Lazy `GetOrCreateView` triggers (Margin/Border/Padding)
- ✅ Frame geometry (GetFrame, FrameToScreen, Contains)
- ✅ Shadow behavior (17 tests)
- ✅ Arrangement (keyboard + mouse, 13 tests)
- ✅ Navigation with adornment SubViews (10 tests)
- ✅ Layout with adornments (25 tests)
- ✅ Border drawing and title rendering (18 tests)

### 6.2 Identified Gaps

| Gap | Priority | File to add tests |
|---|---|---|
| **`IAdornmentView` interface** — zero test references | High | `AdornmentTests.cs` |
| **Convenience pass-through delegation** — no systematic tests verifying each AdornmentImpl method delegates correctly to View AND no-ops when View is null | High | New: `AdornmentImplPassThroughTests.cs` |
| **AdornmentImpl.Draw() no-View path** — `Thickness.Draw()` fallback when View is null (critical for lazy creation) | High | `AdornmentTests.cs` |
| **No-View drawing for Border/Margin/Padding** — verify plain views with Thickness but no View still render correctly | High | `AdornmentTests.cs` or `BorderDrawTests.cs` |
| **AdornmentImpl.GetOrCreateView lifecycle sync** — no test for mid-init creation (BeginInit called, EndInit not yet) | Medium | `AdornmentTests.cs` |
| **AdornmentImpl.FrameToScreen without View** — `ComputeFrameToScreen()` fallback path | Medium | `AdornmentTests.cs` |
| **MarginView.CacheClip / GetCachedClip / ClearCachedClip** — no direct tests | Medium | `MarginTests.cs` |
| **PaddingView.GetSubViews override** — `includePadding` parameter logic | Medium | `PaddingTests.cs` |
| **PaddingView.OnMouseEvent** — click-to-focus behavior | Low | `PaddingTests.cs` |
| **AdornmentView standalone (no Adornment back-ref)** — `_standaloneFallbackThickness` path | Low | `AdornmentTests.cs` |
| **Border.OnThicknessChanged triggers GetOrCreateView** (only when LineStyle is set) | Medium | `BorderTests.cs` |
| **Margin.OnThicknessChanged does NOT trigger GetOrCreateView** (commented out) | Medium | `MarginTests.cs` |
| **Memory savings verification** — plain View creates zero AdornmentViews | Medium | `AdornmentLayoutTests.cs` |

### 6.3 New Test Plan — ✅ DONE

#### `AdornmentImplPassThroughTests.cs` ✅ CREATED — 40 tests

Systematic verification of all convenience pass-throughs on `AdornmentImpl`:
- NeedsDraw/SetNeedsDraw/ClearNeedsDraw (with/without View)
- NeedsLayout/SetNeedsLayout (with/without View)
- Diagnostics (local storage + forwarding)
- SubViews (empty default + delegation)
- Add (forces View creation) / Remove (no-op without View)
- HasFocus, Enabled, Visible (defaults + delegation)
- ViewportSettings, SchemeName (local storage + delegation)
- Contains (thickness calc without View, delegation with View)
- GetAttributeForRole (parent fallback)
- FrameToScreen (computed without View, delegated with View)
- Draw (thickness path vs View delegation)
- GetOrCreateView (creation, idempotency, init synchronization)
- Dispose (clears View+Parent)

#### Remaining test gaps (deferred — not blocking this PR)

- MarginView.CacheClip / GetCachedClip / ClearCachedClip
- PaddingView.GetSubViews override
- PaddingView.OnMouseEvent click-to-focus
- Memory savings verification (plain View creates zero AdornmentViews)

---

## 7. Breaking Changes (Preserved)

### Public API

| API | Change | Mitigation |
|-----|--------|-----------|
| `View.Border` type | `Border : Adornment : View` → `Border : AdornmentImpl` | Convenience methods hide the split |
| `View.Margin` type | Same pattern | Same |
| `View.Padding` type | Same pattern | Same |
| `Adornment` class | Split into `AdornmentImpl` + `AdornmentView` | No `[Obsolete]` alias — clean break |
| `is Adornment` checks | Must use `is AdornmentView` or `is IAdornmentView` | Mechanical find-replace |
| `view.Padding.GetSubViews(...)` | Must use `view.Padding.View?.GetSubViews(...)` | Callers updated |

### Nullability

`View.Border`, `View.Margin`, `View.Padding` remain **always non-null** after construction. The `null` is on `.View` — `Border.View`, `Margin.View`, `Padding.View` are `null` until a feature triggers `GetOrCreateView()`. The entire framework is null-safe via `?.` and `is { }` patterns.

---

## 8. Phase 2 (Future — Not This PR)

Deferred optimizations for a follow-up PR:

1. **Complementary `View` optimizations:** Lazy `TextFormatter`, `KeyBindings`, `LineCanvas`, Command dictionary
2. **Move DrawIndicator from Border into View** (deferred TODOs from BorderView/PaddingView)
3. **Move Ruler diagnostic to LineCanvas** (deferred TODO from BorderView)
4. **Memory profiling** — measure actual per-View savings with lazy adornments vs. baseline

---

## Appendix A: File Inventory

### Source Files (17 in `Terminal.Gui/ViewBase/Adornment/`)

| File | Type | Lines |
|------|------|-------|
| `IAdornment.cs` | Interface | 43 |
| `IAdornmentView.cs` | Interface | 37 |
| `AdornmentImpl.cs` | Abstract base (settings) | 390 |
| `AdornmentView.cs` | Abstract base (View) | 243 |
| `Border.cs` | Lightweight settings | 91 |
| `BorderView.cs` | View rendering | 463 |
| `BorderView.Arrangement.cs` | Arrangement partial | ~200 |
| `Margin.cs` | Lightweight settings | 109 |
| `MarginView.cs` | View rendering | 427 |
| `Padding.cs` | Lightweight settings | 32 |
| `PaddingView.cs` | View rendering | 137 |
| `Arranger.cs` | Arrangement logic | ~200 |
| `ArrangerButton.cs` | Arrange UI | ~100 |
| `ArrangeButtons.cs` | Arrange UI | ~100 |
| `BorderSettings.cs` | Flags enum | ~30 |
| `ShadowStyles.cs` | Enum | ~20 |
| `ShadowView.cs` | Shadow rendering | ~200 |

### Test Files (13 files, 137 test methods, ~4,724 lines)

| File | Tests | Focus |
|------|-------|-------|
| `AdornmentTests.cs` (parallel) | 13 | Core geometry, thickness, FrameToScreen |
| `AdornmentSubViewTests.cs` (parallel) | 4 | SubViews in adornments, shadow in border/padding |
| `AdornmentLayoutTests.cs` (parallel) | 25 | Lazy creation, frame geometry, layout, Contains |
| `AdornmentNavigationTests.cs` (parallel) | 10 | Focus chain with adornment SubViews |
| `BorderTests.cs` (parallel) | 9 | Constructor defaults, GetOrCreateView triggers, GetFrame |
| `BorderDrawTests.cs` (parallel) | 3 | Transparent border title rendering |
| `BorderTests.cs` (non-parallel) | 15 | Title, scheme, line joining, FrameToScreen |
| `MarginTests.cs` (parallel) | 17 | Transparency, shadow styles, GetFrame |
| `PaddingTests.cs` (parallel) | 8 | Constructor defaults, GetFrame, scheme |
| `ShadowTests.cs` (parallel) | 17 | Shadow style/size/thickness, mouse press, wide glyphs |
| `ShadowStyleTests.cs` (non-parallel) | 3 | Colors, visual, mouse press movement |
| `BorderArrangementTests.cs` (parallel) | 4 | Keyboard move, wide glyphs |
| `BorderArrangementKeyboardTests.cs` (parallel) | 9 | Enter arrange mode, button visibility |